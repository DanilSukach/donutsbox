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
    [Required]
    public required int UserTypeId { get; set; }
    /// <summary>
    /// Сущность для авторизации
    /// </summary>
    [Column("user_auth")]
    [Required]
    public required UserAuth UserAuth { get; set; }
    [Required]
    public required Guid UserAuthId { get; set; }
    /// <summary>
    /// Сущность с данными о себе
    /// </summary>
    [Column("user_data")]
    public UserData? UserData { get; set; }
}
