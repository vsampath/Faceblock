 //   FaceBlock - A Facebook Blocker for your PC
 //   Copyright (C) 2010  Varun Sampath <vsampath@seas.upenn.edu>

 //   This program is free software: you can redistribute it and/or modify
 //   it under the terms of the GNU General Public License as published by
 //   the Free Software Foundation, either version 3 of the License, or
 //   (at your option) any later version.

 //   This program is distributed in the hope that it will be useful,
 //   but WITHOUT ANY WARRANTY; without even the implied warranty of
 //   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 //   GNU General Public License for more details.

 //   You should have received a copy of the GNU General Public License
 //   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.IO;

namespace FaceBlock
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool blockStatus = false;
        private const String HEADER = "# FaceBlock Entries";
        private const String IP_ADDRESS = "165.123.34.40";  // Penn Blackboard
        private static String HOSTS_FILE = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
        private static String HOSTS_BAK = HOSTS_FILE + ".fbbak";

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Button Click Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_Click(object sender, RoutedEventArgs e)
        {
            if (!blockStatus)
            {
                block();
                on_GUI();
            }
            else
            {
                unblock();
                off_GUI();
            }
            instTB.Text = "Restart your browser to apply changes";
        }

        /// <summary>
        /// Shows that the block is off
        /// </summary>
        public void off_GUI()
        {
            button.Content = "X";
            statusTB.Text = "Off";
            button.Foreground = statusTB.Foreground = Brushes.Red;
            window.Title = "FaceBlock: Off";
        }

        /// <summary>
        /// Shows that the block is on
        /// </summary>
        public void on_GUI()
        {
            button.Content = "\u2713";
            statusTB.Text = "On";
            button.Foreground = statusTB.Foreground = Brushes.Green;
            window.Title = "FaceBlock: On";
        }

        /// <summary>
        /// Checks if the HOSTS File already contains the Facebook block
        /// </summary>
        /// <returns>true if the block is already present</returns>
        public bool isFaceBlocked()
        {
            StreamReader sr = null;
            try
            {
                sr = File.OpenText(HOSTS_FILE);
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Equals(HEADER))
                    {
                        sr.Close();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("HOSTS File cannot be read because " + e.Message,
                    "Can't read HOSTS File", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            finally
            {
                if (sr != null) sr.Close();
            }

            return false;
        }

        /// <summary>
        /// Backs up the HOSTS File and adds the Facebook block
        /// </summary>
        public void block()
        {
            StreamWriter sw = null;
            try
            {
                File.Copy(HOSTS_FILE, HOSTS_BAK, true);
                sw = File.AppendText(HOSTS_FILE);
                sw.WriteLine("\r\n"+HEADER);
                sw.WriteLine(IP_ADDRESS + "\t" + "www.facebook.com");
                sw.Write(IP_ADDRESS + "\t" + "facebook.com");
            }
            catch (Exception e)
            {
                MessageBox.Show("HOSTS File cannot be edited because " + e.Message,
                    "Can't edit HOSTS File", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(2);
            }
            finally
            {
                if (sw != null) sw.Close();
                blockStatus = true;
            }
        }

        /// <summary>
        /// Removes the Facebook block from the HOSTS File or dies trying
        /// </summary>
        public void unblock()
        {
            StreamReader sr = null;
            StreamWriter sw = null;
            try
            {
                // copy HOSTS File contents into List and delete current HOSTS File
                sr = File.OpenText(HOSTS_FILE);
                List<String> lines = new List<String>();
                String line;
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
                sr.Close();
                File.Delete(HOSTS_FILE);

                // create a new HOSTS File minus the block
                sw = File.CreateText(HOSTS_FILE);
                int counter = 0;
                foreach (String l in lines)
                {
                    // since block is in total 3 lines
                    if (!l.Equals(HEADER) && (counter == 0 || counter > 2))
                    {
                        // doing this avoids extra CR+LF at the end
                        if (!l.Equals(lines.ToArray()[0]))
                            sw.Write("\r\n");
                        sw.Write(l);
                    }
                    else
                        counter++;
                }
            }
            catch (Exception e)
            {
                if (File.Exists(HOSTS_BAK))
                {
                    // try to restore the HOSTS File via the backup
                    try
                    {
                        if (sr != null) sr.Close();
                        if (sw != null) sw.Close();
                        File.Copy(HOSTS_BAK, HOSTS_FILE);
                        MessageBox.Show("HOSTS File restored from backup since overwriting did" +
                            " not work (due to " + e.Message + ")", "Restored Backup HOSTS File",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("HOSTS File cannot be restored because " + e.Message +
                         "\r\nCouldn't restore from the backup at " + HOSTS_BAK + " because\r\n" + e1.Message,
                             "Can't restore HOSTS File", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(3);
                    }
                }
            }
            finally
            {
                if (sr != null) sr.Close();
                if (sw != null) sw.Close();
                blockStatus = false;
            }
        }

        // Code for Aero Glass use borrowed from Adam Nathan's blog post:
        // http://blogs.msdn.com/b/adam_nathan/archive/2006/05/04/589686.aspx
        // and code for DWM Composition Change handling from Joel's blog post in Chaos in a Can:
        // http://chaosinacan.com/programming/aero-glass-glasscalc-part1
        // many thanks, gentlemen

        /// <summary>
        /// Processes messages sent to this window
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Interop.WM_DWMCOMPOSITIONCHANGED)
            {
                if (GlassHelper.IsDwmCompositionEnabled)
                {
                    // Aero was re-enabled.  Extend the glass again.
                    GlassHelper.ExtendGlassFrame(this, new Thickness(-1));
                }
                else
                {
                    // Aero was disabled.  Reset the window's background color to remove black areas
                    HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
                    source.CompositionTarget.BackgroundColor = Colors.LightGray;
                    this.Background = Brushes.LightGray;
                }
            }
            return (IntPtr)0;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Hook up the WndProc function so it receives messages
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            GlassHelper.ExtendGlassFrame(this, new Thickness(-1));

            if (blockStatus = isFaceBlocked()) on_GUI();
            else off_GUI();
        }


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            if (GlassHelper.IsDwmCompositionEnabled) this.DragMove();
        }
    }

    public static class Interop
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;

            public Margins(Thickness t)
            {
                Left = (int)t.Left;
                Right = (int)t.Right;
                Top = (int)t.Top;
                Bottom = (int)t.Bottom;
            }
        }

        public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();
    }

    public static class GlassHelper
    {
        /// <summary>
        /// Gets whether dwmapi.dll is present and DWM functions can be used
        /// </summary>
        public static bool IsDwmCompositionAvailable
        {
            get
            {
                // Vista is version 6.  Don't do aero stuff if not >= Vista because dwmapi.dll won't exist
                return Environment.OSVersion.Version.Major >= 6;
            }
        }

        /// <summary>
        /// Gets whether DWM is enabled
        /// </summary>
        public static bool IsDwmCompositionEnabled
        {
            get
            {
                // Make sure dwmapi.dll is present.  If not, calling DwmIsCompositionEnabled will throw an exception
                if (!IsDwmCompositionAvailable)
                    return false;
                return Interop.DwmIsCompositionEnabled();
            }
        }

        /// <summary>
        /// Extends the glass frame of a window
        /// </summary>
        public static bool ExtendGlassFrame(Window window, Thickness margin)
        {
            if (!IsDwmCompositionEnabled)
                return false;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("The Window must be shown before extending glass.");

            HwndSource source = HwndSource.FromHwnd(hwnd);

            // Set the background to transparent from both the WPF and Win32 perspectives
            window.Background = Brushes.Transparent;
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

            Interop.Margins margins = new Interop.Margins(margin);
            Interop.DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return true;
        }
    }

}
