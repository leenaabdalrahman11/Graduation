using System.Text;
using System.Text.RegularExpressions;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class VoiceTextCorrectionService : IVoiceTextCorrectionService
{
    public VoiceTextCorrectionResult CorrectText(
        string text,
        List<string> catalogProductNames,
        string? language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new VoiceTextCorrectionResult
            {
                OriginalText = text ?? "",
                CorrectedText = text ?? "",
                Confidence = 0
            };
        }

        if (catalogProductNames == null || catalogProductNames.Count == 0)
        {
            return new VoiceTextCorrectionResult
            {
                OriginalText = text,
                CorrectedText = text,
                Confidence = 0
            };
        }

        var normalizedText = Normalize(text);

        var best = catalogProductNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(productName =>
            {
                var normalizedProduct = Normalize(productName);

                var score = CalculateSimilarityScore(normalizedText, normalizedProduct);

                return new
                {
                    OriginalName = productName,
                    NormalizedName = normalizedProduct,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best == null)
        {
            return new VoiceTextCorrectionResult
            {
                OriginalText = text,
                CorrectedText = text,
                Confidence = 0
            };
        }

        
        if (best.Score >= 0.88)
        {
            var corrected = ReplaceClosestProductPart(text, best.OriginalName);

            return new VoiceTextCorrectionResult
            {
                OriginalText = text,
                CorrectedText = corrected,
                BestCatalogMatch = best.OriginalName,
                Confidence = best.Score,
                WasCorrected = !string.Equals(text, corrected, StringComparison.OrdinalIgnoreCase),
                ShouldAutoCorrect = true,
                NeedsConfirmation = false
            };
        }

        if (best.Score >= 0.72)
        {
            return new VoiceTextCorrectionResult
            {
                OriginalText = text,
                CorrectedText = text,
                BestCatalogMatch = best.OriginalName,
                Confidence = best.Score,
                WasCorrected = false,
                ShouldAutoCorrect = false,
                NeedsConfirmation = true
            };
        }

        return new VoiceTextCorrectionResult
        {
            OriginalText = text,
            CorrectedText = text,
            BestCatalogMatch = "",
            Confidence = best.Score,
            WasCorrected = false,
            ShouldAutoCorrect = false,
            NeedsConfirmation = false
        };
    }

    private static double CalculateSimilarityScore(string userText, string catalogName)
    {
        if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(catalogName))
            return 0;

        if (userText.Contains(catalogName, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (catalogName.Contains(userText, StringComparison.OrdinalIgnoreCase))
            return 0.92;

        var userTokens = userText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var catalogTokens = catalogName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var bestTokenScore = 0.0;

        foreach (var userToken in userTokens)
        {
            foreach (var catalogToken in catalogTokens)
            {
                var tokenScore = Similarity(userToken, catalogToken);
                if (tokenScore > bestTokenScore)
                    bestTokenScore = tokenScore;
            }
        }

        var fullScore = Similarity(userText, catalogName);

        return Math.Max(fullScore, bestTokenScore);
    }

    private static string ReplaceClosestProductPart(string originalText, string bestCatalogName)
    {
        /*
         * هنا ما بنحاول نستبدل الأمر كامل.
         * بنرجّع الجملة الأصلية + اسم المنتج الأقرب.
         * هذا آمن أكثر من تغيير كل الجملة.
         */
        var commandWords = new[]
        {
            "ابحث", "ابحثي", "دور", "دوري", "فتش", "فتشي",
            "اعرض", "اعرضي", "بدي", "اريد", "أريد",
            "search", "find", "show", "want"
        };

        var words = originalText.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var commandPart = words
            .Where(w => commandWords.Any(c =>
                Normalize(w).Contains(Normalize(c), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (commandPart.Count == 0)
            return bestCatalogName;

        return $"{string.Join(" ", commandPart)} {bestCatalogName}";
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var value = text.Trim().ToLowerInvariant();

        value = RemoveArabicDiacritics(value);

        value = value
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ى", "ي")
            .Replace("ؤ", "و")
            .Replace("ئ", "ي")
            .Replace("ة", "ه");

        value = Regex.Replace(value, @"[^\p{L}\p{N}\s]", " ");
        value = Regex.Replace(value, @"\s+", " ").Trim();

        return value;
    }

    private static string RemoveArabicDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);

            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static double Similarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0;

        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return 1;

        var distance = LevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        if (maxLength == 0)
            return 1;

        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var matrix = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= b.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}