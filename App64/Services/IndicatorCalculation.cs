using System;
using System.Collections.Generic;
using System.Linq;
using App64.Controls;
using SkiaSharp;

namespace App64.Services
{
    public static class IndicatorCalculation
    {
        public static FastChart.CustomSeries CalculateMA(List<FastChart.OHLCV> data, int period, SKColor color)
        {
            var values = new List<double>();
            for (int i = 0; i < data.Count; i++)
            {
                if (i < period - 1)
                {
                    values.Add(double.NaN);
                    continue;
                }

                double sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += data[i - j].Close;
                }
                values.Add(sum / period);
            }

            return new FastChart.CustomSeries
            {
                SeriesName = $"MA_{period}",
                Title = $"MA({period})",
                Color = color,
                Values = values,
                PanelType = FastChart.PanelType.Overlay,
                Thickness = 1.5f
            };
        }

        public static FastChart.CustomSeries CalculateRSI(List<FastChart.OHLCV> data, int period = 14)
        {
            var values = new List<double>();
            if (data.Count < period) return null;

            double avgGain = 0;
            double avgLoss = 0;

            for (int i = 1; i <= period; i++)
            {
                double diff = data[i].Close - data[i - 1].Close;
                if (diff >= 0) avgGain += diff;
                else avgLoss += Math.Abs(diff);
            }

            avgGain /= period;
            avgLoss /= period;

            for (int i = 0; i < data.Count; i++)
            {
                if (i <= period)
                {
                    values.Add(double.NaN);
                    continue;
                }

                double diff = data[i].Close - data[i - 1].Close;
                double gain = diff >= 0 ? diff : 0;
                double loss = diff < 0 ? Math.Abs(diff) : 0;

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;

                if (avgLoss == 0) values.Add(100);
                else
                {
                    double rs = avgGain / avgLoss;
                    values.Add(100 - (100 / (1 + rs)));
                }
            }

            return new FastChart.CustomSeries
            {
                SeriesName = "RSI",
                Title = $"RSI({period})",
                Color = SKColors.Orange,
                Values = values,
                PanelType = FastChart.PanelType.Bottom,
                PanelName = "RSI",
                Overbought = 70,
                Oversold = 30,
                BaseLine = 50,
                Thickness = 1.5f
            };
        }

        public static FastChart.CustomSeries CalculateMACD(List<FastChart.OHLCV> data, int fast = 12, int slow = 26, int signal = 9)
        {
            var macdLine = new List<double>();
            var signalLine = new List<double>();
            var histogram = new List<double>();
            var colors = new List<SKColor>();

            if (data.Count == 0) return null;

            double fastEma = data[0].Close;
            double slowEma = data[0].Close;
            double kFast = 2.0 / (fast + 1);
            double kSlow = 2.0 / (slow + 1);
            double kSignal = 2.0 / (signal + 1);

            var macdValues = new List<double>();
            for (int i = 0; i < data.Count; i++)
            {
                fastEma = (data[i].Close - fastEma) * kFast + fastEma;
                slowEma = (data[i].Close - slowEma) * kSlow + slowEma;
                macdValues.Add(fastEma - slowEma);
            }

            double signalEma = macdValues[0];
            for (int i = 0; i < data.Count; i++)
            {
                signalEma = (macdValues[i] - signalEma) * kSignal + signalEma;
                double hist = macdValues[i] - signalEma;
                histogram.Add(hist);
                colors.Add(hist >= 0 ? SKColors.DodgerBlue : SKColors.Tomato);
            }

            return new FastChart.CustomSeries
            {
                SeriesName = "MACD",
                Title = $"MACD({fast},{slow},{signal})",
                Values = histogram,
                OutputColors = colors,
                PanelType = FastChart.PanelType.Bottom,
                PanelName = "MACD",
                Style = FastChart.PlotType.Histogram,
                BaseLine = 0
            };
        }

        public static FastChart.CustomSeries CalculateSuperTrend(List<FastChart.OHLCV> data, int period = 10, double multiplier = 3.0)
        {
            var values = new List<double>();
            var colors = new List<SKColor>();
            if (data.Count < period) return null;

            var st = new double[data.Count];
            var trend = new bool[data.Count]; 
            double currentAtr = 0;

            double upperBand = 0, lowerBand = 0;

            for (int i = 0; i < data.Count; i++)
            {
                double tr = (i == 0) ? (data[i].High - data[i].Low) : Math.Max(data[i].High - data[i].Low, Math.Max(Math.Abs(data[i].High - data[i - 1].Close), Math.Abs(data[i].Low - data[i - 1].Close)));
                if (i < period) currentAtr += tr / period;
                else currentAtr = (currentAtr * (period - 1) + tr) / period;

                double basicUpperBand = (data[i].High + data[i].Low) / 2 + multiplier * currentAtr;
                double basicLowerBand = (data[i].High + data[i].Low) / 2 - multiplier * currentAtr;

                if (i == 0)
                {
                    upperBand = basicUpperBand;
                    lowerBand = basicLowerBand;
                    trend[0] = true;
                }
                else
                {
                    upperBand = (basicUpperBand < upperBand || data[i - 1].Close > upperBand) ? basicUpperBand : upperBand;
                    lowerBand = (basicLowerBand > lowerBand || data[i - 1].Close < lowerBand) ? basicLowerBand : lowerBand;

                    trend[i] = trend[i - 1];
                    if (trend[i] && data[i].Close < lowerBand) trend[i] = false;
                    else if (!trend[i] && data[i].Close > upperBand) trend[i] = true;
                }

                st[i] = trend[i] ? lowerBand : upperBand;
                values.Add(st[i]);
                colors.Add(trend[i] ? SKColors.LimeGreen : SKColors.Red);
            }

            return new FastChart.CustomSeries
            {
                SeriesName = "SuperTrend",
                Title = $"SuperTrend({period},{multiplier})",
                Values = values,
                OutputColors = colors,
                PanelType = FastChart.PanelType.Overlay,
                Thickness = 2.0f
            };
        }

        // ═══════════════════════════════════════════════════
        //  증분 업데이트: 마지막 값 갱신 또는 새 값 추가
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// 기존 시리즈의 마지막 값을 Data에 맞게 재계산 또는 확장.
        /// newBar=true이면 새 봉이 추가됨 → series에도 값 추가.
        /// newBar=false이면 마지막 봉 갱신 → series 마지막 값 갱신.
        /// </summary>
        public static void UpdateSeriesRealtime(FastChart.CustomSeries series, List<FastChart.OHLCV> data, bool newBar)
        {
            if (data == null || data.Count == 0) return;

            string baseName = series.SeriesName;

            if (baseName.StartsWith("MA_"))
            {
                int period = 20;
                if (int.TryParse(baseName.Substring(3), out int p)) period = p;
                UpdateMA(series, data, period, newBar);
            }
            else if (baseName == "RSI")
            {
                UpdateRSI(series, data, 14, newBar);
            }
            else if (baseName == "MACD")
            {
                // MACD는 EMA 상태에 의존하므로 전체 재계산
                var recalc = CalculateMACD(data);
                if (recalc != null)
                {
                    series.Values = recalc.Values;
                    series.OutputColors = recalc.OutputColors;
                }
            }
            else if (baseName == "SuperTrend")
            {
                // SuperTrend도 상태에 의존하므로 전체 재계산
                var recalc = CalculateSuperTrend(data);
                if (recalc != null)
                {
                    series.Values = recalc.Values;
                    series.OutputColors = recalc.OutputColors;
                }
            }
        }

        private static void UpdateMA(FastChart.CustomSeries series, List<FastChart.OHLCV> data, int period, bool newBar)
        {
            int idx = data.Count - 1;
            double val = double.NaN;

            if (idx >= period - 1)
            {
                double sum = 0;
                for (int j = 0; j < period; j++)
                    sum += data[idx - j].Close;
                val = sum / period;
            }

            if (newBar)
            {
                series.Values.Add(val);
            }
            else if (series.Values.Count > 0)
            {
                series.Values[series.Values.Count - 1] = val;
            }
        }

        private static void UpdateRSI(FastChart.CustomSeries series, List<FastChart.OHLCV> data, int period, bool newBar)
        {
            // RSI는 연속 EMA 상태가 필요해서 간이적으로 마지막 N개로 계산
            int idx = data.Count - 1;
            double val = double.NaN;

            if (idx > period)
            {
                double avgGain = 0, avgLoss = 0;
                // 최근 period+1개의 데이터로 간이 RSI
                int start = Math.Max(1, idx - period * 3);
                for (int i = start; i <= start + period - 1 && i <= idx; i++)
                {
                    double diff2 = data[i].Close - data[i - 1].Close;
                    if (diff2 >= 0) avgGain += diff2;
                    else avgLoss += Math.Abs(diff2);
                }
                avgGain /= period;
                avgLoss /= period;

                for (int i = start + period; i <= idx; i++)
                {
                    double diff2 = data[i].Close - data[i - 1].Close;
                    double gain = diff2 >= 0 ? diff2 : 0;
                    double loss = diff2 < 0 ? Math.Abs(diff2) : 0;
                    avgGain = (avgGain * (period - 1) + gain) / period;
                    avgLoss = (avgLoss * (period - 1) + loss) / period;
                }

                if (avgLoss == 0) val = 100;
                else
                {
                    double rs = avgGain / avgLoss;
                    val = 100 - (100 / (1 + rs));
                }
            }

            if (newBar)
            {
                series.Values.Add(val);
            }
            else if (series.Values.Count > 0)
            {
                series.Values[series.Values.Count - 1] = val;
            }
        }
    }
}
