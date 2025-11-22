namespace Donutsbox.Api.Services.Payments;

public class YooKassaOptions
{
    /// <summary>
    /// Идентификатор магазина в YooKassa.
    /// </summary>
    public required string ShopId { get; set; }

    /// <summary>
    /// Секретный ключ магазина.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Секрет вебхука (пароль для Basic auth).
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Базовый URL для API YooKassa. По умолчанию https://api.yookassa.ru/v3/
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.yookassa.ru/v3/";

    /// <summary>
    /// Нужно ли автоматически подтверждать платеж.
    /// </summary>
    public bool CapturePayment { get; set; } = true;

    /// <summary>
    /// Базовый URL для возврата пользователя после оплаты (может быть переопределён в запросе).
    /// </summary>
    public string? DefaultReturnUrl { get; set; }
}

