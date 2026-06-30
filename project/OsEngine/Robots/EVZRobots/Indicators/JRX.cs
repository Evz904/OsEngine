using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;


namespace CustomIndicators.Scripts.EVZRobots
{
    [IndicatorAttribute("JRX")]
    public class JRX : Aindicator
    {
        private IndicatorParameterInt _iPeriod;
        private IndicatorParameterString _candlePoint;
        private IndicatorParameterBool _bColor;

        private IndicatorDataSeries _seriesJRX;

        public JRXBase jJRXBase;
        public JRX()
        {
            jJRXBase = new JRXBase(5);
            _iPeriod = CreateParameterInt("Period", 5);
            //  _iPhase = CreateParameterInt("iPhase", 5);
            _bColor = CreateParameterBool("Color", false);

            _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
        }
        public override void OnProcess(List<Candle> source, int index)
        {
            Candle lastCandle = source[index];
            if (index == 0)
            {
                InitParametrs();

            };

            _seriesJRX.Values[index] = jJRXBase.JRXSeries(lastCandle.GetPoint(_candlePoint.ValueString), index);
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
            jJRXBase.InitData();
            jJRXBase.UpdateCoefficients(_iPeriod.ValueInt);
            _seriesJRX = CreateSeries("JMX", Color.OliveDrab, IndicatorChartPaintType.Line, true);
        }
    }
}
