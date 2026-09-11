using Microsoft.AspNetCore.Http;
using MyApi.DAL.Models;
using MyApi.DAL.DTO.Response;
namespace MyApi.BLL.Service;

public interface IOpenAiService
{

    Task<string?> TranscribeAudioAsync(IFormFile file, string? language);
Task<CommandInterpretation?> InterpretCommandAsync(
    string text,
    string? language,
    string? currentScreen = null,
    string? lastAction = null,
    string? lastProductQuery = null,
    string? pendingSlot = null,
    string? lastAssistantReply = null,
    int shownProductsCount = 0,
    int totalMatchedProducts = 0,
    ProductSearchContext? searchContext = null,
    List<string>? catalogProductNames = null,
    List<ProductUserResponse>? shownProducts = null,
    bool awaitingResultRefinement = false,
    bool awaitingPaymentConfirmation = false,
    List<CartResponse>? cartItems = null
);
    string DecideExecutionMode(CommandInterpretation result);

Task<ProductRecommendationResult?> RecommendBestProductAsync(
    List<ProductUserResponse> products,
    CommandInterpretation interpretation,
    string? language);
Task<AiProductMatchResult?> FindClosestProductsAsync(
    string userText,
    List<ProductUserResponse> products,
    CommandInterpretation interpretation,
    string? language);
}