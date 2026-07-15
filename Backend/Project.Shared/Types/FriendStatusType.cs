using System.ComponentModel;

namespace Project.Shared.Types
{
    public enum FriendStatusType
    {
        [Description("待處理")]
        Pending,

        [Description("已接受")]
        Accepted,

        [Description("已封鎖")]
        Blocked,
    }
}
