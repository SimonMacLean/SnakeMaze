namespace SnakeMaze
{
    partial class DrawingForm
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
            this.SuspendLayout();
            // 
            // DrawingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkSlateBlue;
            this.ClientSize = new System.Drawing.Size(1784, 961);
            this.Name = "DrawingForm";
            this.Text = "MazeSnake";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DrawingForm_FormClosed);
            this.Click += new System.EventHandler(this.DrawingForm_Click);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DrawingForm_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DrawingForm_KeyDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.DrawingForm_MouseMove);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

