namespace Kapi_Mu_Utility
{
    partial class FrmClick
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
            btnToogle = new Button();
            SuspendLayout();
            // 
            // btnToogle
            // 
            btnToogle.Font = new Font("Segoe UI", 18.3396225F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToogle.ImageAlign = ContentAlignment.MiddleRight;
            btnToogle.Location = new Point(12, 12);
            btnToogle.Name = "btnToogle";
            btnToogle.Size = new Size(212, 119);
            btnToogle.TabIndex = 0;
            btnToogle.Text = "Iniciar[F8]";
            btnToogle.UseVisualStyleBackColor = true;
            btnToogle.Click += btnToogle_Click;
            // 
            // FrmClick
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(234, 142);
            Controls.Add(btnToogle);
            MaximumSize = new Size(250, 183);
            Name = "FrmClick";
            Text = "Click";
            ResumeLayout(false);
        }

        #endregion

        private Button btnToogle;
    }
}