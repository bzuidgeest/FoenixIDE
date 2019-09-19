namespace FoenixIDE.Simulator.UI
{
    partial class OPLWindow
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
            this.PictureBoxOPLStatus = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxOPLStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // PictureBoxOPLStatus
            // 
            this.PictureBoxOPLStatus.Location = new System.Drawing.Point(12, 47);
            this.PictureBoxOPLStatus.Name = "PictureBoxOPLStatus";
            this.PictureBoxOPLStatus.Size = new System.Drawing.Size(791, 406);
            this.PictureBoxOPLStatus.TabIndex = 0;
            this.PictureBoxOPLStatus.TabStop = false;
            this.PictureBoxOPLStatus.Paint += new System.Windows.Forms.PaintEventHandler(this.PictureBoxOPLStatus_Paint);
            // 
            // OPLWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(815, 465);
            this.Controls.Add(this.PictureBoxOPLStatus);
            this.Name = "OPLWindow";
            this.Text = "OPLStatusWindow";
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxOPLStatus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox PictureBoxOPLStatus;
    }
}