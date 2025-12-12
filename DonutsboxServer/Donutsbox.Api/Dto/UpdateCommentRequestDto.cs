using System.ComponentModel.DataAnnotations;

namespace Donutsbox.Api.Dto;

public class UpdateCommentRequestDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public required string Text { get; set; }
}
