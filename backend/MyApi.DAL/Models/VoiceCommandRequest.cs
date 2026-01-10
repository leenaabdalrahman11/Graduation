namespace MyApi.DAL.Models;

public class VoiceCommandRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; }
     public int? SelectedProductIndex { get; set; }
}