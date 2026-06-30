using OsEngine.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using static QuikSharp.CandleFunctions;

namespace OsEngine.Robots.EVZRobots.lib
{
    public static class MathLibEVZ
    {
        public static decimal Avg(decimal[] Xi, int W) 
        { 
            decimal vAvg = 0;
            
            for (int i = 0; i < W; i++)
            {
                vAvg += Xi[i];
            }
            vAvg /= W;
            return vAvg; 
        }
        public static decimal Avg(List<decimal> Xi, int W)
        {
            decimal vAvg = 0;

            for (int i = 0; i < W; i++)
            {
                vAvg += Xi[i];
            }
            vAvg /= W;
            return vAvg;
        }
        public static decimal Avg(List<Candle> Xi, int W, string candlePoint)
        {
            decimal vAvg = 0;

            for (int i = 0; i < W; i++)
            {
                vAvg += GetPoint(Xi[i], candlePoint);
            }
            vAvg /= W;
            return vAvg;
        }

        public static decimal stddev(decimal[] Xi, int W)
        {
            decimal vAvg = Avg(Xi, W);
            decimal vStdDev = 0;
            for (int i = 0;i < W; i++)
            {
                //vStdDev += (vAvg - Xi[i]) * (vAvg - Xi[i]);
                vStdDev += Xi[i] * Xi[i];
            }

            vStdDev /= W;
            vStdDev = (decimal)Math.Sqrt((double)(vStdDev + vAvg*vAvg));

            return vStdDev;
        }
        private static decimal GetPoint(Candle candle, string candlePoint)
        {
            if (candlePoint == "Close")
            {
                return candle.Close;
            }
            if (candlePoint == "High")
            {
                return candle.High;
            }
            if (candlePoint == "Low")
            {
                return candle.Low;
            }
            if (candlePoint == "Open")
            {
                return candle.Open;
            }
            if (candlePoint == "Median")
            {
                return (candle.High + candle.Low) / 2;
            }
            if (candlePoint == "Typical")
            {
                return (candle.High + candle.Low + candle.Close) / 3;
            }
            return 0;
        }
    }
    public class vDecimal2
    {
        public Decimal v, v1;

        public vDecimal2()
        {
            this.v = 0;
            this.v1 = 0;
        }
        public vDecimal2(decimal v, decimal v1)
        {
            this.v = v;
            this.v1 = v1;
        }
        public void SetV(Decimal pV)
        {
            v = pV;
        }
        public void SlashValue()
        {
            v1 = v;
        }

        //public implicit operator vDecimal2(int v) => (vDecimal2)(this.v = v);
    }
    public class Values3
    {
        public decimal v, v1, v2;

        public Values3(decimal v, decimal v1, decimal v2)
        {
            this.v = v;
            this.v1 = v1;
            this.v2 = v2;
        }
        public void SetV(decimal pV)
        {
            v = pV;
        }
        public void SlashValue()
        {
            v2 = v1;
            v1 = v;
        }
    };

    public class ATRCalc
    {
        public decimal MaxHi, MinLo, StreamATR;
        protected int Periods;
        protected int index;
        protected int MaxIndex, MinIndex;
        protected MaxLoValue[] Values;
        //        protected SortedList<int, MaxLoValue> Values1;
        public ATRCalc(int pPeriods)
        {
            this.Periods = pPeriods;
            this.index = 0;
            this.MaxIndex = 0;
            this.MinIndex = 0;
            this.MaxHi = 0.0m;
            this.MinLo = 0.0m;
            this.StreamATR = 0.0m;
            Values = new MaxLoValue[pPeriods];
        }
        public void AddValue(decimal pHi, decimal pLo)
        {
            if (index == 0)
            {
                MaxHi = pHi;
                MinLo = pLo;
                MaxIndex = 0;
                MinIndex = 0;
               // Values[index].setValue(MaxHi, MinLo);

            }
            else
            {
                int localIndex = index % Periods;

                if (Values[MaxIndex].MaxHi <= pHi)
                {
                    MaxIndex = localIndex;
                    Values[localIndex].MaxHi = pHi;
                }
                if (Values[MinIndex].MinLo >= pLo)
                {
                    MinIndex = localIndex;
                    Values[localIndex].MinLo = pLo;
                }
                 
                index++;
            }
        }
    };

    public class MA
    {
        protected int Period;
        protected int Index;
        protected decimal CurrenValue, CurrenValue2;
        decimal result;
        protected string MAType;
        protected Queue<decimal> Values;
        private decimal _a;
        public delegate decimal _CalcMA(decimal Value);

        public _CalcMA CalcMA;

        public MA(int period, string mAType)
        {
            Period = period;
            MAType = mAType;
            Index = 0;
            CurrenValue = 0.0m;
            result = 0.0m;

            Values = new Queue<decimal>(Period);
            _a =  2.0m / (Period * 2);
            if (MAType == "SMA")
            {
                CalcMA = SMA;
            }else if (MAType == "EMA")
            {
                CalcMA = EMA;
            }
        }
        private decimal SMA(decimal Value)
        {
           
            Values.Enqueue(Value);
            if (Index < Period)
            {
                CurrenValue += Value;
            }
            else if (Index == Period)
            {
                CurrenValue += Value;
                result = CurrenValue / Period;
            }
            else if (Index > Period)
            {
                decimal Firstvalue = Values.Dequeue();
                CurrenValue = CurrenValue - Firstvalue + Value;
                result = CurrenValue / Period;
            }
            Index++;
            return result;
        }
        private decimal EMA(decimal Value)
        {
            
            Values.Enqueue(Value);
            if (Index < Period)
            {
                CurrenValue += Value;
            }
            else if (Index == Period)
            {
                CurrenValue += Value;
                CurrenValue /= Period;
                result = CurrenValue ;
            }
            else if (Index > Period)
            {
                decimal Firstvalue = Values.Dequeue();
                
                CurrenValue = CurrenValue + _a * (Value - CurrenValue);

                result = CurrenValue;
            }
            Index++;
            return result;
        }
    }
    public class IndexAndValue
    {
        public int Index;
        public decimal Value;

        public IndexAndValue()
        {
            this.Index = 0;
            this.Value = 0.0m;
        }

        public IndexAndValue(int index, decimal value)
        {
            Index = index;
            Value = value;
        }
    }
    public class MaxLoValue
    {
        protected int Period;
        protected int Index;
        protected int CurrenIndex;

        public decimal MaxHi;
        public decimal MinLo;
        public decimal StreamMaxRange;
        //public decimal StreamOpenCloseRange;
        public decimal TotalChange;
        public decimal Value;
        public decimal Value1;
        public decimal Value2;
        //public int iMax;
        //public int iMin;
        protected Queue<decimal> RangeList;
        protected Queue<decimal> OpenList;

        protected Queue<decimal> ListMax;
        protected Queue<decimal> ListMin;

        public MaxLoValue(int pPeriod)
        {
            Value = 0.0m;
            Value1 = 0.0m;
            Value2 = 0.0m;
            Period = pPeriod;
            MaxHi = 0.0m;
            MinLo = Decimal.MaxValue;
            //iMax = 0;
            //iMin = 0;
            StreamMaxRange = 0.0m;
            TotalChange = 0.0m;
            Index = 0;
            CurrenIndex = 0;
            RangeList = new Queue<decimal>(Period);
            OpenList = new Queue<decimal>(Period);

            ListMax = new Queue<decimal>();
            ListMin = new Queue<decimal>();
        }

        public void setValue(List<Candle> candles, int index)//decimal pValue)
        {
            if (index > 0)//Index)
            {
                Candle candle2 = candles[index - 1];
                Candle candle = candles[index];
                decimal hiToLow = Math.Abs(candle.High - candle.Low);
                decimal closeToHigh = Math.Abs(candle2.Close - candle.High);
                decimal closeToLow = Math.Abs(candle2.Close - candle.Low);

                decimal tempRange = Math.Max(Math.Max(hiToLow, closeToHigh), closeToLow);

                //decimal tempRange = candle.High - candle.Low;
                RangeList.Enqueue(tempRange);
                OpenList.Enqueue(candle.Open);
                TotalChange += tempRange;

                ListMax.Enqueue(candle.High);
                ListMin.Enqueue(candle.Low);
                bool bSetlow = false;
                //if (candle.ShadowTop + candle.ShadowBottom < candle.Body)
                //{
                if (candle.Low < MinLo)
                {
                    MinLo = candle.Low;
                    //iMin = Index;
                    bSetlow = true;
                }
                //}
                //else
                //{
                //    if (candle.IsDown && candle.Close < MinLo)
                //    {
                //        MinLo = candle.Close;
                //        iMin = Index;
                //        bSetlow = true;
                //    }
                //    else if(candle.IsUp && candle.Open < MinLo)
                //    {
                //        MinLo = candle.Open;
                //        iMin = Index;
                //        bSetlow = true;

                //    }
                //}

                bool bSetHi = false;
                //if (candle.ShadowTop + candle.ShadowBottom < candle.Body)
                //{
                if (candle.High > MaxHi)
                {
                    MaxHi = candle.High;
                    //iMax = Index;
                    bSetHi = true;
                }
                //}
                //else
                //{
                //    if (candle.IsDown && candle.Open > MaxHi)
                //    {
                //        MaxHi = candle.Open;
                //        iMax = Index;
                //        bSetlow = true;
                //    }
                //    else if (candle.IsUp && candle.Close < MaxHi)
                //    {
                //        MaxHi = candle.Close;
                //        iMax = Index;
                //        bSetlow = true;

                //    }
                //}
                //decimal OpenLow = ListMin.Peek();
                if (Index >= Period)
                {
                    decimal leavingMax = ListMax.Dequeue();
                    if (leavingMax >= MaxHi && !bSetHi)
                    {
                        MaxHi = 0;
                        foreach (decimal current in ListMax)
                        {
                            if (current > MaxHi)
                            {
                                MaxHi = current;
                            }
                        }
                    }

                    decimal leavingMin = ListMin.Dequeue();
                    if (leavingMin <= MinLo && !bSetlow)
                    {
                        MinLo = Decimal.MaxValue;
                        foreach (decimal current in ListMin)
                        {
                            if (current < MinLo)
                            {
                                MinLo = current;
                            }
                        }
                    }
                    TotalChange -= RangeList.Dequeue();
                    //Value1 = Math.Abs(OpenList.Dequeue() - candle.Close);
                    Value1 = candle.Close - OpenList.Dequeue();
                    Value2 = Math.Abs(Value1);
                    
                    //StreamOpenCloseRange = Math.Abs(candles[Index].Close - candles[Index - Period].Open);
                }
                //else
                //{
                    //StreamOpenCloseRange = Math.Abs(candles[Index].Close - candles[Index % Period].Open);
                //}

                StreamMaxRange = MaxHi - MinLo;


                if (TotalChange != 0)
                {
                    Value = StreamMaxRange / TotalChange;
                    Value1 = Value1 / TotalChange;
                    Value2 = Value2 / TotalChange; // (TotalChange / Period) / StreamMaxRange;
                }
                else
                {
                    Value = 0.0m;
                    Value1 = 0.0m;
                    Value2 = 0.0m;

                }

                
            }
            Index++;
        }
        
    }
    public class JRXBase
    {
        int period = 14;
        bool color;
        //local data_type = (Fsettings.data_type or "Close")


        vDecimal2 JRSX;
        // double out;
        int begin_index, last_index;

        vDecimal2 trend;

        decimal w;
        decimal Kg;
        decimal Hg;

        vDecimal2 v8;
        vDecimal2 v8a;
        vDecimal2 f28;
        vDecimal2 f30;
        vDecimal2 f38;
        vDecimal2 f40;
        vDecimal2 f48;
        vDecimal2 f50;
        vDecimal2 f58;
        vDecimal2 f60;
        vDecimal2 f68;
        vDecimal2 f70;
        vDecimal2 f78;
        vDecimal2 f80;

        decimal c;
        decimal c1;

        public JRXBase(int pPeriod)
        {
            InitData();
        }

        public void InitData()
        {
            period = 5;

            JRSX = new vDecimal2();
            trend = new vDecimal2();
            v8 = new vDecimal2(0, 0);
            v8a = new vDecimal2(0, 0);
            f28 = new vDecimal2(0, 0);
            f30 = new vDecimal2(0, 0);
            f38 = new vDecimal2(0, 0);
            f40 = new vDecimal2(0, 0);
            f48 = new vDecimal2(0, 0);
            f50 = new vDecimal2(0, 0);
            f58 = new vDecimal2(0, 0);
            f60 = new vDecimal2(0, 0);
            f68 = new vDecimal2(0, 0);
            f70 = new vDecimal2(0, 0);
            f78 = new vDecimal2(0, 0);
            f80 = new vDecimal2(0, 0);

            c = new decimal(0);
            c1 = new decimal(0);
        }
        public void UpdateCoefficients(int pPeriod)
        {
            this.period = pPeriod;
            w = Math.Max(period - 1, 5);
            Kg = 3m / (period + 2m);
            Hg = 1m - Kg;
        }


        public decimal JRXSeries(decimal a_current, int index)
        {
            if (index == begin_index)
            {
                c1 = a_current;
                return 50m;
            }

            JRSX.SlashValue();
            trend.SlashValue();

            f28.SlashValue();
            f30.SlashValue();
            f38.SlashValue();
            f40.SlashValue();
            f48.SlashValue();
            f50.SlashValue();
            f58.SlashValue();
            f60.SlashValue();
            f68.SlashValue();
            f70.SlashValue();
            f78.SlashValue();
            f80.SlashValue();
            v8.SlashValue();
            v8a.SlashValue();
            c = a_current;
            v8.v = (c - c1);
            v8a.v = Math.Abs(v8.v);

            //            ---- вычисление V14 ------
            f28.v = Hg * f28.v1 + Kg * v8.v1;
            f30.v = Kg * f28.v + Hg * f30.v1;
            decimal v0C = 1.5m * f28.v - 0.5m * f30.v;
            f38.v = Hg * f38.v1 + Kg * v0C;
            f40.v = Kg * f38.v + Hg * f40.v1;
            decimal v10 = 1.5m * f38.v - 0.5m * f40.v;
            f48.v = Hg * f48.v1 + Kg * v10;
            f50.v = Kg * f48.v + Hg * f50.v;
            decimal v14 = 1.5m * f48.v - 0.5m * f50.v;
            //           ---- вычисление V20 ------
            f58.v = Hg * f58.v1 + Kg * v8a.v;
            f60.v = Kg * f58.v + Hg * f60.v1;
            decimal v18 = 1.5m * f58.v - 0.5m * f60.v;
            f68.v = Hg * f68.v1 + Kg * v18;
            f70.v = Kg * f68.v + Hg * f70.v1;
            decimal v1C = 1.5m * f68.v - 0.5m * f70.v;
            f78.v = Hg * f78.v1 + Kg * v1C;
            f80.v = Kg * f78.v + Hg * f80.v1;
            decimal v20 = 1.5m * f78.v - 0.5m * f80.v;

            if ((index - begin_index) > w && (v20 > 0))
            {
                JRSX.v = (v14 / v20 + 1) * 50;
                if (JRSX.v > 100)
                {
                    JRSX.v = 100;
                }
                if (JRSX.v < 0)
                {
                    JRSX.v = 0;
                }
            }
            else
            {
                JRSX.v = 50;
            }

            last_index = index;

            //out[1] = JRSX.v

            if ((JRSX.v > JRSX.v1) && trend.v <= 0)
            {
                trend.v = 1m;
            }

            if ((JRSX.v < JRSX.v1) && trend.v >= 0)
            {
                trend.v = -1m;
            }

            //-- myLog('index', index, os.date('%d.%m.%Y %H:%M:%S', os_time(_G.T(index))), 'trend', trend.v, 'out[1]', out [1])

            //        if (color)
            //        { 
            //out[2] = trend.v1 == 1 and out[1] or nil;
            //out[3] = trend.v1 == -1 and out[1] or nil;
            //            out[1] = nil;
            //        }

            c1 = c;
            return JRSX.v;
        }
    };
    /** JMA structure. */
    public class JMABase
    {
        double f18, f38, fA8, fC0, fC8, s8, s18, v1, v2;
        double v3, f90, f78, f88, f98, vJMA;
        double[] list;   // [128];
        double[] ring1;  // [128];
        double[] ring2;  // [11]
        double[] buffer; // [62]
        double Kg, Pf;
        int s28, s30, s38, s40, s48, f0, s50, s70, LP2;
        int LP1;
        double fA0, vv, v4, f70, s20, s10, fB0, fD0, f8, f60, f20, f28;
        double f30, f40, f48, f58, f68;
        int v5, v6, fE0, fD8, fE8, val, s58, s60, s68, size;
        int m;

        public JMABase()
        {
            list = new double[128];
            ring1 = new double[128];
            ring2 = new double[11];
            buffer = new double[62];
            InitData();
        }

        public void InitData()
        {
            f18 = 0;
            f38 = 0;
            fA8 = 0;
            fC0 = 0;
            fC8 = 0;
            s8 = 0;
            s18 = 0;
            v1 = 0;
            v2 = 0;
            v3 = 0;
            f90 = 0;
            f78 = 0;
            f88 = 0;
            f98 = 0;
            vJMA = 0;
            Kg = 0;
            Pf = 0;
            s38 = 0;
            s40 = 0;
            s48 = 0;
            s50 = 0;
            s70 = 0;
            LP2 = 0;
            LP1 = 0;
            fA0 = 0;
            vv = 0;
            v4 = 0;
            f70 = 0;
            s20 = 0;
            s10 = 0;
            fB0 = 0;
            fD0 = 0;
            f8 = 0;
            f60 = 0;
            f20 = 0;
            f28 = 0;
            f30 = 0;
            f40 = 0;
            f48 = 0;
            f58 = 0;
            f68 = 0;
            v5 = 0;
            v6 = 0;
            fE0 = 0;
            fD8 = 0;
            fE8 = 0;
            val = 0;
            s58 = 0;
            s60 = 0;
            s68 = 0;
            size = 0;
            m = 0;

            int i;
            for (i = 0; i < 128; i++)
            {
                ring1[i] = 0;
            }

            for (i = 0; i < 11; i++)
            {
                ring2[i] = 0;
            }

            for (i = 0; i < 62; i++)
                buffer[i] = 0;

            f0 = 1;
            s28 = 63;
            s30 = 64;

            for (i = 0; i <= s28; i++)
                list[i] = -1000000.0;

            for (i = s30; i <= 127; i++)
                list[i] = 1000000.0;
        }

        ~JMABase()
        {
        }
        /** Updates JMA's coefficients depending on the phase and length parameters. */
        public void UpdateCoefficients(int a_Phase, int a_Length)
        {
            //calculate coefficients
            double Dr, Ds, Dl;
            if (a_Length < 1.0000000002)
                Dr = 0.0000000001;
            else
                Dr = (a_Length - 1.0) / 2.0;
            if ((a_Phase >= -100) && (a_Phase <= 100))
                Pf = a_Phase / 100.0 + 1.5;
            if (a_Phase > 100)
                Pf = 2.5;
            if (a_Phase < -100)
                Pf = 0.5;
            Dr = Dr * 0.9;
            Kg = Dr / (Dr + 2.0);
            Ds = Math.Sqrt(Dr);
            Dl = Math.Log(Ds);
            v1 = Dl;
            v2 = v1;
            if ((v1 / Math.Log(2.0)) + 2.0 < 0.0)
                v3 = 0.0;
            else
                v3 = (v2 / Math.Log(2.0)) + 2.0;
            f98 = v3;
            if (f98 >= 2.5)
                f88 = f98 - 2.0;
            else
                f88 = 0.5;
            f78 = Ds * f98;
            f90 = f78 / (f78 + 1.0);
        }
        /** Calculate the next JMA value in a row. */
        public decimal JJMASeries(decimal a_current)
        {
            int i, j;

            if (LP1 < 61)
            {
                LP1++;
                buffer[LP1] = (double)a_current;
            }

            if (LP1 > 30)
            {
                if (f0 != 0)
                {
                    f0 = 0;
                    v5 = 1;
                    fD8 = v5 * 30;
                    if (fD8 == 0)
                        f38 = (double)a_current;
                    else
                        f38 = buffer[1];
                    f18 = f38;
                    if (fD8 > 29)
                        fD8 = 29;
                }
                else
                    fD8 = 0;

                for (i = fD8; i >= 0; i--)
                {
                    val = 31 - i;
                    if (i == 0)
                        f8 = (double)a_current;
                    else
                        f8 = buffer[val];
                    f28 = f8 - f18;
                    f48 = f8 - f38;
                    if (Math.Abs(f28) > Math.Abs(f48))
                        v2 = Math.Abs(f28);
                    else
                        v2 = Math.Abs(f48);
                    fA0 = v2;
                    vv = fA0 + 0.0000000001; //{1.0e-10;}
                    if (s48 <= 1)
                        s48 = 127;
                    else
                        s48 = s48 - 1;
                    if (s50 <= 1)
                        s50 = 10;
                    else
                        s50 = s50 - 1;
                    if (s70 < 128)
                        s70 = s70 + 1;
                    s8 = s8 + vv - ring2[s50];
                    ring2[s50] = vv;
                    if (s70 > 10)
                        s20 = s8 / 10.0;
                    else
                        s20 = s8 / s70;

                    if (s70 > 127)
                    {
                        s10 = ring1[s48];
                        ring1[s48] = s20;
                        s68 = 64;
                        s58 = s68;
                        while (s68 > 1)
                        {
                            if (list[s58] < s10)
                            {
                                s68 = (int)(s68 * 0.5);
                                s58 = s58 + s68;
                            }
                            else
                            {
                                if (list[s58] <= s10)
                                    s68 = 1;
                                else
                                {
                                    s68 = (int)(s68 * 0.5);
                                    s58 = s58 - s68;
                                }
                            }
                        }

                    }
                    else
                    {
                        ring1[s48] = s20;
                        if (s28 + s30 > 127)
                        {
                            s30 = s30 - 1;
                            s58 = s30;
                        }
                        else
                        {
                            s28 = s28 + 1;
                            s58 = s28;
                        }
                        if (s28 > 96)
                            s38 = 96;
                        else
                            s38 = s28;
                        if (s30 < 32)
                            s40 = 32;
                        else
                            s40 = s30;
                    }
                    s68 = 64;
                    s60 = s68;

                    while (s68 > 1)
                    {
                        if (list[s60] >= s20)
                        {
                            if (list[s60 - 1] <= s20)
                                s68 = 1;
                            else
                            {
                                s68 = (int)(s68 * 0.5);
                                s60 = s60 - s68;
                            }
                        }
                        else
                        {
                            s68 = (int)(s68 * 0.5);
                            s60 = s60 + s68;
                        }
                        if ((s60 == 127) && (s20 > list[127]))
                            s60 = 128;
                    }

                    if (s70 > 127)
                    {
                        if (s58 >= s60)
                        {
                            if ((s38 + 1 > s60) && (s40 - 1 < s60))
                                s18 = s18 + s20;
                            else if ((s40 + 0 > s60) && (s40 - 1 < s58))
                                s18 = s18 + list[s40 - 1];
                        }
                        else if (s40 >= s60)
                        {
                            if ((s38 + 1 < s60) && (s38 + 1 > s58))
                                s18 = s18 + list[s38 + 1];
                        }
                        else if (s38 + 2 > s60)
                            s18 = s18 + s20;
                        else if ((s38 + 1 < s60) && (s38 + 1 > s58))
                            s18 = s18 + list[s38 + 1];

                        if (s58 > s60)
                        {
                            if ((s40 - 1 < s58) && (s38 + 1 > s58))
                                s18 = s18 - list[s58];
                            else
                            if ((s38 < s58) && (s38 + 1 > s60))
                                s18 = s18 - list[s38];
                        }
                        else
                        {
                            if ((s38 + 1 > s58) && (s40 - 1 < s58))
                                s18 = s18 - list[s58];
                            else
                            if ((s40 + 0 > s58) && (s40 - 0 < s60))
                                s18 = s18 - list[s40];
                        }
                    }

                    if (s58 <= s60)
                    {
                        if (s58 >= s60)
                        {
                            list[s60] = s20;
                        }
                        else
                        {
                            for (j = s58 + 1; j <= s60 - 1; j++)
                                list[j - 1] = list[j];
                            list[s60 - 1] = s20;
                        }
                    }
                    else
                    {
                        for (j = s58 - 1; j >= s60; j--)
                            list[j + 1] = list[j];
                        list[s60] = s20;
                    }
                    if (s70 <= 127)
                    {
                        s18 = 0;
                        for (j = s40; j <= s38; j++)
                            s18 = s18 + list[j];
                    }
                    f60 = s18 / (s38 - s40 + 1.0);
                    if (LP2 + 1 > 31)
                        LP2 = 31;
                    else
                        LP2 = LP2 + 1;
                    if (LP2 <= 30)
                    {
                        if (f28 > 0.0)
                            f18 = f8;
                        else
                            f18 = f8 - f28 * f90;
                        if (f48 < 0.0)
                            f38 = f8;
                        else
                            f38 = f8 - f48 * f90;
                        vJMA = (double)a_current;
                        if (LP2 != 30)
                            continue;
                        if (LP2 == 30)
                        {
                            fC0 = (double)a_current;
                            if (Math.Ceiling(f78) >= 1)
                                v4 = Math.Ceiling(f78);
                            else
                                v4 = 1.0;

                            if (v4 > 0)
                                fE8 = (int)Math.Floor(v4);
                            else
                            {
                                if (v4 < 0)
                                    fE8 = (int)Math.Ceiling(v4);
                                else
                                    fE8 = 0;
                            }

                            if (Math.Floor(f78) >= 1)
                                v2 = Math.Floor(f78);
                            else
                                v2 = 1.0;

                            if (v2 > 0)
                                fE0 = (int)Math.Floor(v2);
                            else
                            {
                                if (v2 < 0)
                                    fE0 = (int)Math.Ceiling(v2);
                                else
                                    fE0 = 0;
                            }

                            if (fE8 == fE0)
                                f68 = 1.0;
                            else
                            {
                                v4 = fE8 - fE0; f68 = (f78 - fE0) / v4;
                            }
                            if (fE0 <= 29)
                                v5 = fE0;
                            else
                                v5 = 29;
                            if (fE8 <= 29)
                                v6 = fE8;
                            else
                                v6 = 29;
                            fA8 = ((double)a_current - buffer[LP1 - v5]) * (1.0 - f68) / fE0 + ((double)a_current - buffer[LP1 - v6]) * f68 / fE8;
                        }
                    }
                    else
                    {
                        if (f98 >= Math.Pow(fA0 / f60, f88))
                            v1 = Math.Pow(fA0 / f60, f88);
                        else
                            v1 = f98;
                        if (v1 < 1.0)
                            v2 = 1.0;
                        else
                        {
                            if (f98 >= Math.Pow(fA0 / f60, f88))
                                v3 = Math.Pow(fA0 / f60, f88);
                            else
                                v3 = f98; v2 = v3;
                        }
                        f58 = v2;
                        f70 = Math.Pow(f90, Math.Sqrt(f58));
                        if (f28 > 0.0)
                            f18 = f8;
                        else
                            f18 = f8 - f28 * f70;
                        if (f48 < 0.0)
                            f38 = f8;
                        else f38 = f8 - f48 * f70;
                    }
                }
                if (LP2 > 30)
                {
                    f30 = Math.Pow(Kg, f58);
                    fC0 = (1.0 - f30) * (double)a_current + f30 * fC0;
                    fC8 = ((double)a_current - fC0) * (1.0 - Kg) + Kg * fC8;
                    fD0 = Pf * fC8 + fC0;
                    f20 = f30 * (-2.0);
                    f40 = f30 * f30;
                    fB0 = f20 + f40 + 1.0;
                    fA8 = (fD0 - vJMA) * fB0 + f40 * fA8;
                    vJMA = vJMA + fA8;
                }
            }

            if (LP1 <= 30)
                vJMA = 0.0;

            return (decimal)vJMA;
        }
    };

    public class SecurityRatingData
    {
        public string SecurityName;

        //public decimal Rsi;
        public int iCondition;
        public int iConditionOpenLong;
        public int iConditionOpenShort;
        public bool bShort;
        public bool bStopOpenDeal;
        public int iConditionCloseShort;
        public int iConditionCloseLong;

        public string SecurityRatingString 
        { get => SecurityName + ":iConditionOpenLong=" + iConditionOpenLong.ToString()
                              + ":iConditionOpenShort=" + iConditionOpenShort.ToString()
                             + ";StopOpenDeal="+ bStopOpenDeal.ToString()
                             + ";Short="+ bShort.ToString()
                             + ";CloseShort=" + iConditionCloseShort.ToString()
                             + ";CloseLong=" + iConditionCloseLong.ToString() ; 
        }
        //public decimal Volume;
    }
}
