using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public interface IVoiceTextCorrectionService
{
    VoiceTextCorrectionResult CorrectText(
        string text,
        List<string> catalogProductNames,
        string? language);
}