using FoenixIDE.Simulator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoenixIDE.Simulator.UI
{
    public partial class ConfigurationWindow : Form
    {
        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        private void ConfigurationWindow_Load(object sender, EventArgs e)
        {
            // Common
            textBoxStartupHexFile.Text = Configuration.Current.StartUpHexFile;


            // Audio
            comboBoxOPLSystem.DataSource = Enum.GetNames(typeof(OPLSystem));
            comboBoxOPLSystem.SelectedItem = Configuration.Current.OPLSystem.ToString();

            comboBoxOPLParallelPort.SelectedText = Configuration.Current.OPLParallelPort.ToString();

            
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ButtonApply_Click(object sender, EventArgs e)
        {
            // Common
            Configuration.Current.StartUpHexFile = textBoxStartupHexFile.Text;

            // Audio
            Configuration.Current.OPLSystem = (OPLSystem)Enum.Parse(typeof(OPLSystem), comboBoxOPLSystem.SelectedItem.ToString());

            try
            {
                if (comboBoxOPLParallelPort.SelectedText != String.Empty)
                {
                    Configuration.Current.OPLParallelPort = Convert.ToInt32(comboBoxOPLParallelPort.SelectedText, 16);
                }
            }
            catch
            {
                // handle error here
                return;
            }

            Configuration.Current.Save();
            FoenixSystem.Current.ResetCPU(false);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
