namespace Kapi_Mu_Utility
{
    partial class FrmWeb
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
            webMain = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)webMain).BeginInit();
            SuspendLayout();
            // 
            // webMain
            // 
            webMain.AllowExternalDrop = true;
            webMain.CreationProperties = null;
            webMain.DefaultBackgroundColor = Color.White;
            webMain.Dock = DockStyle.Fill;
            webMain.Location = new Point(0, 0);
            webMain.Name = "webMain";
            webMain.Size = new Size(1264, 679);
            webMain.TabIndex = 0;
            webMain.ZoomFactor = 1D;
            // 
            // FrmWeb
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 679);
            Controls.Add(webMain);
            Name = "FrmWeb";
            Text = "FrmWeb";
            Load += FrmWeb_Load;
            KeyDown += FrmWeb_KeyDown;
            ((System.ComponentModel.ISupportInitialize)webMain).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webMain;
    }
}