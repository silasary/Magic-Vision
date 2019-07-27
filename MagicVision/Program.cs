using MagicVision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PoolVision {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            using (var mainForm = new MainForm())
            {
                Application.Run(mainForm);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
        }
    }
}
