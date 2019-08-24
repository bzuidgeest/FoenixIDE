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

namespace FoenixIDE.Simulator.UI.SDCardDebugger
{
    public partial class SDCardWindow : Form
    {
        private long commandCounter = 0;
        private int readByteCouter = 0;

        public SDCardWindow()
        {
            InitializeComponent();
            rootTextBox.Text = FoenixSystem.Current.Memory.SDCARD.Root;

            dataRichTextBox.SelectionTabs = new int[] { 30, 100, 300, 400 };
            //dataRichTextBox.AppendText("Dir\tC nr.\tData");

            FoenixSystem.Current.Memory.SDCARD.OnRead += SDCARD_OnRead;
            FoenixSystem.Current.Memory.SDCARD.OnWrite += SDCARD_OnWrite;
        }

        private void SDCARD_OnWrite(object sender, Devices.SDCard.SDCardWriteEvent e)
        {
            //Debug.WriteLine($"Write address: {e.Address}, value: {e.Value}");

            this.Invoke(new MethodInvoker(delegate () 
            {
                switch (e.Address)
                {
                    case SDCardRegister.SDCARD_CMD:
                        commandTextBox.Text = Enum.GetName(typeof(SDCommand), e.Value);
                        commandCounter++;
                        commandCounterTextBox.Text = commandCounter.ToString();
                        if (readByteCouter > 0)
                            dataRichTextBox.AppendText("\r\n");
                        readByteCouter = 0;
                        dataRichTextBox.SelectionBackColor = Color.Green;
                        dataRichTextBox.AppendText($"<\t{commandCounter}\tcommand: {(Enum.GetName(typeof(SDCommand), e.Value)):X2}");
                        break;
                    case SDCardRegister.SDCARD_DATA:
                        dataRichTextBox.SelectionBackColor = Color.Gray;
                        dataRichTextBox.AppendText($"<\t{commandCounter}\tdata: {e.Value:X2}, {(e.Value >= 32 ? (char)e.Value : ' ')}");
                        break;
                    default:
                        dataRichTextBox.SelectionBackColor = Color.Red;
                        dataRichTextBox.AppendText($"<\tunknown address {e.Address}");
                        break;
                }
                dataRichTextBox.AppendText("\r\n");
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
                        dataRichTextBox.SelectionBackColor = Color.Yellow;
                        dataRichTextBox.AppendText($"\r\n> command: {e.Value:X2}");
                        break;
                    case SDCardRegister.SDCARD_DATA:
                        dataRichTextBox.SelectionBackColor = Color.CadetBlue;
                        if (readByteCouter == 0 || readByteCouter == 36)
                        {
                            readByteCouter = 0;
                            dataRichTextBox.AppendText($"\r\n>\t{commandCounter}\t");
                        }
                        dataRichTextBox.AppendText($"{e.Value:X2} ");
                        readByteCouter++;
                        //, {(e.Value >= 32 ? (char)e.Value : ' ')}
                        break;
                    default:
                        dataRichTextBox.SelectionBackColor = Color.Red;
                        dataRichTextBox.AppendText($"\r\n> unknown address {e.Address}\r\n");
                        break;
                }
            }));
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            if (rootBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                FoenixSystem.Current.Memory.SDCARD.Root = rootBrowserDialog.SelectedPath;
            }

            rootTextBox.Text = FoenixSystem.Current.Memory.SDCARD.Root;
        }

        private void IsMountedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            FoenixSystem.Current.Memory.SDCARD.SDCardInserted = isInsertedCheckBox.Checked;
        }
    }
}
