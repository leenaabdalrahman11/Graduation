using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class VoiceInterpretationService : IVoiceInterpretationService
{
    public CommandInterpretation? BuildInterpretation(
        CommandInterpretation? interpretationResult,
        string text,
        string? language)
    {
        if (interpretationResult == null)
            return RuleBasedFallback(text, language);

        var action = interpretationResult.Action?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(action) || action == "Unknown")
            return RuleBasedFallback(text, language);

        if (interpretationResult.Confidence < 0.60)
            return RuleBasedFallback(text, language);

        if (action == "SearchProduct" &&
            string.IsNullOrWhiteSpace(interpretationResult.Query) &&
            string.IsNullOrWhiteSpace(interpretationResult.ProductName) &&
            string.IsNullOrWhiteSpace(interpretationResult.ProductCategory) &&
            string.IsNullOrWhiteSpace(interpretationResult.Brand) &&
            string.IsNullOrWhiteSpace(interpretationResult.MatchedCatalogName))
        {
            return RuleBasedFallback(text, language);
        }

        return interpretationResult;
    }
public string DecideExecutionMode(CommandInterpretation interpretation)
{
    if (interpretation == null)
        return "Repeat";

    var action = interpretation.Action?.Trim() ?? "Unknown";

    if (action == "Unknown")
        return "Repeat";

    if (action == "Clarify")
        return "Clarify";

    var safeExecutableActions = new[]
    {
        "SearchProduct",
        "SearchAndRecommend",
        "RecommendProduct",
        "ShowMoreProducts",
        "ViewCart",
        "TrackLatestOrder",
        "ViewOrders",
        "OpenOrderDetails",
        "OpenProductDetails",
        "OpenPayment",
        "AddToCart",
        "RemoveFromCart"
    };

    if (safeExecutableActions.Contains(action) &&
        interpretation.Confidence >= 0.70 &&
        !interpretation.NeedsConfirmation)
    {
        return "Execute";
    }

    if (interpretation.Confidence >= 0.85 && !interpretation.NeedsConfirmation)
        return "Execute";

    if (interpretation.Confidence >= 0.60 || interpretation.NeedsConfirmation)
        return "Confirm";

    interpretation.Action = "Unknown";
    interpretation.ReplyText = ByLanguage(
        null,
        "لم أفهم ما قلت، هل يمكنك إعادة ما قلته؟",
        "I did not understand what you said. Could you please repeat?"
    );

    return "Repeat";
}
    public string ByLanguage(string? language, string ar, string en)
    {
        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.ToLowerInvariant();
        return selectedLanguage == "ar" ? ar : en;
    }

    public CommandInterpretation RuleBasedFallback(string text, string? language)
    {
        return new CommandInterpretation
        {
            Action = "Unknown",
            Query = "",
            ProductName = "",
            ProductCategory = "",
            Brand = "",
            Model = "",
            Keywords = "",
            Attributes = "",
            MatchedCatalogName = "",
            CorrectedText = text,
            Confidence = 0.0,
            NeedsConfirmation = false,
            ReplyText = ByLanguage(
                language,
                "لم أفهم ما قلت، هل يمكنك إعادة ما قلته؟",
                "I did not understand what you said. Could you please repeat?"
            ),
            Reason = "The user's speech was unclear or the AI interpretation was not reliable."
        };
    }

    public string CleanSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var separators = new[] { ".", ",", "?", "!", "،", ":", ";", "-", "_", "/" };

        var cleaned = query.Trim();

        foreach (var sep in separators)
        {
            cleaned = cleaned.Replace(sep, " ");
        }

        var words = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(" ", words);
    }
    public string GetNaturalReply(string text, string? language)
    {
        var lower = text.Trim().ToLowerInvariant();
        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.ToLowerInvariant();

        if (selectedLanguage == "ar")
        {
            if (lower.Contains("مرحبا") || lower.Contains("هلو") || lower.Contains("اهلا"))
                return "أهلا، كيف بقدر أساعدك؟";

            if (lower.Contains("كيفك") || lower.Contains("شلونك"))
                return "أنا بخير، كيف بقدر أساعدك اليوم؟";

            if (lower.Contains("شكرا"))
                return "على الرحب والسعة، كيف بقدر أساعدك كمان؟";

            return "أكيد، كيف بقدر أساعدك؟";
        }

        if (selectedLanguage == "en")
        {
            if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey"))
                return "Hello, how can I help you?";

            if (lower.Contains("how are you"))
                return "I am fine. How can I help you today?";

            if (lower.Contains("thanks") || lower.Contains("thank you"))
                return "You're welcome. How else can I help you?";

            return "Sure, how can I help you?";
        }

        return "How can I help you?";
    }
    private string CorrectUserText(string text, string? language)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = text.Trim()
            .Replace("،", " ")
            .Replace(".", " ")
            .Replace("!", " ")
            .Replace("?", " ")
            .Replace("-", " ");

        return cleaned;
    }
}