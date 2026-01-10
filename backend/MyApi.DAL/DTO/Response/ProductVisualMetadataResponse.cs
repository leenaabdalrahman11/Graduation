
public class ProductVisualMetadataResponse
{
    public string? CategoryAr { get; set; }
    public string? CategoryEn { get; set; }

    public string? SubCategoryAr { get; set; }
    public string? SubCategoryEn { get; set; }

    public string? ColorsAr { get; set; }
    public string? ColorsEn { get; set; }

    public string? MaterialAr { get; set; }
    public string? MaterialEn { get; set; }

    public string? StyleAr { get; set; }
    public string? StyleEn { get; set; }

    public string? PatternAr { get; set; }
    public string? PatternEn { get; set; }

    public string? CaptionAr { get; set; }
    public string? CaptionEn { get; set; }

    public string? KeywordsAr { get; set; }
    public string? KeywordsEn { get; set; }

    public decimal Confidence { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string? ModelName { get; set; }
}