namespace Donutsbox.Api.Dto;

public class SubscriptionDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public required Guid Id { get; set; }
    /// <summary>
    /// Цена подписки
    /// </summary>
    public required string Price { get; set; }
    /// <summary>
    /// Название подписки
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Описание подписки
    /// </summary>
    public required string Description { get; set; }
    /// <summary>
    /// Ссылка на картинку подписки
    /// </summary>
    public string? PictureURL { get; set; }
    /// <summary>
    /// Идентификатор периода подписки
    /// </summary>
    public required int SubscriptionPeriodId { get; set; }
    /// <summary>
    /// Количество месяцев в периоде подписки
    /// </summary>
    public required int SubscriptionPeriodMonths { get; set; }
    /// <summary>
    /// Цена в расчете на один месяц
    /// </summary>
    public required string MonthlyPrice { get; set; }
    /// <summary>
    /// Родительская подписка (уровень)
    /// </summary>
    public Guid? ParentSubscriptionId { get; set; }
}