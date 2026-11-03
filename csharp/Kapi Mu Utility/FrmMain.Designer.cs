namespace Kapi_Mu_Utility
{
    partial class FrmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            btnActive = new Button();
            btnHideShow = new Button();
            btnLoad = new Button();
            cboPerfil = new ComboBox();
            btnAdd = new Button();
            gvMain = new DataGridView();
            btnQuitar = new Button();
            ((System.ComponentModel.ISupportInitialize)gvMain).BeginInit();
            SuspendLayout();
            // 
            // btnActive
            // 
            btnActive.Location = new Point(174, 43);
            btnActive.Name = "btnActive";
            btnActive.Size = new Size(75, 40);
            btnActive.TabIndex = 7;
            btnActive.Text = "Organizar";
            btnActive.UseVisualStyleBackColor = true;
            btnActive.Click += btnActive_Click;
            // 
            // btnHideShow
            // 
            btnHideShow.Location = new Point(93, 43);
            btnHideShow.Name = "btnHideShow";
            btnHideShow.Size = new Size(75, 40);
            btnHideShow.TabIndex = 6;
            btnHideShow.Text = "Ocultar";
            btnHideShow.UseVisualStyleBackColor = true;
            btnHideShow.Click += btnHideShow_Click;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(12, 43);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(75, 40);
            btnLoad.TabIndex = 5;
            btnLoad.Text = "Cargar";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // cboPerfil
            // 
            cboPerfil.FormattingEnabled = true;
            cboPerfil.Location = new Point(12, 12);
            cboPerfil.Name = "cboPerfil";
            cboPerfil.Size = new Size(175, 25);
            cboPerfil.TabIndex = 4;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(193, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(25, 25);
            btnAdd.TabIndex = 8;
            btnAdd.Text = "+";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // gvMain
            // 
            gvMain.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvMain.Location = new Point(12, 89);
            gvMain.Name = "gvMain";
            gvMain.RowHeadersWidth = 45;
            gvMain.Size = new Size(238, 185);
            gvMain.TabIndex = 9;
            // 
            // btnQuitar
            // 
            btnQuitar.Location = new Point(224, 12);
            btnQuitar.Name = "btnQuitar";
            btnQuitar.Size = new Size(25, 25);
            btnQuitar.TabIndex = 10;
            btnQuitar.Text = "-";
            btnQuitar.UseVisualStyleBackColor = true;
            btnQuitar.Click += btnQuitar_Click;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(265, 290);
            Controls.Add(btnQuitar);
            Controls.Add(gvMain);
            Controls.Add(btnAdd);
            Controls.Add(btnActive);
            Controls.Add(btnHideShow);
            Controls.Add(btnLoad);
            Controls.Add(cboPerfil);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FrmMain";
            Text = "Loader";
            FormClosed += FrmMain_FormClosed;
            Load += FrmMain_Load;
            KeyDown += FrmMain_KeyDown;
            ((System.ComponentModel.ISupportInitialize)gvMain).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btnActive;
        private Button btnHideShow;
        private Button btnLoad;
        private ComboBox cboPerfil;
        private Button btnAdd;
        private DataGridView gvMain;
        private Button btnQuitar;
    }
}