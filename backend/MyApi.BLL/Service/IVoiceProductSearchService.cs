using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public interface IVoiceProductSearchService
{
    Task<(string Action, string QueryText, double Confidence, bool NeedsConfirmation)> HandlePendingSearchAsync(
        string sessionId,
        string text,
        CommandInterpretation interpretation);
Task<VoiceResponseDto> HandleRemoveFromCartAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    CommandInterpretation interpretation,
    string userId);
    void ResetPendingSearchIfNeeded(string sessionId, string action);
void SetAwaitingPaymentConfirmation(string sessionId, bool value);
bool GetAwaitingPaymentConfirmation(string sessionId);
    Task<VoiceResponseDto> HandleSearchProductAsync(
        string sessionId,
        string text,
        string? language,
        string? correctedText,
        string queryText,
        CommandInterpretation interpretation);
ProductUserResponse? ResolveProductFromAiOrLastShown(
    CommandInterpretation interpretation,
    string text,
    string sessionId
);
    Task<VoiceResponseDto> HandleShowMoreProductsAsync(
        string sessionId,
        string text,
        string? language,
        string? correctedText);

    string GetLastAction(string sessionId);
    string GetLastQuery(string sessionId);
    string GetPendingSlot(string sessionId);
    int GetShownProductsCount(string sessionId);
    int GetTotalMatchedProductsCount(string sessionId);
    ProductSearchContext? GetSearchContext(string sessionId);
    string GetLastAssistantReply(string sessionId);
bool GetAwaitingResultRefinement(string sessionId);
Task<VoiceResponseDto> HandleAddToCartAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    CommandInterpretation interpretation,
    string? userId);
    ProductUserResponse? ResolveProductFromLastShown(string text, string sessionId);
    
void SetLastAction(string sessionId, string action);
void SetLastAssistantReply(string sessionId, string reply);
void ClearLastShownProducts(string sessionId);
Task<VoiceResponseDto> HandleSearchAndRecommendAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    string queryText,
    CommandInterpretation interpretation);
List<ProductUserResponse> GetLastShownProducts(string sessionId);
void SetCurrentProductDetails(
    string sessionId,
    ProductUserResponse product,
    string assistantReply);
Task<VoiceResponseDto> HandleRecommendProductAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    CommandInterpretation interpretation);
}