using iRacingSdkWrapper;
using iRacingSdkWrapper.Broadcast;

namespace CrewChief
{

    static public class Car
    {
        public static float TapeConfiguration { get; set; }
        public static bool FastRepair { get; set; }
        public static int ClassId { get; set; }
        public static string ScreenName { get; set; }
        public static bool PitStopCleared { get; set;}
        public static float Speed { get; set; }

        public static void ClearPitStop()
        {
            FastRepair = false;
            Car.Fuel.Amount = 0;
            Car.Tire.Change.LF = false;
            Car.Tire.Change.RF = false;
            Car.Tire.Change.LR = false;
            Car.Tire.Change.RR = false;
            Car.Tire.Pressure.LF = 0;
            Car.Tire.Pressure.RF = 0;
            Car.Tire.Pressure.LR = 0;
            Car.Tire.Pressure.RR = 0;
            Car.Windscreen.TearOff = false;
            Car.PitStopCleared = false;
        }

        public class Fuel : MainWindow
        {
            public static float MaxLtr { get; set; }
            public static float MaxFuelPct { get; set; }
            public static float FuelLevelPct { get; set; }
            public static float Amount { get; set; }

            public Fuel()
            { }

            public static float ToGallons(float input)
            {
                return input * (float).264172;
            }

            public static float ToLiters(float input)
            {
                return input * (float)3.78541;
            }
        }

        static public class Tire 
        {
            public class Change
            {
                public static bool LF { get; set; }
                public static bool RF { get; set; }
                public static bool LR { get; set; }
                public static bool RR { get; set; }
            }

            static public class Pressure
            {
                public static float LFcold { get; set; }
                public static float RFcold { get; set; }
                public static float LRcold { get; set; }
                public static float RRcold { get; set; }
                public static float LF { get; set; }
                public static float RF { get; set; }
                public static float LR { get; set; }
                public static float RR { get; set; }

                public static float ToKpa(float input)
                {
                    return input * (float)6.894757293168361;
                }
                public static float ToPsi(float input)
                {
                    return input * (float)0.145037738;
                }
            }
        }
        public class Chassis
        {
            public static float Wedge;

            public static void AdjustWedge()
            {
                PitStop.BlackBox.ResetBlackBox();


            }
        }

        public class Windscreen : MainWindow
        {
            public static bool TearOff;

        }
    }
}


