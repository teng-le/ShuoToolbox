using System;
using System.IO;
using System.Windows.Forms;

namespace Toolbox
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.ThreadException += (s, e) => ShowException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ShowException(ex);
                }
            };

            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private static void ShowException(Exception ex)
        {
            try
            {
                string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.WriteAllText(log, DateTime.Now + "\r\n" + ex.ToString());
            }
            catch { }

            MessageBox.Show(ex.ToString(), "程序崩溃", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}