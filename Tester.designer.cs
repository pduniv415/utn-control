namespace AimTrainer
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados deben desecharse; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStart = new System.Windows.Forms.Button();
            this.lblScore = new System.Windows.Forms.Label();
            this.btnExport = new System.Windows.Forms.Button();
            this.progressBarTimer = new System.Windows.Forms.ProgressBar();
            this.pbLife1 = new System.Windows.Forms.PictureBox();
            this.pbLife2 = new System.Windows.Forms.PictureBox();
            this.pbLife3 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbLife1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLife2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLife3)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(12, 5);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(91, 36);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Iniciar";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblScore
            // 
            this.lblScore.AutoSize = true;
            this.lblScore.Location = new System.Drawing.Point(208, 17);
            this.lblScore.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblScore.Name = "lblScore";
            this.lblScore.Size = new System.Drawing.Size(73, 13);
            this.lblScore.TabIndex = 1;
            this.lblScore.Text = "Puntuación: 0";
            this.lblScore.Click += new System.EventHandler(this.lblScore_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(688, 5);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(100, 36);
            this.btnExport.TabIndex = 2;
            this.btnExport.Text = "Guardar";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // progressBarTimer
            // 
            this.progressBarTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarTimer.Location = new System.Drawing.Point(400, 14);
            this.progressBarTimer.Name = "progressBarTimer";
            this.progressBarTimer.Size = new System.Drawing.Size(280, 20);
            this.progressBarTimer.TabIndex = 3;
            this.progressBarTimer.Value = 100;
            // 
            // pbLife1
            // 
            this.pbLife1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbLife1.BackColor = System.Drawing.Color.LimeGreen;
            this.pbLife1.Location = new System.Drawing.Point(658, 42);
            this.pbLife1.Name = "pbLife1";
            this.pbLife1.Size = new System.Drawing.Size(20, 20);
            this.pbLife1.TabIndex = 6;
            this.pbLife1.TabStop = false;
            // 
            // pbLife2
            // 
            this.pbLife2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbLife2.BackColor = System.Drawing.Color.LimeGreen;
            this.pbLife2.Location = new System.Drawing.Point(632, 42);
            this.pbLife2.Name = "pbLife2";
            this.pbLife2.Size = new System.Drawing.Size(20, 20);
            this.pbLife2.TabIndex = 5;
            this.pbLife2.TabStop = false;
            // 
            // pbLife3
            // 
            this.pbLife3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbLife3.BackColor = System.Drawing.Color.LimeGreen;
            this.pbLife3.Location = new System.Drawing.Point(606, 42);
            this.pbLife3.Name = "pbLife3";
            this.pbLife3.Size = new System.Drawing.Size(20, 20);
            this.pbLife3.TabIndex = 4;
            this.pbLife3.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pbLife1);
            this.Controls.Add(this.pbLife2);
            this.Controls.Add(this.pbLife3);
            this.Controls.Add(this.progressBarTimer);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.lblScore);
            this.Controls.Add(this.btnStart);
            this.Name = "Form1";
            this.Text = "Air Link Tester";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseUp);
            ((System.ComponentModel.ISupportInitialize)(this.pbLife1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLife2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLife3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblScore;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.ProgressBar progressBarTimer;
        private System.Windows.Forms.PictureBox pbLife1;
        private System.Windows.Forms.PictureBox pbLife2;
        private System.Windows.Forms.PictureBox pbLife3;
    }
}
