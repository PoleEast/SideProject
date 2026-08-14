using System.ComponentModel.DataAnnotations;

namespace Project.Core.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class NotFutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
            {
                return date.Date <= DateTime.Today;
            }
            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return "查詢日期不可大於今天";
        }
    }
}
