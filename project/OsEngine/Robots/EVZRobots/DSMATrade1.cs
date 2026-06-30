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
    [Bot("DSMATrade1")]
    public class DSMATrade1 : BotPanel
    {

        private BotTabSimple _tabToTrade;
        //  private BotTabSimple _tabSmallTf;

        private Aindicator _DSMAFast;

        //  private Aindicator _EMAMedium;

        private Aindicator _volatilityStages;

        private RSI3RAY _RSI;

        private ParabolicSaR _sar;

        private decimal _lastPrice;

        private decimal _lastSar;

        public StrategyParameterString _Regime;
        private StrategyParameterInt _DSMALenFast;
        // private StrategyParameterInt _emaLenMedium;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterDecimal _RSIUp;
        private StrategyParameterDecimal _RSILow;
        // private StrategyParameterInt _RSILength;

        private StrategyParameterInt _RSILengthFast;
        private StrategyParameterInt _RSILengthMedium;
        private StrategyParameterInt _RSILengthSlow;
        private StrategyParameterBool _bByVolume;

        //  private StrategyParameterString _candlePoint;


        private StrategyParameterTimeOfDay _timeEndOfTrade;
        private StrategyParameterInt _closeDaily;

        private StrategyParameterDecimal _ProfitProcent;
        private StrategyParameterDecimal _StopLossProcent;

        private StrategyParameterBool _UseRevers;
        public StrategyParameterBool VolatilityFilterIsOn;
        public StrategyParameterString VolatilityStageToTrade;
        public StrategyParameterInt VolatilitySlowSmaLength;
        public StrategyParameterInt VolatilityFastSmaLength;
        public StrategyParameterDecimal VolatilityChannelDeviation;

        public StrategyParameterString VolumeType;
        public StrategyParameterDecimal Volume;

        public StrategyParameterString TradeAssetInPortfolio;

        public StrategyParameterDecimal ParabolicAf;
        public StrategyParameterDecimal ParabolicMaxAf;

        //private StrategyParameterInt _maxCountDotInLine;
        //private WindowsFormsHost _host;
        //private Chart _chart;
        // Exit 
        private StrategyParameterInt _TrailBars;
        private StrategyParameterBool _UseTrailing;
        private int _iStepTrail;
        private Decimal dLastBestPrice;
        private Decimal dLastProfitProcent;
        private Decimal dLastTrailValue;

        // last calculation mode / последнее значение метода вычисления
        private string _lastCalculationMode = "";


        private decimal _valueEMAFast2 = 0;
        public DSMATrade1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            // TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];
            // _tabSmallTf = TabsSimple[1];
            //_tabToTrade.;

            //   _maxCountDotInLine = CreateParameter("Max count dot in line", 10, 10, 100, 10);


            _tabToTrade.CandleFinishedEvent += _tabToTrade_CandleFinishedEvent;


            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });

            _timeEndOfTrade = CreateParameterTimeOfDay("Time close trade", 23, 00, 0, 0);
            _closeDaily = CreateParameter("Close daily", 0, 0, 1, 1);
            _DSMALenFast = CreateParameter("DSMA len", 40, 4, 500, 1);
            //_emaLenMedium = CreateParameter("Ema medium len", 20, 9, 32, 1);

            _RSIUp = CreateParameter("RSI upper range", 60m, 50m, 99.9m, 0.5m);
            _RSILow = CreateParameter("RSI low range", 40m, 1m, 49.99m, 0.5m);
            _RSILengthFast = CreateParameter("RSI periods fast", 6, 1, 10, 1);
            _RSILengthMedium = CreateParameter("RSI periods medium", 14, 1, 10, 1);
            _RSILengthSlow = CreateParameter("RSI periods slow", 24, 1, 10, 1);
            _bByVolume = CreateParameter("By volume", false);
            //  _candlePoint = CreateParameter("Candle Point", "Close", Entity.CandlePointsArray);


            _ProfitProcent = CreateParameter("Profit %", 0.5m, 0.01m, 99.9m, 0.1m);
            _StopLossProcent = CreateParameter("Stop los %", 0.5m, 0.01m, 99.9m, 0.1m);
            VolumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            Volume = CreateParameter("Volume", 40, 1.0m, 50, 4);

            _slippage = CreateParameter("Slippage percent", 5.0m, 0, 10, 1);

            _RSI = (RSI3RAY)IndicatorsFactory.CreateIndicatorByName("RSI3RAY", "RSI3RAY", false); //new RSI3RAY(); //
            _RSI = (RSI3RAY)_tabToTrade.CreateCandleIndicator(_RSI, "RSIArea");
            _RSI.ParametersDigit[0].Value = _RSILengthFast.ValueInt;
            _RSI.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;
            _RSI.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;
            //_RSI.ParametersDigit[3].Value = _bByVolume.ValueBool;

            _DSMAFast = IndicatorsFactory.CreateIndicatorByName("DSMA", "DSMA", false);
            _DSMAFast = (Aindicator)_tabToTrade.CreateCandleIndicator(_DSMAFast, "Prime");
            //_EMAFast.DataSeries
            _DSMAFast.ParametersDigit[0].Value = _DSMALenFast.ValueInt;

            ParabolicAf = CreateParameter("Parabolic Af", 0.02m, 0.02m, 0.1m, 0.01m);
            ParabolicMaxAf = CreateParameter("Parabolic Max Af", 0.2m, 0.2m, 0.5m, 0.1m);

            _sar = new ParabolicSaR(name + "Prime", false);
            _sar = (ParabolicSaR)_tabToTrade.CreateCandleIndicator(_sar, "Prime");
            _sar.Af = Convert.ToDouble(ParabolicAf.ValueDecimal);
            _sar.MaxAf = Convert.ToDouble(ParabolicMaxAf.ValueDecimal);
            _sar.Save();


            //_EMAMedium = IndicatorsFactory.CreateIndicatorByName("Ema", "Ema20", false);
            //_EMAMedium = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAMedium, "Prime");
            //_EMAMedium.ParametersDigit[0].Value = _emaLenMedium.ValueInt;

            _UseRevers = CreateParameter("Use revers", false);
            // Exit
            _TrailBars = CreateParameter("Trail Bars", 5, 1, 50, 1, "Exit");

            _UseTrailing = CreateParameter("Use Trailing", false, "Exit");

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
            return "DSMATrade1";
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

            if (_DSMALenFast.ValueInt != _DSMAFast.ParametersDigit[0].Value)
            {
                _DSMAFast.ParametersDigit[0].Value = _DSMALenFast.ValueInt;

                _DSMAFast.Reload();
            }

            //if (_emaLenMedium.ValueInt != _EMAMedium.ParametersDigit[0].Value)
            //{
            //    _EMAMedium.ParametersDigit[0].Value = _emaLenMedium.ValueInt;

            //    _EMAMedium.Reload();
            //}

            if (_RSILengthFast.ValueInt != _RSI.ParametersDigit[0].Value)
            {
                _RSI.ParametersDigit[0].Value = _RSILengthFast.ValueInt;

                _RSI.Reload();
            }
            if (_RSILengthSlow.ValueInt != _RSI.ParametersDigit[2].Value)
            {
                _RSI.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;

                _RSI.Reload();
            }
            if (_RSILengthMedium.ValueInt != _RSI.ParametersDigit[1].Value)
            {
                _RSI.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;

                _RSI.Reload();
            }

            if (_volatilityStages.ParametersDigit[0].Value != VolatilitySlowSmaLength.ValueInt
                || _volatilityStages.ParametersDigit[1].Value != VolatilityFastSmaLength.ValueInt
                || _volatilityStages.ParametersDigit[2].Value != VolatilityChannelDeviation.ValueDecimal)
            {
                _volatilityStages.ParametersDigit[0].Value = VolatilitySlowSmaLength.ValueInt;
                _volatilityStages.ParametersDigit[1].Value = VolatilityFastSmaLength.ValueInt;
                _volatilityStages.ParametersDigit[2].Value = VolatilityChannelDeviation.ValueDecimal;

                _volatilityStages.Reload();
            }
        }

        private void _tabToTrade_CandleFinishedEvent(List<Candle> candles)
        {
            // вызывается на каждой новой свече

            if (_Regime.ValueString == "Off")
            {
                return;
            }

            if (_sar.Values == null)
            {
                return;
            }

            if (candles.Count < _DSMALenFast.ValueInt)
            {
                return;
            }
            _lastPrice = candles[candles.Count - 1].Close;
            _lastSar = _sar.Values[_sar.Values.Count - 1];

            List<Position> openPositions = _tabToTrade.PositionsOpenAll;
            Candle lastCandle = candles[candles.Count - 1];


            string strTime = lastCandle.TimeStart.ToString();

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
            //else
            //{
            //    LogicClosePosition(candles, openPositions[0]);
            //}
        }

        private void LogicOpenPosition(List<Candle> candles)
        {
            //decimal lastPrice = candles[candles.Count - 1].Close;
            //decimal lastPcUp = _bollinger.DataSeries[0].Values[_bollinger.DataSeries[0].Values.Count - 1];
            //decimal lastPcDown = _bollinger.DataSeries[1].Values[_bollinger.DataSeries[1].Values.Count - 1];

            decimal valueEMAFast = _DSMAFast.DataSeries[0].Last;

            decimal valueRSI = _RSI.DataSeries[0].Last;
            decimal valueRSI2 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            decimal valueRSIMedium = _RSI.DataSeries[1].Last;

            if (valueRSI == 0 || valueRSI2 == 0)
            {
                return;
            }

            Candle lastCandle2 = candles[candles.Count - 2];
            // decimal lowLastCandle2 = lastCandle2.Low;
            // decimal closeLastCandle2 = lastCandle2.Close;

            Candle lastCandle = candles[candles.Count - 1];


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

            decimal dLastRSIFast = _RSI.DataSeries[0].Last;
            decimal dLastRSIMedium = _RSI.DataSeries[1].Last;
            decimal dLastRSISlow = _RSI.DataSeries[2].Last;
            decimal dLastRSIFast1 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            int iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
            if (dLastRSIFast < dLastRSIMedium && dLastRSIMedium > 0 && dLastRSIFast < dLastRSIFast1 && dLastRSIFast < _RSIUp.ValueDecimal)// dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = -1;
            }
            else if (dLastRSIFast > dLastRSIMedium && dLastRSIMedium > 0 && dLastRSIFast > dLastRSIFast1 && dLastRSIFast > _RSILow.ValueDecimal) // dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = 1;
            }

            //  decimal lowLastCandle = lastCandle.Low;
            //  decimal highLastCandle = lastCandle.High;
            decimal closeLastCandle = lastCandle.Close;
            decimal iConditionOpenLong = 0;
            decimal iConditionOpenShort = 0;

            //if (candles.Count >= _DSMALenFast.ValueInt && valueEMAFast != 0)//_EMAFast.ParametersDigit[0].Value)
            if (valueEMAFast != 0)//_EMAFast.ParametersDigit[0].Value)
            {
                iConditionOpenLong = closeLastCandle > valueEMAFast ? 0.5m : 0;
                iConditionOpenShort = closeLastCandle < valueEMAFast ? 0.5m : 0;
            }
            iConditionOpenLong += closeLastCandle > _lastSar ? 0.5m : 0;
            iConditionOpenShort += closeLastCandle < _lastSar ? 0.5m : 0;
            
            iConditionOpenLong += (iDirectionRSI == 1) ? 0.5m : 0;
            iConditionOpenShort += (iDirectionRSI == -1) ? 0.5m : 0;

            if (iConditionOpenShort == iConditionOpenLong)
            {
                return;
            }

            if (iConditionOpenLong > iConditionOpenShort// && lastCandle.IsUp == true
               && _Regime.ValueString != "OnlyShort")
            {
                decimal stage = 0;
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    if (stage != VolatilityStageToTrade.ValueString.ToDecimal()) //if (stage != VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }

                }

                if (valueRSIMedium > valueRSI || valueRSI2 > valueRSI)//if (valueRSIMedium < valueRSI )//valueRSI < _RSIUp.ValueDecimal
                {
                    return;
                }

                string message2 =
                            "Сигнал на открытие сделки. Лонг. strTime:" + strTime
                            + "valueEMAFast: " + valueEMAFast.ToString()
                            + ", closeLastCandle:" + closeLastCandle.ToString()
                            + ", stage:" + stage.ToString()
                            + ", RSI:" + valueRSI.ToString();

                // 1 only in log window
                SendNewLogMessage(message2, Logging.LogMessageType.User);
                dLastTrailValue = 0;
                _iStepTrail = 0;
                _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
            }
            else
               if (iConditionOpenShort > iConditionOpenLong // && lastCandle.IsDown == true
                   && _Regime.ValueString != "OnlyLong")
            {
                decimal stage = 0;
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    if (stage != VolatilityStageToTrade.ValueString.ToDecimal()) //if (stage != VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }

                }
                if (valueRSIMedium < valueRSI || valueRSI2 < valueRSI)//valueRSI >= _RSILow.ValueDecimal
                {
                    return;
                }

                string message2 =
                      "Сигнал на открытие сделки. Шорт. strTime:" + strTime
                    + "valueEMAFast: " + valueEMAFast.ToString()
                    + ", closeLastCandle:" + closeLastCandle.ToString()
                    + ", stage:" + stage.ToString()
                    + ", RSI:" + valueRSI.ToString();

                // 1 only in log window
                SendNewLogMessage(message2, Logging.LogMessageType.User);
                dLastTrailValue = 0;
                _iStepTrail = 0;
                _tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
            }
        }
        private void LogicClosePosition(List<Candle> candles, Position position, List<Position> positionsAll)
        {
            decimal valueEMAFast = _DSMAFast.DataSeries[0].Last;

            //if (valueEMAFast == 0)
            //{
            //    return;
            //}

            decimal dLastRSIFast1 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            decimal dLastRSIMedium1 = _RSI.DataSeries[1].Values[_RSI.DataSeries[1].Values.Count - 2];
            decimal dLastRSISlow1 = _RSI.DataSeries[2].Values[_RSI.DataSeries[2].Values.Count - 2];
            //int iDirectionRSI1 = 0; // -1 - degree, 0 - flat, 1 - grow
            //if (dLastRSIFast1 < dLastRSIMedium1 && dLastRSIMedium1 > dLastRSISlow1)
            //{
            //    iDirectionRSI1 = -1;
            //}
            //else if (dLastRSIFast1 > dLastRSIMedium1 && dLastRSIMedium1 < dLastRSISlow1)
            //{
            //    iDirectionRSI1 = 1;
            //}
            //else if (dLastRSIFast1 < dLastRSIMedium1 && dLastRSIMedium1 < dLastRSISlow1)
            //{
            //    iDirectionRSI1 = -1;
            //}

            Candle lastCandle2 = candles[candles.Count - 2];
            Candle lastCandle = candles[candles.Count - 1];

            decimal dLastRSIFast = _RSI.DataSeries[0].Last;
            decimal dLastRSIMedium = _RSI.DataSeries[1].Last;
            decimal dLastRSISlow = _RSI.DataSeries[2].Last;
            int iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
            if (dLastRSIFast < dLastRSIMedium && dLastRSIMedium > 0 && dLastRSIFast < dLastRSIFast1 && dLastRSIFast < _RSIUp.ValueDecimal)// dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = -1;
            }
            else if (dLastRSIFast > dLastRSIMedium && dLastRSIMedium > 0 && dLastRSIFast > dLastRSIFast1 && dLastRSIFast > _RSILow.ValueDecimal) // dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = 1;
            }


            decimal closeLastCandle = lastCandle.Close;
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

            decimal iConditionCloseLong;
            decimal iConditionCloseShort;

            //if (candles.Count >= _DSMAFast.ParametersDigit[0].Value)
            //if(valueEMAFast !=0)
            //{
                iConditionCloseLong = (closeLastCandle < valueEMAFast) ? 0.5m : 0;
                iConditionCloseShort = (closeLastCandle > valueEMAFast) ? 0.5m : 0;

                iConditionCloseLong += (iDirectionRSI == -1) ? 0.5m : 0;
                iConditionCloseShort += (iDirectionRSI == 1) ? 0.5m : 0;

                iConditionCloseLong += closeLastCandle < _lastSar ? 0.5m : 0;
                iConditionCloseShort += closeLastCandle > _lastSar ? 0.5m : 0;
            if (_UseTrailing.ValueBool)
            {
                iConditionCloseLong += position.ProfitOperationPersent < 0.0m && Math.Abs(position.ProfitOperationPersent) > _StopLossProcent.ValueDecimal ? 1.0m : -1.0m;
                iConditionCloseShort += position.ProfitOperationPersent < 0.0m && Math.Abs(position.ProfitOperationPersent) > _StopLossProcent.ValueDecimal ? 1.0m : -1.0m;
            }

            //}
            //else
            //{
            //    iConditionCloseLong = closeLastCandle < _lastSar ? 0.5m : 0;
            //    iConditionCloseShort = closeLastCandle > _lastSar ? 0.5m : 0;
            //}

            //if (iConditionCloseShort == iConditionCloseLong && iDirectionRSI == 0)
            //{
            //    return;
            //}

            if (position.Direction == Side.Buy)
            {
                if (iConditionCloseLong > iConditionCloseShort) //|| closeLastCandle < valueEMAFast  || dLastRSI < 50.0m)// _RSIUp.ValueDecimal)// && lastCandle2.IsDown == true) 
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
                            _iStepTrail = 0;
                            _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            //LogicOpenPosition(candles);
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
                        if ((position.ProfitOperationPersent >= _ProfitProcent.ValueDecimal || _iStepTrail >= _TrailBars.ValueInt) && dLastTrailValue <= lastCandle.Low )
                        {
                            dLastTrailValue = lastCandle.Low;
                            //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                            decimal price = dLastTrailValue; //  GetPriceStop(Side.Sell, candles, candles.Count - 1);
                            if (price == 0)
                            {
                                return;
                            }
                            _iStepTrail = 0;
                            _tabToTrade.CloseAtTrailingStop(position, price, price - _slippage.ValueDecimal);
                        }
                    }
                }
            }
            else if (position.Direction == Side.Sell)
            {
                if (iConditionCloseShort > iConditionCloseLong) // closeLastCandle > valueEMAFast dLastRSI > 50.0m) //&& lastCandle2.IsUp == true)
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
                            //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            _iStepTrail = 0;
                            _tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), _lastPrice + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            //LogicOpenPosition(candles);
                        }

                    }
                }
                else // If the direction of the position is sale
                {
                    if (dLastBestPrice == 0)
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
                            if ((position.ProfitOperationPersent >= _ProfitProcent.ValueDecimal || _iStepTrail >= _TrailBars.ValueInt) && (dLastTrailValue >= lastCandle.High || dLastTrailValue == 0) )
                            {
                                dLastTrailValue = lastCandle.High;
                                //decimal price = GetPriceStop(Side.Sell, _EMAFast.DataSeries[0].Values, _EMAFast.DataSeries[0].Values.Count - 1);
                                decimal price = dLastTrailValue; // GetPriceStop(Side.Sell, candles, candles.Count - 1);
                                if (price == 0)
                                {
                                    return;
                                }
                                _iStepTrail = 0;
                                _tabToTrade.CloseAtTrailingStop(position, price, price + _slippage.ValueDecimal);
                            }
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