using iRacingSdkWrapper;
using iRacingSdkWrapper.Broadcast;     
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChief

  
{
    class SimOld : Car
    {

        public bool BlackBoxOn { get; set; }
        private static BlackBox _blackBoxStatus { get; set; }
        public static string CarScreenName { get; set; }
        public static int CarClassID { get; set; }
        //public static PitCommand PitCommands { get; set; }

        [Flags]
        //public enum PitCommand
        //{
        //    ChangeLF = 0x00000001,
        //    ChangeRF = 0x00000002,
        //    ChangeLR = 0x00000004,
        //    ChangeRR = 0x00000008,
        //    AddAirLF = 0x00000010,
        //    AddAirRF = 0x00000020,
        //    AddAirLR = 0x00000040,
        //    AddAirRR = 0x00000080,
        //    AddAirLS = 0x00000100,
        //    AddAirRS = 0x00000200,
        //    AddAirAF = 0x00000400,
        //    RemoveAirLF = 0x00000800,
        //    RemoveAirRF = 0x00001000,
        //    RemoveAirLR = 0x00002000,
        //    RemoveAirRR = 0x00004000,
        //    RemoveAirLS = 0x00008000,
        //    RemoveAirRS = 0x00010000,
        //    RemoveAirAF = 0x00020000,
        //    CleanWindshield = 0x00040000,
        //    AddFuel = 0x00080000,
        //}

        public enum BlackBox
        {
            [Description("{F1}")]
            LapTiming = 1,
            [Description("{F2}")]
            Standings,
            [Description("{F3}")]
            Relative,
            [Description("{F4}")]
            Fuel,
            [Description("{F5}")]
            Tires,
            [Description("{F6}")]
            TireInformation,
            [Description("{F7}")]
            PitStopAdjustments,
            [Description("{F8}")]
            InCarAdjustments,
            [Description("{F9}")]
            GraphicsAdjustments,
            [Description("{F10}")]
            Radio,
        }


        public static void ResetBlackBox()
        {
            ProcessHelper.SetFocusToExternalApp("iRacingSim64");
            SendKeys.SendWait("{F1}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F2}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F4}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F5}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F7}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F8}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F9}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F10}");
            Thread.Sleep(100);
            SendKeys.SendWait("{HOME}");
            Thread.Sleep(100);
            SendKeys.SendWait("{F3}");
            Thread.Sleep(100);
            _blackBoxStatus = BlackBox.Relative;
        }

        public static void Change(BlackBox bb)
        {
            if (_blackBoxStatus != bb)
            {
                ProcessHelper.SetFocusToExternalApp("iRacingSim64");
                string text = GetEnumDescription(bb);
                Thread.Sleep(100);
                SendKeys.SendWait(text);
                _blackBoxStatus = bb;
             }
        }

        //public static void ChangeTires()
        //{
        //    ProcessHelper.SetFocusToExternalApp("iRacingSim64");

        //    Console.Write("ChangeTires() PitCommands: {0}\n", PitCommands);
        //    Console.Write("ChangeTires() ChangeLFcoldPressure: {0}\n", ChangeLFcoldPressure);
        //    Console.Write("ChangeTires() ChangeRFcoldPressure: {0}\n", ChangeRFcoldPressure);
        //    Console.Write("ChangeTires() ChangeLRcoldPressure: {0}\n", ChangeLRcoldPressure);
        //    Console.Write("ChangeTires() ChangeRRcoldPressure: {0}\n", ChangeRRcoldPressure);

        //    if (PitCommands.HasFlag(PitCommand.ChangeLF) || PitCommands.HasFlag(PitCommand.AddAirLF) 
        //        || PitCommands.HasFlag(PitCommand.RemoveAirLF))
        //    {

        //        //var change = new PitCommandControl.TireChange();
        //        //change.LeftFront.Change = true;
        //        //change.LeftFront.Pressure = 145;
        //        //var wrapper = 
        //        //wrapper.PitCommands.ChangeTires(change);



        //        SendKeys.SendWait("t");
        //        Thread.Sleep(200);
        //        if (PitCommands.HasFlag(PitCommand.RemoveAirLF))
        //        {
        //            SendKeys.SendWait(string.Format("#lf {0}psi", ChangeLFcoldPressure * -1));
        //        }
        //        else
        //        {
        //            SendKeys.SendWait(string.Format("#lf {0}psi",ChangeLFcoldPressure));
                    
        //        }
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //        Thread.Sleep(200);
        //    }
        //    if (PitCommands.HasFlag(PitCommand.ChangeLR) || PitCommands.HasFlag(PitCommand.AddAirLR)
        //        || PitCommands.HasFlag(PitCommand.RemoveAirLR))
        //    {
        //        SendKeys.SendWait("t");
        //        Thread.Sleep(200);
        //        if (PitCommands.HasFlag(PitCommand.RemoveAirLR))
        //        {
        //            SendKeys.SendWait(String.Format("#lr {0}psi", ChangeLRcoldPressure * -1));
        //        }
        //        else
        //        {
        //            SendKeys.SendWait(String.Format("#lr {0}psi", ChangeLRcoldPressure));
        //        }
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //        Thread.Sleep(200);
        //    }
        //    if (PitCommands.HasFlag(PitCommand.ChangeRF) || PitCommands.HasFlag(PitCommand.AddAirRF)
        //        || PitCommands.HasFlag(PitCommand.RemoveAirRF))
        //    {
        //        SendKeys.SendWait("t");
        //        Thread.Sleep(200);
        //        if (PitCommands.HasFlag(PitCommand.RemoveAirRF))
        //        {
        //            SendKeys.SendWait(String.Format("#rf {0}psi", ChangeRFcoldPressure * -1));
        //        }
        //        else
        //        {
        //            SendKeys.SendWait(String.Format("#rf {0}psi", ChangeRFcoldPressure));
        //        }
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //        Thread.Sleep(200);
        //    }
        //    if (PitCommands.HasFlag(PitCommand.ChangeRR) || PitCommands.HasFlag(PitCommand.AddAirRR)
        //        || PitCommands.HasFlag(PitCommand.RemoveAirRR))
        //    {
        //        SendKeys.SendWait("t");
        //        Thread.Sleep(200);
        //        if (PitCommands.HasFlag(PitCommand.RemoveAirRR))
        //        {
        //            SendKeys.SendWait(String.Format("#rr {0}psi", ChangeRRcoldPressure * -1));
        //        }
        //        else
        //        {
        //            SendKeys.SendWait(String.Format("#rr {0}psi", ChangeRRcoldPressure));
        //        }
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //        Thread.Sleep(200);
        //    }
        //}

        //public static void Fuel()
        //{
        //    Console.Write("Fuel() PitCommands: {0}\n", PitCommands);
        //    if (PitCommands.HasFlag(PitCommand.AddFuel))
        //    {
        //        ProcessHelper.SetFocusToExternalApp("iRacingSim64");
        //        SendKeys.SendWait("t");
        //        Thread.Sleep(100);
        //        SendKeys.SendWait(String.Format("#fuel {0}g", Car.AddFuelGal));
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //    }
        //}

        //public static void Clear()
        //{
        //    ProcessHelper.SetFocusToExternalApp("iRacingSim64");
        //    SendKeys.SendWait("t");
        //    Thread.Sleep(100);
        //    SendKeys.SendWait("#clear");
        //    Thread.Sleep(200);
        //    SendKeys.SendWait("{ENTER}");
        //}

        //public static void CleanWindshield()
        //{
        //    Console.Write("CleanWindshield() PitCommands: {0}\n", PitCommands);
        //    if (PitCommands.HasFlag(PitCommand.CleanWindshield))
        //    {
        //        ProcessHelper.SetFocusToExternalApp("iRacingSim64");
        //        SendKeys.SendWait("t");
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("#ws");
        //        Thread.Sleep(200);
        //        SendKeys.SendWait("{ENTER}");
        //    }
        //}

        public static void DisableInGameSpotter()
        {
            ProcessHelper.SetFocusToExternalApp("iRacingSim64");
            for (int x = 0; x < 13; x++)
            {
                SendKeys.SendWait("-");
            }
        }

        public static void EnableInGameSpotter()
        {
            ProcessHelper.SetFocusToExternalApp("iRacingSim64");
            for (int x = 0; x < 13; x++)
            {
                SendKeys.SendWait("=");
            }
        }


        private static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }

    class ProcessHelper
    {
        public static void SetFocusToExternalApp(string strProcessName)
        {
            Process[] arrProcesses = Process.GetProcessesByName(strProcessName);
            if (arrProcesses.Length > 0)
            {
                IntPtr ipHwnd = arrProcesses[0].MainWindowHandle;
                Thread.Sleep(100);
                SetForegroundWindow(ipHwnd);
            }
        }
        //API-declaration
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }


    
}

