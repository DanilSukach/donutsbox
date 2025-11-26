using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

[Table("subscription_payment")]
public class SubscriptionPayment
{
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }

    [Column("user_id", TypeName = "uuid")]
    [Required]
    public required Guid UserId { get; set; }
    public required User User { get; set; }

    [Column("subscription_id", TypeName = "uuid")]
    [Required]
    public required Guid SubscriptionId { get; set; }
    public required Subscription Subscription { get; set; }

    [Column("payment_id")]
    [MaxLength(128)]
    public string? PaymentId { get; set; }

    [Column("status")]
    [MaxLength(32)]
    [Required]
    public string Status { get; set; } = "pending";

    [Column("amount", TypeName = "numeric(12,2)")]
    [Required]
    public decimal Amount { get; set; }

    [Column("currency")]
    [MaxLength(8)]
    [Required]
    public string Currency { get; set; } = "RUB";

    [Column("confirmation_url")]
    [MaxLength(512)]
    public string? ConfirmationUrl { get; set; }

    [Column("description")]
    [MaxLength(256)]
    public string? Description { get; set; }

    [Column("idempotence_key")]
    [MaxLength(64)]
    public string? IdempotenceKey { get; set; }

    [Column("created_at", TypeName = "timestamptz")]
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("expires_at", TypeName = "timestamptz")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }

    [Column("user_subscription_id", TypeName = "uuid")]
    public Guid? UserSubscriptionId { get; set; }
    public UserSubscription? UserSubscription { get; set; }
}

