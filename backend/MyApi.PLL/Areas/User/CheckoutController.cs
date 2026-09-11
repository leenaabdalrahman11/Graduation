using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.DTO.Requests;

using Stripe;

namespace MyApi.PLL.Areas.User
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly ICartService _cartService;
        private readonly IConfiguration _configuration;

        public CheckoutController(
            ICheckoutService checkoutService,
            ICartService cartService,
            IConfiguration configuration)
        {
            _checkoutService = checkoutService;
            _cartService = cartService;
            _configuration = configuration;
        }
/*
        [HttpPost("")]
        public async Task<IActionResult> Payment([FromBody] CheckoutRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _checkoutService.ProccesPaymentAsync(request, userId);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            return Ok(result);
        }
*/
        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult GetStripeConfig()
        {
            return Ok(new
            {
                publishableKey = _configuration["Stripe:PublishableKey"]
            });
        }

        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new
                {
                    isSuccess = false,
                    message = "User not found."
                });

            var cart = await _cartService.GetUserCartAsync(userId);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    message = "Cart is empty."
                });
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = secretKey;

            long amount = (long)(cart.CartTotal * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = "ils",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "paymentMethod", request.PaymentMethod ?? "visa" }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return Ok(new
            {
                isSuccess = true,
                message = "Payment intent created successfully.",
                clientSecret = paymentIntent.ClientSecret,
                paymentIntentId = paymentIntent.Id,
                amount = cart.CartTotal
            });
        }

        [HttpPost("confirm-payment")]
public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
{
    if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
    {
        return BadRequest(new
        {
            isSuccess = false,
            message = "PaymentIntentId is required."
        });
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Unauthorized(new
        {
            isSuccess = false,
            message = "User not found."
        });
    }

    var secretKey = _configuration["Stripe:SecretKey"];
    StripeConfiguration.ApiKey = secretKey;

    var paymentIntentService = new PaymentIntentService();
    var paymentIntent = await paymentIntentService.GetAsync(request.PaymentIntentId);

    if (paymentIntent == null)
    {
        return BadRequest(new
        {
            isSuccess = false,
            message = "Payment intent not found."
        });
    }

    if (paymentIntent.Status != "succeeded")
    {
        return BadRequest(new
        {
            isSuccess = false,
            message = $"Payment not completed yet. Current status: {paymentIntent.Status}"
        });
    }

var result = await _checkoutService.CreateOrderAfterPaymentAsync(
    userId,
    paymentIntent.Id
);

if(!result.IsSuccess)
{
    return BadRequest(result);
}
    return Ok(new
    {
        isSuccess = true,
        message = "Payment completed successfully.",
        paymentIntentId = paymentIntent.Id,
        status = paymentIntent.Status
    });
}
        [HttpGet("success")]
[AllowAnonymous]
public async Task<IActionResult> Success([FromQuery] string session_id)
{
    if (string.IsNullOrWhiteSpace(session_id))
    {
        return BadRequest(new
        {
            isSuccess = false,
            message = "Session ID is required."
        });
    }

    var response = await _checkoutService.HandleSuccessAsync(session_id);

    if (!response.IsSuccess)
    {
        return BadRequest(new
        {
            isSuccess = false,
            message = response.Message
        });
    }

    return Ok(response);
}

[HttpGet("cancel")]
[AllowAnonymous]
public IActionResult Cancel()
{
    return Ok(new
    {
        isSuccess = false,
        message = "Payment cancelled."
    });
}
/*
 [HttpPost("complete-card-payment")]
    public async Task<IActionResult> CompleteCardPayment([FromBody] CardPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.CardNumber) ||
            string.IsNullOrWhiteSpace(request.CardHolderName) ||
            string.IsNullOrWhiteSpace(request.ExpiryMonth) ||
            string.IsNullOrWhiteSpace(request.ExpiryYear) ||
            string.IsNullOrWhiteSpace(request.Cvv))
        {
            return BadRequest("Card information is required.");
        }

        var checkoutResult = await _checkoutService.ProccesPaymentAsync(
            new CheckoutRequest
            {
                PaymentMethod = (MyApi.DAL.Models.PaymentMethod)Enum.Parse(
    typeof(MyApi.DAL.Models.PaymentMethod),
    "visa",
    true
)
            },
            userId
        );

        if (!checkoutResult.IsSuccess)
            return BadRequest(checkoutResult);

        return Ok(checkoutResult);
    }
    }
*/
    public class CreatePaymentIntentRequest
    {
        public string? PaymentMethod { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    }   
}