public class AiProductMatchResult
{
    public bool ExactMatchFound { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public string MatchType { get; set; } = "";
    public string ReplyText { get; set; } = "";
    public string Reason { get; set; } = "";
    public double Confidence { get; set; }
}