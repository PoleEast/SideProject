using Project.Shared.DTOs;
using Project.Shared.Types;

namespace Project.Api.Helpers;

public static class MemberCapacityRules
{
    public const int MaxActiveMembers = 16;

    /// <summary>
    /// 一個群組歷來能有的成員數
    /// </summary>
    /// <remarks>
    /// 已移除成員仍占著名額，移除不會歸還。
    /// </remarks>
    public const int MaxMemberSlots = 25;

    /// <summary>
    /// 檢查群組是否容得下這次要新增的成員
    /// </summary>
    /// <remarks>
    /// 名額用盡先於人員已滿判斷 —— 兩者同時成立時移除成員也解除不了，訊息不該引導去移除。
    /// </remarks>
    /// <param name="activeMemberCount">群組的現役成員數</param>
    /// <param name="usedSlotCount">群組已用掉的名額，即含已移除成員的成員數</param>
    /// <param name="addingCount">這次要新增的成員數</param>
    /// <returns>
    /// 容得下時為成功；名額用盡、人員已滿、或可新增的數量不足以容納這次新增時回 BusinessRuleViolation，
    /// 訊息指出是哪一種，數量不足時列出還能新增幾位
    /// </returns>
    public static Result Check(int activeMemberCount, int usedSlotCount, int addingCount)
    {
        if (usedSlotCount >= MaxMemberSlots)
        {
            return Result.Failure(ResultCode.BusinessRuleViolation, "這個群組的名額已用盡，無法再加入新成員");
        }

        if (activeMemberCount >= MaxActiveMembers)
        {
            return Result.Failure(ResultCode.BusinessRuleViolation, $"這個群組人員已滿，現役成員最多 {MaxActiveMembers} 位");
        }

        int addableCount = Math.Min(MaxActiveMembers - activeMemberCount, MaxMemberSlots - usedSlotCount);

        if (addingCount > addableCount)
        {
            return Result.Failure(ResultCode.BusinessRuleViolation, $"這個群組目前只能再新增 {addableCount} 位成員");
        }

        return Result.Success();
    }
}
