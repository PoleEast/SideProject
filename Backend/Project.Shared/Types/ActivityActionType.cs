using System.ComponentModel;

namespace Project.Shared.Types
{
    /// <summary>
    /// 群組動態的變動類型
    /// </summary>
    /// <remarks>
    /// 前端不解析該字串（見 ADR 20260731_扁平信任權限與群組動態）。
    /// </remarks>
    public enum ActivityActionType
    {
        [Description("新增花費")]
        ExpenseCreated,
        [Description("編輯花費")]
        ExpenseUpdated,
        [Description("刪除花費")]
        ExpenseDeleted,
        [Description("新增成員")]
        MemberAdded,
        [Description("移除成員")]
        MemberRemoved,
        [Description("記錄還款")]
        SettlementRecorded
    }
}
