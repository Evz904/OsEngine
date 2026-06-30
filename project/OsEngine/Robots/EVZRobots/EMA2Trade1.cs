using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Market.Servers;
using OsEngine.Market;
using CustomIndicators.Scripts.EVZRobots;


namespace OsEngine.Robots.EVZRobots
{
    [Bot("EMA2Trade1")]
    public class EMA2Trade1 : BotPanel
    {

        private BotTabSimple _tabToTrade;
        // private BotTabSimple _tabSmallTf;

        private Aindicator _EMAFast;
        private Aindicator _EMAMedium;
        private Aindicator _volatilityStages;
        private RSI3RAY _RSI;
        private ParabolicSaR _sar;
        private Zamah _zamah;
        private StrategyParameterInt _ZamahLenght;

        public StrategyParameterString _Regime;
        private StrategyParameterInt _emaLenFast;
        private StrategyParameterInt _emaLenMedium;
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

        public StrategyParameterString VolumeType;
        public StrategyParameterDecimal Volume;

        public StrategyParameterString TradeAssetInPortfolio;

        // Exit 
        private StrategyParameterInt _TrailBars;

        //private StrategyParameterInt _maxCountDotInLine;
        //private WindowsFormsHost _host;
        //private Chart _chart;
        private Decimal dLastBestPrice;
        private Decimal dLastProfitProcent;
        private Decimal dLastTrailValue;
        private int _iStepTrail;

        private decimal _lastSar;
        private decimal valueEMAFast;
        private decimal valueEMAFast2;
        private decimal valueEMAMedium;
        private decimal valueRSIFast;
        private decimal valueRSIFast2;
        private decimal valueRSIMedium;
        private decimal valueRSIMedium2;

        private decimal valueRSISlow;
        private decimal valueRSISlow2;

        private Candle lastCandle;
        private Candle lastCandle2;

        private decimal dBullsZamah;
        private decimal dBearsZamah;
        private decimal closeLastCandle;

        private int iCondition;
        private int iConditionCloseShort;
        private int iConditionCloseLong;

        private int iDirectionRSI; // -1 - degree, 0 - flat, 1 - grow
        private int iDirectionRSI1; // -1 - degree, 0 - flat, 1 - grow

        public EMA2Trade1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            // TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];
            //_tabSmallTf = TabsSimple[1];
            _tabToTrade.CandleFinishedEvent += _tabToTrade_CandleFinishedEvent;
            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });


            _emaLenFast = CreateParameter("Ema fast len", 9, 4, 10, 1);
            _emaLenMedium = CreateParameter("Ema medium len", 20, 9, 32, 1);

            _RSIUp = CreateParameter("RSI upper range", 65m, 50m, 99.9m, 0.5m);
            _RSILow = CreateParameter("RSI low range", 35m, 1m, 49.99m, 0.5m);
            _RSILengthFast = CreateParameter("RSI periods fast", 6, 1, 200, 1);
            _RSILengthMedium = CreateParameter("RSI periods medium", 14, 2, 100, 1);
            _RSILengthSlow = CreateParameter("RSI periods slow", 24, 1, 400, 1);
            _bByVolume = CreateParameter("By volume", false);
            _ZamahLenght = CreateParameter("Zamah lenght", 6, 1, 300, 1);

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
            _TrailBars = CreateParameter("Trail Bars", 5, 1, 50, 1, "Exit");
            _AlgorithmSensitivityLong = CreateParameter("Algorithm sensitivity long", 3, 1, 10, 1);
            _AlgorithmSensitivityShort = CreateParameter("Algorithm sensitivity short", -3, 1, 10, 1);

            _ProfitProcent = CreateParameter("Profit %", 1.5m, 0.01m, 99.9m, 0.1m);
            _StopLossProcent = CreateParameter("Stop los %", 1.0m, 0.01m, 99.9m, 0.1m);
            VolumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            Volume = CreateParameter("Volume", 40, 1.0m, 50, 4);

            _slippage = CreateParameter("Slippage percent", 5.0m, 0, 10, 1);

            _RSI = (RSI3RAY)IndicatorsFactory.CreateIndicatorByName("RSI3RAY", "RSI3RAY", false); //new RSI3RAY(); //
            _RSI = (RSI3RAY)_tabToTrade.CreateCandleIndicator(_RSI, "RSIArea");
            _RSI.ParametersDigit[0].Value = _RSILengthFast.ValueInt;
            _RSI.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;
            _RSI.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;
            _RSI.bByVolume.ValueBool = _bByVolume.ValueBool;

            _EMAFast = IndicatorsFactory.CreateIndicatorByName("Ema", "Ema9", false);
            _EMAFast = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAFast, "Prime");

            _EMAFast.ParametersDigit[0].Value = _emaLenFast.ValueInt;

            _EMAMedium = IndicatorsFactory.CreateIndicatorByName("Ema", "Ema20", false);
            _EMAMedium = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAMedium, "Prime");
            _EMAMedium.ParametersDigit[0].Value = _emaLenMedium.ValueInt;

            _zamah = (Zamah)IndicatorsFactory.CreateIndicatorByName("Zamah", "Zamah", false);
            _zamah = (Zamah)_tabToTrade.CreateCandleIndicator(_zamah, "ZamahArea");
            _zamah.ParametersDigit[0].Value = _ZamahLenght.ValueInt;

            _UseRevers = CreateParameter("Use revers", false);

            VolatilityFilterIsOn = CreateParameter("Volatility filter is on", true);
            VolatilityStageToTrade = CreateParameter("Volatility stage to trade", "2", new[] { "1", "2", "3", "4" });
            VolatilitySlowSmaLength = CreateParameter("Volatility slow sma length", 25, 10, 80, 3);
            VolatilityFastSmaLength = CreateParameter("Volatility fast sma length", 7, 10, 80, 3);
            VolatilityChannelDeviation = CreateParameter("Volatility channel deviation", 0.4m, 1.0m, 50, 4);

            _volatilityStages = IndicatorsFactory.CreateIndicatorByName("VolatilityStagesAW", name + "VolatilityStages", false);
            _volatilityStages = (Aindicator)_tabToTrade.CreateCandleIndicator(_volatilityStages, "VolatilityStagesArea");
            _volatilityStages.ParametersDigit[0].Value = VolatilitySlowSmaLength.ValueInt;
            _volatilityStages.ParametersDigit[1].Value = VolatilityFastSmaLength.ValueInt;
            _volatilityStages.ParametersDigit[2].Value = VolatilityChannelDeviation.ValueDecimal;
            //  _volatilityStages.Parameters[0].LoadParamFromString("3");

            TradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            ParametrsChangeByUser += Event_ParametrsChangeByUser;
            // Мои переменные
            dLastBestPrice = 0.0m;
            dLastProfitProcent = 0.0m;
        }

        public override string GetNameStrategyType()
        {
            return "EMA2Trade1";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        void Event_ParametrsChangeByUser()
        {
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

            if (_emaLenFast.ValueInt != _EMAFast.ParametersDigit[0].Value)
            {
                _EMAFast.ParametersDigit[0].Value = _emaLenFast.ValueInt;

                _EMAFast.Reload();
            }

            if (_emaLenMedium.ValueInt != _EMAMedium.ParametersDigit[0].Value)
            {
                _EMAMedium.ParametersDigit[0].Value = _emaLenMedium.ValueInt;

                _EMAMedium.Reload();
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
        private void _tabToTrade_CandleFinishedEvent(List<Candle> candles)
        {
            // вызывается на каждой новой свече
            int iCount = candles.Count;
            if (_Regime.ValueString == "Off" || iCount < _emaLenMedium.ValueInt) 
            {
                return;
            }

            lastCandle = candles[candles.Count - 1];
            lastCandle2 = candles[candles.Count - 2];
            string strTime = lastCandle.TimeStart.ToString();

            _lastSar = _sar.Values[_sar.Values.Count - 1];
            
            List<Position> openPositions = _tabToTrade.PositionsOpenAll;
          

            valueEMAFast = _EMAFast.DataSeries[0].Last;
            valueEMAFast2 = _EMAFast.DataSeries[0].Values[_EMAFast.DataSeries[0].Values.Count - 2];
            
            valueEMAMedium = _EMAMedium.DataSeries[0].Last;

            valueRSIFast = _RSI.DataSeries[0].Last;
            valueRSIFast2 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            
            valueRSIMedium = _RSI.DataSeries[1].Last;
            valueRSIMedium2 = _RSI.DataSeries[1].Values[_RSI.DataSeries[1].Values.Count - 2];

            valueRSISlow = _RSI.DataSeries[2].Last;
            valueRSISlow2 = _RSI.DataSeries[2].Values[_RSI.DataSeries[2].Values.Count - 2];

            dBullsZamah = _zamah.SeriesBulls.Last;
            dBearsZamah = _zamah.SeriesBears.Last;

            closeLastCandle = lastCandle.Close;

            CalculateCondition();

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
                    LogicOpenPosition(candles);
                }
            }
        }
        private void CalculateCondition()
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
            else if (valueRSIFast > valueRSIMedium && valueRSIFast > valueRSIFast2)// && dLastRSIFast >= _RSILow.ValueDecimal) // dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = 1;// + (dLastRSIMedium > dLastRSISlow ? 1 : 0);
            }

            iCondition = iDirectionRSI;

            iConditionCloseShort = iDirectionRSI == 1 ? 1 : 0;
            iConditionCloseLong = iDirectionRSI == -1 ? 1 : 0;

            iCondition += dBearsZamah > dBullsZamah ? -1 : dBearsZamah < dBullsZamah ? 1 : 0;

            iConditionCloseShort += dBearsZamah < dBullsZamah ? 1 : 0;
            iConditionCloseLong += dBearsZamah > dBullsZamah ? 1 : 0;

            iCondition += closeLastCandle > _lastSar ? 1 : closeLastCandle < _lastSar ? -1 : 0;

            iConditionCloseShort += closeLastCandle > _lastSar ? 1 : 0;
            iConditionCloseLong += closeLastCandle < _lastSar ? 1 : 0;

            iCondition += lastCandle.IsDown == true ? -1 : lastCandle.IsUp == true ? 1 : 0;

            iConditionCloseShort += lastCandle.IsDown != true ? 1 : 0;
            iConditionCloseLong += lastCandle.IsUp != true ? 1 : 0;

            iCondition += valueEMAFast - valueEMAFast2 > 0 ? 1 : valueEMAFast - valueEMAFast2 == 0 ? 0 : -1;

            iConditionCloseShort += valueEMAFast - valueEMAFast2 > 0 ? 1 : 0;
            iConditionCloseLong += valueEMAFast - valueEMAFast2 < 0 ? 1 : 0;

            iCondition += valueEMAFast - valueEMAMedium >= 0 ? 1 : -1;

            iConditionCloseShort += valueEMAFast - valueEMAMedium > 0 ? 1 : 0;
            iConditionCloseLong += valueEMAFast - valueEMAMedium < 0 ? 1 : 0;

            iCondition += closeLastCandle - valueEMAFast > 0 ? 1 : -1;

            iConditionCloseShort += closeLastCandle - valueEMAFast > 0 ? 1 : 0;
            iConditionCloseLong += closeLastCandle - valueEMAFast < 0 ? 1 : 0;

            iCondition += closeLastCandle - valueEMAMedium > 0 ? 1 : -1;

            iConditionCloseShort += closeLastCandle - valueEMAMedium > 0 ? 1 : 0;
            iConditionCloseLong += closeLastCandle - valueEMAMedium < 0 ? 1 : 0;
        }

        private void LogicOpenPosition(List<Candle> candles)
        {

            string strTime = lastCandle.TimeStart.ToString();
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
                decimal stage = 0;
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    //if (stage < VolatilityStageToTrade.ValueString.ToDecimal()) 
                    if (stage != VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }
                 }
                
                string message2 =
                            "Сигнал на открытие сделки. Лонг. strTime:" + strTime
                            + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString()
                            + ", closeLastCandle:" + closeLastCandle.ToString()
                            + ", stage:" + stage.ToString()
                            + ", RSI:" + valueRSIFast.ToString();

                // 1 only in log window
                SendNewLogMessage(message2, Logging.LogMessageType.User);
                dLastTrailValue = 0;

                _iStepTrail = 0;
                _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
            }
            else
            if (iCondition <= _AlgorithmSensitivityShort.ValueInt //if(iConditionOpenLong < iConditionOpenShort //(valueEMAFast < valueEMAMedium && closeLastCandle < valueEMAFast // && lastCandle.IsDown == true
                   && _Regime.ValueString != "OnlyLong")
            {
                decimal stage = 0;
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    //if (stage < VolatilityStageToTrade.ValueString.ToDecimal()) 
                    if (stage != VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }
                }
                string message2 =
                      "Сигнал на открытие сделки. Шорт. strTime:" + strTime
                    + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString()
                    + ", closeLastCandle:" + closeLastCandle.ToString()
                    + ", stage:" + stage.ToString()
                    + ", RSI:" + valueRSIFast.ToString();

                // 1 only in log window
                SendNewLogMessage(message2, Logging.LogMessageType.User);
                dLastTrailValue = 0;
                _iStepTrail = 0;
                _tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
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

            string strTime = lastCandle.TimeStart.ToString();

            if (_UseTrailing.ValueBool)
            {
                iCondition += position.ProfitOperationPersent < 0.0m && Math.Abs(position.ProfitOperationPersent) > _StopLossProcent.ValueDecimal ? 1 : -1;
                //iConditionCloseShort += position.ProfitOperationPersent < 0.0m && Math.Abs(position.ProfitOperationPersent) > _StopLossProcent.ValueDecimal ? 1.0m : -1.0m;
            }

            if (position.Direction == Side.Buy)
            {
                //if (valueEMAFast < valueEMAMedium && closeLastCandle < valueEMAFast || iDirectionRSI == -1) //|| dLastRSI < 50.0m)// _RSIUp.ValueDecimal)// && lastCandle2.IsDown == true) 
                //if (iConditionCloseLong > iConditionCloseShort) // (closeLastCandle < valueEMAMedium || iDirectionRSI == -1) //|| dLastRSI < 50.0m)// _RSIUp.ValueDecimal)// && lastCandle2.IsDown == true) 
                // if (iCondition <= _AlgorithmSensitivityShort.ValueInt)
                if (iConditionCloseLong >= Math.Abs(_AlgorithmSensitivityLong.ValueInt))
                {
                    //string message2 =
                    //        "Сигнал на закрытие лонга. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();
                    //// 1 only in log window
                    //SendNewLogMessage(message2, Logging.LogMessageType.User);
                    // _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
                    // _tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);
                    //_tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
                    _tabToTrade.CloseAtLimit(position, closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep, position.OpenVolume);

                    if (_Regime.ValueString != "OnlyLong"
                        && _Regime.ValueString != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        if (_UseRevers.ValueBool == true)
                        {
                            // _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            LogicOpenPosition(candles);
                        }
                    }
                }
                else // If the direction of the position is sale
                {
                    if(dLastBestPrice == 0)
                    {
                        dLastBestPrice = position.EntryPrice;
                    }
                    if (closeLastCandle < dLastBestPrice)
                    {
                        dLastBestPrice = closeLastCandle;
                    }
                        if (_UseTrailing.ValueBool)
                    {
                        _iStepTrail++;
                       // if (position.ProfitOperationPersent >= _ProfitProcent.ValueDecimal && dLastTrailValue <= lastCandle.Low)
                       // {
                            dLastTrailValue = lastCandle.Low;
                            //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                            //decimal price = dLastTrailValue; //  GetPriceStop(Side.Sell, candles, candles.Count - 1);
                            decimal price =  GetPriceStop(Side.Buy, candles, candles.Count - 1);
                            if (price == 0)
                            {
                                return;
                            }
                            _iStepTrail = 0;
                            _tabToTrade.CloseAtTrailingStop(position, price, price - _slippage.ValueDecimal);
                        //}
                    }
                }
            }
            else if (position.Direction == Side.Sell)
            {
                //if (valueEMAFast > valueEMAMedium && closeLastCandle > valueEMAFast || iDirectionRSI == 1) // dLastRSI > 50.0m) //&& lastCandle2.IsUp == true)
                //if (iConditionCloseShort > iConditionCloseLong)  // (closeLastCandle > valueEMAMedium || iDirectionRSI == 1) // dLastRSI > 50.0m) //&& lastCandle2.IsUp == true)
                //if (iCondition >= _AlgorithmSensitivityLong.ValueInt)
                if (iConditionCloseShort >= Math.Abs(_AlgorithmSensitivityShort.ValueInt))
                {
                    //string message2 =
                    // "Сигнал на закрытие шорта. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();

                    //// 1 only in log window
                    //SendNewLogMessage(message2, Logging.LogMessageType.User);

                    // _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
                    //_tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);
                    _tabToTrade.CloseAtLimit(position, closeLastCandle + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep, position.OpenVolume);

                    if (_Regime.ValueString != "OnlyShort"
                        && _Regime.ValueString != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), closeLastCandle + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                        if (_UseRevers.ValueBool == true)
                        {
                            // _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            LogicOpenPosition(candles);
                        }
                    }
                }
                else // If the direction of the position is sale
                {
                    if (dLastBestPrice == 0)
                    {
                        dLastBestPrice = position.EntryPrice;
                    }
                    if (closeLastCandle > dLastBestPrice)
                    {
                        dLastBestPrice = closeLastCandle;
                    }
                        if (_UseTrailing.ValueBool)
                        {
                        _iStepTrail++;
                        //if (position.ProfitOperationPersent >= _ProfitProcent.ValueDecimal && dLastTrailValue >= lastCandle.High)
                        //    {
                                dLastTrailValue = lastCandle.High;
                        //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                        // decimal price = dLastTrailValue; // GetPriceStop(Side.Sell, candles, candles.Count - 1);
                        decimal price = GetPriceStop(Side.Sell, candles, candles.Count - 1);
                        if (price == 0)
                                {
                                    return;
                                }
                            _iStepTrail = 0;
                            _tabToTrade.CloseAtTrailingStop(position, price, price + _slippage.ValueDecimal);
                        //    }
                        }
                
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
                    tab.Securiti.Lot != 0 &&
                        tab.Securiti.Lot > 1)
                    {
                        volume = Volume.ValueDecimal / (contractPrice * tab.Securiti.Lot);
                    }

                    volume = Math.Round(volume, tab.Securiti.DecimalsVolume);
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

                decimal moneyOnPosition = portfolioPrimeAsset * (Volume.ValueDecimal / 100);

                decimal qty = moneyOnPosition / tab.PriceBestAsk / tab.Securiti.Lot;

                if (tab.StartProgram == StartProgram.IsOsTrader)
                {
                    qty = Math.Round(qty, tab.Securiti.DecimalsVolume);
                }
                else
                {
                    qty = Math.Round(qty, 7);
                }

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