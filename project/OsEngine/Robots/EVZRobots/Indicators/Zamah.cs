using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;

namespace CustomIndicators.Scripts.EVZRobots
{
    [Indicator("Zamah")]
    public class Zamah : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterString _MAType;

        private IndicatorParameterInt _lengthForRetrospective;
        private IndicatorParameterBool _StdDevOnOff;

        private IndicatorParameterDecimal _triggerLevelParametr;
        private IndicatorParameterDecimal _deviation;

        private IndicatorDataSeries _seriesBulls;
        private IndicatorDataSeries _seriesBears;
        private IndicatorDataSeries _seriesTriggerLevelBulls;
        private IndicatorDataSeries _seriesTriggerLevelBears;

        private decimal _a;
        private Values3 _vBulls;
        private Values3 _vBears;
        private decimal _triggerLevelBulls;
        private decimal _triggerLevelBears;
        
        private decimal[] _candlesUp;
        private decimal[] _candlesDown;
        private decimal[] _stddevBulls;
        private decimal[] _stddevBears;

        public decimal upLine;
        public decimal downLine;
        private decimal upLine2;
        private decimal downLine2;


        private MA mABulls;
        private MA mABears;
        //private decimal _deviation;

        public Zamah()
        {
            _length = CreateParameterInt("Length", 6);
            _MAType = CreateParameterStringCollection("MA Type", "EMA", ["EMA", "SMA", "VMA"]);
            _lengthForRetrospective = CreateParameterInt("Length for retrospective", 48);
            _triggerLevelParametr = CreateParameterDecimal("Trigger level", 0.1m);
            _deviation = CreateParameterDecimal("Deviation", 2.00m);
            _StdDevOnOff = CreateParameterBool("Std dev On/Off", false);

            _vBulls = new Values3(0, 0, 0);
            _vBears = new Values3(0, 0, 0);
        }

        public IndicatorDataSeries SeriesBulls { get => _seriesBulls; set => _seriesBulls = value; }
        public IndicatorDataSeries SeriesBears { get => _seriesBears; set => _seriesBears = value; }
        public IndicatorDataSeries SeriesTriggerLevelBulls { get => _seriesTriggerLevelBulls; set => _seriesTriggerLevelBulls = value; }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                InitParametrs();
            }
            else if (index > 0)
            {
                Candle candle = candles[index];

                if (candle.State == CandleState.Finished)
                {
                    decimal dBullZamah = 100.0m * candle.High / candle.Open - 100.0m;
                    decimal dBearZamah = 100.0m - 100.0m * candle.Low / candle.Open;
                    _vBulls.SlashValue();
                    _vBulls.v = _vBulls.v1 + _a * (dBullZamah - _vBulls.v1);
                    _vBears.SlashValue();
                    _vBears.v = _vBears.v1 + _a * (dBearZamah - _vBears.v1);
                    decimal vValue = mABulls.CalcMA(_vBulls.v);
                    _seriesBulls.Values[index] = vValue;// dBullZamah;
                    vValue = mABears.CalcMA(_vBears.v);
                    _seriesBears.Values[index] = vValue;// dBearZamah;
                    if (_StdDevOnOff)
                    {
                        int _indUp = index % _lengthForRetrospective;
                        int _indDown = index % _lengthForRetrospective;
                        int _indstdDev = index % _lengthForRetrospective;
                        _stddevBulls[_indstdDev] = _vBulls.v;
                        _stddevBears[_indstdDev] = _vBears.v;
                        if (index > _lengthForRetrospective)
                        {
                            decimal stdDevBulls = MathLibEVZ.stddev(_stddevBulls, _lengthForRetrospective);
                            decimal stdDevBears = MathLibEVZ.stddev(_stddevBears, _lengthForRetrospective);

                            //_candlesUp[_indUp] = _deviation * stdDevBulls;// lastFastChange;
                            //_candlesDown[_indDown] = _deviation * stdDevBears;//lastFastChange;
                            //CalculatePriceChannelTrailingLevel(_candlesUp, _candlesDown, index);
                            _seriesTriggerLevelBulls.Values[index] = _deviation * stdDevBulls;//upLine;
                            _seriesTriggerLevelBears.Values[index] = _deviation * stdDevBears;// downLine;
                                                                                              //if (index > _lengthUp)
                                                                                              //{
                                                                                              //    _LineUp.Values[index] = upLine;
                                                                                              //}
                                                                                              //if (index > _lengthDown)
                                                                                              //{
                                                                                              //    _LineDown.Values[index] = downLine;
                                                                                              //}
                        }
                    }
                    else
                    {
                        _seriesTriggerLevelBulls.Values[index] = _triggerLevelBulls;
                    }

                }//_seriesTriggerLevelBulls.Values[index] = _triggerLevelBulls;
            }
        }

        public override void OnStateChange(IndicatorState state)
        {
            InitParametrs();
        }

        private void InitParametrs()
        {
            _a = Math.Round(2.0m / (_length.ValueInt + 1.0m), 8);
            _seriesBulls = CreateSeries("Bulls zamah", Color.LightBlue, IndicatorChartPaintType.Line, true);
            _seriesBears = CreateSeries("Bears zamah", Color.Red, IndicatorChartPaintType.Line, true);

            _seriesTriggerLevelBulls = CreateSeries("Trigger Bulls level", Color.MediumBlue, IndicatorChartPaintType.Line, true);
            
            _triggerLevelBulls = _triggerLevelParametr;
            _triggerLevelBears = 0;
           
            _vBulls.v = 0;
            _vBulls.v1 = 0;
            _vBulls.v2 = 0;
            _vBears.v = 0;
            _vBears.v1 = 0;
            _vBears.v2 = 0;
            mABulls = new MA(_length.ValueInt, _MAType.ValueString);
            mABears = new MA(_length.ValueInt, _MAType.ValueString);
            if (_StdDevOnOff)
            {
                _seriesTriggerLevelBears = CreateSeries("Trigger Bears level", Color.OrangeRed, IndicatorChartPaintType.Line, true);
                _candlesUp = new decimal[_lengthForRetrospective];
                _candlesDown = new decimal[_lengthForRetrospective];
                _stddevBulls = new decimal[_lengthForRetrospective];
                _stddevBears = new decimal[_lengthForRetrospective];
            }

        }
        private void CalculatePriceChannelTrailingLevel(decimal[] candlesUp, decimal[] candlesDown, int index)
        {

            upLine2 = upLine;
            downLine2 = downLine;
            upLine = 0;
            downLine = decimal.MaxValue;

            if (index - _lengthForRetrospective.ValueInt > 0)
            {
                for (int i = index; i > -1 && i > index - _lengthForRetrospective.ValueInt; i--)
                {
                    int _indUp = i % _lengthForRetrospective;
                    if (upLine < candlesUp[_indUp])
                    {
                        upLine = candlesUp[_indUp];
                    }
                }
            }

            if (index - _lengthForRetrospective.ValueInt > 0)
            {
                downLine = decimal.MaxValue;

                for (int i = index; i > -1 && i > index - _lengthForRetrospective.ValueInt; i--)
                {
                    int _indDown = i % _lengthForRetrospective;
                    if (downLine > candlesDown[_indDown])
                    {
                        downLine = candlesDown[_indDown];
                    }
                }
            }
        }


    }

}

//Zamah