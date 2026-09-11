namespace MyApi.DAL.Models;


public class CommandInterpretation
{
    public int? Quantity { get; set; }
    public string? Action { get; set; }
    public string? Query { get; set; }
    public string? ConversationMode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCategory { get; set; }
    public string? Brand { get; set; }
    public bool ShouldShowProducts { get; set; } = false;
    public string? Model { get; set; }
    public string? Keywords { get; set; }
    public string? Attributes { get; set; }
    public string? CorrectedText { get; set; }
    public double Confidence { get; set; }
    public bool NeedsConfirmation { get; set; }
    public string? MissingInfo { get; set; }
    public bool EnoughInfoToSearch { get; set; }
    public bool ShouldSearchNow { get; set; }
    public bool ShouldShowMore { get; set; }
    public string? ReplyText { get; set; }
    public string? Reason { get; set; }
    public string? CatalogMatchedName { get; set; }
    public decimal? MinPrice { get; set; }
public decimal? MaxPrice { get; set; }
public string? Color { get; set; }
public string? Size { get; set; }
public string? Material { get; set; }
public string? MatchedCatalogName { get; set; }
public string? ContextualMeaning { get; set; }
public int? SelectedProductIndex { get; set; }
public int? SelectedProductId { get; set; }
public string? SelectedProductName { get; set; }


}