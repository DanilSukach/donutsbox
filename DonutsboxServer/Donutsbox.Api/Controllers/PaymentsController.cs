using Donutsbox.Api.Dto;
using Donutsbox.Api.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donutsbox.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(ISubscriptionPaymentService paymentService, ILogger<PaymentsController> logger) : ControllerBase
{
    /// <summary>
    /// Создать платеж для подписки и получить ссылку YooKassa.
    /// </summary>
    [Authorize]
    [HttpPost("subscriptions")]
    public async Task<ActionResult<SubscriptionPaymentResponseDto>> CreateSubscriptionPayment([FromBody] SubscriptionPaymentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await paymentService.CreateSubscriptionPaymentAsync(dto, User, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to create subscription payment");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить статус платежа подписки.
    /// </summary>
    [Authorize]
    [HttpGet("subscriptions/{paymentRequestId:guid}")]
    public async Task<ActionResult<SubscriptionPaymentStatusDto>> GetSubscriptionPaymentStatus([FromRoute] Guid paymentRequestId, CancellationToken cancellationToken)
    {
        var status = await paymentService.GetPaymentStatusAsync(paymentRequestId, User, cancellationToken);
        if (status == null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    /// <summary>
    /// Webhook от YooKassa о статусах платежей.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("yookassa/webhook")]
    public async Task<IActionResult> HandleYooKassaWebhook([FromBody] YooKassaWebhook webhook, CancellationToken cancellationToken)
    {
        try
        {
            await paymentService.HandleWebhookAsync(webhook, Request.Headers, cancellationToken);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process YooKassa webhook");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

