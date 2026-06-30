using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Market.Servers;
using OsEngine.Market;
using CustomIndicators.Scripts;
using System.IO;
using OsEngine.Charts.CandleChart.Indicators;

namespace OsEngine.Robots.EVZRobots
{
    public class EVZRobotBase : BotPanel
    {
        private BotTabSimple _tabToTrade;

        public StrategyParameterString _Regime;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterBool _bByVolume;
        private StrategyParameterTimeOfDay _timeEndOfTrade;
        private StrategyParameterInt _closeDaily;
        private StrategyParameterDecimal _ProfitProcent;
        private StrategyParameterDecimal _StopLossProcent;
        private StrategyParameterInt _AlgorithmSensitivityLong;
        private StrategyParameterInt _AlgorithmSensitivityShort;
        private StrategyParameterBool _UseRevers;
        private StrategyParameterBool _UseTrailing;

        public StrategyParameterString VolumeType;
        public StrategyParameterDecimal Volume;

        public StrategyParameterString TradeAssetInPortfolio;

        // Exit 
        private StrategyParameterInt _TrailBars;
        private StrategyParameterInt _PrevBars;

        private Decimal dLastBestPrice;
        private Decimal dLastProfitProcent;
        private Decimal dLastTrailValue;
        private int _iStepTrail;

        private int iCondition;
        private int iConditionCloseShort;
        private int iConditionCloseLong;

        // сервис
        private StrategyParameterBool _UseDataLog;
        private const string V = "C:\\temp\\dataJMA2.csv";
        StreamWriter writer;
        private int lineNum = 1;


        public EVZRobotBase(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tabToTrade = TabsSimple[0];

            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            _PrevBars = CreateParameter("Prev bars for retrospective", 2, 1, 10, 1);
            _bByVolume = CreateParameter("By volume", false);

            _AlgorithmSensitivityLong = CreateParameter("Algorithm sensitivity long", 3, 1, 10, 1);
            _AlgorithmSensitivityShort = CreateParameter("Algorithm sensitivity short", -3, 1, 10, 1);
            _UseRevers = CreateParameter("Use revers", false);


            // Exit
            _ProfitProcent = CreateParameter("Profit %", 1.5m, 0.01m, 99.9m, 0.1m, "Exit");
            _StopLossProcent = CreateParameter("Stop los %", 1.0m, 0.01m, 99.9m, 0.1m, "Exit");
            _UseTrailing = CreateParameter("Use Trailing", false, "Exit");
            _TrailBars = CreateParameter("Trail Bars", 4, 1, 50, 1, "Exit");

            _timeEndOfTrade = CreateParameterTimeOfDay("Time close trade", 23, 50, 0, 0, "Exit");
            _closeDaily = CreateParameter("Close daily", 0, 0, 1, 1, "Exit");

            _UseDataLog = CreateParameter("_Use data log", false, "Exit");

            VolumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            Volume = CreateParameter("Volume", 40, 1.0m, 50, 4);

            _slippage = CreateParameter("Slippage percent", 5.0m, 0, 10, 1, "Exit");

            

        //    VolatilityFilterIsOn = CreateParameter("Volatility filter is on", true);

        }
        ~EVZRobotBase()
        {
            if (writer != null)
            {
                writer.Close();
            }
        }

        public override string GetNameStrategyType()
        {
            return "EVZRobotBase";
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
                    if (myPortfolio.ValueCurrent <= 0)
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

                if (GO == 1)
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

    }
}
