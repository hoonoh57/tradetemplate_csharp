using System;
using System.Drawing;
using System.Windows.Forms;
using Common.Models;
using WeifenLuo.WinFormsUI.Docking;

namespace App64.Forms
{
    public class PortfolioForm : DockContent
    {
        private readonly DataGridView _gridPositions;
        private readonly Label _lblSummary;

        public PortfolioForm()
        {
            this.Text = "포트폴리오";
            this.DockAreas = DockAreas.DockBottom | DockAreas.DockTop
                           | DockAreas.Float | DockAreas.Document;
            this.ShowHint = DockState.DockBottom;
            this.CloseButton = false;
            this.CloseButtonVisible = false;

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(35, 35, 45),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "  총자산: -- | 실현손익: -- | 미체결: --",
                Padding = new Padding(5, 0, 0, 0)
            };

            _gridPositions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 50),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 50),
                    ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 9f, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(25, 25, 35),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 9f)
                }
            };

            _gridPositions.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Code",     HeaderText = "코드",   Width = 70 },
                new DataGridViewTextBoxColumn { Name = "Name",     HeaderText = "종목명", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Qty",      HeaderText = "수량",   Width = 60 },
                new DataGridViewTextBoxColumn { Name = "AvgPrice", HeaderText = "평균가", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "CurPrice", HeaderText = "현재가", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "PnL",      HeaderText = "손익",   Width = 80 },
                new DataGridViewTextBoxColumn { Name = "PnLRate",  HeaderText = "수익률", Width = 70 },
            });

            this.Controls.Add(_gridPositions);
            this.Controls.Add(_lblSummary);
        }

        public void UpdateSummary(string totalAsset, string realizedPnL, string pendingCount)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string>(UpdateSummary),
                    totalAsset, realizedPnL, pendingCount);
                return;
            }
            _lblSummary.Text = $"  총자산: {totalAsset} | 실현손익: {realizedPnL} | 미체결: {pendingCount}";
        }

        public void UpdatePosition(BalanceInfo balance)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<BalanceInfo>(UpdatePosition), balance);
                return;
            }

            // 기존 행 찾기 또는 추가
            DataGridViewRow targetRow = null;
            foreach (DataGridViewRow row in _gridPositions.Rows)
            {
                if (row.Cells["Code"].Value?.ToString() == balance.Code)
                {
                    targetRow = row;
                    break;
                }
            }

            if (targetRow == null)
            {
                if (balance.Qty <= 0) return; // 수량 0이면 추가 불필요
                int idx = _gridPositions.Rows.Add();
                targetRow = _gridPositions.Rows[idx];
            }

            if (balance.Qty <= 0)
            {
                // 수량 0이면 행 제거
                _gridPositions.Rows.Remove(targetRow);
                return;
            }

            targetRow.Cells["Code"].Value = balance.Code;
            targetRow.Cells["Name"].Value = balance.Name;
            targetRow.Cells["Qty"].Value = balance.Qty.ToString("N0");
            targetRow.Cells["AvgPrice"].Value = balance.AvgPrice.ToString("N0");
            targetRow.Cells["CurPrice"].Value = balance.CurrentPrice.ToString("N0");
            targetRow.Cells["PnL"].Value = balance.ProfitLoss.ToString("N0");
            targetRow.Cells["PnLRate"].Value = balance.ProfitRate.ToString("F2") + "%";

            // 색상
            Color plColor = balance.ProfitLoss > 0 ? Color.Red :
                            balance.ProfitLoss < 0 ? Color.RoyalBlue : Color.White;
            targetRow.Cells["PnL"].Style.ForeColor = plColor;
            targetRow.Cells["PnLRate"].Style.ForeColor = plColor;
        }
    }
}
