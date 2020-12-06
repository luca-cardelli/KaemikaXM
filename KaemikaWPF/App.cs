using System;
using System.Windows.Forms;

namespace KaemikaWPF
{
    public static class App
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WinGui());
        }

    }
}
