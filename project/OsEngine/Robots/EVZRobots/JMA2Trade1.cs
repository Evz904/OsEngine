using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Market.Servers;
using OsEngine.Market;
using System.IO;
using CustomIndicators.Scripts.EVZRobots;


namespace OsEngine.Robots.EVZRobots
{
    [Bot("JMA2Trade1")]
    public class JMA2Trade1 : BotPanel
    {
        private const string V = "C:\\temp\\dataJMA.csv";
        private BotTabSimple _tabToTrade;
        
        private Aindicator _JMAFast;
        private Aindicator _JMAMedium;

        private Aindicator _volatilityStages;

        private RSI3RAY _RSI;

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
        private StrategyParameterBool _bByVolume;

        //  private StrategyParameterString _candlePoint;

        private StrategyParameterBool _UseDataLog;

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
        private int lineNum = 1;

        StreamWriter writer;
        public JMA2Trade1(
            string name,
            StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            //TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];
            //_tabSmallTf = TabsSimple[1];
            //_tabToTrade.; 

            //   _maxCountDotInLine = CreateParameter("Max count dot in line", 10, 10, 100, 10);



            _tabToTrade.CandleFinishedEvent += _tabToTrade_CandleFinishedEvent;


            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            _UseDataLog = CreateParameter("_Use data log", false);
            _timeEndOfTrade = CreateParameterTimeOfDay("Time close trade", 23, 50, 0, 0);
            _closeDaily = CreateParameter("Close daily", 0, 0, 1, 1);
            _jmaLenFast = CreateParameter("JMA fast len", 6, 4, 10, 1);
            _jmaLenMedium = CreateParameter("JMA medium len", 14, 9, 32, 1);

            _RSIUp = CreateParameter("RSI upper range", 65m, 50m, 99.9m, 0.5m);
            _RSILow = CreateParameter("RSI low range", 35m, 1m, 49.99m, 0.5m);
            _RSILengthFast = CreateParameter("RSI periods fast", 6, 1, 10, 1);
            _RSILengthMedium = CreateParameter("RSI periods medium", 14, 1, 10, 1);
            _RSILengthSlow = CreateParameter("RSI periods slow", 24, 1, 10, 1);
            _bByVolume = CreateParameter("By Volume", false);
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

            _JMAFast = IndicatorsFactory.CreateIndicatorByName("Jma", "Ema6", false);
            _JMAFast = (Aindicator)_tabToTrade.CreateCandleIndicator(_JMAFast, "Prime");
            //_EMAFast.DataSeries
            _JMAFast.ParametersDigit[0].Value = _jmaLenFast.ValueInt;

            _JMAMedium = IndicatorsFactory.CreateIndicatorByName("Jma", "Jma12", false);
            _JMAMedium = (Aindicator)_tabToTrade.CreateCandleIndicator(_JMAMedium, "Prime");
            _JMAMedium.ParametersDigit[0].Value = _jmaLenMedium.ValueInt;

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
            //dLastBestPrice = 0.0m;
            //dLastProfitProcent = 0.0m;
            string pathToData = V;
            if (_UseDataLog.ValueBool)
            {
                writer = new StreamWriter(pathToData, false);
                writer.WriteLine("index;date;Value;Open;High;Low;Close;JMAFast;JMAMedium;RSI Fast;RSI Medium;RSI slow");
            }
        }
        ~JMA2Trade1() 
        { 
            if (writer != null) 
            {
                writer.Close();
            }
        }
        public override string GetNameStrategyType()
        {
            return "JMA2Trade1";
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
                lineNum = 1;
            }
            else 
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
            if (_jmaLenFast.ValueInt != _JMAFast.ParametersDigit[0].Value)
            {
                _JMAFast.ParametersDigit[0].Value = _jmaLenFast.ValueInt;

                _JMAFast.Reload();
            }

            if (_jmaLenMedium.ValueInt != _JMAMedium.ParametersDigit[0].Value)
            {
                _JMAMedium.ParametersDigit[0].Value = _jmaLenMedium.ValueInt;

                _JMAMedium.Reload();
            }

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

            //if (candles.Count < _JMAMedium.ParametersDigit[0].Value)
            //{
            //    return;
            //}
            List<Position> openPositions = _tabToTrade.PositionsOpenAll;
            Candle lastCandle = candles[candles.Count - 1];
            decimal valueEMAFast = _JMAFast.DataSeries[0].Last;

            decimal valueEMAMedium = _JMAMedium.DataSeries[0].Last;

            decimal valueRSIFast = _RSI.DataSeries[0].Last;
            decimal valueRSIMedium = _RSI.DataSeries[1].Last;
            decimal valueRSISlow = _RSI.DataSeries[2].Last;


            string strTime = lastCandle.TimeStart.ToString();
            if (_UseDataLog.ValueBool)
            { 
                //writer.WriteLine("index;date;Value;Open;High;Low;Close;JMAFast;JMAMedium;RSI Fast;RSI Medium;RSI slow\r\n");
                writer.WriteLine(lineNum.ToString() + ";" + strTime + ";" + lastCandle.Volume.ToString() + ";" + lastCandle.Open.ToString() + ";" + lastCandle.High.ToString() + ";" + lastCandle.Low.ToString() + ";" + lastCandle.Close.ToString() + ";" + valueEMAFast.ToString() + ";" + valueEMAMedium.ToString() + ";" + valueRSIFast.ToString() + ";" + valueRSIMedium.ToString() + ";" + valueRSISlow.ToString() + ";");
            }
            lineNum++;


            

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

            decimal valueEMAFast = _JMAFast.DataSeries[0].Last;

            if (valueEMAFast == 0)
            {
                return;
            }

            decimal valueEMAMedium = _JMAMedium.DataSeries[0].Last;

            if (valueEMAMedium == 0)
            {
                return;
            }

            decimal valueRSI = _RSI.DataSeries[0].Last;
            decimal valueRSI2 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            decimal valueRSIMedium = _RSI.DataSeries[1].Last;

            if (valueRSI == 0)
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
            //  decimal lowLastCandle = lastCandle.Low;
            //  decimal highLastCandle = lastCandle.High;
            decimal closeLastCandle = lastCandle.Close;
            if (valueEMAFast >= valueEMAMedium && closeLastCandle > valueEMAFast //&& lastCandle.IsUp == true
               && _Regime.ValueString != "OnlyShort")
            {
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    decimal stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    if (stage < VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }
                    if (valueRSIMedium > valueRSI)// valueRSI < _RSIUp.ValueDecimal)
                    {
                        return;
                    }
                    string message2 =
                        "Сигнал на открытие сделки. Лонг. strTime:" + strTime
                            + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString()
                            + ", closeLastCandle:" + closeLastCandle.ToString()
                            + ", stage:" + stage.ToString()
                            + ", RSI:" + valueRSI.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);
                }

                _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
            }
            else
               if (valueEMAFast < valueEMAMedium && closeLastCandle < valueEMAFast //&& lastCandle.IsDown == true
                   && _Regime.ValueString != "OnlyLong")
            {
                if (VolatilityFilterIsOn.ValueBool == true)
                {
                    decimal stage = _volatilityStages.DataSeries[0].Values[_volatilityStages.DataSeries[0].Values.Count - 2];

                    if (stage < VolatilityStageToTrade.ValueString.ToDecimal()) //if (stage != VolatilityStageToTrade.ValueString.ToDecimal())
                    {
                        return;
                    }

                    if (valueRSIMedium < valueRSI)// (valueRSI >= _RSILow.ValueDecimal)
                    {
                        return;
                    }
                    string message2 =
                          "Сигнал на открытие сделки. Шорт. strTime:" + strTime
                        + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString()
                        + ", closeLastCandle:" + closeLastCandle.ToString()
                        + ", stage:" + stage.ToString()
                        + ", RSI:" + valueRSI.ToString();

                    // 1 only in log window
                    SendNewLogMessage(message2, Logging.LogMessageType.User);

                }
                _tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
            }
        }

        private void LogicClosePosition(List<Candle> candles, Position position, List<Position> positionsAll)
        {
            decimal valueEMAFast = _JMAFast.DataSeries[0].Last;

            if (valueEMAFast == 0)
            {
                return;
            }

            decimal valueEMAMedium = _JMAMedium.DataSeries[0].Last;

            if (valueEMAMedium == 0)
            {
                return;
            }
            decimal dLastRSIFast1 = _RSI.DataSeries[0].Values[_RSI.DataSeries[0].Values.Count - 2];
            decimal dLastRSIMedium1 = _RSI.DataSeries[1].Values[_RSI.DataSeries[1].Values.Count - 2];
            decimal dLastRSISlow1 = _RSI.DataSeries[2].Values[_RSI.DataSeries[2].Values.Count - 2];
            int iDirectionRSI1 = 0; // -1 - degree, 0 - flat, 1 - grow
            if (dLastRSIFast1 < dLastRSIMedium1 && dLastRSIMedium1 > dLastRSISlow1)
            {
                iDirectionRSI1 = -1;
            }
            else if (dLastRSIFast1 > dLastRSIMedium1 && dLastRSIMedium1 < dLastRSISlow1)
            {
                iDirectionRSI1 = 1;
            }
            else if (dLastRSIFast1 < dLastRSIMedium1 && dLastRSIMedium1 < dLastRSISlow1)
            {
                iDirectionRSI1 = -1;
            }


            decimal dLastRSIFast = _RSI.DataSeries[0].Last;
            decimal dLastRSIMedium = _RSI.DataSeries[1].Last;
            decimal dLastRSISlow = _RSI.DataSeries[2].Last;
            int iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
            if (dLastRSIFast < dLastRSIMedium && dLastRSIFast >= _RSIUp.ValueDecimal)// dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = -1;
            }
            else if (dLastRSIFast > dLastRSIMedium && dLastRSIFast <= _RSILow.ValueDecimal) // dLastRSIMedium > dLastRSISlow)
            {
                iDirectionRSI = 1;
            }
            //else if(dLastRSIFast < dLastRSIMedium && dLastRSIMedium < dLastRSISlow)
            //{  
            //    iDirectionRSI = 0;
            //}

            //  _tabToTrade.TimeFrame;
            //Candle lastCandle2 = candles[candles.Count - 2];
            Candle lastCandle = candles[candles.Count - 1];
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
            if (position.Direction == Side.Buy)
            {
                //if (valueEMAFast < valueEMAMedium && closeLastCandle < valueEMAFast || iDirectionRSI == -1) //|| dLastRSI < 50.0m)// _RSIUp.ValueDecimal)// && lastCandle2.IsDown == true) 
                if (closeLastCandle < valueEMAMedium || iDirectionRSI == -1)
                {
                    //string message2 =
                    //        "Сигнал на закрытие лонга. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();

                    //// 1 only in log window
                    //SendNewLogMessage(message2, Logging.LogMessageType.User);

                    // _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
                    // _tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);

                    //_tabToTrade.SellAtMarket(GetVolume(_tabToTrade));
                    // _tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);
                    _tabToTrade.CloseAtLimit(position, closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep, position.OpenVolume);

                    if (_Regime.ValueString != "OnlyLong"
                        && _Regime.ValueString != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                        if (_UseRevers.ValueBool == true)
                        {
                            // _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            LogicOpenPosition(candles);
                        }

                    }

                }
                //else
                //{
                //    if (closeLastCandle < dLastBestPrice)
                //    {
                //        string message2 =
                //            "Двигаем стоп при лонге. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();

                //        // 1 only in log window
                //        SendNewLogMessage(message2, Logging.LogMessageType.User);


                //        dLastBestPrice = closeLastCandle;
                //        _tabToTrade.CloseAtTrailingStopMarket(position, dLastBestPrice);
                //    }
                //}

            }
            else if (position.Direction == Side.Sell)
            {
                //if (valueEMAFast >= valueEMAMedium && closeLastCandle > valueEMAFast || iDirectionRSI == 1) // dLastRSI > 50.0m) //&& lastCandle2.IsUp == true)
                if (closeLastCandle > valueEMAMedium || iDirectionRSI == 1)
                    {
                    //string message2 =
                    // "Сигнал на закрытие шорта. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();

                    //// 1 only in log window
                    //SendNewLogMessage(message2, Logging.LogMessageType.User);

                    // _tabToTrade.BuyAtMarket(GetVolume(_tabToTrade));
                    // _tabToTrade.CloseAtTrailingStopMarket(position, closeLastCandle);
                    _tabToTrade.CloseAtLimit(position, closeLastCandle + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep, position.OpenVolume);

                    if (_Regime.ValueString != "OnlyShort"
                        && _Regime.ValueString != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        if (_UseRevers.ValueBool == true)
                        {
                            // _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), closeLastCandle - _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                            LogicOpenPosition(candles);
                        }

                    //    _tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), closeLastCandle + _slippage.ValueDecimal * _tabToTrade.Security.PriceStep);
                    }

                }
                //else
                //{
                //    //_ProfitProcent
                //    //position.ProfitPortfolioPercent
                //    if (closeLastCandle > dLastBestPrice) // добавить условие по изменению RSI
                //    {
                //        string message2 =
                //            "Двигаем стоп при шорте. strTime:" + strTime + "valueEMAFast: " + valueEMAFast.ToString() + ", valueEMAMedium:" + valueEMAMedium.ToString() + ", closeLastCandle:" + closeLastCandle.ToString();

                //        // 1 only in log window
                //        SendNewLogMessage(message2, Logging.LogMessageType.User);

                //        dLastBestPrice = closeLastCandle;
                //        _tabToTrade.CloseAtTrailingStopMarket(position, dLastBestPrice);
                //    }
                //}
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
    }
}
