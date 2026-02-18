namespace App64
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
        private System.Windows.Forms.ToolStripMenuItem mnuView;
        private System.Windows.Forms.ToolStripMenuItem mnuViewLog;
        private System.Windows.Forms.ToolStripMenuItem mnuServer;
        private System.Windows.Forms.ToolStripMenuItem mnuServerConnect;
        private System.Windows.Forms.ToolStripMenuItem mnuServerDisconnect;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblConnection;
        private System.Windows.Forms.ToolStripStatusLabel lblKiwoom;
        private System.Windows.Forms.ToolStripStatusLabel lblCybos;
        private System.Windows.Forms.ToolStripStatusLabel lblMsgCount;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TabControl tabLeft;
        private System.Windows.Forms.TabPage tabWatchList;
        private System.Windows.Forms.TabPage tabCondition;
        private System.Windows.Forms.TabControl tabRight;
        private System.Windows.Forms.TabPage tabChart;
        private System.Windows.Forms.TabPage tabOrder;
        private System.Windows.Forms.TabPage tabBalance;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.ListView lvWatchList;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnOrderTest;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuView = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewLog = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuServer = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuServerConnect = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuServerDisconnect = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblConnection = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblKiwoom = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblCybos = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblMsgCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tabLeft = new System.Windows.Forms.TabControl();
            this.tabWatchList = new System.Windows.Forms.TabPage();
            this.tabCondition = new System.Windows.Forms.TabPage();
            this.tabRight = new System.Windows.Forms.TabControl();
            this.tabChart = new System.Windows.Forms.TabPage();
            this.tabOrder = new System.Windows.Forms.TabPage();
            this.tabBalance = new System.Windows.Forms.TabPage();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.lvWatchList = new System.Windows.Forms.ListView();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnOrderTest = new System.Windows.Forms.Button();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.tabLeft.SuspendLayout();
            this.tabWatchList.SuspendLayout();
            this.tabRight.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.SuspendLayout();
            // menuStrip
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(30, 30, 40);
            this.menuStrip.ForeColor = System.Drawing.Color.FromArgb(200, 200, 220);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFile, this.mnuView, this.mnuServer});
            // mnuFile
            this.mnuFile.Text = "파일(&F)";
            this.mnuFile.DropDownItems.Add(this.mnuFileExit);
            this.mnuFileExit.Text = "종료(&X)";
            //this.mnuFileExit.Click += (s, e) => this.Close();
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // mnuView
            this.mnuView.Text = "보기(&V)";
            this.mnuView.DropDownItems.Add(this.mnuViewLog);
            this.mnuViewLog.Text = "로그 탭(&L)";

            //this.mnuViewLog.Click += (s, e) => this.tabRight.SelectedTab = this.tabLog;
            this.mnuViewLog.Click += new System.EventHandler(this.mnuViewLog_Click);

            // mnuServer
            this.mnuServer.Text = "서버(&S)";
            this.mnuServer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuServerConnect, this.mnuServerDisconnect});
            this.mnuServerConnect.Text = "연결(&C)";
            this.mnuServerConnect.Click += new System.EventHandler(this.mnuServerConnect_Click);
            this.mnuServerDisconnect.Text = "해제(&D)";
            this.mnuServerDisconnect.Enabled = false;
            this.mnuServerDisconnect.Click += new System.EventHandler(this.mnuServerDisconnect_Click);
            // statusStrip
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblConnection, this.lblKiwoom, this.lblCybos, this.lblMsgCount});
            this.lblConnection.Text = "● 미연결";
            this.lblConnection.ForeColor = System.Drawing.Color.Gray;
            this.lblKiwoom.Text = "K:--";
            this.lblKiwoom.ForeColor = System.Drawing.Color.Gray;
            this.lblCybos.Text = "C:--";
            this.lblCybos.ForeColor = System.Drawing.Color.Gray;
            this.lblMsgCount.Text = "Msg: 0";
            this.lblMsgCount.ForeColor = System.Drawing.Color.FromArgb(150, 150, 170);
            this.lblMsgCount.Spring = true;
            this.lblMsgCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // splitMain
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.SplitterDistance = 250;
            this.splitMain.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            // tabLeft
            this.tabLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabLeft.TabPages.AddRange(new System.Windows.Forms.TabPage[] {
                this.tabWatchList, this.tabCondition});
            // tabWatchList
            this.tabWatchList.Text = "관심종목";
            this.tabWatchList.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            this.tabWatchList.Controls.Add(this.lvWatchList);
            // lvWatchList
            this.lvWatchList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvWatchList.View = System.Windows.Forms.View.Details;
            this.lvWatchList.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            this.lvWatchList.ForeColor = System.Drawing.Color.FromArgb(200, 220, 200);
            this.lvWatchList.Font = new System.Drawing.Font("Consolas", 9F);
            this.lvWatchList.FullRowSelect = true;
            this.lvWatchList.GridLines = true;
            this.lvWatchList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new System.Windows.Forms.ColumnHeader() { Text = "종목코드", Width = 70 },
                new System.Windows.Forms.ColumnHeader() { Text = "종목명", Width = 80 },
                new System.Windows.Forms.ColumnHeader() { Text = "현재가", Width = 70, TextAlign = System.Windows.Forms.HorizontalAlignment.Right },
                new System.Windows.Forms.ColumnHeader() { Text = "등락%", Width = 60, TextAlign = System.Windows.Forms.HorizontalAlignment.Right },
                new System.Windows.Forms.ColumnHeader() { Text = "거래량", Width = 80, TextAlign = System.Windows.Forms.HorizontalAlignment.Right }
            });
            // tabCondition
            this.tabCondition.Text = "조건검색";
            this.tabCondition.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            // tabRight
            this.tabRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabRight.TabPages.AddRange(new System.Windows.Forms.TabPage[] {
                this.tabChart, this.tabOrder, this.tabBalance, this.tabLog});
            // tabChart
            this.tabChart.Text = "차트";
            this.tabChart.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            // tabOrder
            this.tabOrder.Text = "주문";
            this.tabOrder.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            // tabBalance
            this.tabBalance.Text = "잔고";
            this.tabBalance.BackColor = System.Drawing.Color.FromArgb(25, 25, 35);
            // tabLog
            this.tabLog.Text = "로그";
            this.tabLog.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            // btnOrderTest
            this.btnOrderTest.Text = "주문 극한테스트";
            this.btnOrderTest.Size = new System.Drawing.Size(130, 32);
            this.btnOrderTest.Location = new System.Drawing.Point(3, 3);
            this.btnOrderTest.BackColor = System.Drawing.Color.FromArgb(40, 60, 120);
            this.btnOrderTest.ForeColor = System.Drawing.Color.White;
            this.btnOrderTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOrderTest.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 120, 200);
            this.btnOrderTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOrderTest.Enabled = false;
            this.btnOrderTest.Click += new System.EventHandler(this.btnOrderTest_Click);
            // tabLog에 버튼과 txtLog 배치
            this.tabLog.Controls.Clear();
            var pnlLogTop = new System.Windows.Forms.Panel();
            pnlLogTop.Dock = System.Windows.Forms.DockStyle.Top;
            pnlLogTop.Height = 38;
            pnlLogTop.BackColor = System.Drawing.Color.FromArgb(25, 25, 38);
            pnlLogTop.Controls.Add(this.btnOrderTest);
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(160, 230, 160);
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.WordWrap = false;
            this.tabLog.Controls.Add(this.txtLog);
            this.tabLog.Controls.Add(pnlLogTop);
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MainMenuStrip = this.menuStrip;
            this.Text = "TradingSystem — 64bit Trading App";
            this.splitMain.Panel1.Controls.Add(this.tabLeft);
            this.splitMain.Panel2.Controls.Add(this.tabRight);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.tabLeft.ResumeLayout(false);
            this.tabWatchList.ResumeLayout(false);
            this.tabRight.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}