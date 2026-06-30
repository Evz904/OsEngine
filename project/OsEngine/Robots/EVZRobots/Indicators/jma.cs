using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;


namespace CustomIndicators.Scripts.EVZRobots
{


    [IndicatorAttribute("Jma")]
public class Jma : Aindicator
{
    private IndicatorParameterInt _iPeriod;
    private IndicatorParameterInt _iPhase;
    private IndicatorParameterString _candlePoint;
    //private IndicatorParameterBool _bColor;

    private IndicatorDataSeries _seriesJMA;

    public JMABase jJMABase;

        public Jma()
        {
            jJMABase = new JMABase();
            _iPeriod = CreateParameterInt("Period", 5);
            _iPhase = CreateParameterInt("iPhase", 5);
            _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
            //_bColor = CreateParameterBool("Color", false);
        }

        public override void OnProcess(List<Candle> source, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }
            else
            {
                Candle lastCandle = source[index];
                if (lastCandle.State == CandleState.Finished)
                {
                    decimal dValue = jJMABase.JJMASeries(lastCandle.GetPoint(_candlePoint.ValueString));
                    if (index > _iPeriod.ValueInt)
                    {
                        _seriesJMA.Values[index] = dValue;
                    }
                }
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

            //if (jJMABase != null)
            //{
            //    jJMABase.InitData();
            //}

            jJMABase.UpdateCoefficients(_iPhase.ValueInt, _iPeriod.ValueInt);
            _seriesJMA = CreateSeries("JMA", Color.Orange, IndicatorChartPaintType.Line, true);
            //_seriesMedium = CreateSeries("Ma medium", Color.DarkCyan, IndicatorChartPaintType.Line, true);
            //_seriesSlow = CreateSeries("Ma slow", Color.BlueViolet, IndicatorChartPaintType.Line, true);

            //_priceChangeHighAverageTemp = 0;
            //_priceChangeLowAverageTemp = 0;
            //_VolumeAverageTemp = 0;
        }
    }
}
