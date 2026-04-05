using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MyApiProject.MyApi.DAL.DTO.Requests;
namespace MyApi.PLL.Controllers;

[ApiController]
[Route("api/voice-test")]
public class VoiceTestController : ControllerBase
{
    private readonly IVoiceAssistantService _voiceAssistantService;

    public VoiceTestController(IVoiceAssistantService voiceAssistantService)
    {
        _voiceAssistantService = voiceAssistantService;
    }

    [HttpPost("text")]
    public async Task<IActionResult> TestByText([FromBody] VoiceTextTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text is required");
        }

        var userId = string.IsNullOrWhiteSpace(request.UserId)
            ? "test-user"
            : request.UserId;

        var result = await _voiceAssistantService.ExecuteCommandAsync(
            request.Text,
            request.Language,
            userId
        );

        return Ok(result);
    }
}