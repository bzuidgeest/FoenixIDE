﻿using FoenixIDE.Basic;
using FoenixIDE.Display;
using FoenixIDE.MemoryLocations;
using FoenixIDE.Simulator.Devices;
using FoenixIDE.Simulator.FileFormat;
using FoenixIDE.Simulator.UI;
using FoenixIDE.Simulator.UI.SDCardDebugger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoenixIDE.UI
{
    public partial class MainWindow : Form
    {
        public FoenixSystem system;

        public UI.CPUWindow debugWindow;
        public SDCardWindow sDCardWindow;
        public OPLWindow oPLWindow;
        public MemoryWindow memoryWindow;
        public UploaderWindow uploaderWindow;
        private TileEditor tileEditor;
        public SerialTerminal terminal;

        private byte previousGraphicMode;
        private delegate void TileClickEvent(Point tile);
        public delegate void TileLoadedEvent(int layer);
        private TileClickEvent TileClicked;
        private ResourceChecker ResChecker = new ResourceChecker();
        private delegate void TransmitByteFunction(byte Value);
        private delegate void ShowFormFunction();

        //private Gpu gpu = new Gpu();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BasicWindow_Load(object sender, EventArgs e)
        {
            system = FoenixSystem.Current;
            AddGPUControl(system.GPU);

            terminal = new SerialTerminal();

            system.UART1.TransmitByte += SerialTransmitByte;
            system.UART2.TransmitByte += SerialTransmitByte;

            system.GPU.StartOfFrame += SOF;
            system.ResetCPU(true);
            ShowDebugWindow();
            //fix
            //ShowMemoryWindow();

            this.Top = 0;
            this.Left = 0;
            this.Width = debugWindow.Left;
            if (this.Width > 1200)
            {
                this.Width = 1200;
            }
            this.Height = Convert.ToInt32(this.Width * 0.75);
            
        }

        
        private void AddGPUControl(Gpu gpu)
        {
            if (Controls.ContainsKey("name"))
                Controls.Remove(this.Controls.Find("gpu", true)[0]);

            gpu.BackColor = System.Drawing.Color.Blue;
            gpu.Dock = System.Windows.Forms.DockStyle.Fill;
            gpu.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            gpu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            gpu.Location = new System.Drawing.Point(16, 40);
            gpu.Margin = new System.Windows.Forms.Padding(4);
            gpu.MinimumSize = new System.Drawing.Size(640, 480);
            gpu.Name = "gpu";
            gpu.Size = new System.Drawing.Size(654, 526);
            gpu.TabIndex = 0;
            gpu.TabStop = false;
            gpu.TileEditorMode = false;
            gpu.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Gpu_MouseClick);
            gpu.MouseEnter += new System.EventHandler(this.Gpu_MouseEnter);
            gpu.MouseLeave += new System.EventHandler(this.Gpu_MouseLeave);
            gpu.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Gpu_MouseMove);

            Controls.Add(gpu);
        }

        private void ShowSDCardWindow()
        {
            if (sDCardWindow == null || sDCardWindow.IsDisposed)
            {
                system.CPU.DebugPause = true;
                sDCardWindow = new SDCardWindow()
                {
                    Top = Screen.PrimaryScreen.WorkingArea.Top,
                };
                sDCardWindow.Left = Screen.PrimaryScreen.WorkingArea.Width - sDCardWindow.Width;
                sDCardWindow.Show();
            }
            else
            {
                sDCardWindow.BringToFront();
            }
        }

        private void ShowOPLWindow()
        {
            if (oPLWindow == null || oPLWindow.IsDisposed)
            {
                system.CPU.DebugPause = true;
                oPLWindow = new OPLWindow()
                {
                    Top = Screen.PrimaryScreen.WorkingArea.Top,
                };
                oPLWindow.Left = Screen.PrimaryScreen.WorkingArea.Width - oPLWindow.Width;
                oPLWindow.Show();
            }
            else
            {
                oPLWindow.BringToFront();
            }
        }

        private void ShowDebugWindow()
        {
            if (debugWindow == null || debugWindow.IsDisposed)
            {
                system.CPU.DebugPause = true;
                debugWindow = new UI.CPUWindow
                {
                    Top = Screen.PrimaryScreen.WorkingArea.Top,
                };
                debugWindow.Left = Screen.PrimaryScreen.WorkingArea.Width - debugWindow.Width;
                debugWindow.SetKernel(system);
                debugWindow.Show();
            } 
            else
            {
                debugWindow.BringToFront();
            }
        }

        private void ShowMemoryWindow()
        {
            if (memoryWindow == null || memoryWindow.IsDisposed)
            {
                memoryWindow = new MemoryWindow
                {
                    // fix Memory = system.RAM,
                    Left = debugWindow.Left,
                    Top = debugWindow.Top + debugWindow.Height
                };
                memoryWindow.Show();
            }
            else
            {
                memoryWindow.BringToFront();
            }
            memoryWindow.UpdateMCRButtons();
        }

        public void SerialTransmitByte(byte Value)
        {
            if (terminal.textBox1.InvokeRequired)
            {
                Invoke(new TransmitByteFunction(SerialTransmitByte), Value);
            }
            else
            {
                terminal.textBox1.Text += Convert.ToChar(Value);
            }
        }
        void ShowUploaderWindow()
        {
            if (uploaderWindow == null || uploaderWindow.IsDisposed)
            {
                uploaderWindow = new UploaderWindow();
                int left = this.Left + (this.Width - uploaderWindow.Width) / 2;
                int top =  this.Top + (this.Height - uploaderWindow.Height) / 2;
                uploaderWindow.Location = new Point(left, top);
                uploaderWindow.Memory = system.CPU.memoryManager;
                uploaderWindow.Show();
            }
            else
            {
                uploaderWindow.BringToFront();
            }
        }

        private void NewTileLoaded(int layer)
        {
            tileEditor?.SelectLayer(layer);
        }
        /*
         * Loading image into memory requires the user to specify what kind of image (tile, bitmap, sprite).
         * What address location in video RAM.
         */
        private void LoadImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BitmapLoader loader = new BitmapLoader
            {
                StartPosition = FormStartPosition.CenterParent,
                Memory = system.CPU.memoryManager,
                ResChecker = ResChecker
            };
            loader.OnTileLoaded += NewTileLoaded;
            loader.ShowDialog(this);
        }

        public void SOF()
        {
            byte mask = system.MemoryManager.ReadByte(MemoryLocations.MemoryMap.INT_MASK_REG0);
            if (!system.CPU.Flags.IrqDisable && ((~mask & 1) == 1))
            {
                // Set the Keyboard Interrupt
                byte IRQ0 = system.MemoryManager.ReadByte(MemoryLocations.MemoryMap.INT_PENDING_REG0);
                IRQ0 |= 1;
                system.MemoryManager.WriteByte(MemoryLocations.MemoryMap.INT_PENDING_REG0, IRQ0);
                system.CPU.Pins.IRQ = true;
            }
        }

        private void BasicWindow_KeyDown(object sender, KeyEventArgs e)
        {
            ScanCode scanCode = ScanCodes.GetScanCode(e.KeyCode);
            if (scanCode != ScanCode.sc_null)
            {
                lastKeyPressed.Text = "$" + ((byte)scanCode).ToString("X2");
                system.KEYBOARD.WriteKey(system, scanCode);
            }
            else
            {
                lastKeyPressed.Text = "";
            }
        }

        private void BasicWindow_KeyUp(object sender, KeyEventArgs e)
        {
            ScanCode scanCode = ScanCodes.GetScanCode(e.KeyCode);
            if (scanCode != ScanCode.sc_null)
            {
                scanCode += 0x80;
                lastKeyPressed.Text = "$" + ((byte)scanCode).ToString("X2");
                system.KEYBOARD.WriteKey(system, scanCode);
            }
            else
            {
                lastKeyPressed.Text = "";
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugWindow.Close();
            memoryWindow.Close();
            sDCardWindow.Close();
            this.Close();
        }

        int previousCounter = 0;
        int previousFrame = 0;
        DateTime previousTime = DateTime.Now;
        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan s = currentTime - previousTime;
            int currentCounter = system.CPU.CycleCounter;
            int currentFrame = system.GPU.paintCycle;
            double cps = (currentCounter - previousCounter) / s.TotalSeconds;
            double fps = (currentFrame - previousFrame) / s.TotalSeconds;

            previousCounter = currentCounter;
            previousTime = currentTime;
            previousFrame = currentFrame;
            cpsPerf.Text = "CPS: " + cps.ToString("N0");
            fpsPerf.Text = "FPS: " + fps.ToString("N0");
            // write the time to memory - values are BCD
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_SEC - system.VICKY.BaseAddress, BCD(currentTime.Second));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_MIN - system.VICKY.BaseAddress, BCD(currentTime.Minute));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_HRS - system.VICKY.BaseAddress, BCD(currentTime.Hour));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_DAY - system.VICKY.BaseAddress, BCD(currentTime.Day));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_MONTH - system.VICKY.BaseAddress, BCD(currentTime.Month));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_YEAR - system.VICKY.BaseAddress, BCD(currentTime.Year % 100));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_CENTURY - system.VICKY.BaseAddress, BCD(currentTime.Year / 100));
            //system.VICKY.WriteByte(MemoryLocations.MemoryMap.RTC_DOW - system.VICKY.BaseAddress, (byte)(currentTime.DayOfWeek+1));

            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_SEC, BCD(currentTime.Second));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_MIN, BCD(currentTime.Minute));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_HRS, BCD(currentTime.Hour));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_DAY, BCD(currentTime.Day));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_MONTH, BCD(currentTime.Month));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_YEAR, BCD(currentTime.Year % 100));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_CENTURY, BCD(currentTime.Year / 100));
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryLocations.MemoryMap.RTC_DOW, (byte)(currentTime.DayOfWeek + 1));
        }

        private byte BCD(int val)
        {
            return (byte)(val / 10 * 0x10 + val % 10);
        }

        private void CPUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowDebugWindow();
        }

        private void MemoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMemoryWindow();
        }

        private void UploaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowUploaderWindow();
        }

        /**
         * Restart the CPU
         */
        private void RestartMenuItemClick(object sender, EventArgs e)
        {
            debugWindow.PauseButton_Click(null, null);
            debugWindow.ClearTrace();
            previousCounter = 0;
            system.ResetCPU(true);
            memoryWindow.UpdateMCRButtons();
            system.CPU.Run();
            debugWindow.UpdateQueue();
            debugWindow.RunButton_Click(null, null);
        }
        
        /** 
         * Reset the system and go to step mode.
         */
        private void DebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            system.CPU.DebugPause = true;
            debugWindow.ClearTrace();
            previousCounter = 0;
            system.ResetCPU(true);
            memoryWindow.UpdateMCRButtons();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            ModeText.Text = "Shutting down CPU thread";
            system.CPU.DebugPause = true;
            if (system.CPU.CPUThread != null)
            {
                system.CPU.CPUThread.Abort();
                system.CPU.CPUThread.Join(1000);
            }
        }

        private void LoadHexFile(bool ResetMemory)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Hex Filed|*.hex",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                debugWindow.Close();
                if (memoryWindow != null)
                    memoryWindow.Close();
                if (ResetMemory)
                {
                    system = FoenixSystem.Current;
                    AddGPUControl(system.GPU);
                }
                system.SetKernel(dialog.FileName);
                system.ResetCPU(ResetMemory);
                ShowDebugWindow();
                //ShowMemoryWindow();
                if (tileEditor != null && tileEditor.Visible)
                {
                    tileEditor.SetMemory(system.MemoryManager);
                }
            }
        }
        private void MenuOpenHexFile_Click(object sender, EventArgs e)
        {
            LoadHexFile(true);
        }

        private void OpenHexFileWoZeroingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadHexFile(false);
        }

        /*
         * Read a Foenix XML file
         */
        private void LoadFNXMLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Load Project File",
                Filter = "Foenix Project File|*.fnxml",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                debugWindow.Close();
                memoryWindow.Close();
                system = FoenixSystem.Current;

                system.Resources = ResChecker;
                system.Breakpoints = CPUWindow.Instance.breakpoints;
                
                system.SetKernel(dialog.FileName);
                system.ResetCPU(true);
                ShowDebugWindow();
                ShowMemoryWindow();
            }
        }

        /*
         * Export all memory content to an XML file.
         */
        private void SaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Pick the file to create
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Save Project File",
                CheckPathExists = true,
                Filter = "Foenix Project File| *.fnxml"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FoeniXmlFile fnxml = new FoeniXmlFile(system.MemoryManager, ResChecker, CPUWindow.Instance.breakpoints);
                fnxml.Write(dialog.FileName, true);
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutFrom about = new AboutFrom();
            about.ShowDialog();
        }

        // When the editor window is closed, exit the TileEditorMode
        private void EditorWindowClosed(object sender, FormClosedEventArgs e)
        {
            system.GPU.TileEditorMode = false;
            // Restore the previous graphics mode
            //system.VICKY.WriteByte(0, previousGraphicMode);
            FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.VICKY_BASE_ADDR, previousGraphicMode);
            tileEditor.Dispose();
            tileEditor = null;
        }
        
        private void TileEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tileEditor == null)
            {
                tileEditor = new TileEditor();
                tileEditor.SetMemory(system.MemoryManager);
                system.GPU.TileEditorMode = true;
                //// Set Vicky into Tile mode
                //previousGraphicMode = system.VICKY.ReadByte(0);
                //system.VICKY.WriteByte(0, 0x10);
                //// Enable borders
                //system.VICKY.WriteByte(4, 1);
                // Set Vicky into Tile mode
                previousGraphicMode = FoenixSystem.Current.MemoryManager.ReadByte(MemoryMap.VICKY_BASE_ADDR);
                FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.VICKY_BASE_ADDR, 0x10);
                // Enable borders
                FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.VICKY_BASE_ADDR + 4, 1);
                tileEditor.Show();
                tileEditor.FormClosed += new FormClosedEventHandler(EditorWindowClosed);

                // coordinate between the tile editor window and the GPU canvas
                this.TileClicked += new TileClickEvent(tileEditor.TileClicked_Click);
            }
            else
            {
                tileEditor.BringToFront();
            }
    }

        private void Gpu_MouseMove(object sender, MouseEventArgs e)
        {
            double ratioW = system.GPU.Width / 640d;
            double ratioH = system.GPU.Height / 480d;
            if (system.GPU.TileEditorMode)
            {
                if ((e.X / ratioW > 32 && e.X / ratioW < 608) && (e.Y / ratioH > 32 && e.Y / ratioH < 448))
                {
                    this.Cursor = Cursors.Hand;
                }
                else
                {
                    this.Cursor = Cursors.No;
                }
            }
            else
            {
                // Read the mouse pointer register
                //byte mouseReg = system.VICKY.ReadByte(0x700);
                byte mouseReg = FoenixSystem.Current.MemoryManager.ReadByte(0xAF_0700);
                if ((mouseReg & 1) == 1)
                {
                    int X = (int)(e.X / ratioW);
                    int Y = (int)(e.Y / ratioH);
                    //system.VICKY.WriteWord(0x702, X);
                    //system.VICKY.WriteWord(0x704, Y);
                    FoenixSystem.Current.MemoryManager.WriteWord(0xAF_0702, X);
                    FoenixSystem.Current.MemoryManager.WriteWord(0xAF_0704, Y);

                }
                else
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void Gpu_MouseLeave(object sender, EventArgs e)
        {
            if (system.GPU.MousePointerMode || system.GPU.TileEditorMode)
            {
                Cursor.Show();
            }
            this.Cursor = Cursors.Default;
        }

        private void Gpu_MouseClick(object sender, MouseEventArgs e)
        {
            if (system.GPU.TileEditorMode && system.GPU.Cursor != Cursors.No)
            {
                double ratioW = system.GPU.Width / 640d;
                double ratioH = system.GPU.Height / 480d;
                TileClicked?.Invoke(new Point((int)(e.X / ratioW / 16), (int)(e.Y / ratioH / 16)));
            }
        }


        private void Gpu_MouseEnter(object sender, EventArgs e)
        {
            if (system.GPU.MousePointerMode && !system.GPU.TileEditorMode)
            {
                Cursor.Hide();
            }
        }

        private void TerminalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            terminal.Show();
        }

        private void SDCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSDCardWindow();
        }

        private void PreferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigurationWindow configurationWindow = new ConfigurationWindow();
            configurationWindow.ShowDialog();
        }

        private void OPLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowOPLWindow();
        }
    }
}

