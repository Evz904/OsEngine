using System;
using System.Collections.Generic;
using System.Linq;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using System.Threading;
using System.Drawing;
using OsEngine.Market.Servers.Alor.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Market.Servers;
using OsEngine.Market;
using OsEngine.Market.Servers.TraderNet.Entity;
using System.IO;
using System.Reflection;
using CustomIndicators.Scripts.EVZRobots;

namespace OsEngine.Robots.EVZRobots
{
    [Bot("EMA2Trade2")]
    public class EMA2Trade2 : BotPanel
    {

        private BotTabSimple _tabToTrade;
        private BotTabSimple _tabSmallTf;

        private Aindicator _EMAFast;
        private Aindicator _EMAMedium;
        private Aindicator _volatilityStages;
        private RSI3RAY _RSI;
        private ParabolicSaR _sar;
        private Zamah _zamah;
        private StrategyParameterInt _ZamahLenght;

        public StrategyParameterString _Regime;
        private StrategyParameterInt _jmaLenFast;
        private StrategyParameterInt _jmaLenMedium;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterDecimal _RSIUp;
        private StrategyParameterDecimal _RSILow;
        // private StrategyParameterInt _RSILength;

        private StrategyParameterInt _RSILengthFast;
        private StrategyParameterInt _RSILengthMedium;
        private StrategyParameterInt _RSILengthSlow;

        public StrategyParameterDecimal ParabolicAf;
        public StrategyParameterDecimal ParabolicMaxAf;


        private StrategyParameterBool _bByVolume;

        //  private StrategyParameterString _candlePoint;

        private StrategyParameterTimeOfDay _timeEndOfTrade;
        private StrategyParameterInt _closeDaily;

        private StrategyParameterDecimal _ProfitProcent;
        private StrategyParameterDecimal _StopLossProcent;
        private StrategyParameterInt _AlgorithmSensitivityLong;
        private StrategyParameterInt _AlgorithmSensitivityShort;
        private StrategyParameterBool _UseRevers;
        private StrategyParameterBool _UseTrailing;
        public StrategyParameterBool VolatilityFilterIsOn;
        public StrategyParameterString VolatilityStageToTrade;
        public StrategyParameterInt VolatilitySlowSmaLength;
        public StrategyParameterInt VolatilityFastSmaLength;
        public StrategyParameterDecimal VolatilityChannelDeviation;

        public StrategyParameterInt _lengthUp;

        public StrategyParameterInt _lengthDown;

        public StrategyParameterString VolumeType;
        public StrategyParameterDecimal Volume;

        public StrategyParameterString TradeAssetInPortfolio;

        // Exit 
        private StrategyParameterInt _TrailBars;
        private StrategyParameterInt _PrevBars;

        //private StrategyParameterInt _maxCountDotInLine;
        //private WindowsFormsHost _host;
        //private Chart _chart;
        private Decimal dLastBestPrice;
        private Decimal dLastProfitProcent;
        private Decimal dLastTrailValue;
        private decimal upLine;
        private decimal downLine;
        private decimal upLine2;
        private decimal downLine2;

        private int _iStepTrail;

        private decimal _lastSar;
        private decimal valueEMAFast;
        private decimal valueEMAFast2;
        private decimal valueEMAMedium;
        private decimal valueEMAMedium2;
        private decimal valueRSIFast;
        private decimal valueRSIFast2;
        private decimal valueRSIMedium;
        private decimal valueRSIMedium2;

        private decimal valueRSISlow;
        private decimal valueRSISlow2;

        private decimal valueVolume;
        private decimal valueVolume2;

        private Candle lastCandle;
        private Candle lastCandle2;

        private decimal dBullsZamah;
        private decimal dBearsZamah;
        private decimal closeLastCandle;

        private decimal dDeltaEMAN;
        private decimal dDeltaEMA;

        private int iCondition;
        private int iConditionCloseShort;
        private int iConditionCloseLong;
        private int iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
        private int iDirectionRSI1;
        //private int iTrail

        // сервис
        private StrategyParameterBool _UseDataLog;
        private const string V = "C:\\temp\\dataJMA2.csv";
        StreamWriter writer;
        private int lineNum = 1;
        private string strTime;
        public EMA2Trade2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
             TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];
            _tabSmallTf = TabsSimple[1];
            _tabToTrade.CandleFinishedEvent += _tabToTrade_CandleFinishedEvent;
            _tabToTrade.PositionClosingSuccesEvent += _tabToTrade_PositionClosingSuccesEvent;
            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });


            _jmaLenFast = CreateParameter("Ema fast len", 50, 5, 200, 2);
            _jmaLenMedium = CreateParameter("Ema medium len", 100, 10, 300, 2);

            _RSIUp = CreateParameter("RSI upper range", 65m, 50m, 99.9m, 0.5m);
            _RSILow = CreateParameter("RSI low range", 35m, 1m, 49.99m, 0.5m);
            _RSILengthFast = CreateParameter("RSI periods fast", 6, 1, 200, 1);
            _RSILengthMedium = CreateParameter("RSI periods medium", 14, 2, 100, 1);
            _RSILengthSlow = CreateParameter("RSI periods slow", 24, 1, 400, 1);
            _bByVolume = CreateParameter("By volume", false);
            _ZamahLenght = CreateParameter("Zamah lenght", 6, 1, 300, 1);
            _PrevBars = CreateParameter("Prev bars for retrospective", 2, 1, 10, 1);

            ParabolicAf = CreateParameter("Parabolic Af", 0.02m, 0.02m, 0.1m, 0.01m);
            ParabolicMaxAf = CreateParameter("Parabolic Max Af", 0.2m, 0.2m, 0.5m, 0.1m);

            _sar = new ParabolicSaR(name + "Prime", false);
            _sar = (ParabolicSaR)_tabToTrade.CreateCandleIndicator(_sar, "Prime");
            _sar.Af = Convert.ToDouble(ParabolicAf.ValueDecimal);
            _sar.MaxAf = Convert.ToDouble(ParabolicMaxAf.ValueDecimal);
            _sar.Save();

            //  _candlePoint = CreateParameter("Candle Point", "Close", Entity.CandlePointsArray);

            // Exit
            _timeEndOfTrade = CreateParameterTimeOfDay("Time close trade", 23, 50, 0, 0, "Exit");
            _closeDaily = CreateParameter("Close daily", 0, 0, 1, 1, "Exit");
            _UseTrailing = CreateParameter("Use Trailing", false, "Exit");
            _TrailBars = CreateParameter("Trail Bars", 4, 1, 50, 1, "Exit");
            _lengthUp = CreateParameter("Length up", 9, 1, 50, 1, "Exit");
            _lengthDown = CreateParameter("Length down", 9, 1, 50, 1, "Exit");


            _UseDataLog = CreateParameter("_Use data log", false, "Exit");

            _AlgorithmSensitivityLong = CreateParameter("Algorithm sensitivity long", 3, 1, 10, 1);
            _AlgorithmSensitivityShort = CreateParameter("Algorithm sensitivity short", -3, 1, 10, 1);

            _ProfitProcent = CreateParameter("Profit %", 1.5m, 0.01m, 99.9m, 0.1m, "Exit");
            _StopLossProcent = CreateParameter("Stop los %", 1.0m, 0.01m, 99.9m, 0.1m, "Exit");
            VolumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            Volume = CreateParameter("Volume", 40, 1.0m, 50, 4);

            _slippage = CreateParameter("Slippage percent", 5.0m, 0, 10, 1);

            _RSI = (RSI3RAY)IndicatorsFactory.CreateIndicatorByName("RSI3RAY", "RSI3RAY", false); //new RSI3RAY(); //
            _RSI = (RSI3RAY)_tabToTrade.CreateCandleIndicator(_RSI, "RSIArea");
            _RSI.ParametersDigit[0].Value = _RSILengthFast.ValueInt;
            _RSI.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;
            _RSI.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;
            _RSI.bByVolume.ValueBool = _bByVolume.ValueBool;

            _EMAFast = (Aindicator)IndicatorsFactory.CreateIndicatorByName("Ema", "Ema fast", false);
            _EMAFast = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAFast, "Prime");
            //_JMAFast.


            _EMAMedium = (Aindicator)IndicatorsFactory.CreateIndicatorByName("Ema", "Ema medium", false);
            _EMAMedium = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAMedium, "Prime");
            _EMAMedium.ParametersDigit[0].Value = _jmaLenMedium.ValueInt;

            _zamah = (Zamah)IndicatorsFactory.CreateIndicatorByName("Zamah", "Zamah", false);
            _zamah = (Zamah)_tabToTrade.CreateCandleIndicator(_zamah, "ZamahArea");
            _zamah.ParametersDigit[0].Value = _ZamahLenght.ValueInt;

            _UseRevers = CreateParameter("Use revers", false);

            VolatilityFilterIsOn = CreateParameter("Volatility filter is on", true);
            VolatilityStageToTrade = CreateParameter("Volatility stage to trade", "2", new[] { "1", "2", "3", "4" });
            VolatilitySlowSmaLength = CreateParameter("Volatility slow sma length", 25, 10, 80, 3);
            VolatilityFastSmaLength = CreateParameter("Volatility fast sma length", 7, 10, 80, 3);
            VolatilityChannelDeviation = CreateParameter("Volatility channel deviation", 0.4m, 1.0m, 50, 4);

            _volatilityStages = IndicatorsFactory.CreateIndicatorByName("VolatilityStagesAWE", name + "VolatilityStages", false);
            _volatilityStages = (Aindicator)_tabToTrade.CreateCandleIndicator(_volatilityStages, "VolatilityStagesArea");
            _volatilityStages.ParametersDigit[0].Value = VolatilitySlowSmaLength.ValueInt;
            _volatilityStages.ParametersDigit[1].Value = VolatilityFastSmaLength.ValueInt;
            _volatilityStages.ParametersDigit[2].Value = VolatilityChannelDeviation.ValueDecimal;
            //  _volatilityStages.Parameters[0].LoadParamFromString("3");

            TradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            ParametrsChangeByUser += Event_ParametrsChangeByUser;
            _tabToTrade.NewTickEvent += _tab_NewTickEvent;
            // Мои переменные
            dLastBestPrice = 0.0m;
            dLastProfitProcent = 0.0m;

            string pathToData = V;
            if (_UseDataLog.ValueBool)
            {
                writer = new StreamWriter(pathToData, false);
                writer.WriteLine("index;date;Value;Open;High;Low;Close;JMAFast;JMAMedium;RSI Fast;RSI Medium;RSI slow");
            }

        }

        ~EMA2Trade2()
        {
            if (writer != null)
            {
                writer.Close();
            }
        }

        public override string GetNameStrategyType()
        {
            return "EMA2Trade2";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
        void Event_ParametrsChangeByUser()
        {
            if (_UseDataLog.ValueBool && writer == null)
            {
                string pathToData = V;
                writer = new StreamWriter(pathToData, false);
                writer.WriteLine("index;date;Value;Open;High;Low;Close;JMAFast;JMAMedium;RSI Fast;RSI Medium;RSI slow");

            }
            else
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }

            double af = Convert.ToDouble(ParabolicAf.ValueDecimal);
            double maxAf = Convert.ToDouble(ParabolicMaxAf.ValueDecimal);

            if (_sar.Af != af ||
                _sar.MaxAf != maxAf)
            {
                _sar.Af = Convert.ToDouble(ParabolicAf.ValueDecimal);
                _sar.MaxAf = Convert.ToDouble(ParabolicMaxAf.ValueDecimal);
                _sar.Reload();
                _sar.Save();
            }

            if (_ZamahLenght.ValueInt != _zamah.ParametersDigit[0].Value)
            {
                _zamah.ParametersDigit[0].Value = _ZamahLenght.ValueInt;

                _zamah.Reload();
                _zamah.Save();
            }

            if (_jmaLenFast.ValueInt != _EMAFast.ParametersDigit[0].Value)
            {
                _EMAFast.ParametersDigit[0].Value = _jmaLenFast.ValueInt;

                _EMAFast.Reload();
                _EMAFast.Save();
            }

            if (_jmaLenMedium.ValueInt != _EMAMedium.ParametersDigit[0].Value)
            {
                _EMAMedium.ParametersDigit[0].Value = _jmaLenMedium.ValueInt;

                _EMAMedium.Reload();
                _EMAMedium.Save();
            }

            if (_RSILengthFast.ValueInt != _RSI.ParametersDigit[0].Value ||
               _RSILengthSlow.ValueInt != _RSI.ParametersDigit[2].Value ||
               _RSILengthMedium.ValueInt != _RSI.ParametersDigit[1].Value ||
               _RSI.bByVolume.ValueBool != _bByVolume.ValueBool)
            {
                _RSI.ParametersDigit[0].Value = _RSILengthFast.ValueInt;
                _RSI.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;
                _RSI.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;
                _RSI.bByVolume.ValueBool = _bByVolume.ValueBool;

                _RSI.Reload();
                _RSI.Save();
            }

            if (_volatilityStages.ParametersDigit[0].Value != VolatilitySlowSmaLength.ValueInt
                || _volatilityStages.ParametersDigit[1].Value != VolatilityFastSmaLength.ValueInt
                || _volatilityStages.ParametersDigit[2].Value != VolatilityChannelDeviation.ValueDecimal)
            {
                _volatilityStages.ParametersDigit[0].Value = VolatilitySlowSmaLength.ValueInt;
                _volatilityStages.ParametersDigit[1].Value = VolatilityFastSmaLength.ValueInt;
                _volatilityStages.ParametersDigit[2].Value = VolatilityChannelDeviation.ValueDecimal;

                _volatilityStages.Reload();
                _volatilityStages.Save();
            }
        }
        private void _tabToTrade_PositionClosingSuccesEvent(Position obj)
        {
            dLastTrailValue = 0;
            if (obj.State == PositionStateType.Done)
            {
                List<Position> openPositions = _tabToTrade.PositionsOpenAll;
                //if (obj.Direction == Side.Buy)
                decimal price = obj.ClosePrice;
                if (obj.SignalTypeStop == "Close long")
                {
                    if (_Regime.ValueString != "OnlyLong"
                     && _Regime.ValueString != "OnlyClosePosition")
                    {
                        if (_UseRevers.ValueBool == true)
                        {
                            //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), price - price * (_slippage.ValueDecimal / 100));
                            LogicOpenPosition();
                        }
                    }
                }
                else if (obj.SignalTypeStop == "Close short")
                {
                    if (_Regime.ValueString != "OnlyShort"
                        && _Regime.ValueString != "OnlyClosePosition")
                    {
                        if (_UseRevers.ValueBool == true)
                        {
                            //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), price + price * (_slippage.ValueDecimal / 100));

                            LogicOpenPosition();
                        }
                    }
                }

            }
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }

            //TradeLogic();
        }
        private void _tabToTrade_CandleFinishedEvent(List<Candle> candles)
        {
            // вызывается на каждой новой свече
            int iCount = candles.Count;
            if (iCount == 1)
            {
                upLine = 0;
                upLine2 = 0;

                downLine = 0;
                downLine2 = 0;

                lineNum = 0;
                dDeltaEMAN = 0;
                dDeltaEMA = 0;
                return;
            }

            lastCandle = candles[^1];
            lastCandle2 = candles[^2];
            strTime = lastCandle.TimeStart.ToString();

            valueVolume = lastCandle.Volume;
            valueVolume2 = lastCandle2.Volume;


            _lastSar = _sar.Values[^1];

            List<Position> openPositions = _tabToTrade.PositionsOpenAll;


            valueEMAFast = _EMAFast.DataSeries[0].Last;


            valueEMAMedium = _EMAMedium.DataSeries[0].Last;


            valueRSIFast = _RSI.DataSeries[0].Last;
            valueRSIFast2 = _RSI.DataSeries[0].Values[^2];

            valueRSIMedium = _RSI.DataSeries[1].Last;

            valueRSISlow = _RSI.DataSeries[2].Last;
            valueRSISlow2 = _RSI.DataSeries[2].Values[^2];

            dBullsZamah = _zamah.SeriesBulls.Last;
            dBearsZamah = _zamah.SeriesBears.Last;

            closeLastCandle = lastCandle.Close;
            if (_UseDataLog.ValueBool)
            {
                //writer.WriteLine("index;date;Value;Open;High;Low;Close;JMAFast;JMAMedium;RSI Fast;RSI Medium;RSI slow\r\n");
                writer.WriteLine(lineNum.ToString() + ";" + strTime + ";" + lastCandle.Volume.ToString() + ";" + lastCandle.Open.ToString() + ";" + lastCandle.High.ToString() + ";" + lastCandle.Low.ToString() + ";" + lastCandle.Close.ToString() + ";" + valueEMAFast.ToString() + ";" + valueEMAMedium.ToString() + ";" + valueRSIFast.ToString() + ";" + valueRSIMedium.ToString() + ";" + valueRSISlow.ToString() + ";");
            }
            lineNum++;
            if (_Regime.ValueString == "Off" || iCount < _jmaLenMedium.ValueInt + _PrevBars.ValueInt || valueEMAMedium == 0)
            {
                return;
            }

            valueEMAFast2 = _EMAFast.DataSeries[0].Values[_EMAFast.DataSeries[0].Values.Count - _PrevBars.ValueInt - 1];
            valueEMAMedium2 = _EMAMedium.DataSeries[0].Values[_EMAMedium.DataSeries[0].Values.Count - _PrevBars.ValueInt - 1];
            if(valueEMAMedium2 == 0)
            {
                return;
            }
            //   тревожность. Отношение количества свечей вверх и вниз (смена направления) по отношению к общему количеству свечей за период
            // полная тервожность - сумма длины свечей вверх и длины свечей вниз к общему изменению цены (High - Lo) за период
            CalculateCondition(candles);

            if (iCondition != 0)
            {
                if (openPositions != null && openPositions.Count != 0)
                {
                    for (int i = 0; i < openPositions.Count; i++)
                    {
                        LogicClosePosition(candles, openPositions[i], openPositions);
                    }
                }

                if (openPositions == null || openPositions.Count == 0)
                {
                    if (_Regime.ValueString == "OnlyClosePosition")
                    {
                        return;
                    }
                    LogicOpenPosition();
                }
            }
        }
        private void CalculateCondition(List<Candle> candles)
        {
            iDirectionRSI1 = 0; // -1 - degree, 0 - flat, 1 - grow
            if (valueRSIFast2 < valueRSIMedium2 && valueRSIMedium2 > valueRSISlow2)
            {
                iDirectionRSI1 = -1;
            }
            else if (valueRSIFast2 > valueRSIMedium2 && valueRSIMedium2 < valueRSISlow2)
            {
                iDirectionRSI1 = 1;
            }

            iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
            if (valueRSIFast < valueRSIMedium && valueRSIFast < valueRSIFast2) // && dLastRSIFast <= _RSIUp.ValueDecimal)// dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = -1;// + (dLastRSIMedium < dLastRSISlow ? -1 : 0);
            }
            else if (valueRSIFast > valueRSIMedium && valueRSIFast > valueRSIFast2)// && valueRSIFast >= _RSILow.ValueDecimal) // dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = 1;// + (dLastRSIMedium > dLastRSISlow ? 1 : 0);
            }

            iCondition = 0;
            iConditionCloseShort = 0;
            iConditionCloseLong = 0;
            // iCondition = 1
            iCondition = iDirectionRSI;

            iConditionCloseShort = iDirectionRSI == 1 ? 1 : 0;
            iConditionCloseLong = iDirectionRSI == -1 ? 1 : 0;

            // Проверка на крайние значения RSI для выхода из позиции
            iConditionCloseShort += iDirectionRSI == 1 && _RSIUp < valueRSIFast ? 1 : 0;
            iConditionCloseLong += iDirectionRSI == -1 && _RSILow > valueRSIFast ? 1 : 0;

            //iCondition += iDirectionRSI == 1 && _RSIUp > valueRSIFast ? -2 : iDirectionRSI == -1 && _RSILow > valueRSIFast ? 2 : 0;
            // iCondition = 2
            iCondition += dBearsZamah > dBullsZamah ? -1 : dBearsZamah < dBullsZamah ? 1 : 0;

            iConditionCloseShort += dBearsZamah < dBullsZamah ? 1 : 0;
            iConditionCloseLong += dBearsZamah > dBullsZamah ? 1 : 0;
            // iCondition = 3
            iCondition += closeLastCandle > _lastSar ? 1 : closeLastCandle < _lastSar ? -1 : 0;

            iConditionCloseShort += closeLastCandle > _lastSar ? 1 : 0;
            iConditionCloseLong += closeLastCandle < _lastSar ? 1 : 0;
            // iCondition = 4
            iCondition += lastCandle.IsDown == true ? -1 : lastCandle.IsUp == true ? 1 : 0;

            iConditionCloseShort += lastCandle.IsDown != true ? 1 : 0;
            iConditionCloseLong += lastCandle.IsUp != true ? 1 : 0;

            dDeltaEMAN = valueEMAFast2 - valueEMAMedium2;
            dDeltaEMA = valueEMAFast - valueEMAMedium;

            //iCondition += dDeltaEMAN < dDeltaEMA && dDeltaEMA > 0 ? 1 : 0;
            //iCondition += dDeltaEMAN < dDeltaEMA && dDeltaEMA < 0 ? 1 : 0;
            //iCondition += dDeltaEMAN > dDeltaEMA && dDeltaEMA > 0 ? -1 : 0;
            //iCondition += dDeltaEMAN > dDeltaEMA && dDeltaEMA < 0 ? -1 : 0; //  dDeltaEMAN = 17645 - 17779 dDeltaEMA = 17324 - 17628
            // iCondition = 5
            iCondition += dDeltaEMAN < dDeltaEMA ? 1 : dDeltaEMAN > dDeltaEMA ? -1 : 0;
            iConditionCloseShort += dDeltaEMAN < dDeltaEMA ? 1 : 0;
            iConditionCloseLong += dDeltaEMAN > dDeltaEMA ? 1 : 0;

            //iCondition += valueEMAFast - valueEMAFast2 > 0 ? 1 : valueEMAFast - valueEMAFast2 == 0 ? 0 : -1;

            //iConditionCloseShort += valueEMAFast - valueEMAFast2 > 0 ? 1 : 0;
            //iConditionCloseLong += valueEMAFast - valueEMAFast2 < 0 ? 1 : 0;
            // iCondition = 6
            iCondition += valueEMAFast - valueEMAMedium > 0 && closeLastCandle - valueEMAFast > 0 ? 1 : -1;

            iConditionCloseShort += valueEMAFast - valueEMAMedium > 0 && closeLastCandle - valueEMAFast > 0 ? 1 : 0;
            iConditionCloseLong += valueEMAFast - valueEMAMedium < 0 && closeLastCandle - valueEMAFast < 0 ? 1 : 0;
            // iCondition = 7
            iCondition += closeLastCandle - valueEMAFast > 0 ? 1 : -1;

            iConditionCloseShort += closeLastCandle - valueEMAFast > 0 ? 1 : 0;
            iConditionCloseLong += closeLastCandle - valueEMAFast < 0 ? 1 : 0;
            // iCondition = 8
            iCondition += closeLastCandle - valueEMAMedium > 0 ? 1 : -1;

            iConditionCloseShort += closeLastCandle - valueEMAMedium > 0 ? 1 : 0;
            iConditionCloseLong += closeLastCandle - valueEMAMedium < 0 ? 1 : 0;

            CalculatePriceChannelTrailingLevel(candles);

            // iCondition = 9
            iCondition += upLine > upLine2 && downLine >= downLine2 ? 1 : downLine < downLine2 && upLine <= upLine2 ? -1 : 0;

            iConditionCloseShort += upLine > upLine2 && downLine >= downLine2 ? 1 : 0;
            iConditionCloseLong += downLine < downLine2 && upLine <= upLine2 ? 1 : 0;
            //PSARTrailingLevel();
        }

        private void _tab_NewTickEvent(Trade trade)
        {
            if (_Regime.ValueString == "Off"
                || _Regime.ValueString == "Выключен")
            {
                return;
            }

            //if (RegimeLogicEntry.ValueString != "On new trade")
            //{
            //    return;
            //}

 
            //if (_lastTradePrice == trade.Price)
            //{
            //    return;
            //}

            //_lastTradePrice = trade.Price;

            //if (_tab.PositionsOpenAll.Count != 0)
            //{
            //    CloseLogic();
            //}
            //else if (Regime.ValueString == "Only Close")
            //{
            //    return;
            //}

            //if (Regime.ValueString == "Only Close")
            //{
            //    TryCancelOrdersToOpenPos();
            //}
            //else
            //{
            //    TradeLogic();
            //}
        }
        private void PSARTrailingLevel()
        {
            int index = _sar.Values.Count - 1;
            //_sar.Values
            if (_sar.Values[index] < closeLastCandle)
            {
                downLine = _sar.Values[index];
                upLine = 0;
            }
            else
            {
                upLine = _sar.Values[index];
                downLine = 0;
            }
        }

        private void CalculatePriceChannelTrailingLevel(List<Candle> candles)
        {
            int index = candles.Count - 1;
            upLine2 = upLine;
            downLine2 = downLine;
            upLine = 0;
            downLine = decimal.MaxValue;

            if (index - _lengthUp.ValueInt > 0)
            {
                for (int i = index; i > -1 && i > index - _lengthUp.ValueInt; i--)
                {
                    if (upLine < candles[i].High)
                    {
                        upLine = candles[i].High;
                    }
                }
            }

            if (index - _lengthDown.ValueInt > 0)
            {
                downLine = decimal.MaxValue;

                for (int i = index; i > -1 && i > index - _lengthDown.ValueInt; i--)
                {
                    if (downLine > candles[i].Low)
                    {
                        downLine = candles[i].Low;
                    }
                }
            }
        }

        private void LogicOpenPosition()
        {

            strTime = lastCandle.TimeStart.ToString();
            TimeSpan CurrentTimeFrame = _tabToTrade.TimeFrame;
            DateTime CurrentDateTime = _tabToTrade.TimeServerCurrent;
            TimeSpan TimeSpan = CurrentDateTime.TimeOfDay;
            TimeSpan TimeAdded = TimeSpan + CurrentTimeFrame;
            Boolean bCheck = TimeAdded >= _timeEndOfTrade.Value.TimeSpan;

            if (bCheck && _closeDaily.ValueInt == 1)
            {
                return;
            }

            //  decimal lowLastCandle = lastCandle.Low;
            //  decimal highLastCandle = lastCandle.High;

            if (iCondition == 0)
            {
                return;
            }

            if (iCondition >= _AlgorithmSensitivityLong.ValueInt
                && _Regime.ValueString != "OnlyShort")
            {
                if (upLine > upLine2 && downLine >= downLine2)
                {
                    decimal stage = 0;
                    decimal stage2 = 0;
                    if (VolatilityFilterIsOn.ValueBool == true)
                    {
                        stage = _volatilityStages.DataSeries[0].Values[^2];
                        stage2 = _volatilityStages.DataSeries[0].Values[^1];

                        //if (stage < VolatilityStageToTrade.ValueString.ToDecimal()) //
                        if (stage != VolatilityStageToTrade.ValueString.ToDecimal() || stage2 != VolatilityStageToTrade.ValueString.ToDecimal())
                        {
                            return;
                        }
                    }

                    string message2 =
                                "Сигнал на открытие сделки. Лонг. strTime: " + strTime
                                + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                                + ", closeLastCandle: " + closeLastCandle.ToString()
                                + ", stage: " + stage.ToString()
                                + ", RSI: " + valueRSIFast.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);
                    dLastTrailValue = 0;// decimal.MaxValue;
                    _iStepTrail = 0;

                    _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
                }
            }
            else
            if (iCondition <= _AlgorithmSensitivityShort.ValueInt //if(iConditionOpenLong < iConditionOpenShort //(valueEMAFast < valueEMAMedium && closeLastCandle < valueEMAFast // && lastCandle.IsDown == true
                   && _Regime.ValueString != "OnlyLong")
            {
                if (downLine < downLine2 && upLine <= upLine2)
                {
                    decimal stage = 0;
                    decimal stage2;
                    if (VolatilityFilterIsOn.ValueBool == true)
                    {
                        stage = _volatilityStages.DataSeries[0].Values[^2];
                        stage2 = _volatilityStages.DataSeries[0].Values[^1];

                        //if (stage < VolatilityStageToTrade.ValueString.ToDecimal()) //
                        if (stage != VolatilityStageToTrade.ValueString.ToDecimal() || stage2 != VolatilityStageToTrade.ValueString.ToDecimal())
                        {
                            return;
                        }
                    }
                    string message2 =
                          "Сигнал на открытие сделки. Шорт. strTime: " + strTime
                        + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                        + ", closeLastCandle: " + closeLastCandle.ToString()
                        + ", stage: " + stage.ToString()
                        + ", RSI: " + valueRSIFast.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);
                    dLastTrailValue = 0;//  decimal.MinValue;
                    _iStepTrail = 0;
                    _tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
                }
            }
        }
        private void LogicClosePosition(List<Candle> candles, Position position, List<Position> positionsAll)
        {
            TimeSpan CurrentTimeFrame = _tabToTrade.TimeFrame;
            DateTime CurrentDateTime = _tabToTrade.TimeServerCurrent;
            TimeSpan TimeSpan = CurrentDateTime.TimeOfDay;
            TimeSpan TimeAdded = TimeSpan + CurrentTimeFrame;
            Boolean bCheck = TimeAdded >= _timeEndOfTrade.Value.TimeSpan;

            if (bCheck && _closeDaily.ValueInt == 1)
            {
                _tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);
                return;
            }

            strTime = lastCandle.TimeStart.ToString();

            if (position.Direction == Side.Buy)
            {
                if (dLastBestPrice == 0)
                {
                    dLastBestPrice = position.EntryPrice;
                }
                if (closeLastCandle > dLastBestPrice)
                {
                    dLastBestPrice = closeLastCandle;// position.EntryPrice;
                }
                if (dLastTrailValue == 0)
                {
                    dLastTrailValue = downLine;// position.EntryPrice;
                }
                if (_UseTrailing.ValueBool && downLine < closeLastCandle)
                {
                    iConditionCloseLong--;
                }
                if (_UseTrailing.ValueBool)
                {
                    iConditionCloseLong += position.ProfitPortfolioPercent < 0.0m && Math.Abs(position.ProfitPortfolioPercent) > _StopLossProcent.ValueDecimal ? 1 : -1;
                }

                if (iConditionCloseLong >= Math.Abs(_AlgorithmSensitivityLong.ValueInt))
                {
                   string message2 =
                     "Сигнал на закрытие сделки Лонг. strTime: " + strTime
                   + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                   + ", closeLastCandle: " + closeLastCandle.ToString()
                   + ", RSI: " + valueRSIFast.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);
                    _CloseAndReversLong(position, positionsAll);
                }
                else // If the direction of the position is sale
                {
                    if (_UseTrailing.ValueBool)
                    {
                        _iStepTrail++;
                        if (downLine > dLastTrailValue)
                        {
                            dLastTrailValue = downLine;

                            //if (_iStepTrail >= _TrailBars.ValueInt)
                            //if (_iStepTrail >= _TrailBars.ValueInt)
                            // {
                            //   dLastTrailValue = lastCandle.Low;
                            //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                            decimal price = dLastTrailValue; //  GetPriceStop(Side.Sell, candles, candles.Count - 1);
                                                             // decimal price = GetPriceStop(Side.Buy, candles, candles.Count - 1);
                            if (price == 0)
                            {
                                return;
                            }
                            _iStepTrail = 0;
                            decimal priceOrder;
                            if (StartProgram == StartProgram.IsOsOptimizer ||
                                StartProgram == StartProgram.IsTester)
                            {
                                priceOrder = price;
                            }
                            else
                            {
                                priceOrder = price - price * (_slippage.ValueDecimal / 100.0m);
                            }

                            _tabToTrade.CloseAtTrailingStop(position, price, priceOrder, "Close long");
                        }
                        else if (downLine > closeLastCandle)
                        {
                            string message2 =
                            "Сигнал на закрытие сделки лонг. strTime: " + strTime
                          + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                          + ", closeLastCandle: " + closeLastCandle.ToString()
                          + ", RSI: " + valueRSIFast.ToString();

                           // 1 only in log window
                           SendNewLogMessage(message2, Logging.LogMessageType.User);

                            _CloseAndReversLong(position, positionsAll);
                        }

                    }
                }
            }
            else if (position.Direction == Side.Sell)
            {
                if (dLastBestPrice == 0)
                {
                    dLastBestPrice = position.EntryPrice;
                }
                if (closeLastCandle > dLastBestPrice)
                {
                    dLastBestPrice = closeLastCandle;
                }
                if (dLastTrailValue == 0)
                {
                    dLastTrailValue = upLine;// decimal.MaxValue;
                }
                if (_UseTrailing.ValueBool && upLine > closeLastCandle)
                {
                    iConditionCloseShort--;
                }
                if (_UseTrailing.ValueBool)
                {
                    iConditionCloseShort += position.ProfitPortfolioPercent < 0.0m && Math.Abs(position.ProfitPortfolioPercent) > _StopLossProcent.ValueDecimal ? 1 : -1;
                }
                if (iConditionCloseShort >= Math.Abs(_AlgorithmSensitivityShort.ValueInt))
                {
                    string message2 =
                        "Сигнал на закрытие сделки шорт. strTime: " + strTime
                      + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                      + ", closeLastCandle: " + closeLastCandle.ToString()
                      + ", RSI: " + valueRSIFast.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);

                    _tabToTrade.CloseAtLimit(position, closeLastCandle + closeLastCandle * (_slippage.ValueDecimal / 100), position.OpenVolume);

                    if (_Regime.ValueString != "OnlyShort"
                        && _Regime.ValueString != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), closeLastCandle + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                        if (_UseRevers.ValueBool == true)
                        {
                            //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), closeLastCandle + closeLastCandle * (_slippage.ValueDecimal / 100));

                            LogicOpenPosition();
                        }
                    }
                }
                else // If the direction of the position is sale
                {
                    if (_UseTrailing.ValueBool)
                    {
                        _iStepTrail++;
                        //if (_iStepTrail >= _TrailBars.ValueInt)
                        if (upLine < dLastTrailValue)
                        {
                            dLastTrailValue = upLine;
                            //if (position.ProfitPortfolioPercent >= _ProfitProcent.ValueDecimal && dLastTrailValue >= lastCandle.High)
                            //    {
                            //dLastTrailValue = lastCandle.High;
                            //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                            decimal price = dLastTrailValue; // GetPriceStop(Side.Sell, candles, candles.Count - 1);
                                                             //decimal price = GetPriceStop(Side.Sell, candles, candles.Count - 1);
                            if (price == 0)
                            {
                                return;
                            }
                            _iStepTrail = 0;
                            decimal priceOrder;
                            if (StartProgram == StartProgram.IsOsOptimizer ||
                                StartProgram == StartProgram.IsTester)
                            {
                                priceOrder = price;
                            }
                            else
                            {
                                priceOrder = price + price * (_slippage.ValueDecimal / 100.0m);
                            }
                            string message2 =
                                "Сигнал на закрытие сделки шорт. strTime: " + strTime
                              + ", valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium: " + valueEMAMedium.ToString()
                              + ", closeLastCandle: " + closeLastCandle.ToString()
                              + ", RSI: " + valueRSIFast.ToString();

                            // 1 only in log window
                            SendNewLogMessage(message2, Logging.LogMessageType.User);
                            _tabToTrade.CloseAtTrailingStop(position, price, priceOrder, "Close short");
                        }
                    }

                }

            }
        }

        private void _CloseAndReversLong(Position position, List<Position> positionsAll)
        {
            _tabToTrade.CloseAtLimit(position, closeLastCandle - closeLastCandle * (_slippage.ValueDecimal / 100), position.OpenVolume);

            if (_Regime.ValueString != "OnlyLong"
                && _Regime.ValueString != "OnlyClosePosition"
                && positionsAll.Count == 1)
            {
                if (_UseRevers.ValueBool == true)
                {
                    //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - closeLastCandle * (_slippage.ValueDecimal / 100));
                    LogicOpenPosition();
                }
            }
        }

        private decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (VolumeType.ValueString == "Contracts")
            {
                volume = Volume.ValueDecimal;
            }
            else if (VolumeType.ValueString == "Contract currency")
            {
                decimal contractPrice = tab.PriceBestAsk;
                volume = Volume.ValueDecimal / contractPrice;

                if (StartProgram == StartProgram.IsOsTrader)
                {
                    IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);

                    if (serverPermission != null &&
                        serverPermission.IsUseLotToCalculateProfit &&
                    tab.Security.Lot != 0 &&
                        tab.Security.Lot > 1)
                    {
                        volume = Volume.ValueDecimal / (contractPrice * tab.Security.Lot);
                    }

                    volume = Math.Round(volume, tab.Security.DecimalsVolume);
                }
                else // Tester or Optimizer
                {
                    volume = Math.Round(volume, 6);
                }
            }
            else if (VolumeType.ValueString == "Deposit percent")
            {
                Portfolio myPortfolio = tab.Portfolio;

                if (myPortfolio == null)
                {
                    return 0;
                }

                decimal portfolioPrimeAsset = 0;

                if (TradeAssetInPortfolio.ValueString == "Prime")
                {
                    portfolioPrimeAsset = myPortfolio.ValueCurrent;
                    if(myPortfolio.ValueCurrent <= 0)
                    {
                        SendNewLogMessage("Portfolio empty!!!" + TradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                        return 0;

                    }
                }
                else
                {
                    List<PositionOnBoard> positionOnBoard = myPortfolio.GetPositionOnBoard();

                    if (positionOnBoard == null)
                    {
                        return 0;
                    }

                    for (int i = 0; i < positionOnBoard.Count; i++)
                    {
                        if (positionOnBoard[i].SecurityNameCode == TradeAssetInPortfolio.ValueString)
                        {
                            portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                            break;
                        }
                    }
                }

                if (portfolioPrimeAsset == 0)
                {
                    SendNewLogMessage("Can`t found portfolio " + TradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                    return 0;
                }
                decimal GO = tab.Security.Go;

                decimal PriceBestAsk;

                if (GO == 0)
                {
                    PriceBestAsk = tab.PriceBestAsk;
                }
                else
                {
                    PriceBestAsk = GO;
                }

                decimal moneyOnPosition = portfolioPrimeAsset * (Volume.ValueDecimal / 100);

                //decimal qty = moneyOnPosition / tab.PriceBestAsk / tab.Security.Lot;
                decimal qty = moneyOnPosition / PriceBestAsk / tab.Security.Lot;

                //if (tab.StartProgram == StartProgram.IsOsTrader)
                //{
                qty = Math.Round(qty, tab.Security.DecimalsVolume);
                //}
                //else
                //{
                //    qty = Math.Round(qty, 7);
                //}

                return qty;
            }

            return volume;
        }
        
        private decimal GetPriceStop(Side side, List<Decimal> candles, int index)
        {
            if (candles == null || index < _TrailBars.ValueInt)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                decimal price = decimal.MaxValue;

                for (int i = index; i > index - _TrailBars.ValueInt; i--)
                {
                    if (candles[i] < price)
                    {
                        price = candles[i];
                    }
                }
                return price;
            }

            if (side == Side.Sell)
            {
                decimal price = 0;

                for (int i = index; i > index - _TrailBars.ValueInt; i--)
                {
                    if (candles[i] > price)
                    {
                        price = candles[i];
                    }
                }

                return price;
            }
            return 0;
        }
        private decimal GetPriceStop(Side side, List<Candle> candles, int index)
        {
            if (candles == null || index < _TrailBars.ValueInt)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                decimal price = decimal.MaxValue;

                for (int i = index; i > index - _TrailBars.ValueInt; i--)
                {
                    if (candles[i].Low < price)
                    {
                        price = candles[i].Low;
                    }
                }
                return price;
            }

            if (side == Side.Sell)
            {
                decimal price = 0;

                for (int i = index; i > index - _TrailBars.ValueInt; i--)
                {
                    if (candles[i].High > price)
                    {
                        price = candles[i].High;
                    }
                }

                return price;
            }
            return 0;
        }

    }

}