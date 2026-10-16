/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using CustomIndicators.Scripts.EVZRobots;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.EVZRobots.lib;
using OsEngine.Robots.VolatilityStageRotationSamples;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using static Google.Api.LabelDescriptor.Types;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;
using SecurityRatingData = OsEngine.Robots.EVZRobots.lib.SecurityRatingData;

/* Description
trading robot for osengine

The trend robot on Sma Screener.

Buy: If there is no position. Open long if the last N candles we were above the moving average

Exit: by trailing stop.
*/

namespace OsEngine.Robots.Screeners
{
    [Bot("EVZScreener")] // We create an attribute so that we don't write anything to the BotFactory
    public class EVZScreener : BotPanel
    {
        private BotTabScreener _tabScreener;

        // Basic settings
        private StrategyParameterString _Regime;
       // private StrategyParameterInt _candlesLookBack;
        private StrategyParameterInt _maxPoses;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterBool    _AutoCalcStop;      // Признак авторасчёта профита/убытка
        private StrategyParameterDecimal _FastProfit;     // % "быстрого" выхода - для выхода если после входа в позицию прошло(ещё идёт) 
        private StrategyParameterDecimal _PlannedIncome;  // % обычного, планового выхода с установкой тейка с параметрами IndentProfit и slippage
        private StrategyParameterDecimal _AbsoluteProfit; // %  супер-доход - для безусловного выхода из позиции по достижению профита
        private StrategyParameterDecimal _AbsoluteLoss;   // %  процент допустимого убытка абсолютный.При достижении этого уровня позиция закрывается однозначно...
        private StrategyParameterDecimal _FastLoss;       // % "быстрого" выхода - для выхода если после входа в позицию прошло(ещё идёт) 1,2 интервала тайм фрейма
        private StrategyParameterInt     _FastInterval;   // Количество интервалов для быстрого выхода по профиту или стопу
        private StrategyParameterDecimal _IdentProfit;   // отступ после достижения максимума посе которого закрываем позицию
        private StrategyParameterTimeOfDay _timeEndOfTrade;
        private StrategyParameterInt _closeDaily;
        private StrategyParameterInt _AlgorithmSensitivityLong;
        private StrategyParameterInt _AlgorithmSensitivityShort;
        public StrategyParameterBool VolatilityFilterIsOn;
        private StrategyParameterDecimal _deviation;
        private StrategyParameterDecimal _RSIUp;
        private StrategyParameterDecimal _RSILow;

        private StrategyParameterString _MAType;

        // GetVolume settings
        private StrategyParameterString  _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString  _tradeAssetInPortfolio;

        // Indicator settings
        //private Aindicator _MAFast;
        private StrategyParameterInt _MALenFast;
        private StrategyParameterInt _MALenSlow;

        //private RSI3RAY _RSI;
        private StrategyParameterInt _RSILengthFast;
        private StrategyParameterInt _RSILengthMedium;
        private StrategyParameterInt _RSILengthSlow;

        //private Zamah _zamahFast;
        //private Zamah _zamahSlow;
        private StrategyParameterInt _ZamahFastLenght;
        //private StrategyParameterInt _ZamahSlowLenght;
        // VolatilityEVZ
        private StrategyParameterInt _lengthFastVol;
        private StrategyParameterInt _lengthSlowVol;
//        private StrategyParameterDecimal _deviationVol;
//        private StrategyParameterString _MATypeVol;

        // Exit setting
        private StrategyParameterDecimal _trailStop;


        // Securities rating
        DateTime _lastTimeRating = DateTime.MinValue;

        // Securities settings
        private StrategyParameterInt _maxSecuritiesToTrade;
        private StrategyParameterInt _topVolumeSecurities;
        //private StrategyParameterInt _topVolumeDaysLookBack;
        private StrategyParameterString _securitiesToTrade;
        private string strTime;
        private decimal _ValueCurrent; // Текущая сумма в рублях. Расчётная
        private decimal _ValueBegin; // Стартовая сумма в рублях. Расчётная
        private decimal _ValueOpenLong; // Объем открытых позиций лонг
        private decimal _ValueOpenShort; // Объем открытых позиций шорт

        List<SecurityRatingData> securityRatingData;
        private bool _bStartWork;
        public EVZScreener(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Screener);
            _tabScreener = TabsScreener[0];

            // Subscribe to the candle finished event
            _tabScreener.CandleFinishedEvent += _screenerTab_CandleFinishedEvent;
            _tabScreener.PositionClosingSuccesEvent += _tabToTrade_PositionClosingSuccesEvent;
            _tabScreener.PositionOpeningSuccesEvent += _tabScreener_PositionOpeningSuccesEvent;

            // Basic settings
            _Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            _MAType = CreateParameter("MA type", "Ema", new[] {  "Ema", "Jma" });
            // Securities settings
            _topVolumeSecurities = CreateParameter("Top volume securities", 15, 0, 20, 1);
            _maxPoses = CreateParameter("Max poses", 2, 1, 20, 1);
            _securitiesToTrade = CreateParameter("Securities to trade", "");
            _AlgorithmSensitivityLong = CreateParameter("Algorithm sensitivity long", 4, 1, 10, 1);
            _AlgorithmSensitivityShort = CreateParameter("Algorithm sensitivity short", 4, 1, 10, 1);

            // exit setting
            string sExit = "Exit";
            _slippage = CreateParameter("Slippage", 0.06m, 0.02m, 10.0m, 0.01m, sExit);
            _AutoCalcStop = CreateParameter("_Auto calc stop", false, sExit);
            _FastProfit = CreateParameter("Fast profit %", 1.0m, 0.8m, 3.0m, 0.2m, sExit);
            _PlannedIncome = CreateParameter("Planned income %", 1.2m, 0.9m, 3.0m, 0.2m, sExit); 
            _AbsoluteProfit = CreateParameter("Absolute profit %", 3.0m, 1.5m, 4.0m, 0.5m, sExit); 
            _AbsoluteLoss = CreateParameter("Absolute loss %", 2.0m, 0.9m, 3.0m, 0.2m, sExit); 
            _FastLoss = CreateParameter("Fast loss %", 1.0m, 0.8m, 3.0m, 0.2m, sExit); 
            _FastInterval = CreateParameter("Fast interval", 2, 1, 4, 1, sExit); 
            _IdentProfit = CreateParameter("Ident profit %", 1.0m, 0.8m, 3.0m, 0.2m, sExit); 
            _trailStop = CreateParameter("Trail Stop", 0.7m, 0.5m, 5, 0.1m, sExit);
            _timeEndOfTrade = CreateParameterTimeOfDay("Time close trade", 23, 50, 0, 0, sExit);
            _closeDaily = CreateParameter("Close daily", 0, 0, 1, 1, sExit);
            VolatilityFilterIsOn = CreateParameter("Volatility filter is on", true);
            _deviation = CreateParameter("Deviation", 2.0m, 0.4m, 2.5m, 0.4m);
            _RSIUp = CreateParameter("RSI upper range", 65m, 50m, 99.9m, 0.5m);
            _RSILow = CreateParameter("RSI low range", 35m, 1m, 49.99m, 0.5m);

            // _candlesLookBack = CreateParameter("Candles Look Back count", 10, 5, 100, 1);

            // GetVolume settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            _volume = CreateParameter("Volume", 20, 1.0m, 50, 4);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            // Indicator setting
            _MALenFast = CreateParameter("MA length fast", 50, 40, 150, 5);
            _MALenSlow = CreateParameter("MA length slow", 100, 60, 300, 5);

            //_MAFast = (Jma)IndicatorsFactory.CreateIndicatorByName("Jma", "Jma fast", false);
            //_MAFast = (Jma)_tabToTrade.CreateCandleIndicator(_MAFast, "Prime");


            // Create indicator Jma _MAType
            //_tabScreener.CreateCandleIndicator(1, "Jma", new List<string>() { _MALenFast.ValueInt.ToString(), "6", "Close" }, "Prime");
            //_tabScreener.CreateCandleIndicator(1, _MAType.ValueString, new List<string>() { _MALenFast.ValueInt.ToString(), "Close" }, "Prime");
            _tabScreener.CreateCandleIndicator(1, "Ema", new List<string>() { _MALenFast.ValueInt.ToString(), "Close" }, "Prime");
            _RSILengthFast = CreateParameter("RSI length fast", 6, 5, 28, 2);
            _RSILengthMedium = CreateParameter("RSI length мedium", 14, 10, 48, 2);
            _RSILengthSlow = CreateParameter("RSI length slow", 48, 15, 52, 2);

            _tabScreener.CreateCandleIndicator(2, "RSI3RAY", new List<string>() {_RSILengthFast.ValueInt.ToString(),
                                                                                _RSILengthMedium.ValueInt.ToString(),
                                                                                _RSILengthSlow.ValueInt.ToString(),
                                                                                "false",
                                                                                "Close" }, "RSI3RAY");

            _ZamahFastLenght = CreateParameter("Zamah fast lenght", 6, 5, 48, 2);
            _tabScreener.CreateCandleIndicator(3, "Zamah", new List<string>() {_ZamahFastLenght.ValueInt.ToString(),
                                                                                "EMA",
                                                                                "48",
                                                                                "0.05",
                                                                                "2.00",
                                                                                "false" }, "Zamah");

            _lengthFastVol = CreateParameter("Length fast volatility", 7, 5, 28, 2); ;
            _lengthSlowVol = CreateParameter("Length slow volatility", 48, 15, 52, 2);
            _tabScreener.CreateCandleIndicator(4, "VolatilityEVZ", new List<string>() {_lengthFastVol.ValueInt.ToString(),
                                                                                       _lengthSlowVol.ValueInt.ToString(),
                                                                                       _deviation.ValueDecimal.ToString(),
                                                                                       "EMA" }, 
                                                                                       "VolatilityEVZ");
            _tabScreener.CreateCandleIndicator(5, _MAType.ValueString, new List<string>() { _MALenSlow.ValueInt.ToString(), "Close" }, "Prime");

            // Subscribe to the indicator update event
            ParametrsChangeByUser += EVZScreener_ParametrsChangeByUser;

             securityRatingData = new List<SecurityRatingData>();

            //Description = OsLocalization.Description.DescriptionLabel94;
            if (StartProgram == StartProgram.IsTester
                 && ServerMaster.GetServers() != null)
            {
                List<IServer> servers = ServerMaster.GetServers();

                if (servers != null
                    && servers.Count > 0
                    && servers[0].ServerType == ServerType.Tester)
                {
                    TesterServer server = (TesterServer)servers[0];
                    server.TestingStartEvent += Server_TestingStartEvent;
                }
            }

        }


        private void Server_TestingStartEvent()
        {
            _securitiesToTrade.ValueString = "";
            _lastTimeRating = DateTime.MinValue;
            _bStartWork = false;
            if (securityRatingData != null)
            {
                securityRatingData.Clear();
            }
        }
        private void EVZScreener_ParametrsChangeByUser()
        {
          //  string listMA = "Jma EMA";

          //if (listMA.Contains(_tabScreener._indicators[0].Type) && _MAType != _tabScreener._indicators[0].Type)
          //  {
          //      _tabScreener._indicators.RemoveAt(0);
          //      _tabScreener.CreateCandleIndicator(1, _MAType.ValueString, new List<string>() { _MALenFast.ValueInt.ToString(), "Close" }, "Prime");
          //      //_EMAFast.ParametersDigit[0].Value = _jmaLenFast.ValueInt;

          //      //_EMAFast.Reload();
          //      //_EMAFast.Save();
          //  }


            _tabScreener._indicators[0].Parameters = new List<string>() { _MALenFast.ValueInt.ToString(), "Close" };
            _tabScreener._indicators[1].Parameters = new List<string>() {_RSILengthFast.ValueInt.ToString(),
                                                                                _RSILengthMedium.ValueInt.ToString(),
                                                                                _RSILengthSlow.ValueInt.ToString(),
                                                                                "false",
                                                                                "Close" };
            _tabScreener._indicators[2].Parameters = new List<string>() {_ZamahFastLenght.ValueInt.ToString(),
                                                                                "EMA",
                                                                                "48",
                                                                                "0.05",
                                                                                "2.00",
                                                                                "false" };
            _tabScreener._indicators[3].Parameters = new List<string>() {_lengthFastVol.ValueInt.ToString(),
                                                                         _lengthSlowVol.ValueInt.ToString(),
                                                                         _deviation.ValueDecimal.ToString(),
                                                                         "EMA" };
            _tabScreener._indicators[4].Parameters = new List<string>() { _MALenSlow.ValueInt.ToString(), "Close" };


            _tabScreener.UpdateIndicatorsParameters();
        }

        // The name of the robot in OsEngine
        public override string GetNameStrategyType()
        {
            return "EVZScreener";
        }

        // Show settings GUI
        public override void ShowIndividualSettingsDialog()
        {

        }
        private void CheckSecuritiesRating()
        {
            if (_tabScreener.Tabs == null
                || _tabScreener.Tabs.Count == 0
                || _tabScreener.Tabs[0].IsConnected == false)
            {
                return;
            }
            strTime = _tabScreener.Tabs[0].TimeServerCurrent.ToString();
            //DateTime currentTime = _tabScreener.Tabs[0].TimeServerCurrent;

            //if (currentTime.Date == _lastTimeRating.Date)
            //{
            //    return;
            //}
            //_lastTimeRating = _tabScreener.Tabs[0].TimeServerCurrent;


            //if (securityRatingData.Count > 0)
            //{
            securityRatingData.Clear();
            //}

            List<BotTabSimple> tabs = _tabScreener.Tabs;
            int iCondition;
            int iConditionOpenShort;
            int iConditionOpenLong;
            int iConditionCloseShort;
            int iConditionCloseLong;

            for (int i = 0; i < tabs.Count; i++)
            {
                SecurityRatingData newData = new SecurityRatingData();
                
                newData.SecurityName = tabs[i].Security.Name;

                var tab = tabs[i];

                Aindicator ma = (Aindicator)tab.Indicators[0];

                if (ma.ParametersDigit[0].Value != _MALenFast.ValueInt)
                {
                    ma.ParametersDigit[0].Value = _MALenFast.ValueInt;
                    ma.Save();
                    ma.Reload();
                }

                if (ma.DataSeries[0].Values.Count == 0 ||
                    ma.DataSeries[0].Last == 0)
                {
                    return;
                }

                Aindicator maSlow = (Aindicator)tab.Indicators[4];

                if (maSlow.ParametersDigit[0].Value != _MALenSlow.ValueInt)
                {
                    maSlow.ParametersDigit[0].Value = _MALenSlow.ValueInt;
                    maSlow.Save();
                    maSlow.Reload();
                }

                if (maSlow.DataSeries[0].Values.Count == 0 ||
                    maSlow.DataSeries[0].Last == 0)
                {
                    return;
                }
                RSI3RAY rsi3ray = (RSI3RAY)tab.Indicators[1];
                if (rsi3ray.ParametersDigit[0].Value != _RSILengthFast.ValueInt ||
                    rsi3ray.ParametersDigit[1].Value != _RSILengthMedium.ValueInt ||
                    rsi3ray.ParametersDigit[2].Value != _RSILengthSlow.ValueInt) // ||
                    //rsi3ray.ParametersDigit[3].Value != _RSILengthSlow.ValueInt ||
                    //rsi3ray.ParametersDigit[4].Value != _RSILengthSlow.ValueInt) 
                {
                    rsi3ray.ParametersDigit[0].Value = _RSILengthFast.ValueInt;
                    rsi3ray.ParametersDigit[1].Value = _RSILengthMedium.ValueInt;
                    rsi3ray.ParametersDigit[2].Value = _RSILengthSlow.ValueInt;
                    rsi3ray.Save();
                    rsi3ray.Reload();
                }

                if (rsi3ray.DataSeries[0].Values.Count == 0 ||
                    rsi3ray.DataSeries[0].Last == 0)
                {
                    return;
                }
                Zamah zamah = (Zamah)tab.Indicators[2];
                if (zamah.ParametersDigit[0].Value != _ZamahFastLenght.ValueInt) 
                {
                    zamah.ParametersDigit[0].Value = _ZamahFastLenght.ValueInt;
                    zamah.Save();
                    zamah.Reload();
                }

                if (zamah.DataSeries[0].Values.Count == 0 ||
                    zamah.DataSeries[0].Last == 0)
                {
                    return;
                }
                VolatilityEVZ volatility = (VolatilityEVZ)tab.Indicators[3];
                if (volatility.ParametersDigit[0].Value != _lengthFastVol.ValueInt ||
                    volatility.ParametersDigit[1].Value != _lengthSlowVol.ValueInt ||
                    volatility.ParametersDigit[2].Value != _deviation.ValueDecimal)
                {
                    volatility.ParametersDigit[0].Value = _lengthFastVol.ValueInt;
                    volatility.ParametersDigit[1].Value = _lengthSlowVol.ValueInt;
                    volatility.ParametersDigit[2].Value = _deviation.ValueDecimal;
                    volatility.Save();
                    volatility.Reload();
                }

                if (volatility.DataSeries[0].Values.Count == 0 ||
                    volatility.DataSeries[0].Last == 0)
                {
                    return;
                }

                iConditionOpenLong = 0;
                iConditionOpenShort = 0;
                
                iConditionCloseShort = 0;
                iConditionCloseLong = 0;
                iCondition = 0;

                //if (VolatilityFilterIsOn.ValueBool == true)
                //{
                var streamVolatility = volatility._seriesFastVolatilityChangeMA.Last;
                var UpRange = volatility._LineUp.Last;
                var DownRange = volatility._LineDown.Last;
                if (streamVolatility <= UpRange && streamVolatility >= DownRange)
                {
                    newData.bStopOpenDeal = true;//continue;
                }
                //}
                // iCondition = 1
                iCondition = streamVolatility > UpRange ? 1 : streamVolatility < DownRange ? -1 : 0;
                iConditionOpenLong = streamVolatility > UpRange ? 1 : streamVolatility < DownRange ? -1 : 0;
                iConditionOpenShort = streamVolatility < DownRange ? 1 : streamVolatility > UpRange ? -1 : 0;
                iConditionCloseShort = streamVolatility > UpRange ? 1 :  0; ;
                iConditionCloseLong = streamVolatility < DownRange ? -1 : 0; ;

                var valueRSIFast = rsi3ray._seriesFast.Last; // DataSeries[0].Last;
                var valueRSIMedium = rsi3ray._seriesMedium.Last;//DataSeries[1].Last;
                var valueRSISlow = rsi3ray._seriesSlow.Last;//DataSeries[2].Last;

                var valueRSIFast2 = rsi3ray._seriesFast.Values[^2];

                var valueRSIMedium2 = rsi3ray._seriesMedium.Values[^2];

                var valueRSISlow2 = rsi3ray._seriesSlow.Values[^2]; ;

                int iDirectionRSI1 = 0; // -1 - degree, 0 - flat, 1 - grow
                if (valueRSIFast2 < valueRSIMedium2 && valueRSIMedium2 < valueRSISlow2)
                {
                    iDirectionRSI1 = -1;
                }
                else if (valueRSIFast2 > valueRSIMedium2 && valueRSIMedium2 > valueRSISlow2)
                {
                    iDirectionRSI1 = 1;
                }

                int iDirectionRSI = 0; // -1 - degree, 0 - flat, 1 - grow
                if (valueRSIFast < valueRSIMedium && valueRSIFast < valueRSIFast2) 
                {
                    iDirectionRSI = -1;// + (dLastRSIMedium < dLastRSISlow ? -1 : 0);
                }
                else if (valueRSIFast > valueRSIMedium && valueRSIFast > valueRSIFast2)
                {
                    iDirectionRSI = 1;// + (dLastRSIMedium > dLastRSISlow ? 1 : 0);
                }

                // iCondition = 2
                iCondition += iDirectionRSI;
                iConditionOpenLong += iDirectionRSI == 1 ? 1 : iDirectionRSI == -1 ? -1 : 0;
                iConditionOpenShort += iDirectionRSI == -1 ? 1 : iDirectionRSI == 1 ? -1 : 0;

                iConditionCloseShort = iDirectionRSI == 1 ? 1 : 0;
                iConditionCloseLong = iDirectionRSI == -1 ? 1 : 0;
                // iCondition = 3
                iCondition += iDirectionRSI == 1 && _RSIUp < valueRSIFast ? 1 : iDirectionRSI == -1 && _RSILow > valueRSIFast ? -1 : 0;
                iConditionOpenLong += iDirectionRSI == 1 && _RSIUp < valueRSIFast ? 1 : iDirectionRSI == -1 && _RSILow > valueRSIFast ? -1 : 0;
                iConditionOpenShort += iDirectionRSI == -1 && _RSILow > valueRSIFast ? 1 : iDirectionRSI == 1 && _RSIUp < valueRSIFast ? -1 : 0;
                // Проверка на крайние значения RSI для выхода из позиции
                iConditionCloseShort += iDirectionRSI == 1  && _RSIUp  < valueRSIFast ? 1 : 0;
                iConditionCloseLong  += iDirectionRSI == -1 && _RSILow > valueRSIFast ? 1 : 0;

                var dBearsZamah2 = zamah.SeriesBears.Values[^2];
                var dBullsZamah2 = zamah.SeriesBulls.Values[^2];

                var dBearsZamah = zamah.SeriesBears.Last;
                var dBullsZamah = zamah.SeriesBulls.Last;
                var dTriggerLevelBulls = zamah.SeriesTriggerLevelBulls.Last;
                // Разделить iCondition на iConditionOpenLong и iConditionOpenShort
                // iCondition = 4
                iCondition += dBearsZamah > dBullsZamah && dBearsZamah > dTriggerLevelBulls ? -1 : dBearsZamah < dBullsZamah && dBullsZamah > dTriggerLevelBulls ? 1 : 0;
                
                iConditionOpenLong += dBearsZamah < dBullsZamah && dBullsZamah > dTriggerLevelBulls ? 2 : dBearsZamah > dBullsZamah && dBearsZamah > dTriggerLevelBulls ? -2 : 0;
                iConditionOpenShort += dBearsZamah > dBullsZamah && dBearsZamah > dTriggerLevelBulls ? 2 : dBearsZamah < dBullsZamah && dBullsZamah > dTriggerLevelBulls ? -2 : 0;
                
                iConditionCloseShort += dBearsZamah < dBullsZamah ? 1 : 0;/// Подумать над уточнением условия!!! Возможно надо применитьболее высокие коэфициенты!! 
                iConditionCloseLong += dBearsZamah > dBullsZamah ? 1 : 0;


                var valueEMAFast = ma.DataSeries[0].Last;
                var valueEMAFast2 = ma.DataSeries[0].Values[^2];
                var valueEMASlow = maSlow.DataSeries[0].Last;
                var valueEMASlow2 = maSlow.DataSeries[0].Values[^2];
                Candle lastCandle = tab.CandlesAll.Last();
                var dDeltaEMAN = valueEMAFast2 - valueEMASlow2;
                var dDeltaEMA = valueEMAFast - valueEMASlow;

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
                iCondition += valueEMAFast - valueEMASlow > 0 && lastCandle.Close - valueEMAFast > 0 ? 1 : -1;

                iConditionCloseShort += valueEMAFast - valueEMASlow > 0 && lastCandle.Close - valueEMAFast > 0 ? 1 : 0;
                iConditionCloseLong += valueEMAFast - valueEMASlow < 0 && lastCandle.Close - valueEMAFast < 0 ? 1 : 0;

                // iCondition = 7
                iCondition += lastCandle.Close - valueEMAFast > 0 ? 1 : -1;
                iConditionCloseShort += lastCandle.Close - valueEMAFast > 0 ? 1 : 0;
                iConditionCloseLong += lastCandle.Close - valueEMAFast < 0 ? 1 : 0;
                // iCondition = 8
                iCondition += lastCandle.IsDown == true ? -1 : lastCandle.IsUp == true ? 1 : 0;
                iConditionCloseShort += lastCandle.IsDown != true ? 1 : 0;
                iConditionCloseLong += lastCandle.IsUp != true ? 1 : 0;

                //if (iCondition == 0)
                //{
                //    continue;
                //}
                newData.iCondition = Math.Abs(iCondition);
                //newData.bStopOpenDeal
                newData.iConditionCloseLong = iConditionCloseLong;
                newData.iConditionCloseShort = iConditionCloseShort;
                newData.bShort = iCondition < 0;

                securityRatingData.Add(newData);
            }

            if (securityRatingData.Count == 0)
            {
                return;
            }

            for (int i = 0; i < securityRatingData.Count; i++)
            {
                for (int j = 1; j < securityRatingData.Count; j++)
                {
                    if (securityRatingData[j - 1].iCondition < securityRatingData[j].iCondition)
                    {
                        (securityRatingData[j], securityRatingData[j - 1]) = (securityRatingData[j - 1], securityRatingData[j]);
                    }
                }
            }

            int count = _topVolumeSecurities.ValueInt;

            if (count > securityRatingData.Count)
            {
                count = securityRatingData.Count;
            }

            securityRatingData = securityRatingData.GetRange(0, count);

            string securitiesInTrade = "";

            for (int i = 0; i < securityRatingData.Count; i++)
            {
                securitiesInTrade += securityRatingData[i].SecurityName + " ";
            }

            _securitiesToTrade.ValueString = securitiesInTrade;
        }

        // Logic
        private void _screenerTab_CandleFinishedEvent(List<Candle> candles, BotTabSimple tab)
        {
            // 1 If there is a position, then we close the trailing stop

            // 2 No position. Open long if we were above the moving average for the last N candles
            if (!_bStartWork)
            {
                _bStartWork = true;
                _ValueCurrent = tab.Portfolio.ValueCurrent;
                _ValueBegin = _ValueCurrent;
                _ValueOpenLong = 0;
                _ValueOpenShort = 0;    
            }



            if (_Regime.ValueString == "Off")
            {
                return;
            }

            if (candles.Count < _lengthSlowVol)
            {
                return;
            }

           // if (securityRatingData.Count > 0)
           if(candles.Count == 0 && securityRatingData != null)
            {
                securityRatingData.Clear();
            }


            //if(candles.Count - 1 - _candlesLookBack.ValueInt - 1 <= 0)
            //{
            //    return;
            //}
            CheckSecuritiesRating();

            List<Position> openPositions = tab.PositionsOpenAll;
            strTime = candles[^1].TimeStart.ToString();
            if (openPositions != null && openPositions.Count != 0)
            {
                //for (int i = 0; i < openPositions.Count; i++)
                //{
                    LogicClosePosition(candles, tab, openPositions[0]);
                //}
            }

            if (openPositions.Count == 0)
            { // opening logic

                int allPosesInAllTabs = this.PositionsCount;

                if (allPosesInAllTabs >= _maxPoses.ValueInt)
                {
                    return;
                }
                //if(_ValueCurrent )
                LogicOpenPosition(candles, tab);
            }
        }

        // Opening logic
        private void LogicOpenPosition(List<Candle> candles, BotTabSimple tab)
        {
            if (_tabScreener.PositionsOpenAll.Count >= _maxPoses.ValueInt)
            {
                return;
            }

            if (_securitiesToTrade.ValueString.Contains(tab.Security.Name) == false)
            {
                return;
            }
            SecurityRatingData sr = securityRatingData.Find(p => p.SecurityName == tab.Security.Name);
            if (sr != null && !sr.bStopOpenDeal)
            {
                decimal dWorkVolume = 0.0m;
                strTime = candles[^1].TimeStart.ToString();
                if (!sr.bShort && sr.iCondition >= _AlgorithmSensitivityLong.ValueInt && _Regime.ValueString != "OnlyShort")
                {
                    dWorkVolume = GetVolume(tab, Side.Buy);
                    if (dWorkVolume > 0)
                    {
                        string message2 = "Сигнал на открытие сделки Лонг. strTime: " + strTime + sr.SecurityRatingString + "; _ValueCurrent = " + _ValueCurrent.ToString();
                        // 1 only in log window
                        SendNewLogMessage(message2, Logging.LogMessageType.User);

                        tab.BuyAtMarket(dWorkVolume);
                    }
                }
                else
                {
                    if (sr.bShort && sr.iCondition >= _AlgorithmSensitivityShort.ValueInt && _Regime.ValueString != "OnlyLong")
                    {
                        dWorkVolume = GetVolume(tab, Side.Sell);
                        if (dWorkVolume > 0) 
                        { 
                            string message2 = "Сигнал на открытие сделки шорт . strTime: " + strTime + sr.SecurityRatingString + "; _ValueCurrent = " + _ValueCurrent.ToString();
                            // 1 only in log window
                            SendNewLogMessage(message2, Logging.LogMessageType.User);
                            //if()
                            tab.SellAtMarket(dWorkVolume);
                        }
                    }
                }
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles, BotTabSimple tab, Position position)
        {
            if (position.State != PositionStateType.Open
                          ||
                          (position.CloseOrders != null
                          && position.CloseOrders.Count > 0)
                          )
            {
                return;
            }
            SecurityRatingData sr = securityRatingData.Find(p => p.SecurityName == tab.Security.Name);

            if (sr != null)
            {



                decimal lastClose = candles[^1].Close;

                decimal stop = 0;
                decimal stopWithSlippage = 0;
                if (position.Direction == Side.Buy)
                {
                    if (sr.iConditionCloseLong >= _AlgorithmSensitivityLong.ValueInt)
                    {


                        //stop = lastClose - lastClose * (_trailStop.ValueDecimal / 100);
                        //stopWithSlippage = stop - stop * (_slippage.ValueDecimal / 100);

                        //if (stop > lastClose)
                        //{
                        string message2 = "Сигнал на закрытие сделки Лонг. strTime: " + strTime + sr.SecurityRatingString + "; _ValueCurrent = " + _ValueCurrent.ToString();
                        // 1 only in log window
                        SendNewLogMessage(message2, Logging.LogMessageType.User);

                        tab.CloseAtMarket(position, position.OpenVolume);
                        //    return;
                        //}

                        //tab.CloseAtTrailingStop(position, stop, stopWithSlippage);
                    }
                }
                else
                {
                    if (sr.iConditionCloseShort >= _AlgorithmSensitivityShort.ValueInt)
                    {
                        string message2 = "Сигнал на закрытие сделки Шорт. strTime: " + strTime + sr.SecurityRatingString + "; _ValueCurrent = " + _ValueCurrent.ToString();
                        // 1 only in log window
                        SendNewLogMessage(message2, Logging.LogMessageType.User);

                        tab.CloseAtLimit(position, lastClose + lastClose * (_slippage.ValueDecimal / 100), position.OpenVolume);
                    }
                }
            }
        }

        private void _tabScreener_PositionOpeningSuccesEvent(Position pos, BotTabSimple tab)
        {
            if (pos.State == PositionStateType.Open)
            {
                decimal _ValueOpen = (pos.Direction == Side.Buy ? pos.EntryPrice * pos.OpenVolume : -pos.EntryPrice * pos.OpenVolume);
                _ValueCurrent -= _ValueOpen;// - pos.CommissionTotal();
                if(_ValueOpen > 0)
                {
                    _ValueOpenLong += _ValueOpen;// - pos.CommissionTotal();
                }
                else
                {
                    _ValueOpenShort += -_ValueOpen;// - pos.CommissionTotal();
                }
            }
        }

        private void _tabToTrade_PositionClosingSuccesEvent(Position pos, BotTabSimple tab)
        {
            //dLastTrailValue = 0;
            if (pos.State == PositionStateType.Done)
            {
                decimal _ValueClose = (pos.Direction == Side.Buy ? pos.ClosePrice : -pos.ClosePrice) * pos.MaxVolume;
                _ValueCurrent += _ValueClose-pos.CommissionTotal();
                if (_ValueClose > 0)
                {
                    _ValueOpenLong -= pos.EntryPrice *pos.MaxVolume;// _ValueClose;// -pos.ProfitPortfolioAbs;// ;
                }
                else
                {
                    _ValueOpenShort -= pos.EntryPrice * pos.MaxVolume;// _ValueClose;
                }
                    string message2 = "Успешное  закрытие сделки Лонг. strTime: " + pos.SecurityName;
                // 1 only in log window
                SendNewLogMessage(message2, Logging.LogMessageType.User);

                //List<Position> openPositions = tab.PositionsOpenAll;
                ////if (obj.Direction == Side.Buy)
                //decimal price = pos.ClosePrice;
                //if (pos.SignalTypeStop == "Close long")
                //{
                //    if (_Regime.ValueString != "OnlyLong"
                //     && _Regime.ValueString != "OnlyClosePosition")
                //    {
                //        if (_UseRevers.ValueBool == true)
                //        {
                //            //_tabToTrade.SellAtLimit(GetVolume(_tabToTrade), price - price * (_slippage.ValueDecimal / 100));
                //            LogicOpenPosition();
                //        }
                //    }
                //}
                //else if (obj.SignalTypeStop == "Close short")
                //{
                //    if (_Regime.ValueString != "OnlyShort"
                //        && _Regime.ValueString != "OnlyClosePosition")
                //    {
                //        if (_UseRevers.ValueBool == true)
                //        {
                //            //_tabToTrade.BuyAtLimit(GetVolume(_tabToTrade), price + price * (_slippage.ValueDecimal / 100));

                //            LogicOpenPosition();
                //        }
                //    }
                //}

            }
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }

            //TradeLogic();
        }

        // Method for calculating the volume of entry into a position
        private decimal GetVolume(BotTabSimple tab, Side side)
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
                    List<PositionOnBoard> positionOnBoard = myPortfolio.GetPositionOnBoard();
                    //portfolioPrimeAsset = myPortfolio.ValueCurrent;
                    // decimal ValueCurrent = 0; // _ValueCurrent
                    if (StartProgram == StartProgram.IsOsTrader)
                    {
                        portfolioPrimeAsset = myPortfolio.ValueCurrent;
                    }
                    else
                    {
                        if (side == Side.Sell)
                        {
                            portfolioPrimeAsset = _ValueBegin - _ValueOpenShort;
                        }
                        else
                        {
                            portfolioPrimeAsset = _ValueCurrent;
                        }
                    }
                    if (portfolioPrimeAsset <= myPortfolio.ValueBegin * ((100 - _volume.ValueDecimal) / 100))
                    {
                        //SendNewLogMessage("Portfolio limit is depleted!!!" + _tradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
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

                if (GO == 1)
                {
                    PriceBestAsk = tab.PriceBestAsk;
                }
                else
                {
                    PriceBestAsk = GO;
                }

                decimal moneyOnPosition = 0;// portfolioPrimeAsset * (_volume.ValueDecimal / 100);//(portfolioPrimeAsset-_ValueBegin)
                if ((portfolioPrimeAsset - _ValueBegin)>= 0)
                    //(_volume.ValueDecimal / 100))
                {
                    moneyOnPosition = portfolioPrimeAsset * (_volume.ValueDecimal / 100);
                }
                else
                {
                    moneyOnPosition = portfolioPrimeAsset - _ValueBegin * ((100 - _volume.ValueDecimal) / 100);
                }
                if (moneyOnPosition <= 0)
                {
                    return 0;
                }
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