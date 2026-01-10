namespace MyApi.DAL.DTO.Response;

public class ProductImageAnalysisResult
{
    public string CategoryAr { get; set; } = "";
    public string CategoryEn { get; set; } = "";

    public string SubCategoryAr { get; set; } = "";
    public string SubCategoryEn { get; set; } = "";

    public List<string> ColorsAr { get; set; } = new();
    public List<string> ColorsEn { get; set; } = new();

    public string MaterialAr { get; set; } = "";
    public string MaterialEn { get; set; } = "";

    public string StyleAr { get; set; } = "";
    public string StyleEn { get; set; } = "";

    public string PatternAr { get; set; } = "";
    public string PatternEn { get; set; } = "";

    public string CaptionAr { get; set; } = "";
    public string CaptionEn { get; set; } = "";

public List<string> KeywordsAr { get; set; } = new();
public List<string> KeywordsEn { get; set; } = new();

    public double Confidence { get; set; }

    public string ModelName { get; set; } = "";
}