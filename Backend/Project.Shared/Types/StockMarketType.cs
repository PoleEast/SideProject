using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Project.Shared.Types
{
    public enum StockMarketType
    {
        [Description("台股")]
        TW,
        [Description("美股")]
        US,
        [Description("日股")]
        JP
    }
}
