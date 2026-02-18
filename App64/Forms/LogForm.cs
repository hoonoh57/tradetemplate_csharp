using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    public class LogForm : DockContent
    {
        private readonly RichTextBox _txtLog;
        private const int MAX_LINES = 5000;

        public LogForm()
        {
            this.Text = "로그";
            this.DockAreas = DockAreas.DockBottom | DockAreas.DockTop | DockAreas.Float;
            this.ShowHint = DockState.DockBottom;
            this.CloseButton = false;
            this.CloseButtonVisible = false;

            _txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 30),
                ForeColor = Color.FromArgb(160, 230, 160),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                WordWrap = false
            };
            this.Controls.Add(_txtLog);
        }

        public void AppendLog(string message)
        {
            if (_txtLog == null || _txtLog.IsDisposed) return;
            if (_txtLog.InvokeRequired)
            {
                _txtLog.BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            // 라인 수 제한
            if (_txtLog.Lines.Length > MAX_LINES)
            {
                _txtLog.SelectionStart = 0;
                _txtLog.SelectionLength = _txtLog.GetFirstCharIndexFromLine(MAX_LINES / 2);
                _txtLog.SelectedText = "";
            }

            // 색상 분류
            Color color = Color.FromArgb(160, 230, 160); // 기본 녹색
            if (message.Contains("[오류]") || message.Contains("[ERR]"))
                color = Color.OrangeRed;
            else if (message.Contains("연결") || message.Contains("로그인"))
                color = Color.FromArgb(80, 250, 120);
            else if (message.Contains("[조건검색]") || message.Contains("[실시간 조건]"))
                color = Color.Cyan;
            else if (message.Contains("[체결]") || message.Contains("[주문]"))
                color = Color.Yellow;
            else if (message.Contains("[잔고]"))
                color = Color.FromArgb(255, 180, 100);

            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.SelectionLength = 0;
            _txtLog.SelectionColor = color;
            _txtLog.AppendText(message + Environment.NewLine);
            _txtLog.ScrollToCaret();
        }

        public void Clear() => _txtLog?.Clear();
    }
}
