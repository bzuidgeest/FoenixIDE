namespace FoenixIDE.Simulator.UI
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
            this.SDCardLogTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // SDCardLogTextBox
            // 
            this.SDCardLogTextBox.Location = new System.Drawing.Point(13, 13);
            this.SDCardLogTextBox.Multiline = true;
            this.SDCardLogTextBox.Name = "SDCardLogTextBox";
            this.SDCardLogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.SDCardLogTextBox.Size = new System.Drawing.Size(699, 398);
            this.SDCardLogTextBox.TabIndex = 0;
            this.SDCardLogTextBox.WordWrap = false;
            // 
            // SDCardWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 423);
            this.Controls.Add(this.SDCardLogTextBox);
            this.Name = "SDCardWindow";
            this.Text = "SDCard debugger";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SDCardLogTextBox;
    }
}