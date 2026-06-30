using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace CustomIndicators.Scripts.EVZRobots
{
    [IndicatorAttribute("VolumeAverageChanel")]
    public class VolumeAverageChanel : Aindicator
    {
        private IndicatorParameterInt _PeriodLength;
        // Volume channel
        public IndicatorParameterInt _lengthUp;
        public IndicatorParameterInt _lengthDown;

        private IndicatorParameterString _MAType;

        private IndicatorDataSeries _seriesVolumeMA;
        private IndicatorDataSeries _seriesActualVolume;

        private IndicatorDataSeries _seriesUp;
        private IndicatorDataSeries _seriesDown;

        private decimal lastMoving;

        private MA mAVolume;
       // private MA mASlow;

        public VolumeAverageChanel()
        {
            _PeriodLength = CreateParameterInt("Period length ", 24);
            //_lengthUp = CreateParameterInt("Length Up", 14);
            //_lengthDown = CreateParameterInt("Length Down", 14);

            _MAType = CreateParameterStringCollection("MA Type", "EMA", ["EMA", "SMA"]);

            _seriesVolumeMA = CreateSeries("VolumeMA", Color.Brown, IndicatorChartPaintType.Line, true);
            _seriesActualVolume = CreateSeries("ActualVolume", Color.LightYellow, IndicatorChartPaintType.Column, true);

            //_seriesUp = CreateSeries("Up line", Color.Aqua, IndicatorChartPaintType.Line, true);
            //_seriesDown = CreateSeries("Down line", Color.BlueViolet, IndicatorChartPaintType.Line, true);


        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }
            Candle candle = candles[index];
            
            //decimal upLine = 0;

            //if (index - _lengthUp.ValueInt > 0)
            //{
            //    for (int i = index; i > -1 && i > index - _lengthUp.ValueInt; i--)
            //    {
            //        if (upLine < candles[i].Volume)
            //        {
            //            upLine = candles[i].Volume;
            //        }
            //    }
            //}

            //decimal downLine = 0;

            //if (index - _lengthDown.ValueInt > 0)
            //{
            //    downLine = decimal.MaxValue;

            //    for (int i = index; i > -1 && i > index - _lengthDown.ValueInt; i--)
            //    {
            //        if (downLine > candles[i].Volume)
            //        {
            //            downLine = candles[i].Volume;
            //        }
            //    }
            //}
            decimal Volume = candle.Volume;

            _seriesActualVolume.Values[index] = Volume;

            lastMoving = Math.Round(mAVolume.CalcMA(Volume), 0);// tab.Security.DecimalsVolume);

            if (index > _PeriodLength.ValueInt)
            {
                _seriesVolumeMA.Values[index] = lastMoving;
            }
            //_seriesUp.Values[index] = upLine;
            //_seriesDown.Values[index] = downLine;
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
            lastMoving = 0;
            //lastMoving21 = 0;
            //lastMoving22 = 0;
            //_CVint = new decimal[_lengthFast.ValueInt];
            //ExtremeHiLo = new MaxLoValue(_PeriodLength.ValueInt);
            // _CVi.v = 0; _CVi.v1 = 0; //idx = 1;
            mAVolume = new MA(_PeriodLength.ValueInt, _MAType.ValueString);
           // mASlow = new MA(_lengthSlow.ValueInt, _MAType.ValueString);

        }
    }
}
