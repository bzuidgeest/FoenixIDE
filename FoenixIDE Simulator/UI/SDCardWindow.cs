using FoenixIDE.Simulator.Devices.SDCard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoenixIDE.Simulator.UI
{
    public partial class SDCardWindow : Form
    {
        FoenixSystem system;

        public SDCardWindow()
        {
            InitializeComponent();
        }

        public void SetSystem(FoenixSystem system)
        {
            this.system = system;

            this.system.Memory.SDCARD.OnRead += SDCARD_OnRead;
            this.system.Memory.SDCARD.OnWrite += SDCARD_OnWrite;
            //registerDisplay1.CPU = kernel.CPU;
            //UpdateQueue();
        }

        private void SDCARD_OnWrite(object sender, Devices.SDCard.SDCardWriteEvent e)
        {
            //Debug.WriteLine($"Write address: {e.Address}, value: {e.Value}");

            this.Invoke(new MethodInvoker(delegate () {
                switch (e.Address)
                {
                    case SDCardRegister.SDCARD_CMD:
                        SDCardLogTextBox.AppendText($"Write command: {Enum.GetName(typeof(SDCommand), e.Value)}\r\n");
                        break;
                    case SDCardRegister.SDCARD_DATA:
                        SDCardLogTextBox.AppendText($"Write data: {e.Value}, {(e.Value >= 32 ? (char)e.Value : ' ')}\r\n");
                        break;
                    default:
                        SDCardLogTextBox.AppendText($"Write to unknown address {e.Address}\r\n");
                        break;
                }
            }));
        }

        private void SDCARD_OnRead(object sender, Devices.SDCard.SDCardReadEvent e)
        {
            //Debug.WriteLine($"Read address: {e.Address}, value: {e.Value}");
            this.Invoke(new MethodInvoker(delegate ()
            {
                switch (e.Address)
                {
                    case SDCardRegister.SDCARD_CMD:
                        SDCardLogTextBox.AppendText($"Read from command: {e.Value}\r\n");
                        break;
                    case SDCardRegister.SDCARD_DATA:
                        SDCardLogTextBox.AppendText($"Read from data: {e.Value}, {(e.Value >= 32 ? (char)e.Value : ' ')}\r\n");
                        break;
                    default:
                        SDCardLogTextBox.AppendText($"Read from unknown address {e.Address}\r\n");
                        break;
                }
            }));
        }
    }
}
