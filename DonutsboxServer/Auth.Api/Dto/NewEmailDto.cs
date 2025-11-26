using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dto;

public class NewEmailDto
{
    [EmailAddress]
    public required string Email { get; set; }
}
