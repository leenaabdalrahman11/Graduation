namespace MyApi.DAL.Models;

public class VoiceResponseDto
{
    public string Action { get; set; } = "";
    public string ReplyText { get; set; } = "";
    public string ScreenText { get; set; } = "";
    public string RecognizedText { get; set; } = "";
    public string CorrectedText { get; set; } = "";
    public bool NeedsConfirmation { get; set; }
    public string SuggestedAction { get; set; } = "";
    public string SuggestedQuery { get; set; } = "";
    public object? Data { get; set; }
}