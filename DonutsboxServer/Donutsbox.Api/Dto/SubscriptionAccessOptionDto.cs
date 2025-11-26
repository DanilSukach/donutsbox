namespace Donutsbox.Api.Dto;

public class SubscriptionAccessOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Price { get; set; } = string.Empty;
}

