using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace CustomIndicators.Scripts.EVZRobots
{
    [Indicator("DSMA")]
    public class DSMA : Aindicator
    {
        private IndicatorParameterInt _Period;

        private IndicatorParameterString _candlePoint;

        private IndicatorDataSeries _series;
        private decimal a1 = 0;
        private decimal b1 = 0;
        private decimal c2 = 0;
        private decimal c3 = 0;
        private decimal c1 = 0;
        private decimal alpha1 = 0;
        private decimal ScaledFilt = 0;

        public struct _Values3
        {
            public decimal v, v1, v2;

            public _Values3(decimal v, decimal v1, decimal v2)
            {
                this.v = v;
                this.v1 = v1;
                this.v2 = v2;
            }
            public void SlashValue()
            {
                v2 = v1;
                v1 = v;
            }
        };

        _Values3 Zeros;
        int m_arraysize = 0;
        decimal[] Filt;
        _Values3 itemsDSMA;

        private decimal RMS = 0;

        public override void OnProcess(List<Candle> candles, int index)
        {
            decimal result = 0;
            Candle lastCandle = candles[index];
            if (index == 1)
                {
                m_arraysize = _Period.ValueInt;// +32;
                    Filt = new decimal[m_arraysize];
                    a1 = (decimal)Math.Exp(-1.414 * 3.14159 /  (0.5 * _Period.ValueInt));//_Period.ValueInt); //
                    b1 = 2 * a1 * (decimal)Math.Cos(1.414 * 180 / (0.5 * _Period.ValueInt)); //_Period.ValueInt); //
                    c2 = b1;
                    c3 = -(a1 * a1);
                    c1 = 1 - c2 - c3;
                     
                    //Filt.Insert(Filt.Count - 1, 0);
            }
            else if (index > 1)
                {
                    int _indC = index % m_arraysize;
                    int _indP = (index - 1) % m_arraysize;
                    int _indP2 = (index - 2) % m_arraysize;

                // Выдает номинальное нулевое среднее значение с нулями в характеристике
                // передачи при постоянном токе и Найквисте без спектральных искажений
                // Номинально отбеливает спектр из-за спада в 6 дБ на октаву 

                    Candle Candle2 = index>2 ? candles[index - 3] : candles[index - 2];

                    Zeros.v = lastCandle.GetPoint(_candlePoint.ValueString) - Candle2.GetPoint(_candlePoint.ValueString);//lastCandle.Close - Candle2.Close;
                //SuperSmoother Filter
                Filt[_indC] = c1 * (Zeros.v + Zeros.v1) / 2 + c2 * Filt[_indP] + c3 * Filt[_indP2];
               //Вычислить стандартное отклонение 
                RMS = 0;
                if (index > _Period.ValueInt) 
                { 
                    for (int k = 0; k < _Period.ValueInt-1 ; k++)//&& index >= k
                    {
                        try
                        {
                            //RMS += (Filt[_indC - k] * Filt[_indC - k]);
                            RMS += Filt[k] * Filt[k];
                        }
                        catch (Exception)
                        {

                            throw;
                        } 
                    
                    }
                         RMS = (decimal)Math.Sqrt((double)(RMS / _Period.ValueInt));
                        //Масштабировать Filt в терминах стандартных отклонений 
                        if (RMS != 0)
                        {
                            ScaledFilt = Filt[_indC] / RMS;
                        } 
                        else
                        { 
                            ScaledFilt = 1; 
                        }

                        alpha1 = Math.Abs(ScaledFilt) * 5 / _Period.ValueInt;
                        itemsDSMA.v = alpha1 * lastCandle.GetPoint(_candlePoint.ValueString) + (1 - alpha1) * itemsDSMA.v1; //alpha1 * lastCandle.Close + (1 - alpha1) * itemsDSMA.v1;

                }
                else 
                {
                    itemsDSMA.v = (lastCandle.GetPoint(_candlePoint.ValueString) + Candle2.GetPoint(_candlePoint.ValueString))/2; // Close;
                     
                }
                result = Math.Round(itemsDSMA.v, 8);
                itemsDSMA.SlashValue();
                Zeros.SlashValue();
                   // SlashValue(Filt);
                    
                }
            //result = (lastCandle.Close + lastCandle.Open) / 2;
            _series.Values[index] = result;
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _Period = CreateParameterInt("Period", 40);
                _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
                _series = CreateSeries("DSMA", Color.DarkRed, IndicatorChartPaintType.Line, true);
                
                
                Zeros = new _Values3(0, 0, 0);
                itemsDSMA = new _Values3(0, 0, 0);
                RMS = 0;
            }
            if (state == IndicatorState.Dispose) 
            {
 
            }
        }
    }
}
