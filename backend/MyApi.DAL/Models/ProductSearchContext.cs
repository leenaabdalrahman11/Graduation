namespace MyApi.DAL.Models;

public class ProductSearchContext
{
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? ProductName { get; set; }
    public string? Model { get; set; }
    public string? Attributes { get; set; }
}