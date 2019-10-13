using System;
using System.Collections.Generic;
using System.Linq;
using FoenixIDE.Simulator;
using FoenixIDE.UI;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoenixIDE
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Configuration.Current.Save();
            }
            catch
            {
                throw new Exception($"Cannot save file: {Configuration.configFilename}");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
