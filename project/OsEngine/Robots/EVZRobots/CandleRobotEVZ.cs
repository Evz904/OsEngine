using CustomIndicators.Scripts.EVZRobots;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.EVZRobots.lib;
using System;
using System.Collections.Generic;



namespace OsEngine.Robots.EVZRobots
{
    [Bot("CandleRobotEVZ")]
    public class CandleRobotEVZ : BotPanel
    {
        private BotTabSimple _tabToTrade;
       
        private Aindicator _EMAFast;
        

        private JMABase jJMABase;
        private StrategyParameterString _Regime;
        //  public StrategyParameterString _candlePoint;
        private StrategyParameterInt _emaLenFast;
        private StrategyParameterInt _iPhase;
        private StrategyParameterInt _PrevBars;
        private StrategyParameterInt _PrevBarsForClose;
        private StrategyParameterDecimal _slippage;
        // GetVolume settings
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString _tradeAssetInPortfolio;

        // The last value of the indicator and close
        private decimal _lastClose;
        private decimal _lastOpen;
        private Candle lastCandle;
        private Side _predSide;
        private Side _streamSide;
        private Side _streamDeal;
        private int iCondition;
        private int iDirectCount;
        private int iDirectCountClose;
        private decimal valueEMAFast;
        private decimal valueEMAFastfromGraf;
        private decimal valueEMAFast2;


        public CandleRobotEVZ(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];
            _tabToTrade.CandleFinishedEvent += _tabToTrade_CandleFinishedEvent;
            _tabToTrade.PositionClosingSuccesEvent += _tabToTrade_PositionClosingSuccesEvent;
            ParametrsChangeByUser += Event_ParametrsChangeByUser;
            _tabToTrade.NewTickEvent += _tab_NewTickEvent;

            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            _slippage = CreateParameter("Slippage percent", 0.05m, 0, 10, 1);
            //  _candlePoint = CreateParameter("Candle Point", "Close", Entity.CandlePointsArray);
            _emaLenFast = CreateParameter("Ema fast len", 6, 5, 50, 2);

            _iPhase = CreateParameter("Jma Phase ", 5, -50, 50, 2);
            _PrevBars = CreateParameter("Prev bars for open", 5, 1, 10, 1);
            _PrevBarsForClose = CreateParameter("Prev bars for close", 3, 1, 10, 1);
            // GetVolume settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            _volume = CreateParameter("Volume", 20, 1.0m, 50, 4);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            //_EMAFast = (Aindicator)IndicatorsFactory.CreateIndicatorByName("Ema", "Ema fast", false);
            //_EMAFast = (Aindicator)_tabToTrade.CreateCandleIndicator(_EMAFast, "Prime");
            _EMAFast = (Jma)IndicatorsFactory.CreateIndicatorByName("Jma", "Jma fast", false);
            _EMAFast = (Jma)_tabToTrade.CreateCandleIndicator(_EMAFast, "Prime");

            _EMAFast.ParametersDigit[0].Value = _emaLenFast.ValueInt;

            jJMABase = new JMABase();

            InitRobot();
        }

        private void InitRobot()
        {
            iCondition = 0;
            iDirectCount = 0;
            iDirectCountClose = 0;
            _streamDeal = Side.None;
            _predSide = Side.None;
            valueEMAFastfromGraf = 0;
            valueEMAFast = 0;
            jJMABase.InitData();
            jJMABase.UpdateCoefficients(_iPhase.ValueInt, _emaLenFast.ValueInt);
            

        }

        public override string GetNameStrategyType()
        {
            return "CandleRobotEVZ";
        }

        private void _tabToTrade_CandleFinishedEvent(List<Candle> candles)
        {
            // вызывается на каждой новой свече
            lastCandle = candles[^1];
            int iCount = candles.Count;
            _streamSide = lastCandle.IsUp == true ? Side.Buy : lastCandle.IsDown == true ? Side.Sell : Side.None;
            if (iCount == 1)
            {
                InitRobot();
            }
            //{
             List<Position> openPositions = _tabToTrade.PositionsOpenAll;
             _lastClose = candles[^1].Close;
             _lastOpen = candles[^1].Open;
            //if (iDirectCountClose < _PrevBarsForClose && _streamDeal != Side.None)
            //{
            //    if (_streamSide != _streamDeal)
            //    {
            //        iDirectCountClose++;
            //    }
            //}

            //if (_streamSide == Side.Buy)
            //{
            //    iDirectCount++;
            //}
            //else if(_streamSide == Side.Sell)
            //{
            //    iDirectCount--;
            //}

            //if(_streamDeal != Side.None)
            //{
            //    iDirectCountClose++;
            //}

            if (_streamSide == _predSide && (_streamDeal == Side.None || _streamDeal == _streamSide))//|| _streamSide == _streamDeal)
            {
                iDirectCount++;
                iDirectCountClose = 0;
            }
            else if (_streamSide == _predSide && (_streamDeal != Side.None && _streamDeal != _streamSide))//&& iDirectCountClose < _PrevBarsForClose)
            {
                iDirectCountClose++;
            }
            else if (_streamSide != _predSide && (_streamDeal != Side.None && _streamDeal == _streamSide))
            {
                iDirectCount++;
                if (iDirectCountClose > 0)
                {
                    iDirectCountClose = 0;
                }
            }
            else
            {
                if (openPositions.Count == 0)
                {
                    if (iDirectCount > 1)
                    {
                        iDirectCount--;// = 1;
                    }
                    iDirectCountClose = 0;
                }
                else
                {
                    iDirectCountClose++;
                }
            }
            valueEMAFast = Math.Round(jJMABase.JJMASeries(_lastClose), _tabToTrade.Security.DecimalsVolume);
            if (iCount > _emaLenFast && valueEMAFast != 0)
            {
                valueEMAFastfromGraf = Math.Round(_EMAFast.DataSeries[0].Last, _tabToTrade.Security.DecimalsVolume);
                //valueEMAFast = valueEMAFastfromGraf;
                if (openPositions == null || openPositions.Count == 0)
                {
                    if (_Regime.ValueString == "OnlyClosePosition")
                    {
                        return;
                    }
                    LogicOpenPosition(candles, openPositions);
                }


                if (openPositions != null && openPositions.Count != 0)
                {
                    for (int i = 0; i < openPositions.Count; i++)
                    {
                        LogicClosePosition(candles, openPositions[i]);
                    }
                }
                valueEMAFast2 = valueEMAFast;

            }
            _predSide = _streamSide;
        }
        // Logic open position
        private void LogicOpenPosition(List<Candle> candles, List<Position> position)
        {
            string strTime = lastCandle.TimeStart.ToString();
            //if (iDirectCount >= _PrevBars && _streamSide == Side.Buy && _Regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
            if (valueEMAFast < _lastClose && valueEMAFast < _lastOpen && _streamSide == Side.Buy && _Regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
            {//
                decimal exitPrice = _lastClose + _slippage * _tabToTrade.Security.PriceStep;
                _tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), exitPrice);
                _streamDeal = Side.Buy;
                iDirectCountClose = 0;
                string message2 =
                            "Сигнал на открытие сделки Лонг. strTime:" + strTime
                            + ", exitPrice:" + exitPrice.ToString()
                            + ", valueEMAFast: " + valueEMAFast.ToString()
                            + ", valueEMAFastfromGraf: " + valueEMAFastfromGraf.ToString()
                            + ", closeLastCandle: " + _lastClose.ToString(); //valueEMAFastfromGraf
                SendNewLogMessage(message2, Logging.LogMessageType.User);
            }
            //if (iDirectCount >= _PrevBars &&  _streamSide == Side.Sell && _Regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
            if (valueEMAFast > _lastClose && valueEMAFast > _lastOpen && _streamSide == Side.Sell && _Regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
            {
                decimal exitPrice = _lastClose - _slippage * _tabToTrade.Security.PriceStep;
                _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), exitPrice);
                _streamDeal = Side.Sell;
                iDirectCountClose = 0;
                string message2 =
                            "Сигнал на открытие сделки Шорт. strTime:" + strTime
                            + ", exitPrice:" + exitPrice.ToString()
                            + ", valueEMAFast: " + valueEMAFast.ToString()
                            + ", valueEMAFastfromGraf: " + valueEMAFastfromGraf.ToString()
                            + ", closeLastCandle: " + _lastClose.ToString();
                SendNewLogMessage(message2, Logging.LogMessageType.User);
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles, Position position)
        {
            if (position.State != PositionStateType.Open)
            {
                return;
            }
            string strTime = lastCandle.TimeStart.ToString();

            if (position.Direction == Side.Buy) // If the direction of the position is long
            {
                decimal exitPrice = _lastClose - _slippage * _tabToTrade.Security.PriceStep;

                if (iDirectCountClose >= _PrevBarsForClose)
                //if (valueEMAFast > _lastClose)
                    {
                    _tabToTrade.CloseAtLimit(position, exitPrice, position.OpenVolume, "Close long");
                    iDirectCount = iDirectCountClose;
                    iDirectCountClose = 0;
                    //if (_Regime.ValueString != "OnlyLong"
                    //        && _Regime.ValueString != "OnlyClosePosition")
                    //{
                    //    _tabToTrade.SellAtLimit(GetVolume(_tabToTrade), exitPrice, "Close long");
                    //}
                    _streamDeal = Side.None;
                    string message2 =
                                "Сигнал на закрытие сделки Лонг. strTime:" + strTime
                                + ", exitPrice:" + exitPrice.ToString()
                                + ", valueEMAFast: " + valueEMAFast.ToString()
                                + ", valueEMAFastfromGraf: " + valueEMAFastfromGraf.ToString()
                                + ", closeLastCandle: " + _lastClose.ToString();
                    SendNewLogMessage(message2, Logging.LogMessageType.User);

                }
            }

            if (position.Direction == Side.Sell) // If the direction of the position is short
            {
                decimal exitPrice = _lastClose + _slippage * _tabToTrade.Security.PriceStep;

                if (iDirectCountClose >= _PrevBarsForClose)
                //if (valueEMAFast < _lastClose)
                    {
                    _tabToTrade.CloseAtLimit(position, exitPrice, position.OpenVolume, "Close short");
                    iDirectCount = iDirectCountClose;
                    iDirectCountClose = 0;
                    _streamDeal = Side.None;
                    string message2 =
                                "Сигнал на закрытие" +
                                " сделки Шорт. strTime:" + strTime
                                + ", exitPrice:"+ exitPrice.ToString()
                                + ", valueEMAFast: " + valueEMAFast.ToString()
                                + ", valueEMAFastfromGraf: " + valueEMAFastfromGraf.ToString()
                                + ", closeLastCandle: " + _lastClose.ToString();
                    SendNewLogMessage(message2, Logging.LogMessageType.User);
                    //if (_Regime.ValueString != "OnlyShort"
                    //    && _Regime.ValueString != "OnlyClosePosition")
                    //{
                    //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), exitPrice, "Close short");
                    //}
                }
            }
        }

         private void _tabToTrade_PositionClosingSuccesEvent(Position obj)
        {
            if (obj.State == PositionStateType.Done)
            {
                List<Position> openPositions = _tabToTrade.PositionsOpenAll;
                //if (obj.Direction == Side.Buy)
                decimal price = obj.ClosePrice;
                if (obj.SignalTypeStop == "Close long")
                {
                    //if (_Regime.ValueString != "OnlyLong"
                    // && _Regime.ValueString != "OnlyClosePosition")
                    //{
                    //    if (_UseRevers.ValueBool == true)
                    //    {
                    //        //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), price - price * (_slippage.ValueDecimal / 100));
                    //        LogicOpenPosition();
                    //    }
                    //}
                }
                else if (obj.SignalTypeStop == "Close short")
                {
                    //if (_Regime.ValueString != "OnlyShort"
                    //    && _Regime.ValueString != "OnlyClosePosition")
                    //{
                    //    if (_UseRevers.ValueBool == true)
                    //    {
                    //        //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), price + price * (_slippage.ValueDecimal / 100));

                    //        LogicOpenPosition();
                    //    }
                    //}
                }

            }
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }

            //TradeLogic();
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

        // Method for calculating the volume of entry into a position
        private decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == "Contracts")
            {
                volume = _volume.ValueDecimal;
            }
            else if (_volumeType.ValueString == "Contract currency")
            {
                decimal contractPrice = tab.PriceBestAsk;
                volume = _volume.ValueDecimal / contractPrice;

                if (StartProgram == StartProgram.IsOsTrader)
                {
                    IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);

                    if (serverPermission != null &&
                        serverPermission.IsUseLotToCalculateProfit &&
                    tab.Security.Lot != 0 &&
                        tab.Security.Lot > 1)
                    {
                        volume = _volume.ValueDecimal / (contractPrice * tab.Security.Lot);
                    }

                    volume = Math.Round(volume, tab.Security.DecimalsVolume);
                }
                else // Tester or Optimizer
                {
                    volume = Math.Round(volume, 6);
                }
            }
            else if (_volumeType.ValueString == "Deposit percent")
            {
                Portfolio myPortfolio = tab.Portfolio;

                if (myPortfolio == null)
                {
                    return 0;
                }

                decimal portfolioPrimeAsset = 0;

                if (_tradeAssetInPortfolio.ValueString == "Prime")
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
                        if (positionOnBoard[i].SecurityNameCode == _tradeAssetInPortfolio.ValueString)
                        {
                            portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                            break;
                        }
                    }
                }

                if (portfolioPrimeAsset == 0)
                {
                    SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                    return 0;
                }
                decimal GO = tab.Security.Go;

                decimal PriceBestAsk;

                if(GO == 1)
                {
                    PriceBestAsk = tab.PriceBestAsk;
                }
                else
                {
                    PriceBestAsk = GO;
                }

                    decimal moneyOnPosition = portfolioPrimeAsset * (_volume.ValueDecimal / 100);

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

        public override void ShowIndividualSettingsDialog()
        {

        }
        void Event_ParametrsChangeByUser()
        {
            if (_emaLenFast.ValueInt != _EMAFast.ParametersDigit[0].Value)
            {
                _EMAFast.ParametersDigit[0].Value = _emaLenFast.ValueInt;
                 jJMABase.UpdateCoefficients(_iPhase.ValueInt, _emaLenFast.ValueInt);
                _EMAFast.Reload();
                _EMAFast.Save();
            }
        }
        }
}
