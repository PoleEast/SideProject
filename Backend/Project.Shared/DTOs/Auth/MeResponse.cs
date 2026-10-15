using System.ComponentModel;

namespace Project.Shared.DTOs.Auth;

public class MeResponse
{
    [Description("使用者名稱")]
    public string Name { get; set; } = string.Empty;
}
