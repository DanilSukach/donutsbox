namespace Auth.Api.Dto;

public class NewPasswordDto
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string RepeatNewPassword { get; set; }
}
