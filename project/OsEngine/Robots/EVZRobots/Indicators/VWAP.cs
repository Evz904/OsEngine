using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;

namespace CustomIndicators.Scripts.EVZRobots
{
    [IndicatorAttribute("VWAP")]
    public class VWAP : Aindicator
    {
        private IndicatorParameterString _Period;

        private IndicatorParameterString _candlePoint;

        private IndicatorDataSeries _series;

       // _Values3 Zeros;
        public vDecimal2 _VWAP;

        public delegate long _CalcPeriod(DateTime T);

        public _CalcPeriod DT;
        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }
            else if (index > 0)
            {
                Candle candle = candles[index];
                Candle candle1 = candles[index - 1];
                decimal Price = candle.GetPoint(_candlePoint.ValueString);
                decimal Price1 = candle1.GetPoint(_candlePoint.ValueString);
                decimal Value = candle.Volume;
                decimal Value1 = candle1.Volume;
                //if i > idx then
                //    idx = i;
                if (DT(candle.TimeStart) > DT(candle1.TimeStart)) {

                    _VWAP.v = 0;
                    _VWAP.v1 = 0;
                }
                else
                {
                    _VWAP.v = _VWAP.v + Value1;
                    _VWAP.v1 = _VWAP.v1 + Price1 * Value1;
                }
                decimal res = (_VWAP.v1 + Price * Value) / (_VWAP.v + Value);
                _series.Values[index] = res;
            }
            //return res;
            //_vBulls.SlashValue();
            //_vBulls.v = _vBulls.v1 + (_a * (dBullZamah - _vBulls.v1));
            //_vBears.SlashValue();
            //_vBears.v = _vBears.v1 + (_a * (dBearZamah - _vBears.v1));
            //_seriesBulls.Values[index] = _vBulls.v;// dBullZamah;
            //_seriesBears.Values[index] = _vBears.v;// dBearZamah;

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

        private long TimeMonth(DateTime t) { return (t.Year * 100 + t.Month); }
        private long TimeDay(DateTime t) { return ((t.Year * 100 + t.Month) * 100 + t.Day); }
        private long TimeYear(DateTime t) { return (t.Year * 100); }
        //private long TimeWeek(DateTime t) { return (t.Year * 100) + t.DayOfWeek; }
        private long TimeOther(DateTime t) { return 0; }
        private void InitParametrs()
        {
            _Period = CreateParameterStringCollection("Period", "Day", ["Day", "Month",  "Year"]); // { "Day", "Week", "Month", "Quarter", "Year" });
            _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
            _series = CreateSeries("VWAP", Color.DeepSkyBlue, IndicatorChartPaintType.Line, true);
            if (_Period._valueString == "Month")  // месяц
            {
                DT = TimeMonth;// (DateTime t)-> { return (t.year * 100 + t.month)}
            }
            else if (_Period._valueString == "Day")
            {  // день
                DT = TimeDay;
            }
            else if (_Period._valueString == "Year")
            {
                DT = TimeYear;
            }

            else
                DT = TimeOther;

        _VWAP = new vDecimal2(0, 0);

        // _VWAP.v = 0; _VWAP.v1 = 0; //idx = 1;

        }

    }
}
