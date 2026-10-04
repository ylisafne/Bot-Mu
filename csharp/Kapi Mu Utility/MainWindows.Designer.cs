namespace Kapi_Mu_Utility
{
    partial class MainWindows
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
            chk_agi = new CheckBox();
            chkCmd = new CheckBox();
            chkStr = new CheckBox();
            chkEne = new CheckBox();
            chkSta = new CheckBox();
            groupBox1 = new GroupBox();
            nudStr = new NumericUpDown();
            nudEne = new NumericUpDown();
            nudSta = new NumericUpDown();
            nudCmd = new NumericUpDown();
            nudAgi = new NumericUpDown();
            label1 = new Label();
            chkVolver = new CheckBox();
            groupBox2 = new GroupBox();
            txtOtros = new TextBox();
            label4 = new Label();
            label5 = new Label();
            nudY = new NumericUpDown();
            nudX = new NumericUpDown();
            nudFrom = new NumericUpDown();
            nudTO = new NumericUpDown();
            groupBox3 = new GroupBox();
            txtMapa = new TextBox();
            label3 = new Label();
            label2 = new Label();
            btnWeb = new Button();
            btnCliente = new Button();
            btnToogle = new Button();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudStr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudEne).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSta).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCmd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAgi).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTO).BeginInit();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // chk_agi
            // 
            chk_agi.AutoSize = true;
            chk_agi.Checked = true;
            chk_agi.CheckState = CheckState.Checked;
            chk_agi.Location = new Point(6, 24);
            chk_agi.Name = "chk_agi";
            chk_agi.Size = new Size(75, 21);
            chk_agi.TabIndex = 0;
            chk_agi.Text = "Agilidad";
            chk_agi.UseVisualStyleBackColor = true;
            // 
            // chkCmd
            // 
            chkCmd.AutoSize = true;
            chkCmd.Checked = true;
            chkCmd.CheckState = CheckState.Checked;
            chkCmd.Location = new Point(6, 55);
            chkCmd.Name = "chkCmd";
            chkCmd.Size = new Size(87, 21);
            chkCmd.TabIndex = 1;
            chkCmd.Text = "Command";
            chkCmd.UseVisualStyleBackColor = true;
            // 
            // chkStr
            // 
            chkStr.AutoSize = true;
            chkStr.Checked = true;
            chkStr.CheckState = CheckState.Checked;
            chkStr.Location = new Point(6, 86);
            chkStr.Name = "chkStr";
            chkStr.Size = new Size(65, 21);
            chkStr.TabIndex = 2;
            chkStr.Text = "Fuerza";
            chkStr.UseVisualStyleBackColor = true;
            // 
            // chkEne
            // 
            chkEne.AutoSize = true;
            chkEne.Checked = true;
            chkEne.CheckState = CheckState.Checked;
            chkEne.Location = new Point(6, 117);
            chkEne.Name = "chkEne";
            chkEne.Size = new Size(71, 21);
            chkEne.TabIndex = 3;
            chkEne.Text = "Energia";
            chkEne.UseVisualStyleBackColor = true;
            // 
            // chkSta
            // 
            chkSta.AutoSize = true;
            chkSta.Checked = true;
            chkSta.CheckState = CheckState.Checked;
            chkSta.Location = new Point(6, 148);
            chkSta.Name = "chkSta";
            chkSta.Size = new Size(78, 21);
            chkSta.TabIndex = 4;
            chkSta.Text = "Vitalidad";
            chkSta.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(nudStr);
            groupBox1.Controls.Add(nudEne);
            groupBox1.Controls.Add(nudSta);
            groupBox1.Controls.Add(nudCmd);
            groupBox1.Controls.Add(nudAgi);
            groupBox1.Controls.Add(chk_agi);
            groupBox1.Controls.Add(chkCmd);
            groupBox1.Controls.Add(chkStr);
            groupBox1.Controls.Add(chkEne);
            groupBox1.Controls.Add(chkSta);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(190, 181);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            groupBox1.Text = "Command Point";
            // 
            // nudStr
            // 
            nudStr.Location = new Point(106, 82);
            nudStr.Maximum = new decimal(new int[] { 65000, 0, 0, 0 });
            nudStr.Name = "nudStr";
            nudStr.Size = new Size(67, 25);
            nudStr.TabIndex = 32;
            nudStr.Value = new decimal(new int[] { 65000, 0, 0, 0 });
            // 
            // nudEne
            // 
            nudEne.Location = new Point(106, 113);
            nudEne.Maximum = new decimal(new int[] { 65000, 0, 0, 0 });
            nudEne.Name = "nudEne";
            nudEne.Size = new Size(67, 25);
            nudEne.TabIndex = 31;
            nudEne.Value = new decimal(new int[] { 65000, 0, 0, 0 });
            // 
            // nudSta
            // 
            nudSta.Location = new Point(106, 144);
            nudSta.Maximum = new decimal(new int[] { 65000, 0, 0, 0 });
            nudSta.Name = "nudSta";
            nudSta.Size = new Size(67, 25);
            nudSta.TabIndex = 30;
            nudSta.Value = new decimal(new int[] { 65000, 0, 0, 0 });
            // 
            // nudCmd
            // 
            nudCmd.Location = new Point(106, 51);
            nudCmd.Maximum = new decimal(new int[] { 65000, 0, 0, 0 });
            nudCmd.Name = "nudCmd";
            nudCmd.Size = new Size(67, 25);
            nudCmd.TabIndex = 29;
            nudCmd.Value = new decimal(new int[] { 65000, 0, 0, 0 });
            // 
            // nudAgi
            // 
            nudAgi.Location = new Point(106, 20);
            nudAgi.Maximum = new decimal(new int[] { 65000, 0, 0, 0 });
            nudAgi.Name = "nudAgi";
            nudAgi.Size = new Size(67, 25);
            nudAgi.TabIndex = 28;
            nudAgi.Value = new decimal(new int[] { 65000, 0, 0, 0 });
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(91, 53);
            label1.Name = "label1";
            label1.Size = new Size(22, 17);
            label1.TabIndex = 14;
            label1.Text = ", Y";
            // 
            // chkVolver
            // 
            chkVolver.AutoSize = true;
            chkVolver.Location = new Point(6, 24);
            chkVolver.Name = "chkVolver";
            chkVolver.Size = new Size(131, 21);
            chkVolver.TabIndex = 19;
            chkVolver.Text = "Regresar al Punto";
            chkVolver.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtOtros);
            groupBox2.Location = new Point(208, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(154, 181);
            groupBox2.TabIndex = 22;
            groupBox2.TabStop = false;
            groupBox2.Text = "Otros";
            // 
            // txtOtros
            // 
            txtOtros.Location = new Point(6, 22);
            txtOtros.Multiline = true;
            txtOtros.Name = "txtOtros";
            txtOtros.Size = new Size(135, 149);
            txtOtros.TabIndex = 22;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(208, 214);
            label4.Name = "label4";
            label4.Size = new Size(126, 17);
            label4.TabIndex = 24;
            label4.Text = "Rango de ejecucion:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(280, 241);
            label5.Name = "label5";
            label5.Size = new Size(15, 17);
            label5.TabIndex = 27;
            label5.Text = "a";
            // 
            // nudY
            // 
            nudY.Location = new Point(119, 51);
            nudY.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudY.Name = "nudY";
            nudY.Size = new Size(57, 25);
            nudY.TabIndex = 33;
            // 
            // nudX
            // 
            nudX.Location = new Point(28, 51);
            nudX.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudX.Name = "nudX";
            nudX.Size = new Size(57, 25);
            nudX.TabIndex = 34;
            // 
            // nudFrom
            // 
            nudFrom.Location = new Point(214, 234);
            nudFrom.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudFrom.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudFrom.Name = "nudFrom";
            nudFrom.Size = new Size(57, 25);
            nudFrom.TabIndex = 36;
            nudFrom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudTO
            // 
            nudTO.Location = new Point(305, 234);
            nudTO.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudTO.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudTO.Name = "nudTO";
            nudTO.Size = new Size(57, 25);
            nudTO.TabIndex = 35;
            nudTO.Value = new decimal(new int[] { 6, 0, 0, 0 });
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(txtMapa);
            groupBox3.Controls.Add(label3);
            groupBox3.Controls.Add(chkVolver);
            groupBox3.Controls.Add(label2);
            groupBox3.Controls.Add(nudX);
            groupBox3.Controls.Add(nudY);
            groupBox3.Controls.Add(label1);
            groupBox3.Location = new Point(12, 199);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(190, 144);
            groupBox3.TabIndex = 37;
            groupBox3.TabStop = false;
            groupBox3.Text = "Volver Al Mapa";
            // 
            // txtMapa
            // 
            txtMapa.Location = new Point(6, 99);
            txtMapa.Name = "txtMapa";
            txtMapa.Size = new Size(170, 25);
            txtMapa.TabIndex = 36;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 79);
            label3.Name = "label3";
            label3.Size = new Size(101, 17);
            label3.TabIndex = 35;
            label3.Text = "Mover al Mapa:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 53);
            label2.Name = "label2";
            label2.Size = new Size(19, 17);
            label2.TabIndex = 16;
            label2.Text = "X:";
            // 
            // btnWeb
            // 
            btnWeb.Location = new Point(212, 265);
            btnWeb.Name = "btnWeb";
            btnWeb.Size = new Size(67, 25);
            btnWeb.TabIndex = 38;
            btnWeb.Text = "Web";
            btnWeb.UseVisualStyleBackColor = true;
            btnWeb.Click += btnWeb_Click;
            // 
            // btnCliente
            // 
            btnCliente.Location = new Point(295, 265);
            btnCliente.Name = "btnCliente";
            btnCliente.Size = new Size(67, 25);
            btnCliente.TabIndex = 39;
            btnCliente.Text = "Cliente";
            btnCliente.UseVisualStyleBackColor = true;
            btnCliente.Click += btnCliente_Click;
            // 
            // btnToogle
            // 
            btnToogle.Font = new Font("Segoe UI", 12.2264156F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToogle.Location = new Point(208, 296);
            btnToogle.Name = "btnToogle";
            btnToogle.Size = new Size(155, 47);
            btnToogle.TabIndex = 40;
            btnToogle.Text = "Iniciar[F8]";
            btnToogle.UseVisualStyleBackColor = true;
            btnToogle.Click += btnToogle_Click;
            // 
            // MainWindows
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(375, 351);
            Controls.Add(btnToogle);
            Controls.Add(btnCliente);
            Controls.Add(btnWeb);
            Controls.Add(groupBox3);
            Controls.Add(nudFrom);
            Controls.Add(nudTO);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "MainWindows";
            Text = "MainWindows";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudStr).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudEne).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSta).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudCmd).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAgi).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudY).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudX).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTO).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private CheckBox chk_agi;
        private CheckBox chkCmd;
        private CheckBox chkStr;
        private CheckBox chkEne;
        private CheckBox chkSta;
        private GroupBox groupBox1;
        private Label label1;
        private CheckBox chkVolver;
        private GroupBox groupBox2;
        private TextBox txtOtros;
        private Label label4;
        private Label label5;
        private NumericUpDown nudAgi;
        private NumericUpDown nudStr;
        private NumericUpDown nudEne;
        private NumericUpDown nudSta;
        private NumericUpDown nudCmd;
        private NumericUpDown nudY;
        private NumericUpDown nudX;
        private NumericUpDown nudFrom;
        private NumericUpDown nudTO;
        private GroupBox groupBox3;
        private Label label2;
        private TextBox txtMapa;
        private Label label3;
        private Button btnWeb;
        private Button btnCliente;
        private Button btnToogle;
    }
}