namespace Kapi_Mu_Utility
{
    partial class EvalForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EvalForm));
            lbl_machine_info = new Label();
            lbl_key = new Label();
            txt_info = new TextBox();
            txt_key = new TextBox();
            btn_aceptar = new Button();
            btn_cancel = new Button();
            lkl_web = new LinkLabel();
            SuspendLayout();
            // 
            // lbl_machine_info
            // 
            lbl_machine_info.AutoSize = true;
            lbl_machine_info.Location = new Point(12, 19);
            lbl_machine_info.Name = "lbl_machine_info";
            lbl_machine_info.Size = new Size(128, 17);
            lbl_machine_info.TabIndex = 0;
            lbl_machine_info.Text = "Machine Information";
            // 
            // lbl_key
            // 
            lbl_key.AutoSize = true;
            lbl_key.Location = new Point(111, 59);
            lbl_key.Name = "lbl_key";
            lbl_key.Size = new Size(29, 17);
            lbl_key.TabIndex = 1;
            lbl_key.Text = "Key";
            // 
            // txt_info
            // 
            txt_info.Location = new Point(146, 16);
            txt_info.Name = "txt_info";
            txt_info.Size = new Size(517, 25);
            txt_info.TabIndex = 2;
            // 
            // txt_key
            // 
            txt_key.Location = new Point(146, 56);
            txt_key.Name = "txt_key";
            txt_key.Size = new Size(517, 25);
            txt_key.TabIndex = 3;
            // 
            // btn_aceptar
            // 
            btn_aceptar.Location = new Point(248, 96);
            btn_aceptar.Name = "btn_aceptar";
            btn_aceptar.Size = new Size(83, 25);
            btn_aceptar.TabIndex = 4;
            btn_aceptar.Text = "Aceptar";
            btn_aceptar.UseVisualStyleBackColor = true;
            btn_aceptar.Click += btn_aceptar_Click;
            // 
            // btn_cancel
            // 
            btn_cancel.Location = new Point(337, 96);
            btn_cancel.Name = "btn_cancel";
            btn_cancel.Size = new Size(83, 25);
            btn_cancel.TabIndex = 5;
            btn_cancel.Text = "Cancelar";
            btn_cancel.UseVisualStyleBackColor = true;
            btn_cancel.Click += btn_cancel_Click;
            // 
            // lkl_web
            // 
            lkl_web.AutoSize = true;
            lkl_web.Location = new Point(533, 100);
            lkl_web.Name = "lkl_web";
            lkl_web.Size = new Size(130, 17);
            lkl_web.TabIndex = 6;
            lkl_web.TabStop = true;
            lkl_web.Text = "www.kapisystem.com";
            // 
            // EvalForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(679, 134);
            Controls.Add(lkl_web);
            Controls.Add(btn_cancel);
            Controls.Add(btn_aceptar);
            Controls.Add(txt_key);
            Controls.Add(txt_info);
            Controls.Add(lbl_key);
            Controls.Add(lbl_machine_info);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "EvalForm";
            Text = "Activar";
            Load += EvalForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbl_machine_info;
        private Label lbl_key;
        private TextBox txt_info;
        private TextBox txt_key;
        private Button btn_aceptar;
        private Button btn_cancel;
        private LinkLabel lkl_web;
    }
}