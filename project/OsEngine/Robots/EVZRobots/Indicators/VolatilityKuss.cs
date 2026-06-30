using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;

namespace CustomIndicators.Scripts.EVZRobots
{
    [IndicatorAttribute("VolatilityKuss")]
    public class VolatilityKuss : Aindicator
    {

        private IndicatorParameterInt _length;
        private IndicatorParameterString _candlePoint;

        private IndicatorDataSeries _series;
        private IndicatorDataSeries _seriesCVint;

        private decimal _a;
        Values3 _CVi;
        decimal[] _CVint;

        public VolatilityKuss()
        {
            _length = CreateParameterInt("Length", 6);
            _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
            _series = CreateSeries("VolatilityKussCVi", Color.MediumSeaGreen, IndicatorChartPaintType.Line, true);
            _seriesCVint = CreateSeries("VolatilityKussCVint", Color.MediumSlateBlue, IndicatorChartPaintType.Line, true);

        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }
            else if (index > 0)
            {
                int _indC = index % _length.ValueInt;
                Candle candle = candles[index];
                //Candle candle1 = candles[^1];
                decimal ValueDenom = Math.Abs(candle.Open - candle.Close);
                decimal ValueNom = candle.High - candle.Low;
                if (ValueDenom > 0) 
                {
                    _CVi.v = ValueNom / ValueDenom;
                }
                else
                {
                    decimal e = candle.GetPoint("Median") / 10;
                    _CVi.v = (ValueNom + e)/ e;
                }
                //    idx = i;
                _CVint[_indC] = _CVi.v;
                decimal _CVint0 = 0;
                if (index >= _length.ValueInt)
                {
                    for (int i = 0; i < _length.ValueInt; i++)
                    {
                        _CVint0 += _CVint[i];
                    }
                    _CVint0 /= _length.ValueInt;
                    _seriesCVint.Values[index] = _CVint0;
                }
                decimal res = _CVi.v;
                _series.Values[index] = res;
            }
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                InitParametrs();
            }
            if (state == IndicatorState.Dispose)
            {

            }
        }

        private void InitParametrs()
        {
            _CVint = new decimal[_length.ValueInt];
            _CVi = new Values3(0.0m, 0.0m, 0.0m);

        }
    }
}
