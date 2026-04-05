using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public interface IVoiceAssistantService
{
Task<VoiceResponseDto> ExecuteCommandAsync(
    string text,
    string? language,
    string userId,
    int? selectedProductIndex = null
);}