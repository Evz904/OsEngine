using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.EVZRobots
{
    [Bot("RSI3Bot1")]
    internal class RSI3Bot1 : BotPanel
    {
        public RSI3Bot1(string name, StartProgram startProgram) : base(name, startProgram)
        {
        }

        public override string GetNameStrategyType()
        {
            return "RSI3Bot1";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }
    }
}
