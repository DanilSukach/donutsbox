using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donutsbox.Domain.Entities;

/// <summary>
/// Обычный пользователь
/// </summary>
[Table("user")]
public class User
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    [Column("id", TypeName = "uuid")]
    public required Guid Id { get; set; }
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Column("name")]
    [MaxLength(30)]
    [Required]
    public required string Name { get; set; }
    /// <summary>
    /// Тип пользователя
    /// </summary>
    [Required]
    public required UserType UserType { get; set; }
    [Column("user_type_id")]
    [Required]
    public required int UserTypeId { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    [Required]
    public required UserAuth UserAuth { get; set; }
    [Column("user_auth_id", TypeName = "uuid")]
    [Required]
    public required Guid UserAuthId { get; set; }
    /// <summary>
    /// Сущность с данными о себе
    /// </summary>
    public UserData? UserData { get; set; }
    public CreatorPageData? CreatorPageData { get; set; }
    public List<UserSubscription> UserSubscriptions { get; set; } = [];
}
