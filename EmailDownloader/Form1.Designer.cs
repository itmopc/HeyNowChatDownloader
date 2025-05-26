namespace EmailDownloader
{
	partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnClip = new System.Windows.Forms.Button();
            this.groupBoxImpManual = new System.Windows.Forms.GroupBox();
            this.ckVerSinDatos = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtEnd = new System.Windows.Forms.DateTimePicker();
            this.dtBegin = new System.Windows.Forms.DateTimePicker();
            this.btnEjecutarImpManual = new System.Windows.Forms.Button();
            this.timerClose = new System.Windows.Forms.Timer(this.components);
            this.groupBoxImpManual.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.IntegralHeight = false;
            this.listBox1.Location = new System.Drawing.Point(12, 78);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(923, 314);
            this.listBox1.TabIndex = 0;
            // 
            // btnClip
            // 
            this.btnClip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClip.Location = new System.Drawing.Point(707, 398);
            this.btnClip.Name = "btnClip";
            this.btnClip.Size = new System.Drawing.Size(228, 23);
            this.btnClip.TabIndex = 2;
            this.btnClip.Text = "Copiar LOG al portapapeles";
            this.btnClip.UseVisualStyleBackColor = true;
            this.btnClip.Click += new System.EventHandler(this.btnClip_Click);
            // 
            // groupBoxImpManual
            // 
            this.groupBoxImpManual.Controls.Add(this.ckVerSinDatos);
            this.groupBoxImpManual.Controls.Add(this.label2);
            this.groupBoxImpManual.Controls.Add(this.label1);
            this.groupBoxImpManual.Controls.Add(this.dtEnd);
            this.groupBoxImpManual.Controls.Add(this.dtBegin);
            this.groupBoxImpManual.Controls.Add(this.btnEjecutarImpManual);
            this.groupBoxImpManual.Enabled = false;
            this.groupBoxImpManual.Location = new System.Drawing.Point(12, 12);
            this.groupBoxImpManual.Name = "groupBoxImpManual";
            this.groupBoxImpManual.Size = new System.Drawing.Size(923, 60);
            this.groupBoxImpManual.TabIndex = 3;
            this.groupBoxImpManual.TabStop = false;
            this.groupBoxImpManual.Text = "Importación manual";
            // 
            // ckVerSinDatos
            // 
            this.ckVerSinDatos.AutoSize = true;
            this.ckVerSinDatos.Location = new System.Drawing.Point(584, 25);
            this.ckVerSinDatos.Name = "ckVerSinDatos";
            this.ckVerSinDatos.Size = new System.Drawing.Size(211, 17);
            this.ckVerSinDatos.TabIndex = 3;
            this.ckVerSinDatos.Text = "Ver sesiones sin moroso/cliente.";
            this.ckVerSinDatos.UseVisualStyleBackColor = true;
            this.ckVerSinDatos.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(316, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Hasta";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(33, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Desde";
            // 
            // dtEnd
            // 
            this.dtEnd.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtEnd.Location = new System.Drawing.Point(365, 23);
            this.dtEnd.Name = "dtEnd";
            this.dtEnd.Size = new System.Drawing.Size(200, 21);
            this.dtEnd.TabIndex = 1;
            // 
            // dtBegin
            // 
            this.dtBegin.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtBegin.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtBegin.Location = new System.Drawing.Point(82, 23);
            this.dtBegin.Name = "dtBegin";
            this.dtBegin.Size = new System.Drawing.Size(200, 21);
            this.dtBegin.TabIndex = 1;
            // 
            // btnEjecutarImpManual
            // 
            this.btnEjecutarImpManual.Location = new System.Drawing.Point(824, 21);
            this.btnEjecutarImpManual.Name = "btnEjecutarImpManual";
            this.btnEjecutarImpManual.Size = new System.Drawing.Size(75, 23);
            this.btnEjecutarImpManual.TabIndex = 0;
            this.btnEjecutarImpManual.Text = "Ejecutar";
            this.btnEjecutarImpManual.UseVisualStyleBackColor = true;
            this.btnEjecutarImpManual.Click += new System.EventHandler(this.btnEjecutarImpManual_Click);
            // 
            // timerClose
            // 
            this.timerClose.Interval = 5000;
            this.timerClose.Tick += new System.EventHandler(this.timerClose_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(947, 433);
            this.Controls.Add(this.groupBoxImpManual);
            this.Controls.Add(this.btnClip);
            this.Controls.Add(this.listBox1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "[AUTO-CIERRE] HeyNow Chats Downloader v1.0 (MO&PC UY)";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBoxImpManual.ResumeLayout(false);
            this.groupBoxImpManual.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Button btnClip;
        private System.Windows.Forms.GroupBox groupBoxImpManual;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtEnd;
        private System.Windows.Forms.DateTimePicker dtBegin;
        private System.Windows.Forms.Button btnEjecutarImpManual;
		private System.Windows.Forms.Timer timerClose;
		private System.Windows.Forms.CheckBox ckVerSinDatos;
	}
}

