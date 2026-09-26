using System.ComponentModel.DataAnnotations;

namespace Project.Core.Common
{
    /// <summary>
    /// 日期不可晚於 UTC 的今天
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class NotFutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateOnly date)
            {
                return date <= DateOnly.FromDateTime(DateTime.UtcNow);
            }
            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return "查詢日期不可大於今天";
        }
    }
}
