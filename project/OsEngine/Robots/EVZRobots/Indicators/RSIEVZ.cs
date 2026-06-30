using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Robots.EVZRobots.lib;

namespace CustomIndicators.Scripts.EVZRobots
{
    [Indicator("RSI3RAY")]
    public class RSI3RAY : Aindicator
    {
        private IndicatorParameterInt _lengthFast;
        private IndicatorParameterInt _lengthMedium;
        private IndicatorParameterInt _lengthSlow;
        public IndicatorParameterBool bByVolume;

        private IndicatorParameterString _candlePoint;

        public IndicatorDataSeries _seriesFast;
        public IndicatorDataSeries _seriesMedium;
        public IndicatorDataSeries _seriesSlow;

        private SortedList<int, IndicatorDataSeries> _Indicators;

        private decimal _priceChangeHighAverageTemp;
        private decimal _priceChangeLowAverageTemp;

        private decimal _VolumeAverageTemp;


        private List<int> mIndexIndicators;
        private SortedList<int, Values3> _vPriseChangeHigh;
        private SortedList<int, Values3> _vPriseChangeLow;

        private SortedList<int, Values3> _vPriseChangeHighAverage;
        private SortedList<int, Values3> _vPriseChangeLowAverage;
        private SortedList<int, decimal> _m_ACoef;
        private SortedList<int, Values3> _v_RSI;

        Values3 vRSIFast;
        Values3 vRSILow;
        Values3 vRSIMedium;
        Values3 vDirectionRSI;

        public RSI3RAY()
        {
            _lengthFast = CreateParameterInt("Length fast", 6);
            _lengthMedium = CreateParameterInt("Length medium", 14);
            _lengthSlow = CreateParameterInt("Length slow", 24);
            bByVolume = CreateParameterBool("By volume", false);
            _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
        }

        public override void OnStateChange(IndicatorState state)
        {
            InitParametrs();
        }

        private void InitParametrs()
        {
            if(_vPriseChangeHigh != null)
            {
                _vPriseChangeHigh.Clear();
            }
            _vPriseChangeHigh = new SortedList<int, Values3>();
            _vPriseChangeHigh.Add(_lengthFast.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeHigh.Add(_lengthMedium.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeHigh.Add(_lengthSlow.ValueInt, new Values3(0, 0, 0));

            if (_vPriseChangeLow != null)
            {
                _vPriseChangeLow.Clear();
            }
            _vPriseChangeLow = new SortedList<int, Values3>
            {
                { _lengthFast.ValueInt, new Values3(0, 0, 0) },
                { _lengthMedium.ValueInt, new Values3(0, 0, 0) },
                { _lengthSlow.ValueInt, new Values3(0, 0, 0) }
            };
            if (_vPriseChangeHighAverage != null)
            {
                _vPriseChangeHighAverage.Clear();
            }
            _vPriseChangeHighAverage = new SortedList<int, Values3>();
            _vPriseChangeHighAverage.Add(_lengthFast.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeHighAverage.Add(_lengthMedium.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeHighAverage.Add(_lengthSlow.ValueInt, new Values3(0, 0, 0));
            _m_ACoef = new SortedList<int, decimal>();
            _v_RSI = new SortedList<int, Values3>();
            _v_RSI.Add(_lengthFast.ValueInt, new Values3(0, 0, 0));
            _v_RSI.Add(_lengthMedium.ValueInt, new Values3(0, 0, 0));
            _v_RSI.Add(_lengthSlow.ValueInt, new Values3(0, 0, 0));

            mIndexIndicators = new List<int>(3);
            foreach (int iKey in _vPriseChangeHighAverage.Keys) 
            {
                mIndexIndicators.Add(iKey);
                _m_ACoef.Add(iKey, 2.0m / (iKey * 2));
            }

            _vPriseChangeLowAverage = new SortedList<int, Values3>();
            _vPriseChangeLowAverage.Add(_lengthFast.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeLowAverage.Add(_lengthMedium.ValueInt, new Values3(0, 0, 0));
            _vPriseChangeLowAverage.Add(_lengthSlow.ValueInt, new Values3(0, 0, 0));

            _seriesFast = CreateSeries("Ma fast", Color.Orange, IndicatorChartPaintType.Line, true);
            _seriesMedium = CreateSeries("Ma medium", Color.DarkCyan, IndicatorChartPaintType.Line, true);
            _seriesSlow = CreateSeries("Ma slow", Color.BlueViolet, IndicatorChartPaintType.Line, true);

            _priceChangeHighAverageTemp = 0;
            _priceChangeLowAverageTemp  = 0;
            _VolumeAverageTemp = 0;
        }

        public override void OnProcess(List<Candle> candles, int index)
        {

            decimal rsi = 100;
            if(index==0)
            {
                InitParametrs();
                _Indicators = new SortedList<int, IndicatorDataSeries>
                {
                    { _lengthFast.ValueInt, _seriesFast },
                    { _lengthMedium.ValueInt, _seriesMedium },
                    { _lengthSlow.ValueInt, _seriesSlow }
                };
            }
            Candle lastCandle = candles[index];
            if (lastCandle.State == CandleState.Finished)
            {
                if (index > 0)
                {
                    decimal dPriceChangeHigh;
                    decimal dPriceChangeLow;
                    decimal dDelta = candles[index].GetPoint(_candlePoint.ValueString) - candles[index - 1].GetPoint(_candlePoint.ValueString);// candles[index].Close - candles[index - 1].Close;
                    decimal dVolume = bByVolume.ValueBool == true ? candles[index].Volume : 1;
                    if (dDelta > 0)
                    {
                        dPriceChangeHigh = dDelta;
                        dPriceChangeLow = 0;
                    }
                    else
                    {
                        dPriceChangeHigh = 0;
                        dPriceChangeLow = Math.Abs(dDelta);
                    }

                    bool bStepOneCalculated = false;
                    bool bStepTwoCalculated = false;
                    for (int iKey = 0; iKey < mIndexIndicators.Count; iKey++)
                    {
                        int iActualLenght = mIndexIndicators[iKey];
                        if (index < iActualLenght - 1 && bStepOneCalculated == false)
                        {
                            _priceChangeHighAverageTemp += dPriceChangeHigh * dVolume;
                            _priceChangeLowAverageTemp += dPriceChangeLow * dVolume;
                            _VolumeAverageTemp += _VolumeAverageTemp * dVolume;
                            bStepOneCalculated = true;
                        }
                        else if (index == iActualLenght - 1 && bStepTwoCalculated == false)
                        {
                            decimal _priceChangeHighAverageTemp2 = _priceChangeHighAverageTemp + dPriceChangeHigh * dVolume;
                            decimal _priceChangeLowAverageTemp2 = _priceChangeLowAverageTemp + dPriceChangeLow * dVolume;
                            _VolumeAverageTemp += _VolumeAverageTemp * dVolume; ;

                            _priceChangeHighAverageTemp2 /= iActualLenght;// * _VolumeAverageTemp;
                            _priceChangeLowAverageTemp2 /= iActualLenght;// * _VolumeAverageTemp;

                            _vPriseChangeHighAverage[iActualLenght].SetV(_priceChangeHighAverageTemp2);
                            _vPriseChangeLowAverage[iActualLenght].SetV(_priceChangeLowAverageTemp2);
                            bStepTwoCalculated = true;
                        }
                        else if (index > iActualLenght - 1)
                        {
                            decimal _a = _m_ACoef[iActualLenght];
                            _vPriseChangeHighAverage[iActualLenght].SlashValue();
                            _vPriseChangeLowAverage[iActualLenght].SlashValue();

                            _vPriseChangeHighAverage[iActualLenght].SetV(_a * dPriceChangeHigh * dVolume + (1 - _a) * _vPriseChangeHighAverage[iActualLenght].v1);
                            _vPriseChangeLowAverage[iActualLenght].SetV(_a * dPriceChangeLow * dVolume + (1 - _a) * _vPriseChangeLowAverage[iActualLenght].v1);

                            //if (Math.Round(_vPriseChangeHighAverage[iActualLenght].v, 0) != 0.0m && Math.Round(_vPriseChangeLowAverage[iActualLenght].v, 8) != 0.0m)
                            if (Math.Round(_vPriseChangeLowAverage[iActualLenght].v, 8) != 0.0m)
                            {
                                rsi = 100 - 100 / (1 + _vPriseChangeHighAverage[iActualLenght].v / _vPriseChangeLowAverage[iActualLenght].v);//100 - (100 / (1 + (val_Up / val_Down)))
                            }
                            decimal a = Math.Round(2.0m / (iActualLenght + 1), 8);
                            decimal emaLast = _v_RSI[iActualLenght].v;// _series.Values[index - 1];
                                                                      //decimal p = candles[index].GetPoint(_candlePoint.ValueString);
                            decimal result = emaLast + a * (rsi - emaLast);
                            _v_RSI[iActualLenght].SlashValue();
                            _v_RSI[iActualLenght].SetV(result);
                            IndicatorDataSeries _series = _Indicators[iActualLenght];
                            // _series.Values[index] = Math.Round(rsi, 2);
                            _series.Values[index] = Math.Round(result, 2);
                        }
                    }
                }
            }
            
        }

    }
}