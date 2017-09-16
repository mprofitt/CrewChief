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
    public class Sim : MainWindow
    {
        public enum CarScreenNameShort
        {
            [Description("Ford 34C")]
            Ford34C
        }



        static public void Clear()
        {
            Tire.Clear();
            Windscreen.Clear();
            Fuel.Clear();
            Tape.Clear();
            Fast.Clear();
        }

        public class Car
        {
            public static string ScreenName { get; set; }
            public static int ClassId { get; set; }
        }

        public class Fast
        {
            public static bool Repair { get; set; }

            static public void Clear()
            {
                Repair = false;
            }
        }

        public class Tape
        {
            public static int Amount { get; set; }
            public static float Current { get; set; }


            static public void Clear()
            {
                Amount = Convert.ToInt32(Current);
            }

            static public void Add(int amount)
            {
                Amount = Convert.ToInt32(Current) + Amount;
                SendKeys.SendWait("{F8}");
                Thread.Sleep(100);

            }

            static public void Remove(int amount)
            {
                Amount = Convert.ToInt32(Current) - Amount;
                SendKeys.SendWait("{F8}");
                Thread.Sleep(100);

            }

            static public void Change(int amount)
            {
                SendKeys.SendWait("{F8}");
                Thread.Sleep(100);

            }

        }

        public class Tire
        {
            public static float LFPressure { get; set; } = 0;
            public static float LRPressure { get; set; } = 0;
            public static float RFPressure { get; set; } = 0;
            public static float RRPressure { get; set; } = 0;
            public static bool LFChange { get; set; } = false;
            public static bool LRChange { get; set; } = false;
            public static bool RFChange { get; set; } = false;
            public static bool RRChange { get; set; } = false;

            static public void Clear()
            {
                Tire.LFPressure = 0;
                Tire.LRPressure = 0;
                Tire.RFPressure = 0;
                Tire.RRPressure = 0;
                Tire.LFChange = false;
                Tire.LRChange = false;
                Tire.RFChange = false;
                Tire.RRChange = false;
            }
        }

        public class Windscreen
        {
            public static bool Clean { get; set; }

            static public void Clear()
            {
                Windscreen.Clean = false;
            }
        }

        public class Fuel
        {
            public static int Amount {get; set;}
            public static float MaxLtr { get; set; }
            public static float MaxPct { get; set; }
            static public void Clear()
            {
                    Fuel.Amount = 0;
            }

        }

        public class BlackBox
        {
            public static bool On { get; set; }
            private static Type Status { get; set; }

            public enum Type
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
                Status = Type.Relative;
            }

            public static void Change(Type bb)
            {
                if (Status != bb)
                {
                    ProcessHelper.SetFocusToExternalApp("iRacingSim64");
                    string text = GetEnumDescription(bb);
                    Thread.Sleep(100);
                    SendKeys.SendWait(text);
                    Status = bb;
                }
            }

            public static float ToGallon(float input)
            {
                return input * (float)0.26417;
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
}
