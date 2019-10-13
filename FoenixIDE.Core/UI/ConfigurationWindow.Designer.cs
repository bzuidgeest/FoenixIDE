namespace FoenixIDE.Simulator.UI
{
    partial class ConfigurationWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageDisplay = new System.Windows.Forms.TabPage();
            this.tabPageAudio = new System.Windows.Forms.TabPage();
            this.tabPageStorage = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxOPLParallelPort = new System.Windows.Forms.ComboBox();
            this.comboBoxOPLSystem = new System.Windows.Forms.ComboBox();
            this.buttonApply = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.tabPageCommon = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxStartupHexFile = new System.Windows.Forms.TextBox();
            this.tabControl1.SuspendLayout();
            this.tabPageAudio.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPageCommon.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageCommon);
            this.tabControl1.Controls.Add(this.tabPageDisplay);
            this.tabControl1.Controls.Add(this.tabPageAudio);
            this.tabControl1.Controls.Add(this.tabPageStorage);
            this.tabControl1.Location = new System.Drawing.Point(3, 1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(794, 408);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageDisplay
            // 
            this.tabPageDisplay.Location = new System.Drawing.Point(4, 22);
            this.tabPageDisplay.Name = "tabPageDisplay";
            this.tabPageDisplay.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDisplay.Size = new System.Drawing.Size(786, 382);
            this.tabPageDisplay.TabIndex = 0;
            this.tabPageDisplay.Text = "Display";
            this.tabPageDisplay.UseVisualStyleBackColor = true;
            // 
            // tabPageAudio
            // 
            this.tabPageAudio.Controls.Add(this.groupBox1);
            this.tabPageAudio.Location = new System.Drawing.Point(4, 22);
            this.tabPageAudio.Name = "tabPageAudio";
            this.tabPageAudio.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAudio.Size = new System.Drawing.Size(786, 382);
            this.tabPageAudio.TabIndex = 1;
            this.tabPageAudio.Text = "Audio";
            this.tabPageAudio.UseVisualStyleBackColor = true;
            // 
            // tabPageStorage
            // 
            this.tabPageStorage.Location = new System.Drawing.Point(4, 22);
            this.tabPageStorage.Name = "tabPageStorage";
            this.tabPageStorage.Size = new System.Drawing.Size(786, 382);
            this.tabPageStorage.TabIndex = 2;
            this.tabPageStorage.Text = "Storage";
            this.tabPageStorage.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBoxOPLSystem);
            this.groupBox1.Controls.Add(this.comboBoxOPLParallelPort);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(7, 7);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(206, 83);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "OPL";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "System:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "ParallelPort";
            // 
            // comboBoxOPLParallelPort
            // 
            this.comboBoxOPLParallelPort.FormattingEnabled = true;
            this.comboBoxOPLParallelPort.Location = new System.Drawing.Point(72, 44);
            this.comboBoxOPLParallelPort.Name = "comboBoxOPLParallelPort";
            this.comboBoxOPLParallelPort.Size = new System.Drawing.Size(121, 21);
            this.comboBoxOPLParallelPort.TabIndex = 2;
            // 
            // comboBoxOPLSystem
            // 
            this.comboBoxOPLSystem.FormattingEnabled = true;
            this.comboBoxOPLSystem.Location = new System.Drawing.Point(72, 17);
            this.comboBoxOPLSystem.Name = "comboBoxOPLSystem";
            this.comboBoxOPLSystem.Size = new System.Drawing.Size(121, 21);
            this.comboBoxOPLSystem.TabIndex = 3;
            // 
            // buttonApply
            // 
            this.buttonApply.Location = new System.Drawing.Point(632, 415);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 23);
            this.buttonApply.TabIndex = 1;
            this.buttonApply.Text = "Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.ButtonApply_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(713, 415);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // tabPageCommon
            // 
            this.tabPageCommon.Controls.Add(this.textBoxStartupHexFile);
            this.tabPageCommon.Controls.Add(this.label3);
            this.tabPageCommon.Location = new System.Drawing.Point(4, 22);
            this.tabPageCommon.Name = "tabPageCommon";
            this.tabPageCommon.Size = new System.Drawing.Size(786, 382);
            this.tabPageCommon.TabIndex = 3;
            this.tabPageCommon.Text = "Common";
            this.tabPageCommon.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(78, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "StartUpHexFile";
            // 
            // textBoxStartupHexFile
            // 
            this.textBoxStartupHexFile.Location = new System.Drawing.Point(90, 15);
            this.textBoxStartupHexFile.Name = "textBoxStartupHexFile";
            this.textBoxStartupHexFile.Size = new System.Drawing.Size(256, 20);
            this.textBoxStartupHexFile.TabIndex = 1;
            // 
            // ConfigurationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.tabControl1);
            this.Name = "ConfigurationWindow";
            this.Text = "ConfigurationWindow";
            this.Load += new System.EventHandler(this.ConfigurationWindow_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPageAudio.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPageCommon.ResumeLayout(false);
            this.tabPageCommon.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageDisplay;
        private System.Windows.Forms.TabPage tabPageAudio;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox comboBoxOPLSystem;
        private System.Windows.Forms.ComboBox comboBoxOPLParallelPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPageStorage;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.TabPage tabPageCommon;
        private System.Windows.Forms.TextBox textBoxStartupHexFile;
        private System.Windows.Forms.Label label3;
    }
}