using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Common.Models;

namespace App64.Forms
{
    /// <summary>
    /// 전략의 평가 과정을 한눈에 보여주는 투명한 검증 윈도우.
    /// 사용자가 차트의 신호를 클릭했을 때, 어떤 로직이 Pass/Fail 되었는지 '지표 값'과 함께 보여줌.
    /// </summary>
    public class StrategyInspectorForm : Form
    {
        private DataGridView _grid;
        private Label _lblTargetTime;

        public StrategyInspectorForm(EvaluationResult result, MarketSnapshot snapshot, StrategyDefinition strategy)
        {
            this.Text = $"Strategy Inspector - {strategy.Name}";
            this.Size = new Size(600, 400);
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterParent;

            _lblTargetTime = new Label
            {
                Text = $"분석 시점: {result.Time:MM/dd HH:mm:ss}",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false
            };

            _grid.Columns.Add("Type", "구분");
            _grid.Columns.Add("ID", "ID");
            _grid.Columns.Add("Desc", "조건 설명");
            _grid.Columns.Add("Value", "현재 지표값");
            _grid.Columns.Add("Result", "결과");

            PopulateGrid(result, snapshot, strategy);

            this.Controls.Add(_grid);
            this.Controls.Add(_lblTargetTime);
        }

        private void PopulateGrid(EvaluationResult result, MarketSnapshot snapshot, StrategyDefinition strategy)
        {
            // 모든 Buy/Sell 규칙의 조건을 투명하게 나열
            foreach (var gate in strategy.BuyRules)
            {
                foreach (var cond in gate.Conditions)
                {
                    bool pass = result.ConditionStates.TryGetValue(cond.Id, out bool b) && b;
                    double val = snapshot.GetValue(cond.IndicatorA);
                    
                    int rowIndex = _grid.Rows.Add(
                        "매수조건",
                        cond.Id,
                        cond.Description,
                        val.ToString("N2"),
                        pass ? "PASS" : "FAIL"
                    );

                    if (pass) _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 200);
                    else _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                }
            }

            foreach (var gate in strategy.SellRules)
            {
                foreach (var cond in gate.Conditions)
                {
                    bool pass = result.ConditionStates.TryGetValue(cond.Id, out bool b) && b;
                    double val = snapshot.GetValue(cond.IndicatorA);

                    int rowIndex = _grid.Rows.Add(
                        "매매조건",
                        cond.Id,
                        cond.Description,
                        val.ToString("N2"),
                        pass ? "PASS" : "FAIL"
                    );

                    if (pass) _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 255);
                }
            }
        }
    }
}
