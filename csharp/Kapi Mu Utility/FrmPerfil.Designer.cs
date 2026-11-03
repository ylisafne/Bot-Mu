namespace Kapi_Mu_Utility
{
    partial class FrmPerfil
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
            cboPerfil = new ComboBox();
            btnAdd = new Button();
            btnLoad = new Button();
            button1 = new Button();
            SuspendLayout();
            // 
            // cboPerfil
            // 
            cboPerfil.FormattingEnabled = true;
            cboPerfil.Location = new Point(12, 12);
            cboPerfil.Name = "cboPerfil";
            cboPerfil.Size = new Size(260, 25);
            cboPerfil.TabIndex = 0;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(278, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(25, 25);
            btnAdd.TabIndex = 1;
            btnAdd.Text = "+";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(12, 43);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(162, 46);
            btnLoad.TabIndex = 2;
            btnLoad.Text = "Cargar";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // button1
            // 
            button1.Location = new Point(180, 43);
            button1.Name = "button1";
            button1.Size = new Size(123, 46);
            button1.TabIndex = 3;
            button1.Text = "Active";
            button1.UseVisualStyleBackColor = true;
            // 
            // FrmPerfil
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(310, 101);
            Controls.Add(button1);
            Controls.Add(btnLoad);
            Controls.Add(btnAdd);
            Controls.Add(cboPerfil);
            Name = "FrmPerfil";
            Text = "FrmPerfil";
            ResumeLayout(false);
        }

        #endregion

        private ComboBox cboPerfil;
        private Button btnAdd;
        private Button btnLoad;
        private Button button1;
    }
}