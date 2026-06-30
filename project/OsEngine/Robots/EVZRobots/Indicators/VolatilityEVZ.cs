using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;



namespace CustomIndicators.Scripts.EVZRobots
{
    [IndicatorAttribute("VolatilityEVZ")]
    public class VolatilityEVZ : Aindicator
    {

        private IndicatorParameterInt _lengthFast;
        private IndicatorParameterInt _lengthSlow;
        private IndicatorParameterDecimal _deviation;
        private IndicatorParameterString _MAType;
        private IndicatorParameterBool _SmoothStdDev;
        //private IndicatorParameterDecimal _levelUp;
        //private IndicatorParameterDecimal _levelDown;

        //public IndicatorParameterInt _lengthUp;
        //public IndicatorParameterInt _lengthDown;



        //private IndicatorDataSeries _seriesSlow;
        //private IndicatorDataSeries _seriesFast;

        //private IndicatorDataSeries _seriesSlowMA;
        //private IndicatorDataSeries _seriesFastMA;
        public IndicatorDataSeries _seriesFastVolatilityChangeMA;
        public IndicatorDataSeries _seriesSlowVolatilityChangeMA;
        private IndicatorDataSeries _seriesFastVolatilityCandleMA;
        private IndicatorDataSeries _seriesSlowVolatilityCandleMA;
        public IndicatorDataSeries _seriesFastVolatilityCandle;

        public IndicatorDataSeries _LineUp;
        public IndicatorDataSeries _LineDown;

        private decimal lastMoving, lastMoving2;

        private MA mAFast;
        private MA mAFastChange;
        private MA mASlowChange;
        
        private MA mASlow;

        private MA mAFastVolatilityCandle;
        private MA mASlowVolatilityCandle;
        //private decimal[] _candlesUp;
        //private decimal[] _candlesDown;
        private decimal[] _stddevSlow;
        private decimal[] _stddevSmooth;
        private int _candleIndexUp;
        private int _candleIndexDown;
        private decimal upLine;
        private decimal downLine;
        private decimal upLine2;
        private decimal downLine2;

        private MaxLoValue ExtremeHiLo;
        private MaxLoValue ExtremeHiLoSlow;
        private decimal lastFastChange2;

        public VolatilityEVZ()
        {
            _lengthFast = CreateParameterInt("Length Fast", 7);
            _lengthSlow = CreateParameterInt("Length Slow", 48);
            

            //_levelUp = CreateParameterDecimal("Level Up", 0.1m);
            //_levelDown = CreateParameterDecimal("Level Down", -0.1m);
            //_lengthUp = CreateParameterInt("Length up", 9);
            //_lengthDown = CreateParameterInt("Length down", 9);
            _deviation = CreateParameterDecimal("Deviation", 2);

            //_candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
            _MAType = CreateParameterStringCollection("MA Type", "EMA", ["EMA", "SMA", "VMA"]);
            _SmoothStdDev = CreateParameterBool("_Smooth std dev", false);
            //_seriesSlowMA = CreateSeries("SlowVolatilityMA", Color.DarkSeaGreen, IndicatorChartPaintType.Line, true);
            //_seriesFastMA = CreateSeries("FastVolatilityMA", Color.MediumSeaGreen, IndicatorChartPaintType.Line, true);
            _seriesFastVolatilityChangeMA = CreateSeries("FastVolatilityChangeMA", Color.LightBlue, IndicatorChartPaintType.Line, true);
            _seriesSlowVolatilityChangeMA = CreateSeries("SlowVolatilityChangeMA", Color.Blue, IndicatorChartPaintType.Line, true);
            _LineUp = CreateSeries("Line Up", Color.DarkGreen, IndicatorChartPaintType.Line, true);
            _LineDown = CreateSeries("Line Down", Color.DarkRed, IndicatorChartPaintType.Line, true);
            // _seriesSlow = CreateSeries("SlowVolatility", Color.MediumSeaGreen, IndicatorChartPaintType.Line, false);
            //_seriesFast = CreateSeries("FastVolatility", Color.Snow, IndicatorChartPaintType.Line, true);
            _seriesFastVolatilityCandle = CreateSeries("FastVolatilityCandle", Color.GreenYellow, IndicatorChartPaintType.Line, false);
            _seriesFastVolatilityCandleMA = CreateSeries("FastVolatilityCandleMA", Color.Orchid, IndicatorChartPaintType.Line, false);
            _seriesSlowVolatilityCandleMA = CreateSeries("SlowVolatilityCandleMA", Color.Honeydew, IndicatorChartPaintType.Line, false);
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }

            Candle candle = candles[index];
            if (candle.State == CandleState.Finished)
            {
                ExtremeHiLo.setValue(candles, index);
                ExtremeHiLoSlow.setValue(candles, index);
                lastMoving = mAFast.CalcMA(ExtremeHiLo.Value);
                lastMoving2 = mASlow.CalcMA(ExtremeHiLo.Value);
                decimal lastFastChange = mAFastChange.CalcMA(ExtremeHiLo.Value1);
                decimal lastSlowChange = mASlowChange.CalcMA(ExtremeHiLoSlow.Value1);
                decimal lastFastVolatilityCandle = mAFastVolatilityCandle.CalcMA(ExtremeHiLo.Value2);
                decimal lastSlowVolatilityCandle = mASlowVolatilityCandle.CalcMA(ExtremeHiLo.Value2);
                //int _indUp = index % _lengthUp;
                //int _indDown = index % _lengthDown;
                int _indstdDevSlow = index % _lengthSlow;
                int _indstdDevSmoothFast = index % _lengthFast;
                _stddevSlow[_indstdDevSlow] = ExtremeHiLoSlow.Value1;
                _stddevSmooth[_indstdDevSmoothFast] = Math.Abs(lastFastChange - lastFastChange2);
                //if (index > _lengthSlow)
                //{
                //_candlesUp[_indUp] = _deviation*stdDev / 2;// lastFastChange;
                //_candlesDown[_indDown] = -_deviation * stdDev / 2;//lastFastChange;
                //CalculatePriceChannelTrailingLevel(_candlesUp, _candlesDown, index);
                //if (index > _lengthUp)
                //{
                //    _LineUp.Values[index] = upLine;
                //}
                //if (index > _lengthDown)
                //{
                //    _LineDown.Values[index] = downLine;
                //}
                //}

                if (index > _lengthFast.ValueInt)
                {

                    //_seriesSlowMA.Values[index] = lastMoving2;
                    //  _seriesSlow.Values[index] = ExtremeHiLo.Value;

                    //_seriesFastMA.Values[index] = lastMoving;
                    //_seriesFast.Values[index] = ExtremeHiLo.Value;
                    _seriesFastVolatilityChangeMA.Values[index] = lastFastChange;// ExtremeHiLo.Value1;// 

                    _seriesFastVolatilityCandle.Values[index] = ExtremeHiLo.Value1;//ExtremeHiLo.Value2;
                    _seriesFastVolatilityCandleMA.Values[index] = lastFastVolatilityCandle;

                }
                if (index > _lengthSlow.ValueInt)
                {
                    decimal stdDev = MathLibEVZ.stddev(_stddevSlow, _lengthSlow);

                    decimal smoothFilter = 1;

                    if (_SmoothStdDev)
                    {
                        smoothFilter = 1 / (1 + MathLibEVZ.Avg(_stddevSmooth, _lengthFast));
                    }
                    //_LineUp.Values[index] = lastSlowChange + _deviation * stdDev * smoothFilter;//upLine;
                    //_LineDown.Values[index] = lastSlowChange - _deviation * stdDev * smoothFilter;//downLine;
                    _LineUp.Values[index] = _deviation * stdDev * smoothFilter;//upLine;
                    _LineDown.Values[index] = - _deviation * stdDev * smoothFilter;//downLine;


                    _seriesSlowVolatilityChangeMA.Values[index] = lastSlowChange; //  ExtremeHiLoSlow.Value1;//
                    _seriesSlowVolatilityCandleMA.Values[index] = lastSlowVolatilityCandle;
                }
            }
            //lastMoving22 = lastMoving21;
            //if (index < _lengthFast.ValueInt)
            //{
            //    lastMoving += ExtremeHiLo.Value;
            //    lastMoving21 += ExtremeHiLo.Value2;

            //}
            //else if(index == _lengthFast.ValueInt)
            //{
            //    lastMoving += ExtremeHiLo.Value;
            //    lastMoving /= _lengthFast.ValueInt;
            //    lastMoving21 += ExtremeHiLo.Value2;
            //    lastMoving21 /= _lengthFast.ValueInt;
            //}
            //else if (index > _lengthFast.ValueInt)
            //{
            //    lastMoving = lastMoving2 + _a * (ExtremeHiLo.Value - lastMoving2);
            //    seriesSlowMA.Values[index] = lastMoving;
            //    _seriesSlow.Values[index] = ExtremeHiLo.Value;
            //    lastMoving21 = lastMoving22 + _a * (ExtremeHiLo.Value2 - lastMoving22);
            //    _seriesFastMA.Values[index] = lastMoving21;
            //    _seriesFast.Values[index] = ExtremeHiLo.Value2;

            //}
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
            //_a = 2.0m / (_lengthFast.ValueInt * 2);
            upLine = 0;
            upLine2 = 0;

            downLine = 0;
            downLine2 = 0;

            lastMoving = 0;
            lastMoving2 = 0;
            //lastMoving21 = 0;
            //lastMoving22 = 0;
           //_CVint = new decimal[_lengthFast.ValueInt];
            ExtremeHiLo = new MaxLoValue(_lengthFast.ValueInt);
            ExtremeHiLoSlow = new MaxLoValue(_lengthSlow.ValueInt);
            // _CVi.v = 0; _CVi.v1 = 0; //idx = 1;
            mAFast = new MA(_lengthFast.ValueInt, _MAType.ValueString);
            mAFastChange = new MA(_lengthFast.ValueInt, _MAType.ValueString);
            mASlow = new MA(_lengthSlow.ValueInt, _MAType.ValueString);
            mASlowChange = new MA(_lengthSlow.ValueInt, _MAType.ValueString);
            //_candlesUp = new decimal[_lengthUp];
            //_candlesDown = new decimal[_lengthDown];
            _stddevSlow = new decimal[_lengthSlow];
            _stddevSmooth = new decimal[_lengthFast];
            lastFastChange2 = 0;
            _candleIndexDown = 0;
            _candleIndexUp = 0;
            mAFastVolatilityCandle = new MA(_lengthFast.ValueInt, _MAType.ValueString);
            mASlowVolatilityCandle = new MA(_lengthSlow.ValueInt, _MAType.ValueString);
        }
        //private void CalculatePriceChannelTrailingLevel(decimal[] candlesUp, decimal[] candlesDown, int index)
        //{
            
        //    upLine2 = upLine;
        //    downLine2 = downLine;
        //    upLine = 0;
        //    downLine = decimal.MaxValue;

        //    if (index - _lengthUp.ValueInt > 0)
        //    {
        //        for (int i = index; i > -1 && i > index - _lengthUp.ValueInt; i--)
        //        {
        //            int _indUp = i % _lengthUp;
        //            if (upLine < candlesUp[_indUp])
        //            {
        //                upLine = candlesUp[_indUp];
        //            }
        //        }
        //    }

        //    if (index - _lengthDown.ValueInt > 0)
        //    {
        //        downLine = decimal.MaxValue;

        //        for (int i = index; i > -1 && i > index - _lengthDown.ValueInt; i--)
        //        {
        //            int _indDown = i % _lengthDown;
        //            if (downLine > candlesDown[_indDown])
        //            {
        //                downLine = candlesDown[_indDown];
        //            }
        //        }
        //    }
        //}


    }
}
