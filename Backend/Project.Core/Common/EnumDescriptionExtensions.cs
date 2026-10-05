using System.ComponentModel;
using System.Reflection;

namespace Project.Core.Common;

public static class EnumDescriptionExtensions
{
    /// <summary>
    /// 取得列舉值上 <see cref="DescriptionAttribute"/> 標註的文字，沒有標註時為列舉值的名稱
    /// </summary>
    public static string GetDescription(this Enum value)
        => value.GetType()
                .GetField(value.ToString())?
                .GetCustomAttribute<DescriptionAttribute>()?
                .Description
            ?? value.ToString();
}
