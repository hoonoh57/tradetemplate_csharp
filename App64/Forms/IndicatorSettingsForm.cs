using System;
using System.Drawing;
using System.Windows.Forms;
using App64.Controls;

namespace App64.Forms
{
    public class IndicatorSettingsForm : Form
    {
        public FastChart.CustomSeries Series { get; private set; }
        private PropertyGrid _propGrid;

        public IndicatorSettingsForm(FastChart.CustomSeries series)
        {
            this.Series = series;
            this.Text = $"{series.Title} 설정";
            this.Size = new Size(300, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _propGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                SelectedObject = series
            };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(130, 8) };
            var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(210, 8) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            this.Controls.Add(_propGrid);
            this.Controls.Add(btnPanel);
        }
    }
}
