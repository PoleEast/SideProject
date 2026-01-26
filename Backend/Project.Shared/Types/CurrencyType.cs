using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Project.Shared.Types
{
    public enum CurrencyType
    {
        [Description("台幣")]
        TWD,
        [Description("美元")]
        USD,
        [Description("日圓")]
        JPY
    }
}
