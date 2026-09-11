namespace MyApi.DAL.Models;

public class ProductRecommendationResult
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Reason { get; set; } = "";
    public double Confidence { get; set; }
}