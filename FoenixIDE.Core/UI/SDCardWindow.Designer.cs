namespace FoenixIDE.Simulator.UI.SDCardDebugger
{
    partial class SDCardWindow
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
            this.label1 = new System.Windows.Forms.Label();
            this.commandTextBox = new System.Windows.Forms.TextBox();
            this.settngsGroupBox = new System.Windows.Forms.GroupBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.rootTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.rootBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.stateGroupBox = new System.Windows.Forms.GroupBox();
            this.commandCounterTextBox = new System.Windows.Forms.TextBox();
            this.dataRichTextBox = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.isInsertedCheckBox = new System.Windows.Forms.CheckBox();
            this.settngsGroupBox.SuspendLayout();
            this.stateGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Command";
            // 
            // commandTextBox
            // 
            this.commandTextBox.Location = new System.Drawing.Point(150, 13);
            this.commandTextBox.Name = "commandTextBox";
            this.commandTextBox.ReadOnly = true;
            this.commandTextBox.Size = new System.Drawing.Size(193, 20);
            this.commandTextBox.TabIndex = 5;
            // 
            // settngsGroupBox
            // 
            this.settngsGroupBox.Controls.Add(this.isInsertedCheckBox);
            this.settngsGroupBox.Controls.Add(this.browseButton);
            this.settngsGroupBox.Controls.Add(this.rootTextBox);
            this.settngsGroupBox.Controls.Add(this.label4);
            this.settngsGroupBox.Location = new System.Drawing.Point(12, 12);
            this.settngsGroupBox.Name = "settngsGroupBox";
            this.settngsGroupBox.Size = new System.Drawing.Size(700, 64);
            this.settngsGroupBox.TabIndex = 6;
            this.settngsGroupBox.TabStop = false;
            this.settngsGroupBox.Text = "Settings";
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(386, 20);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "Browse";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // rootTextBox
            // 
            this.rootTextBox.Location = new System.Drawing.Point(69, 22);
            this.rootTextBox.Name = "rootTextBox";
            this.rootTextBox.Size = new System.Drawing.Size(311, 20);
            this.rootTextBox.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Root path:";
            // 
            // stateGroupBox
            // 
            this.stateGroupBox.Controls.Add(this.commandCounterTextBox);
            this.stateGroupBox.Controls.Add(this.label1);
            this.stateGroupBox.Controls.Add(this.commandTextBox);
            this.stateGroupBox.Location = new System.Drawing.Point(13, 83);
            this.stateGroupBox.Name = "stateGroupBox";
            this.stateGroupBox.Size = new System.Drawing.Size(699, 60);
            this.stateGroupBox.TabIndex = 7;
            this.stateGroupBox.TabStop = false;
            this.stateGroupBox.Text = "State";
            // 
            // commandCounterTextBox
            // 
            this.commandCounterTextBox.Location = new System.Drawing.Point(67, 13);
            this.commandCounterTextBox.Name = "commandCounterTextBox";
            this.commandCounterTextBox.ReadOnly = true;
            this.commandCounterTextBox.Size = new System.Drawing.Size(77, 20);
            this.commandCounterTextBox.TabIndex = 6;
            // 
            // dataRichTextBox
            // 
            this.dataRichTextBox.Location = new System.Drawing.Point(9, 32);
            this.dataRichTextBox.Name = "dataRichTextBox";
            this.dataRichTextBox.ReadOnly = true;
            this.dataRichTextBox.Size = new System.Drawing.Size(684, 224);
            this.dataRichTextBox.TabIndex = 8;
            this.dataRichTextBox.Text = "";
            this.dataRichTextBox.WordWrap = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.dataRichTextBox);
            this.groupBox1.Location = new System.Drawing.Point(13, 149);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(699, 262);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "SDCard data";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(20, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Dir";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(100, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Data";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Counter";
            // 
            // isInsertedCheckBox
            // 
            this.isInsertedCheckBox.AutoSize = true;
            this.isInsertedCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.isInsertedCheckBox.Checked = true;
            this.isInsertedCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.isInsertedCheckBox.Location = new System.Drawing.Point(467, 24);
            this.isInsertedCheckBox.Name = "isInsertedCheckBox";
            this.isInsertedCheckBox.Size = new System.Drawing.Size(120, 17);
            this.isInsertedCheckBox.TabIndex = 3;
            this.isInsertedCheckBox.Text = "Is SDCard inserted?";
            this.isInsertedCheckBox.UseVisualStyleBackColor = true;
            this.isInsertedCheckBox.CheckedChanged += new System.EventHandler(this.IsMountedCheckBox_CheckedChanged);
            // 
            // SDCardWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 423);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.stateGroupBox);
            this.Controls.Add(this.settngsGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "SDCardWindow";
            this.Text = "SDCard debugger";
            this.settngsGroupBox.ResumeLayout(false);
            this.settngsGroupBox.PerformLayout();
            this.stateGroupBox.ResumeLayout(false);
            this.stateGroupBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox commandTextBox;
        private System.Windows.Forms.GroupBox settngsGroupBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox rootTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.FolderBrowserDialog rootBrowserDialog;
        private System.Windows.Forms.GroupBox stateGroupBox;
        private System.Windows.Forms.RichTextBox dataRichTextBox;
        private System.Windows.Forms.TextBox commandCounterTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox isInsertedCheckBox;
    }
}