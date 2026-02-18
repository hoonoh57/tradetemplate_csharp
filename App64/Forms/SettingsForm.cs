using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    public class SettingsForm : DockContent
    {
        public SettingsForm()
        {
            this.Text = "설정";
            this.DockAreas = DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float;
            this.ShowHint = DockState.DockRight;

            var lbl = new Label
            {
                Text = "전략 파라미터 설정\n\n"
                     + "Phase 7에서 구현 예정:\n"
                     + "- Strategy-0 파라미터 (시가대비%, 틱비율, VI거리%)\n"
                     + "- 손절/익절 비율\n"
                     + "- 자동매매 ON/OFF\n"
                     + "- 데이터 기록 경로",
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(25, 25, 35),
                Font = new Font("맑은 고딕", 11f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lbl);
        }
    }
}
