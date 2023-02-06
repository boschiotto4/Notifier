//
//      Screens.cs
// 
//      This class is an integration of something founded here:
//      https://www.wpftutorial.net/ScreenResolutions.html
//      A very grateful work about the multi-Monitor support in Wpf.
//      I integrate the code with the "getWorkingArea()" static method
//      in order to get the Wpf Window actual WorkingArea
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;

namespace WPFNotify
{
    public class Screens
    {
#region Dll imports - GetMonitorInfo - EnumDisplayMonitors - GetCursorPos
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        private static extern bool GetMonitorInfo
                      (HandleRef hmonitor, [In, Out]MonitorInfoEx info);

        [DllImport("user32.dll", ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        private static extern bool EnumDisplayMonitors
             (HandleRef hdc, IntPtr rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        private delegate bool MonitorEnumProc
                     (IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
#endregion

#region STRUCTs
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        private class MonitorInfoEx
        {
            internal int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
            internal Rect rcMonitor = new Rect();
            internal Rect rcWork = new Rect();
            internal int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            internal char[] szDevice = new char[32];
        }
#endregion

#region GLOBALS
        public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public System.Windows.Rect WorkingArea { get; private set; }

        private Screens(IntPtr screen, IntPtr hdc)
        {
            var info = new MonitorInfoEx();
            GetMonitorInfo(new HandleRef(null, screen), info);
            WorkingArea = new System.Windows.Rect(
                        info.rcWork.left, info.rcWork.top,
                        info.rcWork.right - info.rcWork.left,
                        info.rcWork.bottom - info.rcWork.top);
        }
#endregion

#region GET MOUSE POSITION

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Return the X - Y Mouse position over all Screens
        //-------------------------------------------------------------------------------------------------------------------------------
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
#endregion

#region ALL MONITORS CALLBACK
        private static IEnumerable<Screens> AllMonitors
        {
            get
            {
                var closure = new MonitorEnumCallback();
                var proc = new MonitorEnumProc(closure.Callback);
                EnumDisplayMonitors(NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);
                return closure.Monitors.Cast<Screens>();
            }
        }

        private class MonitorEnumCallback
        {
            public ArrayList Monitors { get; private set; }

            public MonitorEnumCallback()
            {
                Monitors = new ArrayList();
            }

            public bool Callback(IntPtr monitor, IntPtr hdc,
                           IntPtr lprcMonitor, IntPtr lparam)
            {
                Monitors.Add(new Screens(monitor, hdc));
                return true;
            }
        }
#endregion

#region GET WORKING AREA
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Get the WorkingArea of the actual Window
        //-------------------------------------------------------------------------------------------------------------------------------
        internal static System.Windows.Rect getWorkingArea()
        {
            System.Windows.Rect rect = System.Windows.SystemParameters.WorkArea;           // Default: init with the Wpf Primary Screen
            Point mousePosition = Screens.GetMousePosition();
            foreach (Screens monitor in Screens.AllMonitors)
            {
                if (mousePosition.X >  monitor.WorkingArea.X &&
                    mousePosition.X < (monitor.WorkingArea.X + monitor.WorkingArea.Width) &&
                    mousePosition.Y >  monitor.WorkingArea.Y &&
                    mousePosition.Y < (monitor.WorkingArea.Y + monitor.WorkingArea.Height))
                {
                    rect = monitor.WorkingArea;
                }
            }
            return rect;
        }
#endregion
    }
}
