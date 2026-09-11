using MyApi.DAL.Models;
using MyApi.DAL.DTO.Response;
namespace MyApi.BLL.Service;

public interface IVoiceInterpretationService
{
    CommandInterpretation BuildInterpretation(
        CommandInterpretation? interpretationResult,
        string text,
        string? language);

    string DecideExecutionMode(CommandInterpretation interpretation);

    string ByLanguage(string? language, string ar, string en);

    string GetNaturalReply(string text, string? language);

    string CleanSearchQuery(string query);


    CommandInterpretation RuleBasedFallback(string text, string? language);
}