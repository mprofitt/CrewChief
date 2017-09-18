/* This code is from the branch CrewChief developement branch
*/

using System;
using System.Windows;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using iRacingSdkWrapper;
using System.Timers;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Threading;
using iRacingSdkWrapper.Broadcast;
using System.Media;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using System.ComponentModel;
using iRSDKSharp;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Data;


namespace CrewChief
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognitionEngine recognizer;
        private SpeechSynthesizer synthesizer;

        public SdkWrapper wrapper;

        private Queue<PromptBuilder> QueuedSpeech = new Queue<PromptBuilder>();
        private Queue<PromptBuilder> PlayingQueuedSpeech = new Queue<PromptBuilder>();

        private System.Timers.Timer timerCrewChief;
        private System.Timers.Timer timerPitRoad;
        private System.Timers.Timer timerLeaderOnPitRoad;

        private bool isUpdatingDrivers = true;
        private List<Driver> drivers;

        private int currentSessionNum;
        //private Status.Flags Flag;

        private Driver leaderOnPitRoad = null;

        private Status status;

        //[Flags]
        //enum Flags
        //{
        //    Listening = 0x0001,
        //    Ready = 0x0002,
        //    Analyzing = 0x0004,
        //    Speaking = 0x0008,
        //    PitPos = 0x0010,
        //    LeaderOnPitRoad = 0x0011,
        //}

        public MainWindow()
        {
            InitializeComponent();

            wrapper = new SdkWrapper();
            wrapper.EventRaiseType = SdkWrapper.EventRaiseTypes.CurrentThread;
            wrapper.TelemetryUpdateFrequency = 10;
            wrapper.Connected += wrapper_Connected;
            wrapper.Disconnected += wrapper_Disconnected;
            wrapper.SessionInfoUpdated += wrapper_SessionInfoUpdated;
            wrapper.TelemetryUpdated += wrapper_TelemetryUpdated;
            wrapper.Start();

            recognizer = new SpeechRecognitionEngine();
            //recognizer.SetInputToNull();
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(CreateGrammar1());
            recognizer.LoadGrammar(CreateGrammar2());
            recognizer.LoadGrammar(CreateGrammar3());
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
            

            synthesizer = new SpeechSynthesizer();
            synthesizer.Rate = -1;
            synthesizer.Volume = 100;
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.SpeakStarted += synthesizer_SpeakStarted;
            synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

            status = new CrewChief.Status();
            status.PropertyChanged += OnStatusChange;

            timerCrewChief = new System.Timers.Timer();
            timerCrewChief.Interval = 8000;
            timerCrewChief.Elapsed += OnCrewChiefTimer;
            timerCrewChief.Enabled = false;
            timerCrewChief.AutoReset = false;

            timerPitRoad = new System.Timers.Timer();
            timerPitRoad.Interval = 3000;
            timerPitRoad.Elapsed += OnPitRoadTimer;
            timerPitRoad.Enabled = false;
            timerPitRoad.AutoReset = false;

            timerLeaderOnPitRoad = new System.Timers.Timer();
            timerLeaderOnPitRoad.Interval = 3000;
            timerLeaderOnPitRoad.Elapsed += OnLeaderOnPitRoadTimer;
            timerLeaderOnPitRoad.Enabled = false;
            timerLeaderOnPitRoad.AutoReset = false;

            // binding = new BindingSource();
            drivers = new List<Driver>();
            // binding.DataSource = drivers;
            // driverDataGrid.DataSource = drivers;

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            System.Windows.Data.CollectionViewSource driverViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("driverViewSource")));
            // Load data by setting the CollectionViewSource.Source property:
            // driverViewSource.Source = [generic data source]
        }

        private void OnStatusChange(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("**** Status.Flag: {0}", status.Flag);
        }

        private void wrapper_Connected(object sender, EventArgs e)
        {
            Debug.WriteLine("**** Wrapper Connected ****\n");
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void wrapper_Disconnected(object sender, EventArgs e)
        {
            Debug.WriteLine("**** Wrapper Disconnected ****\n");

            recognizer.RecognizeAsyncStop();
            drivers.Clear();
            status.Flag = 0;
            Car.ClearPitStop();
        }

        private void wrapper_SessionInfoUpdated(object sender, SdkWrapper.SessionInfoUpdatedEventArgs e)
        {
            isUpdatingDrivers = true;
            YamlQuery query = null;

            //query = e.SessionInfo["DriverInfo"]["DriverPitTrkPct"];
            //PitStallSignal.DriverPitPos = float.Parse(GetSessionInfoValue(e.SessionInfo, query));

            //query = e.SessionInfo["CarSetup"]["Chassis"]["Front"]["TapeConfiguration"];
            //Sim.Tape.Current = float.Parse(GetSessionInfoValue(e.SessionInfo, query).TrimEnd('%',' '));

            query = e.SessionInfo["WeekendInfo"]["TrackPitSpeedLimit"];
            PitStallSignal.TrackPitSpeedLimit = float.Parse(GetSessionInfoValue(e.SessionInfo, query).TrimEnd('k', 'p', 'h', ' '));

            query = e.SessionInfo["WeekendInfo"]["TrackLength"];
            PitStallSignal.TrackLength = float.Parse(GetSessionInfoValue(e.SessionInfo, query).TrimEnd('k', 'm', ' '));

            query = e.SessionInfo["DriverInfo"]["DriverCarIdx"];
            var myIdx = float.Parse(GetSessionInfoValue(e.SessionInfo, query));

            query = e.SessionInfo["DriverInfo"]["Drivers"]["CarIdx", myIdx]["CarScreenName"];
            Car.ScreenName = GetSessionInfoValue(e.SessionInfo, query);

            query = e.SessionInfo["DriverInfo"]["Drivers"]["CarIdx", myIdx]["CarClassID"];
            Car.ClassId = int.Parse(GetSessionInfoValue(e.SessionInfo, query));

            query = e.SessionInfo["DriverInfo"]["DriverCarFuelMaxLtr"];
            Car.Fuel.MaxLtr = float.Parse(GetSessionInfoValue(e.SessionInfo, query));

            query = e.SessionInfo["DriverInfo"]["DriverCarMaxFuelPct"];
            Car.Fuel.MaxFuelPct = float.Parse(GetSessionInfoValue(e.SessionInfo, query));

            this.ParseDrivers(e.SessionInfo);

            isUpdatingDrivers = false;
        }

        private string GetSessionInfoValue(SessionInfo sessionInfo, YamlQuery query)
        {
            string returnValue;
            if (query.TryGetValue(out returnValue))
            {
                return returnValue;
            }
            else
            {
                Debug.WriteLine("**ERROR**\nYAML query failed - {0}\n", query);
            }
            return "0";
        }

        private void wrapper_TelemetryUpdated(object sender, SdkWrapper.TelemetryUpdatedEventArgs e)
        {
            if (isUpdatingDrivers) return;

            // Enable / Disable CrewChief
            var isOnTrack = wrapper.GetTelemetryValue<bool>("IsOnTrack").Value;

            if (isOnTrack && !status.Flag.HasFlag(Status.Flags.Ready))
            {
                status.Flag |= Status.Flags.Ready;
            }
            else status.Flag = 0;

            this.UpdateCarTelemetry(e.TelemetryInfo);
            this.UpdateDriversTelemetry(e.TelemetryInfo);
        }

        private void UpdateCarTelemetry(TelemetryInfo info)
        {
            //Car.FuelLevelGal = (float)0.26417 * wrapper.GetTelemetryValue<float>("FuelLevel").Value;
            Car.Fuel.FuelLevelPct = wrapper.GetTelemetryValue<float>("FuelLevelPct").Value;
            Car.Tire.Pressure.LFcoldPressure = wrapper.GetTelemetryValue<float>("LFcoldPressure").Value;
            Car.Tire.Pressure.RFcoldPressure = wrapper.GetTelemetryValue<float>("RFcoldPressure").Value;
            Car.Tire.Pressure.LRcoldPressure = wrapper.GetTelemetryValue<float>("LRcoldPressure").Value;
            Car.Tire.Pressure.RRcoldPressure = wrapper.GetTelemetryValue<float>("RRcoldPressure").Value;
            Car.Speed = wrapper.GetTelemetryValue<float>("Speed").Value;
        }

        private void UpdateDriversTelemetry(TelemetryInfo info)
        {
            // Get your own driver entry
            // This strange " => " syntax is called a lambda expression and is short for a loop through all drivers
            Driver me = drivers.FirstOrDefault(d => d.Id == wrapper.DriverId);

            // Get arrays of the laps, distances, track surfaces of every driver
            var laps = info.CarIdxLap.Value;
            var lapDistances = info.CarIdxLapDistPct.Value;
            var trackSurfaces = info.CarIdxTrackSurface.Value;
            var position = info.CarIdxPosition.Value;
            var estTime = info.CarIdxEstTime.Value;

            foreach (Driver driver in drivers)
            {
                driver.Lap = laps[driver.Id];
                driver.LapDistance = lapDistances[driver.Id];
                driver.TrackSurface = trackSurfaces[driver.Id];
                driver.Position = position[driver.Id];
                driver.EstTime = estTime[driver.Id];

                if (driver.IsOnTrack && me.IsOnTrack)
                {
                    driver.RelativeToMe = driver.EstTime - me.EstTime;
                    //Debug.WriteLine("driver.Number: {0} driver.RelativeToMe: {1}", driver.Number, driver.RelativeToMe);
                }

                if (driver.Position == 1 || leaderOnPitRoad == driver)
                {
                    if (leaderOnPitRoad == null && driver.IsInPits && driver.LastTrackSurface == TrackSurfaces.OnTrack)
                    {
                        leaderOnPitRoad = driver;
                        timerLeaderOnPitRoad.Enabled = true;
                        timerLeaderOnPitRoad.Start();
                    }

                    else if (leaderOnPitRoad == driver && !driver.IsInPits)
                    {
                        timerLeaderOnPitRoad.Stop();
                        if (timerLeaderOnPitRoad.Enabled) timerLeaderOnPitRoad.Enabled = false;
                        leaderOnPitRoad = null;
                    }
                }

                // If your own driver exists, use it to calculate the relative distance between you and the other driver
                if (me != null)
                {
                    var relative = driver.LapDistance - me.LapDistance;

                    // If driver is more than half the track behind, subtract 100% track length
                    // and vice versa
                    if (relative > 0.5) relative -= 1;
                    else if (relative < -0.5) relative += 1;

                    driver.RelativeLapDistance = relative;
                }
                else
                {
                    driver.RelativeLapDistance = -1;
                }

                if (driver == me)
                {
                    PitStallSignal.LapDist = driver.LapDistance;
                    if (driver.TrackSurface == TrackSurfaces.AproachingPits)
                    {
                        if (PitStallSignal.MetersToPitStall() / Car.Speed < 5.5 && !status.Flag.HasFlag(Status.Flags.PitPos))
                        {
                            status.Flag |= Status.Flags.PitPos;

                            if (!status.Flag.HasFlag(Status.Flags.Listening))
                            {
                                PromptBuilder pb = new PromptBuilder();
                                pb.AppendText("Pistaall turn in 3");
                                pb.AppendBreak(new TimeSpan(2500000));
                                pb.AppendText("2");
                                pb.AppendBreak(new TimeSpan(1500000));
                                pb.AppendText("1");
                                pb.AppendBreak(new TimeSpan(1500000));
                                pb.AppendText("now!");
                                QueuedSpeech.Enqueue(pb);
                                synthesizerSpeak();
                            }
                        }
                    }

                    if (driver.TrackSurface == TrackSurfaces.InPitStall)
                        if (driver.LastTrackSurface != driver.TrackSurface) Debug.WriteLine("*** InPitStall *** LastTrackSurface: {0}\n", driver.LastTrackSurface);

                    if (driver.TrackSurface == TrackSurfaces.NotInWorld)
                        if (driver.LastTrackSurface != driver.TrackSurface) Debug.WriteLine("*** NotInWorld *** LastTrackSurface: {0}\n", driver.LastTrackSurface);

                    if (driver.TrackSurface == TrackSurfaces.OnTrack)
                        if (driver.LastTrackSurface != driver.TrackSurface) Debug.WriteLine("*** OnTrack *** LastTrackSurface: {0}\n", driver.LastTrackSurface);

                    if (driver.TrackSurface == TrackSurfaces.OffTrack)
                        if (driver.LastTrackSurface != driver.TrackSurface) Debug.WriteLine("*** OffTrack *** LastTrackSurface: {0}\n", driver.LastTrackSurface);

                    if (driver.TrackSurface == TrackSurfaces.AproachingPits && driver.LastTrackSurface == TrackSurfaces.OnTrack)
                        if (driver.LastTrackSurface != driver.TrackSurface) Debug.WriteLine("*** AproachingPits *** LastTrackSurface: {0}\n", driver.LastTrackSurface);

                    if (driver.TrackSurface == TrackSurfaces.InPitStall && driver.LastTrackSurface == TrackSurfaces.AproachingPits)
                    {
                        Car.ClearPitStop();
                        status.Flag &= ~Status.Flags.PitPos;
                    }

                    if (driver.TrackSurface == TrackSurfaces.AproachingPits && driver.LastTrackSurface == TrackSurfaces.OnTrack)
                    {
                        //Run timer for 3 seconds to make sure car stays on pit road.
                        timerPitRoad.Enabled = true;
                    }
                }
                driver.LastTrackSurface = driver.TrackSurface;
            }
        }

        // Parse the YAML DriverInfo section that contains information such as driver id, name, license, car number, etc.
        private void ParseDrivers(SessionInfo sessionInfo)
        {
            int id = 0;
            Driver driver;
            var newDrivers = new List<Driver>();

            // Loop through drivers until none are found anymore
            do
            {
                driver = null;

                // Construct a yaml query that finds each driver and his info in the Session Info yaml string
                // This query can be re-used for every property for one driver (with the specified id)
                YamlQuery query = sessionInfo["DriverInfo"]["Drivers"]["CarIdx", id];

                // Try to get the UserName of the driver (because its the first value given)
                // If the UserName value is not found (name == null) then we found all drivers and we can stop
                string name = query["UserName"].GetValue();
                if (name != null)
                {
                    //// Find this driver in the list
                    //// This strange " => " syntax is called a lambda expression and is short for a loop through all drivers
                    //// Read as: select the first driver 'd', if any, whose Name is equal to name.
                    driver = drivers.FirstOrDefault(d => d.Name == name);

                    if (driver == null)
                    {
                        // Or create a new Driver if we didn't find him before
                        driver = new Driver();
                        driver.Id = id;
                        driver.Name = name;
                        driver.CustomerId = int.Parse(query["UserID"].GetValue("0")); // Account Number - default value 0
                        driver.Number = query["CarNumber"].GetValue("").TrimStart('\"').TrimEnd('\"'); // trim the quotes
                        driver.ClassId = int.Parse(query["CarClassID"].GetValue("0"));
                        driver.CarPath = query["CarPath"].GetValue();
                        driver.CarClassRelSpeed = int.Parse(query["CarClassRelSpeed"].GetValue("0"));
                        driver.Rating = int.Parse(query["IRating"].GetValue("0"));

                        // This doesnt belong here, make a better variable
                        driver.PitPos = float.Parse(sessionInfo["DriverInfo"]["DriverPitPos"].GetValue("0"));

                    }
                    newDrivers.Add(driver);
                    id++;
                }
            } while (driver != null);
            // Replace old list of drivers with new list of drivers and update the grid
            drivers.Clear();
            drivers.AddRange(newDrivers);
        }

        // Parse the YAML SessionInfo section that contains information such as lap times, position, etc.
        private void ParseTimes(SessionInfo sessionInfo)
        {
            int position = 1;
            Driver driver = null;

            // Loop through positions starting at 1 until no more are found
            do
            {
                driver = null;

                // Construct a yaml query that we can re-use again
                YamlQuery query = sessionInfo["SessionInfo"]["Sessions"]["SessionNum", currentSessionNum]
                    ["ResultsPositions"]["Position", position];


                // Find the car id belonging to the current position
                string idString = query["CarIdx"].GetValue();
                if (idString != null)
                {
                    int id = int.Parse(idString);

                    // Find the corresponding driver from the list
                    // This strange " => " syntax is called a lambda expression and is short for a loop through all drivers
                    // Read as: select the first driver 'd', if any, whose Id is equal to id.
                    driver = drivers.FirstOrDefault(d => d.Id == id);

                    if (driver != null)
                    {
                        driver.Position = position;
                        driver.FastestLapTime = float.Parse(query["FastestTime"].GetValue("0"), CultureInfo.InvariantCulture);
                        driver.LastLapTime = float.Parse(query["LastTime"].GetValue("0"), CultureInfo.InvariantCulture);
                    }

                    position++;
                }

            } while (driver != null);
        }

        private void CalculateFollowers()
        {
            foreach (Driver driver in drivers)
            {
                if (driver.RelativeLapDistance < 0 && driver.RelativeLapDistance > -.1)
                {
                    float range20 = 0;
                    float range10 = 0;

                    for (int i = 0; i < 20; i++) range20 = driver.RelativeLapDistanceHistory[i] + range20;
                    for (int i = 9; i < 20; i++) range10 = driver.RelativeLapDistanceHistory[i] + range10;

                    range20 = range20 / 20;
                    range10 = range10 / 10;
                }
            }
        }

        private void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Debug.WriteLine("Recognition result summary:");
            Debug.WriteLine(
              "  Recognized phrase: {0}\n" +
              "  Confidence score {1}\n" +
              "  Grammar used: {2}\n",
              e.Result.Text, e.Result.Confidence, e.Result.Grammar.Name);
            if (e.Result.Confidence > .5)
            {
                if (status.Flag.HasFlag(Status.Flags.Ready) &&
                    !status.Flag.HasFlag(Status.Flags.Analyzing) &&
                    !status.Flag.HasFlag(Status.Flags.Speaking))  // Both Listening and !Listening is valid
                {
                    if (timerCrewChief.Enabled)
                    {
                        Reset(timerCrewChief);
                    }
                    AnalyzeRecognizedSpeech(e.Result.Text.ToLower());
                }
            }
            else return;
        }

        private void synthesizer_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            status.Flag |= Status.Flags.Speaking;
        }

        private void synthesizerSpeak()
        {
            PlayingQueuedSpeech = QueuedSpeech;

            if (!status.Flag.HasFlag(Status.Flags.Speaking))
            {
                int queueCount = PlayingQueuedSpeech.Count;
                while (PlayingQueuedSpeech.Count > 0)
                {
                    if (PlayingQueuedSpeech.Count == queueCount)
                    {
                        SoundPlayer audio = new SoundPlayer(CrewChief.Properties.Resources.RadioMicKeyUp);
                    }
                    synthesizer.SpeakAsync(PlayingQueuedSpeech.Dequeue());
                }
                if (PlayingQueuedSpeech.Count == 0)
                {
                    QueuedSpeech.Clear();
                }
            }
        }

        private void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {

            if (PlayingQueuedSpeech.Count == 0)
            {
                SoundPlayer audio = new SoundPlayer(CrewChief.Properties.Resources.RadioMicKeyUp);
                audio.Play();
                status.Flag &= ~Status.Flags.Speaking;
            }
        }

        private Grammar CreateGrammar1()
        {

            Choices commandChoices = new Choices("lap timing", "standings", "relative", "fuel", "tires", "tire information",
                "tire info", "pit stop adjustments", "in car adjustments", "graphic adjustments", "reset black box",
                "radio", "crew chief", "out", "clear pit stop", "fast repair");
            GrammarBuilder grammarBuilder = new GrammarBuilder(commandChoices);
            Grammar g = new Grammar(grammarBuilder);
            g.Name = "BlackBox";

            return g;
        }

        private Grammar CreateGrammar2()
        {

            Choices commandChoices = new Choices("change", "clean");
            Choices changeChoices = new Choices("left front tire", "front left tire", "right front tire", "front right tire",
                "left rear tire", "rear left tire", "right rear tire", "rear right tire", "left side tires", "left side only",
                "right side tires", "right side only", "all four tires", "windshield");
            GrammarBuilder grammarBuilder = new GrammarBuilder(commandChoices);
            grammarBuilder.Append(changeChoices);
            Grammar g = new Grammar(grammarBuilder);
            g.Name = "Command";

            return g;
        }

        private Grammar CreateGrammar3()
        {

            Choices commandChoices = new Choices("add", "remove");
            Choices numberChoices = new Choices(".5", "1", "1.5", "2", "2.5", "3", "3.5", "4", "4.5", "5", "5.5",
                "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5", "11", "11.5", "12", "12.5",
                "13", "13.5", "14", "14.5", "15.0", "15.5", "16", "16.5", "17", "17.5", "18", "18.5", "19",
                "19.5", "20", "20.5", "full tank", "half tank", "quarter tank");
            Choices airfuelChoices = new Choices("pounds", "gallons", "litres", "rounds");
            Choices tireChoices = new Choices("left front tire", "front left tire", "right front tire", "front right tire",
                "left rear tire", "rear left tire", "right rear tire", "rear right tire", "left side tires", "left side only",
                "right side tires", "right side only", "all four tires", "fuel", "gas", "petro", "wedge");

            GrammarBuilder grammarBuilder = new GrammarBuilder(commandChoices);
            grammarBuilder.Append(commandChoices, 0, 0);
            grammarBuilder.Append(numberChoices, 0, 1);
            grammarBuilder.Append(airfuelChoices, 0, 1);
            grammarBuilder.Append(tireChoices, 0, 1);
            Grammar g = new Grammar(grammarBuilder);
            g.Name = "Command";

            return g;
        }

        private void AnalyzeRecognizedSpeech(string text)
        {
            Debug.WriteLine("**** Analyzing Speech\n");
            //
            // Regex.Match(speech, @"\btomorrow(')*s\b\s+\bdate\b", RegexOptions.IgnoreCase)
            //

            status.Flag |= Status.Flags.Analyzing;

            //PromptBuilder pb = new PromptBuilder();

            if (status.Flag.HasFlag(Status.Flags.Listening))
            {
                if (text.Equals("crew chief"))
                {
                    EnqueueSpeech("still here");
                    synthesizerSpeak();
                }
                else if (text.Equals("out"))
                {
                    OnCrewChiefTimer(null, null);
                }

                else if (text.Equals("fast repair"))
                {
                    Car.FastRepair = true;
                }

                else if (text.Equals("clear pit stop"))
                {

                    Car.PitStopCleared = true;
                }

                else if (text.Contains("change"))
                {
                    if (text.Contains("left front tire") || text.Contains("front left tire"))
                        Car.Tire.Change.LF = true;

                    else if (text.Contains("right front tire") || text.Contains("front right tire"))
                        Car.Tire.Change.RF = true;

                    else if (text.Contains("left rear tire") || text.Contains("rear left tire"))
                        Car.Tire.Change.LR = true;

                    else if (text.Contains("right rear tire") || text.Contains("rear right tire"))
                        Car.Tire.Change.RR = true;

                    else if (text.Contains("left side tires") || text.Contains("left side only"))
                    {
                        Car.Tire.Change.LF = true;
                        Car.Tire.Change.LR = true;
                    }
                    else if (text.Contains("right side tires") || text.Contains("right side only"))
                    {
                        Car.Tire.Change.RF = true;
                        Car.Tire.Change.RR = true;
                    }
                    else if (text.Contains("all four tires") || text.Contains("all tires"))
                    {
                        Car.Tire.Change.LF = true;
                        Car.Tire.Change.LR = true;
                        Car.Tire.Change.RF = true;
                        Car.Tire.Change.RR = true;
                    }
                    else
                    {
                        AnalysisFailed();
                    }
                }
                else if (text.Contains("add") || text.Contains("remove"))
                {
                    if (text.Contains("fuel") || text.Contains("gas") || text.Contains("petrol"))
                    {
                        if (text.Contains("gallons"))
                        {
                            Car.Fuel.Amount = Car.Fuel.ToLiters(Convert.ToInt32(GetFloatFromString(text)));
                        }
                        else if (text.Contains("litres"))
                        {
                            Car.Fuel.Amount = Convert.ToInt32(GetFloatFromString(text));
                        }
                        else if (text.Contains("tank"))
                        {
                            if (text.Contains("full tank"))
                            {
                                Debug.WriteLine("Car.Fuel.Amount: {0} Car.Fuel.MaxLtr: {1}\n", Car.Fuel.Amount, Car.Fuel.MaxLtr);
                                Car.Fuel.Amount = Car.Fuel.MaxLtr;
                            }
                            else if (text.Contains("half tank"))
                            {
                                Car.Fuel.Amount = Car.Fuel.MaxLtr/2;
                            }
                            else if (text.Contains("quarter tank"))
                            {
                                Car.Fuel.Amount = Car.Fuel.MaxLtr/4;
                            }
                            else if (text.Contains("splash"))
                            {
                                Car.Fuel.Amount = 1;
                            }
                            else
                            {
                                AnalysisFailed();
                            }
                        }
                    }
                    else if (text.Contains("pounds"))
                    {
                        if (text.Contains("left front tire") || text.Contains("front left tire"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.LF = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.LF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }
                        }
                        else if (text.Contains("right front tire") || text.Contains("front right tire"))
                        {
                            if (text.Contains("add"))
                            {
                                var input = GetFloatFromString(text);
                                Car.Tire.Pressure.RF = Car.Tire.Pressure.ToKpa(input);
                                Debug.WriteLine("Add pressure to RF tire - PSI: {0} KPA: {1}\n", input, Car.Tire.Pressure.RF);
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.RF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }
                        }
                        else if (text.Contains("left rear tire") || text.Contains("rear left tire"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.LR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.LR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }
                        }
                        else if (text.Contains("right rear tire") || text.Contains("rear right tire"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.RR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.RR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }
                        }
                        else if (text.Contains("left side tires"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.LF = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.LR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.LF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.LR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }

                        }
                        else if (text.Contains("right side tires"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.RF = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.RF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else
                            {
                                AnalysisFailed();
                            }

                        }
                        else if (text.Contains("all four tires"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Tire.Pressure.LF = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.LR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RF = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RR = Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else if (text.Contains("remove"))
                            {
                                Car.Tire.Pressure.LF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.LR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RF = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                                Car.Tire.Pressure.RR = -1 * Car.Tire.Pressure.ToKpa(GetFloatFromString(text));
                            }
                            else AnalysisFailed();
                        }
                        else if (text.Contains("wedge"))
                        {
                            if (text.Contains("add"))
                            {
                                Car.Chassis.Wedge = GetFloatFromString(text);
                            }
                            else
                            {
                                Car.Chassis.Wedge = -1 * GetFloatFromString(text);
                            }
                        }
                        else  AnalysisFailed();
                    }
                }
                else if (text.Contains("clean windshield"))
                {
                    Car.Windscreen.TearOff = true;
                }
                else AnalysisFailed();
            }

            else if (!status.Flag.HasFlag(Status.Flags.Listening))
            {
                if (text.Equals("lap timing") || text.Equals("f1"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.LapTiming);
                else if (text.Equals("standings") || text.Equals("f2"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.Standings);
                else if (text.Equals("relative") || text.Equals("f3"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.Relative);
                else if (text.Equals("fuel") || text.Equals("f4"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.Fuel);
                else if (text.Equals("tires") || text.Equals("f5"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.Tires);
                else if (text.Equals("tire information") || text.Equals("tire info") || text.Equals("f6"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.TireInformation);
                else if (text.Equals("pit stop adjustments") || text.Equals("f7"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.PitStopAdjustments);
                else if (text.Equals("in car adjustments") || text.Equals("f8"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.InCarAdjustments);
                else if (text.Equals("graphic adjustments") || text.Equals("f9"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.GraphicsAdjustments);
                else if (text.Equals("radio") || text.Equals("f10"))
                    Sim.BlackBox.Change(Sim.BlackBox.Type.Radio);
                else if (text.Equals("reset black box"))
                    Sim.BlackBox.ResetBlackBox();
                else if (text.Equals("crew chief"))
                {
                    status.Flag |= Status.Flags.Listening;

                    if (!timerCrewChief.Enabled)
                    {
                        timerCrewChief.Enabled = true;
                        timerCrewChief.Start();
                    }
                    EnqueueSpeech("yes sir");
                    synthesizerSpeak();
                }
            }
            status.Flag &= ~Status.Flags.Analyzing;
        }

        private void AnalysisFailed()
        {
            PromptBuilder pb = new PromptBuilder();
            EnqueueSpeech("your radio broke up, please say it again");
            synthesizerSpeak();
        }

        private void OnLeaderOnPitRoadTimer(object sender, ElapsedEventArgs e)
        {
            timerLeaderOnPitRoad.Stop();
            timerLeaderOnPitRoad.Enabled = false;

            if (!status.Flag.HasFlag(Status.Flags.Listening))
            {
                PromptBuilder pb = new PromptBuilder();
                EnqueueSpeech("the leader has entered pit road");
                synthesizerSpeak();
            }
        }


        private void OnPitRoadTimer(object sender, ElapsedEventArgs e)
        {
            Driver me = drivers.FirstOrDefault(d => d.Id == wrapper.DriverId);

            if (me.TrackSurface == TrackSurfaces.AproachingPits)
            {
                timerPitRoad.Enabled = false;

                var change = new PitCommandControl.TireChange();

                if (Car.Tire.Change.LF) change.LeftFront.Change = true;
                if (Car.Tire.Change.RF) change.RightFront.Change = true;
                if (Car.Tire.Change.LR) change.LeftRear.Change = true;
                if (Car.Tire.Change.RR) change.RightRear.Change = true;

                if (Car.Tire.Pressure.LF != 0)
                {
                    change.LeftFront.Change = true;
                    change.LeftFront.Pressure = (int)Car.Tire.Pressure.LFcoldPressure + (int)Car.Tire.Pressure.LF;
                }
                if (Car.Tire.Pressure.RF != 0)
                {
                    change.RightFront.Change = true;
                    change.RightFront.Pressure = (int)Car.Tire.Pressure.RFcoldPressure + (int)Car.Tire.Pressure.RF;
                }
                if (Car.Tire.Pressure.LR != 0)
                {
                    change.LeftRear.Change = true;
                    change.LeftRear.Pressure = (int)Car.Tire.Pressure.LRcoldPressure + (int)Car.Tire.Pressure.LR;
                }
                if (Car.Tire.Pressure.RR != 0)
                {
                    change.RightRear.Change = true;
                    change.RightRear.Pressure = (int)Car.Tire.Pressure.RRcoldPressure + (int)Car.Tire.Pressure.RR;
                }

                wrapper.PitCommands.ChangeTires(change);

                if (Car.Fuel.Amount > 0) wrapper.PitCommands.AddFuel((int)Car.Fuel.Amount);

                if (Car.Windscreen.TearOff) wrapper.PitCommands.Tearoff();

                if (Car.FastRepair) wrapper.PitCommands.FastRepair();

                if (Car.Chassis.Wedge != 0) Car.Chassis.AdjustWedge();
            }
        }

        private void OnCrewChiefTimer(object sender, ElapsedEventArgs e)
        {

            status.Flag &= ~Status.Flags.Listening;

            timerCrewChief.Stop();
            timerCrewChief.Enabled = false;
            PromptBuilder pb = new PromptBuilder();

            if (Car.PitStopCleared)
            {
                Car.ClearPitStop();
                QueuedSpeech.Clear();
                EnqueueSpeech("pit stop cleared");
            }

            #region ChangeTire

            if (Car.Tire.Change.LF && Car.Tire.Change.RF && Car.Tire.Change.LR && Car.Tire.Change.RR)
            {
                EnqueueSpeech("changing all four tires");
            }
            else if (Car.Tire.Change.LF && Car.Tire.Change.LR)
            {
                EnqueueSpeech("changing left side tires");
            }
            else if (Car.Tire.Change.RF && Car.Tire.Change.RR)
            {
                EnqueueSpeech("changing right side tires");
            }
            if (Car.Tire.Change.LF && !Car.Tire.Change.LR)
            {
                EnqueueSpeech("changing left front tire");
            }
            if (Car.Tire.Change.RF && !Car.Tire.Change.RR)
            {
                EnqueueSpeech("changing right front tire");
            }
            if (!Car.Tire.Change.LF && Car.Tire.Change.LR)
            {
                EnqueueSpeech("changing left rear tire");
            }
            if (!Car.Tire.Change.RF && Car.Tire.Change.RR)
            {
                EnqueueSpeech("changing right rear tire");
            }

            #endregion

            #region ChangeTireAirPressure

            if (Car.Tire.Pressure.LF != 0 || Car.Tire.Pressure.RF != 0 || Car.Tire.Pressure.LR != 0 || Car.Tire.Pressure.RR != 0)
            {
                if (AllEqual(Car.Tire.Pressure.LF, Car.Tire.Pressure.RF, Car.Tire.Pressure.LR, Car.Tire.Pressure.RR))
                {
                    if (Car.Tire.Pressure.LF > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to all four tires", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LF)));
                    }
                    else
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to all four tires", Car.Tire.Pressure.ToPsi(Sim.Tire.LFPressure)));
                    }
                }
                else if (AllEqual(Car.Tire.Pressure.LF, Car.Tire.Pressure.LR))
                {

                    if (Car.Tire.Pressure.LF > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to left side tires", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LF)));
                    }
                    else
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to left side tires", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LF)));
                    }
                }

                else if (AllEqual(Car.Tire.Pressure.RF, Car.Tire.Pressure.RR))
                {
                    if (Car.Tire.Pressure.RF > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to right side tires", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RF)));
                    }
                    else
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to right side tires", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RF)));
                    }
                }
                else
                {
                    if (Car.Tire.Pressure.LF > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to left front tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LF)));
                    }
                    else if (Car.Tire.Pressure.LF < 0)
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to left front tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LF)));
                    }
                    if (Car.Tire.Pressure.LR > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to left rear tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LR)));
                    }
                    else if (Car.Tire.Pressure.LR < 0)
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to left rear tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.LR)));
                    }
                    if (Car.Tire.Pressure.RF > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to right front tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RF)));
                    }
                    else if (Car.Tire.Pressure.RF < 0)
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to right front tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RF)));
                    }
                    if (Car.Tire.Pressure.RR > 0)
                    {
                        EnqueueSpeech(string.Format("adding {0} pounds to right rear tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RR)));
                    }
                    else if (Car.Tire.Pressure.RR < 0)
                    {
                        EnqueueSpeech(string.Format("removing {0} pound to right rear tire", Car.Tire.Pressure.ToPsi(Car.Tire.Pressure.RR)));
                    }
                }
            }

            #endregion


            if (Car.Fuel.Amount != 0)
            {
                if (Car.Fuel.Amount >= Car.Fuel.MaxLtr)
                {
                    EnqueueSpeech("adding a full tank of fuel");
                }
                else
                {
                    EnqueueSpeech(string.Format("adding {0} gallons of fuel", Math.Round(Car.Fuel.ToGallons(Car.Fuel.Amount))));
                }
            }
            if (Car.Chassis.Wedge != 0)
            {
                if (Car.Chassis.Wedge > 0)
                {
                    EnqueueSpeech(string.Format("adding {0} rounds of wedge", Car.Chassis.Wedge));
                }
                else
                {
                    EnqueueSpeech(string.Format("removing {0} rounds of wedge", Car.Chassis.Wedge));
                }
            }

            if (Car.Windscreen.TearOff)
            {
                EnqueueSpeech("removing windscreen tare off");
            }

            if (Car.FastRepair)
            {
                EnqueueSpeech("fast repair enabled");
            }

            EnqueueSpeech("crew chief out");
            synthesizerSpeak();
            status.Flag &= ~Status.Flags.Analyzing;
        }

        private void GetFollowers(float distance)
        {
            foreach (Driver driver in drivers)
            {
                if (driver.RelativeLapDistance < 0 && driver.RelativeLapDistance > distance)
                {
                    if (driver.RelativeLapDistance != 0)
                    {

                    }

                }

            }
        }

        private void EnqueueSpeech(string text)
        {
            PromptBuilder pb = new PromptBuilder();
            pb.AppendText(text);
            pb.AppendBreak();
            QueuedSpeech.Enqueue(pb);
        }

        public static void Reset(System.Timers.Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        public static float GetFloatFromString(string text)
        {
            Debug.WriteLine("GetFloatFromString - text: {0}", text);
            float number = 0;
            float value = 0;
            string[] words = text.Split(' ');

            foreach (string word in words)
            {
                Debug.WriteLine("GetFloatFromString - word: {0}", word);

                bool isNumeric = float.TryParse(word, out number);
                Debug.WriteLine("GetFloatFromString - number: {0}", number);
                if (number > 0)
                {
                    value = number;
                    Debug.WriteLine("GetFloatFromString - value: {0}", value);
                }
            }
            Debug.WriteLine("GetFloatFromString - value: {0}\n", value);
            return value;
        }

        public float KpaToPsi(float value)
        {
            return Convert.ToSingle(value * 0.14503773773020923);
        }

        public float PsiToKpa(float value)
        {
            return Convert.ToSingle(6.894757293168361 * value);
        }

        public bool NearlyEqual(float a, float b, float epsilon)
        {
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(a - b);

            if (a == b)
            { // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || diff < float.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return true;
            }
            else
            { // use relative error
                return false;
            }
        }

        public bool AllEqual<T>(params T[] values)
        {
            if (values == null || values.Length == 0)
                return true;
            return values.All(v => v.Equals(values[0]));
        }

        private void dataGrid_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    class Status : INotifyPropertyChanged
    {
        private Flags _flag;

        public event PropertyChangedEventHandler PropertyChanged;

        [Flags]
        public enum Flags
        {
            Listening = 0x0001,
            Ready = 0x0002,
            Analyzing = 0x0004,
            Speaking = 0x0008,
            PitPos = 0x0010,
           // LeaderOnPitRoad = 0x0011,
        }

        public Status()
        { }

        public Flags Flag
        {
            get { return _flag; }
            set
            {
                _flag = value;
                OnPropertyChanged("Flag");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
    }
}



