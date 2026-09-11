namespace MyApi.DAL.Models;

public class VoiceTextCorrectionResult
{
    public string OriginalText { get; set; } = "";
    public string CorrectedText { get; set; } = "";
    public string BestCatalogMatch { get; set; } = "";
    public double Confidence { get; set; }
    public bool WasCorrected { get; set; }
    public bool ShouldAutoCorrect { get; set; }
    public bool NeedsConfirmation { get; set; }
}