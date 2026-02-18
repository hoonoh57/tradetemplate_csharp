using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using App64.Services;

namespace App64.Forms
{
    public class ConditionSearchForm : Form
    {
        private readonly ConnectionService _conn;
        private readonly ComboBox _cboConditions;
        private readonly Button _btnExecute;
        private readonly List<ConditionItem> _items = new List<ConditionItem>();

        private struct ConditionItem
        {
            public int Index;
            public string Name;
            public override string ToString() => $"[{Index:D2}] {Name}";
        }

        public ConditionSearchForm(ConnectionService conn)
        {
            _conn = conn;

            this.Text = "조건검색";
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lbl = new Label
            {
                Text = "조건식 선택:",
                Location = new Point(20, 25),
                AutoSize = true
            };

            _cboConditions = new ComboBox
            {
                Location = new Point(20, 50),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _btnExecute = new Button
            {
                Text = "실행",
                Location = new Point(290, 48),
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(60, 60, 80),
                FlatStyle = FlatStyle.Flat
            };
            _btnExecute.Click += BtnExecute_Click;

            this.Controls.Add(lbl);
            this.Controls.Add(_cboConditions);
            this.Controls.Add(_btnExecute);

            this.Load += ConditionSearchForm_Load;
        }

        private async void ConditionSearchForm_Load(object sender, EventArgs e)
        {
            try
            {
                string resp = await _conn.GetConditionListAsync();
                if (string.IsNullOrEmpty(resp)) return;

                _items.Clear();
                _cboConditions.Items.Clear();

                var pairs = resp.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in pairs)
                {
                    var parts = p.Split('^');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int idx))
                    {
                        var item = new ConditionItem { Index = idx, Name = parts[1] };
                        _items.Add(item);
                        _cboConditions.Items.Add(item);
                    }
                }

                if (_cboConditions.Items.Count > 0)
                    _cboConditions.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("조건목록 로드 실패: " + ex.Message);
            }
        }

        private async void BtnExecute_Click(object sender, EventArgs e)
        {
            if (_cboConditions.SelectedItem == null) return;
            var item = (ConditionItem)_cboConditions.SelectedItem;

            try
            {
                _btnExecute.Enabled = false;
                await _conn.ExecuteConditionAsync(item.Index, item.Name);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("조건검색 실행 실패: " + ex.Message);
                _btnExecute.Enabled = true;
            }
        }
    }
}
