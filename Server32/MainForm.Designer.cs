namespace Server32
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblKiwoomStatus;
        private System.Windows.Forms.Label lblCybosStatus;
        private System.Windows.Forms.Label lblPipeStatus;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Panel pnlStatus;
        private System.Windows.Forms.Label lblStats;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlStatus = new System.Windows.Forms.Panel();
            this.lblKiwoomStatus = new System.Windows.Forms.Label();
            this.lblCybosStatus = new System.Windows.Forms.Label();
            this.lblPipeStatus = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblStats = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pnlStatus.SuspendLayout();
            this.SuspendLayout();
            // pnlStatus
            this.pnlStatus.BackColor = System.Drawing.Color.FromArgb(30, 30, 40);
            this.pnlStatus.Controls.Add(this.lblKiwoomStatus);
            this.pnlStatus.Controls.Add(this.lblCybosStatus);
            this.pnlStatus.Controls.Add(this.lblPipeStatus);
            this.pnlStatus.Controls.Add(this.btnStart);
            this.pnlStatus.Controls.Add(this.btnStop);
            this.pnlStatus.Controls.Add(this.lblStats);
            this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatus.Height = 50;
            // lblKiwoomStatus
            this.lblKiwoomStatus.AutoSize = true;
            this.lblKiwoomStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblKiwoomStatus.Location = new System.Drawing.Point(10, 8);
            this.lblKiwoomStatus.Text = "● Kiwoom: 미접속";
            this.lblKiwoomStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // lblCybosStatus
            this.lblCybosStatus.AutoSize = true;
            this.lblCybosStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblCybosStatus.Location = new System.Drawing.Point(10, 28);
            this.lblCybosStatus.Text = "● Cybos: 미접속";
            this.lblCybosStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // lblPipeStatus
            this.lblPipeStatus.AutoSize = true;
            this.lblPipeStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblPipeStatus.Location = new System.Drawing.Point(200, 8);
            this.lblPipeStatus.Text = "● Pipe: 대기중";
            this.lblPipeStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // btnStart
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 60);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Location = new System.Drawing.Point(400, 8);
            this.btnStart.Size = new System.Drawing.Size(80, 32);
            this.btnStart.Text = "▶ Start";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // btnStop
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(160, 40, 40);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(490, 8);
            this.btnStop.Size = new System.Drawing.Size(80, 32);
            this.btnStop.Text = "■ Stop";
            this.btnStop.Enabled = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // lblStats
            this.lblStats.AutoSize = true;
            this.lblStats.ForeColor = System.Drawing.Color.FromArgb(180, 180, 200);
            this.lblStats.Location = new System.Drawing.Point(200, 28);
            this.lblStats.Text = "Msg: 0 | RT: 0 codes";
            this.lblStats.Font = new System.Drawing.Font("Consolas", 8.5F);
            // txtLog
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(160, 230, 160);
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.WordWrap = false;
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.ClientSize = new System.Drawing.Size(700, 450);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.pnlStatus);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Text = "TradingSystem Server32 — 키움+Cybos 통합";
            this.pnlStatus.ResumeLayout(false);
            this.pnlStatus.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}