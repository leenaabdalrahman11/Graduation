using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public interface IVoiceContextResolver
{
    CommandInterpretation? Resolve(
        CommandInterpretation? interpretation,
        VoiceSearchSessionState state,
        string userText,
        string? language);
}