namespace Server32
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblKiwoomStatus;
        private System.Windows.Forms.Label lblCybosStatus;
        private System.Windows.Forms.Label lblPipeStatus;
        private System.Windows.Forms.Label lblStats;
        private System.Windows.Forms.Panel pnlStatus;

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
            this.lblStats = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pnlStatus.SuspendLayout();
            this.SuspendLayout();
            // pnlStatus
            this.pnlStatus.BackColor = System.Drawing.Color.FromArgb(30, 30, 40);
            this.pnlStatus.Controls.Add(this.lblKiwoomStatus);
            this.pnlStatus.Controls.Add(this.lblCybosStatus);
            this.pnlStatus.Controls.Add(this.lblPipeStatus);
            this.pnlStatus.Controls.Add(this.lblStats);
            this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatus.Height = 50;
            // lblKiwoomStatus
            this.lblKiwoomStatus.AutoSize = true;
            this.lblKiwoomStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblKiwoomStatus.Location = new System.Drawing.Point(10, 8);
            this.lblKiwoomStatus.Text = "● Kiwoom: 초기화중...";
            this.lblKiwoomStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // lblCybosStatus
            this.lblCybosStatus.AutoSize = true;
            this.lblCybosStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblCybosStatus.Location = new System.Drawing.Point(10, 28);
            this.lblCybosStatus.Text = "● Cybos: 초기화중...";
            this.lblCybosStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // lblPipeStatus
            this.lblPipeStatus.AutoSize = true;
            this.lblPipeStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblPipeStatus.Location = new System.Drawing.Point(200, 8);
            this.lblPipeStatus.Text = "● Pipe: 대기중";
            this.lblPipeStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            // lblStats
            this.lblStats.AutoSize = true;
            this.lblStats.ForeColor = System.Drawing.Color.FromArgb(180, 180, 200);
            this.lblStats.Location = new System.Drawing.Point(200, 28);
            this.lblStats.Text = "Msg: 0 | RT: 0";
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
            this.ClientSize = new System.Drawing.Size(700, 400);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.pnlStatus);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Text = "Server32 — 키움+Cybos (자동)";
            this.pnlStatus.ResumeLayout(false);
            this.pnlStatus.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}