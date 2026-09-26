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
        SettlementRecorded,
        [Description("建立群組")]
        GroupCreated,
        [Description("編輯群組")]
        GroupUpdated,
        [Description("刪除群組")]
        GroupDeleted,
        [Description("結束群組")]
        GroupClosed,
        [Description("重啟群組")]
        GroupReopened,
        [Description("重置邀請碼")]
        InviteCodeReset
    }
}
