using System;
using System.Drawing;
using System.Windows.Forms;

namespace App64.Forms
{
    public class PromptForm : Form
    {
        private TextBox _txtPrompt;
        private Button _btnOk;
        public string ResultText { get; private set; }

        public PromptForm(string title, string defaultText = "")
        {
            this.Text = title;
            this.Size = new Size(500, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _txtPrompt = new TextBox
            {
                Location = new Point(10, 20),
                Width = 460,
                Text = defaultText
            };

            _btnOk = new Button
            {
                Text = "적용",
                Location = new Point(395, 60),
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += (s, e) => { ResultText = _txtPrompt.Text; this.Close(); };

            this.Controls.Add(_txtPrompt);
            this.Controls.Add(_btnOk);
            this.AcceptButton = _btnOk;
        }
    }
}
