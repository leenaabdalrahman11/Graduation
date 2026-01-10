namespace MyApi.DAL.Models;

public class CategoryAlias
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Alias { get; set; } = "";
    public string Language { get; set; } = "ar";
}