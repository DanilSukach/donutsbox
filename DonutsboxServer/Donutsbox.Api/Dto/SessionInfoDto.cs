using System;

namespace Donutsbox.Api.Dto;

public class SessionInfoDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsCreator { get; set; }
    public bool HasCreatorPage { get; set; }
    public Guid? CreatorPageId { get; set; }
    public bool IsFirstLogin { get; set; }
}

