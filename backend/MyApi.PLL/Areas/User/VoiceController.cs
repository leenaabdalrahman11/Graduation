using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace MyApi.PLL.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly IVoiceAssistantService _voiceAssistantService;
    private readonly IOpenAiService _openAiService;

    public VoiceController(
        IVoiceAssistantService voiceAssistantService,
        IOpenAiService openAiService)
    {
        _voiceAssistantService = voiceAssistantService;
        _openAiService = openAiService;
    }

    [HttpGet("start")]
    public IActionResult Start([FromQuery] string? language)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "en" : language.ToLower();

        if (lang == "ar")
        {
            return Ok(new
            {
                message = "مرحبًا، كيف أستطيع مساعدتك؟ يمكنك قول ابحث عن منتج، اعرض عناصر السلة، تتبع آخر طلب، أو اعرض طلباتي.",
                options = new[]
                {
                    "ابحث عن منتج",
                    "اعرض عناصر السلة",
                    "تتبع آخر طلب",
                    "اعرض طلباتي"
                },
                status = "جاهز"
            });
        }

        return Ok(new
        {
            message = "Hi, how can I help you? You can say search for a product, view cart items, track your latest order, or view my orders.",
            options = new[]
            {
                "Search for a product",
                "View cart items",
                "Track your latest order",
                "View my orders"
            },
            status = "Ready"
        });
    }

    [HttpPost("transcribe")]
    public async Task<IActionResult> Transcribe([FromForm] IFormFile file, [FromForm] string? language)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No audio file uploaded." });

        var text = await _openAiService.TranscribeAudioAsync(file, language);
 Console.WriteLine("===== TRANSCRIBED TEXT =====");
    Console.WriteLine(text);
    Console.WriteLine("===== END TRANSCRIBED TEXT =====");
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { message = "Could not transcribe audio." });

        return Ok(new { text });
    }

[Authorize]
[HttpPost("command")]
public async Task<IActionResult> ExecuteCommand([FromBody] VoiceCommandRequest request)
{
    if (request == null || string.IsNullOrWhiteSpace(request.Text))
        return BadRequest(new { message = "Text is required" });

    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized(new { message = "Invalid token." });

   var result = await _voiceAssistantService.ExecuteCommandAsync(
    request.Text,
    request.Language,
    userId,
    request.SelectedProductIndex
);

    return Ok(result);
}
}