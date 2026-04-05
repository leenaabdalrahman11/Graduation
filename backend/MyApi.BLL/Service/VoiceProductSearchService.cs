using System.Linq;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using Microsoft.Extensions.Caching.Memory;
using MyApi.DAL.DTO.Requests;
namespace MyApi.BLL.Service;

public class VoiceProductSearchService : IVoiceProductSearchService
{
    private readonly IProductService _productService;
    private readonly IVoiceInterpretationService _interpretationService;
    private readonly IMemoryCache _cache;
    private readonly ICartService _cartService;
    private readonly IOpenAiService _openAiService;
    public VoiceProductSearchService(
      IProductService productService,
      IVoiceInterpretationService interpretationService,
      IMemoryCache cache,
      ICartService cartService,
      IOpenAiService openAiService)
    {
        _productService = productService;
        _interpretationService = interpretationService;
        _cache = cache;
        _cartService = cartService;
        _openAiService = openAiService;
    }
    private static bool IsSearchExecutionAction(string? action)
    {
        return string.Equals(action, "SearchProduct", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(action, "SearchAndRecommend", StringComparison.OrdinalIgnoreCase);
    }


    public async Task<(string Action, string QueryText, double Confidence, bool NeedsConfirmation)> HandlePendingSearchAsync(
        string sessionId,
        string text,
        CommandInterpretation interpretation)
    {
        string action = interpretation.Action?.Trim() ?? "Unknown";
        var queryText = interpretation.Query?.Trim() ?? string.Empty;
        var confidence = interpretation.Confidence;
        var needsConfirmation = interpretation.NeedsConfirmation;

        var state = GetState(sessionId);

        Console.WriteLine("===== HANDLE PENDING SEARCH START =====");
        Console.WriteLine($"text: {text}");
        Console.WriteLine($"state.LastQuery: {state.LastQuery}");
        Console.WriteLine($"state.PendingIntent: {state.PendingIntent}");
        Console.WriteLine($"state.PendingSlot: {state.PendingSlot}");
        Console.WriteLine($"state.AwaitingFinalSearchConfirmation: {state.AwaitingFinalSearchConfirmation}");
        Console.WriteLine($"state.AwaitingResultRefinement: {state.AwaitingResultRefinement}");
        Console.WriteLine($"interpretation.Action: {interpretation.Action}");
        Console.WriteLine($"interpretation.ConversationMode: {interpretation.ConversationMode}");
        Console.WriteLine($"interpretation.ShouldSearchNow: {interpretation.ShouldSearchNow}");
        Console.WriteLine("===== HANDLE PENDING SEARCH END =====");

        if (state.AwaitingResultRefinement && !string.IsNullOrWhiteSpace(text))
        {
            state.SearchContext ??= new ProductSearchContext();
            UpdateSearchContext(state.SearchContext, interpretation);

            if (interpretation.Action == "Clarify" &&
                interpretation.ConversationMode == "RefineResults" &&
                !interpretation.ShouldSearchNow)
            {
                action = "Clarify";
                queryText = state.LastQuery;
                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.90;
                needsConfirmation = false;

                state.LastAssistantReply = interpretation.ReplyText;
                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }

            if (IsSearchExecutionAction(interpretation.Action) &&
                interpretation.ShouldSearchNow)
            {
                action = interpretation.Action?.Trim() ?? "SearchProduct";

                var pendingContextQuery = BuildSearchQueryFromContext(state.SearchContext);

                queryText = !string.IsNullOrWhiteSpace(state.LastQuery)
                    ? state.LastQuery
                    : (!string.IsNullOrWhiteSpace(pendingContextQuery)
                        ? pendingContextQuery
                        : (!string.IsNullOrWhiteSpace(interpretation.Query)
                            ? interpretation.Query
                            : ""));

                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.95;
                needsConfirmation = false;

                state.AwaitingResultRefinement = false;
                state.SkipFilterStepOnce = true;

                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }

            return (action, queryText, confidence, needsConfirmation);
        }

        if (state.AwaitingFinalSearchConfirmation && !string.IsNullOrWhiteSpace(text))
        {
            state.SearchContext ??= new ProductSearchContext();
            var contextualMeaning = interpretation.ContextualMeaning?.Trim();

            var isNewSearchFromAi =
                string.Equals(contextualMeaning, "NewSearch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contextualMeaning, "StartNewSearch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contextualMeaning, "NewProductSearch", StringComparison.OrdinalIgnoreCase);

            if (isNewSearchFromAi)
            {
                action = interpretation.Action?.Trim() ?? "Clarify";

                queryText = !string.IsNullOrWhiteSpace(interpretation.Query)
                    ? interpretation.Query.Trim()
                    : BuildSearchQueryFromContext(state.SearchContext);

                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.90;
                needsConfirmation = interpretation.NeedsConfirmation;

                state.SearchContext = new ProductSearchContext();
                UpdateSearchContext(state.SearchContext, interpretation);

                var newContextQuery = BuildSearchQueryFromContext(state.SearchContext);

                state.LastQuery = !string.IsNullOrWhiteSpace(newContextQuery)
                    ? newContextQuery
                    : queryText;

                state.AwaitingFinalSearchConfirmation = false;
                state.AwaitingResultRefinement = false;
                state.AwaitingFilterChoice = false;
                state.SkipFinalSummaryOnce = false;
                state.SkipFilterStepOnce = false;
                state.PendingIntent = "";
                state.PendingSlot = "";

                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }

            if (interpretation.Action == "Clarify" &&
                interpretation.ConversationMode == "AskFinalDetails" &&
                !interpretation.ShouldSearchNow)
            {
                UpdateSearchContext(state.SearchContext, interpretation);

                action = "Clarify";
                queryText = state.LastQuery;
                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.95;
                needsConfirmation = false;

                state.LastAssistantReply = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                    ? interpretation.ReplyText
                    : state.LastAssistantReply;

                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }

            if (IsSearchExecutionAction(interpretation.Action) &&
                (interpretation.ShouldSearchNow ||
                 interpretation.ConversationMode == "SearchNow" ||
                 interpretation.ConversationMode == "SearchAndRecommend"))
            {
                action = interpretation.Action?.Trim() ?? "SearchProduct";

                UpdateSearchContext(state.SearchContext, interpretation);

                var pendingContextQuery = BuildSearchQueryFromContext(state.SearchContext);

                queryText = !string.IsNullOrWhiteSpace(interpretation.Query)
                    ? interpretation.Query.Trim()
                    : (!string.IsNullOrWhiteSpace(pendingContextQuery)
                        ? pendingContextQuery
                        : (!string.IsNullOrWhiteSpace(state.LastQuery)
                            ? state.LastQuery
                            : ""));

                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.95;
                needsConfirmation = false;

                state.SkipFinalSummaryOnce = true;
                state.SkipFilterStepOnce = true;
                state.AwaitingFinalSearchConfirmation = false;
                state.AwaitingResultRefinement = false;
                state.AwaitingFilterChoice = false;
                state.PendingIntent = "";
                state.PendingSlot = "";
                state.LastQuery = queryText;
                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }

            action = "Clarify";
            queryText = state.LastQuery;
            confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.70;
            needsConfirmation = false;

            state.LastAssistantReply = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                ? interpretation.ReplyText
                : "Could you clarify what you want to do?";

            SaveState(sessionId, state);

            return (action, queryText, confidence, needsConfirmation);
        }

        if (state.PendingIntent == "AwaitingProductName" &&
            !string.IsNullOrWhiteSpace(text))
        {
            var currentAction = interpretation.Action?.Trim() ?? "Unknown";
            var currentQuery = interpretation.Query?.Trim() ?? "";

            var isNavigationCommand =
                currentAction == "GoBack" ||
                currentAction == "Exit" ||
                currentAction == "Repeat" ||
                currentAction == "ViewCart" ||
                currentAction == "ViewOrders" ||
                currentAction == "TrackLatestOrder" ||
                currentAction == "OpenPayment";

            if (!isNavigationCommand &&
                IsSearchExecutionAction(currentAction))
            {
                state.SearchContext ??= new ProductSearchContext();

                if (!string.IsNullOrWhiteSpace(state.PendingSlot))
                    FillPendingSlot(state.SearchContext, state.PendingSlot, text);

                UpdateSearchContext(state.SearchContext, interpretation);

                action = currentAction;

                queryText = !string.IsNullOrWhiteSpace(currentQuery)
                    ? currentQuery
                    : BuildSearchQueryFromContext(state.SearchContext);

                confidence = interpretation.Confidence > 0 ? interpretation.Confidence : 0.95;
                needsConfirmation = false;

                state.PendingIntent = "";
                state.PendingSlot = "";

                SaveState(sessionId, state);

                return (action, queryText, confidence, needsConfirmation);
            }
        }

        return (action, queryText, confidence, needsConfirmation);
    }
    private VoiceSearchSessionState GetState(string sessionId)
    {
        return _cache.GetOrCreate(sessionId, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return new VoiceSearchSessionState();
        })!;
    }

    private void SaveState(string sessionId, VoiceSearchSessionState state)
    {
        _cache.Set(sessionId, state, TimeSpan.FromMinutes(30));
    }
    public void ResetPendingSearchIfNeeded(string sessionId, string action)
    {
        var state = GetState(sessionId);

        if (action != "SearchProduct" &&
       state.PendingIntent == "AwaitingProductName" &&
       !state.AwaitingFinalSearchConfirmation)
        {
            state.PendingIntent = "";
            state.PendingSlot = "";
            state.SearchContext = null;
            SaveState(sessionId, state);
        }
    }

    private bool MatchesRequestedDetails(ProductUserResponse product, CommandInterpretation interpretation)
    {
        var name = product.Name?.Trim() ?? "";
var description = product.Description?.Trim() ?? "";
var visualSearchText = product.VisualSearchText?.Trim() ?? "";

var searchableText =
    $"{name} {description}";
        var productName = interpretation.ProductName?.Trim() ?? "";
        var model = interpretation.Model?.Trim() ?? "";
        var attributes = interpretation.Attributes?.Trim() ?? "";
        var brand = interpretation.Brand?.Trim() ?? "";
        var matchedCatalogName = interpretation.MatchedCatalogName?.Trim() ?? "";

        var attributeTokens = ExtractImportantAttributeTokens(attributes);

        Console.WriteLine("===== PRICE FILTER CHECK =====");
        Console.WriteLine($"Product: {product.Name}");
        Console.WriteLine($"Product.Price: {product.Price}");
        Console.WriteLine($"MinPrice: {interpretation.MinPrice}");
        Console.WriteLine($"MaxPrice: {interpretation.MaxPrice}");
        Console.WriteLine($"Brand: {brand}");
        Console.WriteLine($"MatchedCatalogName: {matchedCatalogName}");
        Console.WriteLine("===== END PRICE FILTER CHECK =====");

        if (!string.IsNullOrWhiteSpace(matchedCatalogName))
        {
            return name.Equals(matchedCatalogName, StringComparison.OrdinalIgnoreCase);
        }

if (!string.IsNullOrWhiteSpace(model) &&
    !searchableText.Contains(
        model,
        StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(model) &&
            !string.IsNullOrWhiteSpace(productName))
        {
            var query = interpretation.Query?.Trim() ?? "";
            var keywords = interpretation.Keywords?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();


            var safeProductKeywords = keywords
                .Where(k => !attributeTokens.Any(a =>
                    string.Equals(a, k, StringComparison.OrdinalIgnoreCase)))
                .Where(k => !IsGenericSearchWord(k))
                .ToArray();

var productNameMatches =
    searchableText.Contains(
        productName,
        StringComparison.OrdinalIgnoreCase)

    ||

    (!string.IsNullOrWhiteSpace(query) &&
        searchableText.Contains(
            query,
            StringComparison.OrdinalIgnoreCase))

    ||

    safeProductKeywords.Any(k =>
        searchableText.Contains(
            k,
            StringComparison.OrdinalIgnoreCase));

            if (!productNameMatches)
                return false;
        }


        if (attributeTokens.Count > 0 &&
            !interpretation.MinPrice.HasValue &&
            !interpretation.MaxPrice.HasValue)
        {
            var hasAttributeMatch = attributeTokens.Any(token =>
    searchableText.Contains(
        token,
        StringComparison.OrdinalIgnoreCase));

            if (!hasAttributeMatch)
                return false;
        }

        if (interpretation.MinPrice.HasValue &&
            product.Price < interpretation.MinPrice.Value)
        {
            return false;
        }

        if (interpretation.MaxPrice.HasValue &&
            product.Price >= interpretation.MaxPrice.Value)
        {
            return false;
        }

        return true;
    }
public async Task<VoiceResponseDto> HandleRemoveFromCartAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    CommandInterpretation interpretation,
    string userId)
{
    var cart = await _cartService.GetUserCartAsync(userId);

    if (cart == null || cart.Items == null || cart.Items.Count == 0)
    {
        var reply = _interpretationService.ByLanguage(
            language,
            "السلة فارغة، لا يوجد منتجات لحذفها.",
            "Your cart is empty. There are no products to remove."
        );

        return new VoiceResponseDto
        {
            Action = "RemoveFromCart",
            ReplyText = reply,
            ScreenText = reply,
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            Data = cart
        };
    }

    CartResponse? selectedItem = null;

    if (interpretation.SelectedProductId.HasValue)
    {
        selectedItem = cart.Items.FirstOrDefault(x =>
            x.ProductId == interpretation.SelectedProductId.Value);
    }

    if (selectedItem == null && interpretation.SelectedProductIndex.HasValue)
    {
        var index = interpretation.SelectedProductIndex.Value - 1;
        selectedItem = cart.Items.ElementAtOrDefault(index);
    }

    var lower = (correctedText ?? text).Trim().ToLowerInvariant();

    if (selectedItem == null)
    {
        if (lower.Contains("الأول") || lower.Contains("اول") || lower.Contains("first"))
            selectedItem = cart.Items.ElementAtOrDefault(0);
        else if (lower.Contains("الثاني") || lower.Contains("second"))
            selectedItem = cart.Items.ElementAtOrDefault(1);
        else if (lower.Contains("الثالث") || lower.Contains("third"))
            selectedItem = cart.Items.ElementAtOrDefault(2);
    }

    if (selectedItem == null)
    {
        var aiName =
            interpretation.SelectedProductName ??
            interpretation.ProductName ??
            interpretation.MatchedCatalogName ??
            "";

        if (!string.IsNullOrWhiteSpace(aiName))
        {
            selectedItem = cart.Items.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.ProductName) &&
                (
                    x.ProductName.Contains(aiName, StringComparison.OrdinalIgnoreCase) ||
                    aiName.Contains(x.ProductName, StringComparison.OrdinalIgnoreCase)
                ));
        }
    }

    if (selectedItem == null)
    {
        var reply = _interpretationService.ByLanguage(
            language,
            "أي منتج تريدين حذفه من السلة؟ قولي اسم المنتج أو الأول أو الثاني.",
            "Which product do you want to remove from the cart? Say the product name, first, or second."
        );

        return new VoiceResponseDto
        {
            Action = "Clarify",
            ReplyText = reply,
            ScreenText = reply,
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            SuggestedAction = "RemoveFromCart",
            Data = cart
        };
    }

    var result = await _cartService.RemoveFromCartAsync(selectedItem.ProductId, userId);
Console.WriteLine("===== REMOVE CART RESULT =====");
Console.WriteLine($"ProductId: {selectedItem.ProductId}");
Console.WriteLine($"UserId: {userId}");
Console.WriteLine($"Success: {result.IsSuccess}");
Console.WriteLine($"Message: {result.Message}");
Console.WriteLine("===== END REMOVE CART RESULT =====");
    if (!result.IsSuccess)
    {
        var failReply = _interpretationService.ByLanguage(
            language,
            "لم أستطع حذف المنتج من السلة.",
            "I could not remove the product from the cart."
        );

        return new VoiceResponseDto
        {
            Action = "RemoveFromCart",
            ReplyText = failReply,
            ScreenText = failReply,
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            Data = cart
        };
    }

    var updatedCart = await _cartService.GetUserCartAsync(userId);

    var successReply = _interpretationService.ByLanguage(
        language,
        $"تم حذف {selectedItem.ProductName} من السلة.",
        $"{selectedItem.ProductName} was removed from your cart."
    );

    return new VoiceResponseDto
    {
        Action = "RemoveFromCart",
        ReplyText = successReply,
        ScreenText = successReply,
        RecognizedText = text,
        CorrectedText = correctedText ?? text,
        NeedsConfirmation = false,
        Data = updatedCart
    };
}
    public async Task<VoiceResponseDto> HandleSearchProductAsync(
        string sessionId,
        string text,
        string? language,
        string? correctedText,
        string queryText,
        CommandInterpretation interpretation)
    {
        Console.WriteLine("===== HANDLE SEARCH PRODUCT =====");
        Console.WriteLine($"text: {text}");
        Console.WriteLine($"queryText: {queryText}");
        Console.WriteLine($"interpretation.Action: {interpretation.Action}");
        Console.WriteLine($"interpretation.Query: {interpretation.Query}");
        Console.WriteLine($"interpretation.ProductName: {interpretation.ProductName}");
        Console.WriteLine($"interpretation.ProductCategory: {interpretation.ProductCategory}");
        Console.WriteLine($"interpretation.Brand: {interpretation.Brand}");
        Console.WriteLine($"interpretation.Attributes: {interpretation.Attributes}");
        Console.WriteLine("===== END HANDLE SEARCH PRODUCT =====");


        var state = GetState(sessionId);
        var currentProductName = state.SearchContext?.ProductName?.Trim() ?? "";
        var currentCategory = state.SearchContext?.Category?.Trim() ?? "";
        var currentBrand = state.SearchContext?.Brand?.Trim() ?? "";

        var newProductName = interpretation.ProductName?.Trim() ?? "";
        var newCategory = interpretation.ProductCategory?.Trim() ?? "";
        var newBrand = interpretation.Brand?.Trim() ?? "";
        var contextualMeaning = interpretation.ContextualMeaning?.Trim();

        var isProceedFromAi =
            string.Equals(contextualMeaning, "Proceed", StringComparison.OrdinalIgnoreCase);

        var isExplicitNewSearchFromAi =
            string.Equals(contextualMeaning, "NewSearch", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contextualMeaning, "StartNewSearch", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contextualMeaning, "NewProductSearch", StringComparison.OrdinalIgnoreCase);

        var isGenericCategorySearch =
            !string.IsNullOrWhiteSpace(newCategory) &&
            string.IsNullOrWhiteSpace(newBrand) &&
            string.IsNullOrWhiteSpace(newProductName) &&
            string.IsNullOrWhiteSpace(interpretation.Model);
        var isNewSearch =
            !isProceedFromAi &&
            !state.AwaitingFinalSearchConfirmation &&
            !state.AwaitingResultRefinement &&
            (
                isExplicitNewSearchFromAi
                ||
                (
                    !string.IsNullOrWhiteSpace(newProductName) &&
                    !string.Equals(currentProductName, newProductName, StringComparison.OrdinalIgnoreCase)
                )
                ||
                (
                    !string.IsNullOrWhiteSpace(newCategory) &&
                    !string.Equals(currentCategory, newCategory, StringComparison.OrdinalIgnoreCase)
                )
                ||
                (
                    !string.IsNullOrWhiteSpace(newBrand) &&
                    !string.Equals(currentBrand, newBrand, StringComparison.OrdinalIgnoreCase)
                )
                ||
                (
                    isGenericCategorySearch &&
                    (!string.IsNullOrWhiteSpace(currentProductName) ||
                     !string.IsNullOrWhiteSpace(currentBrand))
                )
            );

        if (isNewSearch)
        {
            state.SearchContext = new ProductSearchContext();
            state.LastQuery = "";
            state.PendingIntent = "";
            state.PendingSlot = "";
            state.LastMatchedProducts = new List<ProductUserResponse>();
            state.LastShownProducts = new List<ProductUserResponse>();
            state.LastShownIndex = 0;
        }
        state.SearchContext ??= new ProductSearchContext();

        if (!isProceedFromAi)
        {
            UpdateSearchContext(state.SearchContext, interpretation);
        }

        if (string.IsNullOrWhiteSpace(queryText) &&
            string.IsNullOrWhiteSpace(interpretation.ProductName) &&
            string.IsNullOrWhiteSpace(interpretation.ProductCategory) &&
            string.IsNullOrWhiteSpace(interpretation.Brand))
        {
            state.PendingIntent = "";
            state.PendingSlot = "";

            var smartQuestion = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                ? interpretation.ReplyText
                : _interpretationService.ByLanguage(language,
                    "ممكن توضحي ما المنتج الذي تريدينه؟",
                    "Could you clarify which product you want?");



            state.LastAssistantReply = smartQuestion;
            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = smartQuestion,
                ScreenText = smartQuestion,
                RecognizedText = text,
                CorrectedText = correctedText,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = ""
            };
        }

        var contextQuery = BuildSearchQueryFromContext(state.SearchContext);

        var cleanQuerySource = string.Join(" ", new[]
        {
    contextQuery,
    queryText,
    interpretation.MatchedCatalogName,
    interpretation.Query,
    interpretation.ProductName,
    interpretation.ProductCategory,
    interpretation.Brand,
    interpretation.Model,
    interpretation.Attributes
}.Where(x => !string.IsNullOrWhiteSpace(x)));

        var cleanQuery = _interpretationService.CleanSearchQuery(cleanQuerySource);

        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            var smartQuestion = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                ? interpretation.ReplyText
                : _interpretationService.ByLanguage(language,
                    "ممكن توضحي أكثر ما المنتج الذي تريدينه؟",
                    "Could you clarify which product you want?");

            state.LastAction = "SearchProduct";
            state.PendingIntent = "";
            state.PendingSlot = "";
            state.LastAssistantReply = smartQuestion;

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = smartQuestion,
                ScreenText = smartQuestion,
                RecognizedText = text,
                CorrectedText = correctedText,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = ""
            };
        }

      List<ProductUserResponse> matchedProducts = new();
        var searchCandidates = new List<string>();

        void AddSearchCandidate(string? value)
        {
            var cleaned = _interpretationService.CleanSearchQuery(value ?? "");

            if (!string.IsNullOrWhiteSpace(cleaned) &&
                !searchCandidates.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
            {
                searchCandidates.Add(cleaned);
            }
        }

        AddSearchCandidate(interpretation.MatchedCatalogName);

        if (!IsProceedOnlyText(correctedText) &&
            !string.Equals(interpretation.ContextualMeaning, "Proceed", StringComparison.OrdinalIgnoreCase))
        {
            AddSearchCandidate(correctedText);
        }

        AddSearchCandidate(interpretation.Query);

        AddSearchCandidate(queryText);

        AddSearchCandidate($"{interpretation.Brand} {interpretation.ProductName}");
        AddSearchCandidate($"{interpretation.ProductName} {interpretation.Model}");
        AddSearchCandidate(interpretation.ProductName);
        AddSearchCandidate(contextQuery);
        AddSearchCandidate(interpretation.Brand);
        AddSearchCandidate(interpretation.ProductCategory);

        var attributeTokensForSearch =
            ExtractImportantAttributeTokens(interpretation.Attributes ?? "");
        if (!string.IsNullOrWhiteSpace(interpretation.Keywords))
        {
            var keywordParts = interpretation.Keywords
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var keyword in keywordParts)
            {

                if (attributeTokensForSearch.Any(a =>
                        string.Equals(a, keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                AddSearchCandidate(keyword);
            }
        }
        Console.WriteLine($"DEBUG => candidates: {string.Join(" | ", searchCandidates)}");
       var foundProductsById =
    new Dictionary<int, ProductUserResponse>();

foreach (var candidate in searchCandidates)
{
    var result = await _productService.GetAllProductsForUser(
        language ?? "en",
        page: 1,
        limit: 100,
        search: candidate,
        minPrice: interpretation.MinPrice,
        maxPrice: interpretation.MaxPrice,
        searchMode: ProductSearchMode.Hybrid
    );

    Console.WriteLine(
        $"DEBUG => trying: {candidate} => " +
        $"results: {result.Data.Count}"
    );

    foreach (var product in result.Data)
    {
        foundProductsById[product.Id] = product;
    }
}

var coreSearchTerms = new[]
{
    interpretation.ProductName,
    interpretation.ProductCategory,
    interpretation.Query
}
.Where(value => !string.IsNullOrWhiteSpace(value))
.SelectMany(value =>
    NormalizeSearchText(value!)
        .Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries))
.Where(term => term.Length > 1)
.Where(term => !IsGenericSearchWord(term))
.Distinct(StringComparer.OrdinalIgnoreCase)
.ToList();

var eligibleProducts = foundProductsById.Values
    .Where(product =>
        MatchesHardSearchConstraints(
            product,
            interpretation))
    .Where(product =>
    {
        
        var searchableText = NormalizeSearchText(
            $"{product.VisualSearchText ?? ""} " +
            $"{product.Name ?? ""} " +
            $"{product.Description ?? ""}"
        );

        return coreSearchTerms.Count == 0 ||
               coreSearchTerms.Any(term =>
                   searchableText.Contains(
                       term,
                       StringComparison.OrdinalIgnoreCase));
    })
    .ToList();

matchedProducts = RankMatchedProducts(
    eligibleProducts,
    interpretation
);

matchedProducts = RankMatchedProducts(
    eligibleProducts,
    interpretation
);

Console.WriteLine(
    $"FINAL matchedProducts.Count = {matchedProducts.Count}"
);

state.AwaitingFilterChoice = false;
state.SkipFilterStepOnce = false;
        var enoughInfo = interpretation.EnoughInfoToSearch || HasEnoughProductInfo(state.SearchContext, interpretation);
        if (state.AwaitingFinalSearchConfirmation &&
            interpretation.Action == "Clarify" &&
            interpretation.ConversationMode == "AskFinalDetails" &&
            !interpretation.ShouldSearchNow)
        {
            state.LastAssistantReply = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                ? interpretation.ReplyText
                : _interpretationService.ByLanguage(language,
                    "ما التفاصيل التي تريد إضافتها؟ مثل الموديل أو اللون أو السعة أو السعر.",
                    "What detail would you like to add? For example model, color, storage, or price range.");

            SaveState(sessionId, state);

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = state.LastAssistantReply,
                ScreenText = state.LastAssistantReply,
                RecognizedText = text,
                CorrectedText = correctedText,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = state.LastQuery
            };
        }
        if (!enoughInfo)
        {
            state.LastAction = "SearchProduct";
            state.LastQuery = cleanQuery;
            state.PendingIntent = "";

            if (interpretation.MissingInfo?.Contains("brand", StringComparison.OrdinalIgnoreCase) == true)
            {
                state.PendingSlot = "brand";
            }
            else if (interpretation.MissingInfo?.Contains("category", StringComparison.OrdinalIgnoreCase) == true)
            {
                state.PendingSlot = "category";
            }
            else
            {
                state.PendingSlot = "";
            }

            var followUpQuestion = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
                ? interpretation.ReplyText
                : GetNextSmartQuestion(state.SearchContext ?? new ProductSearchContext(), interpretation, language);
            state.LastAssistantReply = followUpQuestion;
            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = followUpQuestion,
                ScreenText = followUpQuestion,
                RecognizedText = text,
                CorrectedText = correctedText,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = cleanQuery
            };
        }

        if (enoughInfo &&
            !state.AwaitingFinalSearchConfirmation &&
            !state.SkipFinalSummaryOnce &&
            (interpretation.Action == "Clarify" || interpretation.Action == "SearchProduct"))
        {
            if (state.SearchContext == null || isNewSearch)
            {
                state.SearchContext = new ProductSearchContext();
            }

            UpdateSearchContext(state.SearchContext, interpretation);
            var finalContextQuery = BuildSearchQueryFromContext(state.SearchContext);
            if (!string.IsNullOrWhiteSpace(finalContextQuery))
                state.LastQuery = finalContextQuery;
            else
                state.LastQuery = cleanQuery;

            state.LastAction = "SearchProduct";
            state.PendingIntent = "";
            state.PendingSlot = "";
            state.AwaitingFinalSearchConfirmation = true;

            var askForMoreDetails = _interpretationService.ByLanguage(
      language,
      "هل تريد إضافة تفاصيل أخرى قبل أن أعرض النتائج؟",
      "Do you want to add any other details before I show the results?"
  );

            state.LastAssistantReply = askForMoreDetails;
            Console.WriteLine("===== SET FINAL CONFIRMATION STATE =====");
            Console.WriteLine($"state.LastQuery: {state.LastQuery}");
            Console.WriteLine($"state.PendingIntent: {state.PendingIntent}");
            Console.WriteLine($"state.PendingSlot: {state.PendingSlot}");
            Console.WriteLine($"state.AwaitingFinalSearchConfirmation: {state.AwaitingFinalSearchConfirmation}");
            Console.WriteLine($"askForMoreDetails: {askForMoreDetails}");
            Console.WriteLine("===== END SET FINAL CONFIRMATION STATE =====");
            SaveState(sessionId, state);

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = askForMoreDetails,
                ScreenText = askForMoreDetails,
                RecognizedText = text,
                CorrectedText = correctedText,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = state.LastQuery
            };
        }
     
        Console.WriteLine($"FINAL matchedProducts.Count = {matchedProducts.Count}");


state.AwaitingFilterChoice = false;
state.SkipFilterStepOnce = false;
if (matchedProducts.Count == 0)
{
    state.LastMatchedProducts =
        new List<ProductUserResponse>();

    state.LastShownProducts =
        new List<ProductUserResponse>();

    state.LastShownIndex = 0;
    state.AwaitingFinalSearchConfirmation = false;
    state.AwaitingResultRefinement = false;

    var notFoundReply =
        _interpretationService.ByLanguage(
            language,
            $"لم أجد منتجات مطابقة لـ " +
            $"{(correctedText ?? queryText)}.",
            $"I couldn't find matching products for " +
            $"{(correctedText ?? queryText)}."
        );

    state.LastAssistantReply = notFoundReply;
    SaveState(sessionId, state);

    return new VoiceResponseDto
    {
        Action = "SearchProduct",
        ReplyText = notFoundReply,
        ScreenText = notFoundReply,
        RecognizedText = text,
        CorrectedText = correctedText ?? text,
        NeedsConfirmation = false,
        SuggestedAction = "",
        SuggestedQuery = "",
        Data = new List<ProductUserResponse>()
    };
}
        state.LastMatchedProducts = matchedProducts;
        state.LastShownIndex = 0;

        var topProducts = state.LastMatchedProducts
            .Skip(state.LastShownIndex)
            .Take(3)
            .ToList();

        Console.WriteLine("===== RETURNING SEARCH RESPONSE =====");
        Console.WriteLine($"matchedProducts.Count: {matchedProducts.Count}");
        Console.WriteLine($"LastMatchedProducts.Count: {state.LastMatchedProducts?.Count ?? 0}");
        Console.WriteLine($"topProducts.Count: {topProducts.Count}");
        Console.WriteLine($"Data products: {string.Join(" | ", topProducts.Select(p => $"{p.Id}:{p.Name}:{p.Price}"))}");
        Console.WriteLine("===== END RETURNING SEARCH RESPONSE =====");

        state.LastShownIndex += topProducts.Count;
        SaveState(sessionId, state);

        var spokenResults = string.Join(" . ", topProducts.Select(p =>
        {
            var name = p.Name;
            return _interpretationService.ByLanguage(language,
                $"{name}، السعر {p.Price} شيكل",
                $"{name}, price {p.Price} shekels");
        }));

        var screenResults = string.Join(" | ", topProducts.Select(p =>
        {
            var name = p.Name;
            return _interpretationService.ByLanguage(language,
                $"{name} - {p.Price} شيكل",
                $"{name} - {p.Price} NIS");
        }));


var totalProducts = state.LastMatchedProducts.Count;

var remainingProducts = Math.Max(
    0,
    totalProducts - state.LastShownIndex
);

var hasMore = remainingProducts > 0;

var resultBody = hasMore
    ? _interpretationService.ByLanguage(
        language,
        $"وَجَدْتُ {totalProducts} مُنْتَجًا مُطَابِقًا لِطَلَبِكِ. " +
        $"سَأَعْرِضُ لَكِ الْآنَ أَوَّلَ ثَلَاثَةِ مُنْتَجَاتٍ. " +
        $"{spokenResults}. " +
        $"يَتَبَقَّى {remainingProducts} مُنْتَجًا. " +
        $"قُولِي التَّالِي لِعَرْضِ الدُّفْعَةِ التَّالِيَةِ.",

        $"I found {totalProducts} products matching your request. " +
        $"I will now show you the first three products. " +
        $"{spokenResults}. " +
        $"{remainingProducts} product(s) remain. " +
        $"Say next to show the next group."
    )
    : _interpretationService.ByLanguage(
        language,
        $"وَجَدْتُ {totalProducts} مُنْتَجًا مُطَابِقًا لِطَلَبِكِ، " +
        $"وَهَذِهِ جَمِيعُ النَّتَائِجِ. {spokenResults}.",

        $"I found {totalProducts} product(s) matching your request, " +
        $"and these are all the results. {spokenResults}."
    );

var replyText = resultBody;

        state.AwaitingFinalSearchConfirmation = false;
        state.SkipFinalSummaryOnce = false;
        state.AwaitingFilterChoice = false;
        state.AwaitingResultRefinement = false;
        state.LastAssistantReply = replyText;

        state.LastShownProducts = topProducts;

        if (!hasMore)
        {
            state.LastShownIndex = state.LastMatchedProducts?.Count ?? 0;
        }

        SaveState(sessionId, state);

        return new VoiceResponseDto
        {
            Action = "SearchProduct",
            ReplyText = replyText,
            ScreenText = screenResults,
            RecognizedText = text,
            CorrectedText = correctedText,
            NeedsConfirmation = false,
            SuggestedAction = hasMore ? "ShowMoreProducts" : "",
            SuggestedQuery = "",
            Data = topProducts
        };
    }
    public void SetAwaitingPaymentConfirmation(string sessionId, bool value)
    {
        var state = GetState(sessionId);
        state.AwaitingPaymentConfirmation = value;
        SaveState(sessionId, state);
    }

    public bool GetAwaitingPaymentConfirmation(string sessionId)
    {
        return GetState(sessionId).AwaitingPaymentConfirmation;
    }
    private List<ProductUserResponse> FilterMatchedProducts(
        List<ProductUserResponse> products,
        CommandInterpretation interpretation)
    {
        if (products == null || products.Count == 0)
            return new List<ProductUserResponse>();

        IEnumerable<ProductUserResponse> filtered = products;

        string productName = interpretation.ProductName?.Trim() ?? "";
        string brand = interpretation.Brand?.Trim() ?? "";
        string model = interpretation.Model?.Trim() ?? "";
        string category = interpretation.ProductCategory?.Trim() ?? "";
        string attributes = interpretation.Attributes?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(productName))
        {
            filtered = filtered.Where(p =>
                (p.Name?.Contains(productName, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            filtered = filtered.Where(p =>
                (p.Name?.Contains(model, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(attributes))
        {
            filtered = filtered.Where(p =>
                (p.Name?.Contains(attributes, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return filtered.ToList();
    }
    private string BuildSearchSummary(ProductSearchContext? ctx, string? language)
    {
        if (ctx == null)
        {
            return _interpretationService.ByLanguage(language,
                "ممتاز، فهمت طلبك.",
                "Great, I understood your request.");
        }

        var partsAr = new List<string>();
        var partsEn = new List<string>();

        if (!string.IsNullOrWhiteSpace(ctx.Category))
        {
            partsAr.Add($"المنتج {ctx.Category}");
            partsEn.Add($"the product is {ctx.Category}");
        }

        if (!string.IsNullOrWhiteSpace(ctx.ProductName))
        {
            partsAr.Add($"نوعه {ctx.ProductName}");
            partsEn.Add($"its type is {ctx.ProductName}");
        }

        if (!string.IsNullOrWhiteSpace(ctx.Brand))
        {
            partsAr.Add($"الماركة {ctx.Brand}");
            partsEn.Add($"the brand is {ctx.Brand}");
        }

        if (!string.IsNullOrWhiteSpace(ctx.Model))
        {
            partsAr.Add($"الموديل {ctx.Model}");
            partsEn.Add($"the model is {ctx.Model}");
        }

        if (!string.IsNullOrWhiteSpace(ctx.Attributes))
        {
            partsAr.Add($"ومواصفاته {ctx.Attributes}");
            partsEn.Add($"with attributes {ctx.Attributes}");
        }

        if (partsAr.Count == 0)
        {
            return _interpretationService.ByLanguage(language,
                "ممتاز، فهمت طلبك.",
                "Great, I understood your request.");
        }

        return _interpretationService.ByLanguage(language,
            $"ممتاز، إذا أنت تريد {string.Join("، ", partsAr)}.",
            $"Great, so you want {string.Join(", ", partsEn)}.");
    }

    private void ResetAllSearchState(string sessionId)
    {
        var state = new VoiceSearchSessionState();
        SaveState(sessionId, state);
    }
    private string GetNextSmartQuestion(ProductSearchContext ctx, CommandInterpretation interpretation, string? language)
    {
        if (!string.IsNullOrWhiteSpace(interpretation.ReplyText))
            return interpretation.ReplyText;

        return _interpretationService.ByLanguage(language,
            "ممكن توضحي أكثر ما المنتج الذي تريدينه؟",
            "Could you clarify which product you want?");
    }
    public async Task<VoiceResponseDto> HandleShowMoreProductsAsync(
        string sessionId,
        string text,
        string? language,
        string? correctedText)
    {

        var state = GetState(sessionId);


        if (state.LastMatchedProducts == null || state.LastMatchedProducts.Count == 0 || state.LastShownIndex >= state.LastMatchedProducts.Count)
        {
            state.LastAssistantReply = _interpretationService.ByLanguage(language,
    "لا توجد منتجات إضافية لعرضها.",
    "There are no more products to show.");
            SaveState(sessionId, state);
            return new VoiceResponseDto
            {

                Action = "ShowMoreProducts",
                ReplyText = _interpretationService.ByLanguage(language,
                    "لا توجد منتجات إضافية لعرضها.",
                    "There are no more products to show."),
                ScreenText = _interpretationService.ByLanguage(language,
                    "لا توجد نتائج إضافية.",
                    "No more results."),
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

var startNumber = state.LastShownIndex + 1;

var nextProducts = state.LastMatchedProducts
    .Skip(state.LastShownIndex)
    .Take(3)
    .ToList();

state.LastShownProducts = nextProducts;
state.LastShownIndex += nextProducts.Count;

var endNumber = state.LastShownIndex;
        var spokenResults = string.Join(" . ", nextProducts.Select(p =>
        {
            var name = p.Name;
            return _interpretationService.ByLanguage(language,
                $"{name}، السعر {p.Price} شيكل",
                $"{name}, price {p.Price} shekels");
        }));

        var screenResults = string.Join(" | ", nextProducts.Select(p =>
        {
            var name = p.Name;
            return _interpretationService.ByLanguage(language,
                $"{name} - {p.Price} شيكل",
                $"{name} - {p.Price} NIS");
        }));
var totalProducts = state.LastMatchedProducts.Count;

var remainingProducts = Math.Max(
    0,
    totalProducts - state.LastShownIndex
);

var hasMore = remainingProducts > 0;

if (!hasMore)
{
    state.LastShownIndex = totalProducts;
}

var replyText = hasMore
    ? _interpretationService.ByLanguage(
        language,
        $"هَذِهِ النَّتَائِجُ مِنْ رَقْمِ {startNumber} إِلَى رَقْمِ {endNumber}. " +
        $"{spokenResults}. " +
        $"يَتَبَقَّى {remainingProducts} مُنْتَجًا. " +
        $"قُولِي التَّالِي لِعَرْضِ الدُّفْعَةِ التَّالِيَةِ.",

        $"These are results {startNumber} through {endNumber}. " +
        $"{spokenResults}. " +
        $"{remainingProducts} product(s) remain. " +
        $"Say next to show the next group."
    )
    : _interpretationService.ByLanguage(
        language,
        $"هَذِهِ آخِرُ النَّتَائِجِ، مِنْ رَقْمِ {startNumber} " +
        $"إِلَى رَقْمِ {endNumber}. {spokenResults}.",

        $"These are the final results, from number {startNumber} " +
        $"through number {endNumber}. {spokenResults}."
    );

        state.LastAssistantReply = replyText;
        SaveState(sessionId, state);

        return new VoiceResponseDto
        {
            Action = "ShowMoreProducts",
            ReplyText = replyText,
            ScreenText = screenResults,
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            SuggestedAction = hasMore ? "ShowMoreProducts" : "",
            SuggestedQuery = "",
            Data = nextProducts
        };
    }

private List<ProductUserResponse> RankMatchedProducts(
    List<ProductUserResponse> products,
    CommandInterpretation interpretation)
{
    var searchText = BuildRankingSearchText(interpretation);

    if (string.IsNullOrWhiteSpace(searchText))
        return products.OrderBy(product => product.Name).ToList();

    var normalizedQuery = NormalizeSearchText(searchText);

    var queryTokens = normalizedQuery
        .Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries)
        .Where(token => token.Length > 1)
        .Where(token => !IsGenericSearchWord(token))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    double Score(ProductUserResponse product)
    {
        var name = NormalizeSearchText(product.Name ?? "");
        var description =
            NormalizeSearchText(product.Description ?? "");

        var visualText =
            NormalizeSearchText(product.VisualSearchText ?? "");

        var score = 0.0;
var matchedCatalogName = NormalizeSearchText(
    interpretation.MatchedCatalogName ?? "");

if (!string.IsNullOrWhiteSpace(matchedCatalogName))
{
    if (name.Equals(
            matchedCatalogName,
            StringComparison.OrdinalIgnoreCase))
    {
       
        score += 25000;
    }
    else if (name.Contains(
                 matchedCatalogName,
                 StringComparison.OrdinalIgnoreCase))
    {
        score += 10000;
    }
}
        var visualContainsFullQuery =
            !string.IsNullOrWhiteSpace(visualText) &&
            visualText.Contains(
                normalizedQuery,
                StringComparison.OrdinalIgnoreCase);

        var nameContainsFullQuery =
            !string.IsNullOrWhiteSpace(name) &&
            name.Contains(
                normalizedQuery,
                StringComparison.OrdinalIgnoreCase);

        var descriptionContainsFullQuery =
            !string.IsNullOrWhiteSpace(description) &&
            description.Contains(
                normalizedQuery,
                StringComparison.OrdinalIgnoreCase);

        
        if (visualContainsFullQuery)
            score += 10000;

       
        if (nameContainsFullQuery)
            score += 8000;

      
        if (visualContainsFullQuery && nameContainsFullQuery)
            score += 5000;

        var visualMatchedTokens =
            CountMatchedTokens(queryTokens, visualText);

        var nameMatchedTokens =
            CountMatchedTokens(queryTokens, name);

        var descriptionMatchedTokens =
            CountMatchedTokens(queryTokens, description);

        var tokenCount = Math.Max(queryTokens.Count, 1);

        var visualCoverage =
            (double)visualMatchedTokens / tokenCount;

        var nameCoverage =
            (double)nameMatchedTokens / tokenCount;

        var descriptionCoverage =
            (double)descriptionMatchedTokens / tokenCount;

        
        score += visualCoverage * 5000;

        
        score += nameCoverage * 4000;

       
        if (visualMatchedTokens > 0 &&
            nameMatchedTokens > 0)
        {
            var combinedCoverage =
                (visualCoverage + nameCoverage) / 2.0;

            score += combinedCoverage * 3000;
        }

        
        var visualSimilarity =
            CalculateTextSimilarity(
                normalizedQuery,
                visualText);

        var nameSimilarity =
            CalculateTextSimilarity(
                normalizedQuery,
                name);

        var descriptionSimilarity =
            CalculateTextSimilarity(
                normalizedQuery,
                description);

        score += visualSimilarity * 2500;
        score += nameSimilarity * 2000;

       
        if (descriptionContainsFullQuery)
            score += 1000;

        score += descriptionCoverage * 800;
        score += descriptionSimilarity * 500;

        
        var requestedProductName =
            NormalizeSearchText(
                interpretation.ProductName ?? "");

        if (!string.IsNullOrWhiteSpace(requestedProductName))
        {
            if (visualText.Contains(
                    requestedProductName,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 2500;
            }

            if (name.Contains(
                    requestedProductName,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 2200;
            }

            if (description.Contains(
                    requestedProductName,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 400;
            }
        }

        return score;
    }

    return products
        .Select(product => new
        {
            Product = product,
            Score = Score(product)
        })
        .Where(result => result.Score > 0)
        .OrderByDescending(result => result.Score)
        .ThenBy(result => result.Product.Name)
        .Select(result => result.Product)
        .ToList();
}
private string BuildRankingSearchText(
    CommandInterpretation interpretation)
{
    var values = new[]
    {
        interpretation.Query,
        interpretation.ProductName,
        interpretation.ProductCategory,
        interpretation.Brand,
        interpretation.Model,
        interpretation.Attributes,
        interpretation.Keywords,
        interpretation.MatchedCatalogName
    };

    return string.Join(
        " ",
        values.Where(value =>
            !string.IsNullOrWhiteSpace(value))
    );
}
private static int CountMatchedTokens(
    List<string> queryTokens,
    string targetText)
{
    if (queryTokens.Count == 0 ||
        string.IsNullOrWhiteSpace(targetText))
    {
        return 0;
    }

    return queryTokens.Count(token =>
        targetText.Contains(
            token,
            StringComparison.OrdinalIgnoreCase));
}
private static string NormalizeSearchText(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return "";

    var normalized = text
        .Trim()
        .ToLowerInvariant()
        .Replace("أ", "ا")
        .Replace("إ", "ا")
        .Replace("آ", "ا")
        .Replace("ى", "ي")
        .Replace("ؤ", "و")
        .Replace("ئ", "ي")
        .Replace("ة", "ه")
        .Replace("،", " ")
        .Replace(",", " ")
        .Replace(".", " ")
        .Replace("?", " ")
        .Replace("؟", " ")
        .Replace("!", " ")
        .Replace("-", " ");

    return string.Join(
        " ",
        normalized.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries)
    );
}
private static double CalculateTextSimilarity(
    string first,
    string second)
{
    if (string.IsNullOrWhiteSpace(first) ||
        string.IsNullOrWhiteSpace(second))
    {
        return 0;
    }

    if (first.Equals(
            second,
            StringComparison.OrdinalIgnoreCase))
    {
        return 1;
    }

    if (second.Contains(
            first,
            StringComparison.OrdinalIgnoreCase))
    {
        return 1;
    }

    var distance = LevenshteinDistance(first, second);
    var maxLength = Math.Max(first.Length, second.Length);

    if (maxLength == 0)
        return 1;

    var similarity =
        1.0 - ((double)distance / maxLength);

    return Math.Max(0, similarity);
}
private static int LevenshteinDistance(
    string first,
    string second)
{
    var matrix =
        new int[first.Length + 1, second.Length + 1];

    for (var i = 0; i <= first.Length; i++)
        matrix[i, 0] = i;

    for (var j = 0; j <= second.Length; j++)
        matrix[0, j] = j;

    for (var i = 1; i <= first.Length; i++)
    {
        for (var j = 1; j <= second.Length; j++)
        {
            var cost =
                first[i - 1] == second[j - 1]
                    ? 0
                    : 1;

            matrix[i, j] = Math.Min(
                Math.Min(
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1
                ),
                matrix[i - 1, j - 1] + cost
            );
        }
    }

    return matrix[first.Length, second.Length];
}
    private static void UpdateSearchContext(ProductSearchContext ctx, CommandInterpretation interpretation)
    {
        if (!string.IsNullOrWhiteSpace(interpretation.ProductCategory))
            ctx.Category = interpretation.ProductCategory;

        if (!string.IsNullOrWhiteSpace(interpretation.Brand))
            ctx.Brand = interpretation.Brand;

        if (!string.IsNullOrWhiteSpace(interpretation.ProductName))
            ctx.ProductName = interpretation.ProductName;

        if (!string.IsNullOrWhiteSpace(interpretation.Model))
            ctx.Model = interpretation.Model;

        if (!string.IsNullOrWhiteSpace(interpretation.Attributes))
            ctx.Attributes = interpretation.Attributes;
    }
    private static void FillPendingSlot(ProductSearchContext ctx, string pendingSlot, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var value = text.Trim();

        switch (pendingSlot)
        {
            case "category":
                ctx.Category = value;
                break;

            case "brand":
                if (string.IsNullOrWhiteSpace(ctx.ProductName))
                    ctx.ProductName = value;

                if (string.IsNullOrWhiteSpace(ctx.Brand))
                    ctx.Brand = value;
                break;

            case "model":
                ctx.Model = value;
                break;

            case "attributes":
                ctx.Attributes = string.IsNullOrWhiteSpace(ctx.Attributes)
                    ? value
                    : $"{ctx.Attributes} {value}";
                break;
        }
    }

    private static string BuildSearchQueryFromContext(ProductSearchContext? ctx)
    {
        if (ctx == null)
            return string.Empty;

        var parts = new List<string>();

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add(value.Trim());
        }

        Add(ctx.Category);
        Add(ctx.Brand);
        Add(ctx.ProductName);
        Add(ctx.Model);
        Add(ctx.Attributes);

        return string.Join(" ", parts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasEnoughProductInfo(ProductSearchContext? ctx, CommandInterpretation interpretation)
    {
        var category = ctx?.Category?.Trim() ?? interpretation.ProductCategory?.Trim() ?? "";
        var brand = ctx?.Brand?.Trim() ?? interpretation.Brand?.Trim() ?? "";
        var productName = ctx?.ProductName?.Trim() ?? interpretation.ProductName?.Trim() ?? "";
        var model = ctx?.Model?.Trim() ?? "";
        var attributes = ctx?.Attributes?.Trim() ?? interpretation.Attributes?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(productName))
            return true;

        if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(brand))
            return true;

        if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(attributes))
            return true;

        if (!string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(model))
            return true;

        return false;
    }
    public async Task<VoiceResponseDto> HandleAddToCartAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    CommandInterpretation interpretation,
    string? userId)
    {
        var state = GetState(sessionId);

        var selectableProducts =
            state.LastShownProducts != null && state.LastShownProducts.Count > 0
                ? state.LastShownProducts
                : state.LastMatchedProducts ?? new List<ProductUserResponse>();
if (selectableProducts.Count == 0)
        {
            var reply = _interpretationService.ByLanguage(
                language,
                "لا يوجد منتج واضح لإضافته إلى السلة. اعرضي المنتجات أولًا ثم قولي مثلًا أضيفي الأول للسلة.",
                "There is no clear product to add to the cart. Show products first, then say something like add the first one to the cart."
            );

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = reply,
                ScreenText = reply,
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "SearchProduct",
                SuggestedQuery = state.LastQuery ?? ""
            };
        }
var selectedProduct = ResolveProductFromAiSelection(interpretation, selectableProducts)
                      ?? ResolveProductFromUserText(text, selectableProducts);
        
if (selectedProduct == null &&
    selectableProducts.Count == 1 &&
    string.Equals(
        interpretation.Action,
        "AddToCart",
        StringComparison.OrdinalIgnoreCase))
{
    selectedProduct = selectableProducts[0];
}
        if (selectedProduct == null)
        {
            var reply = _interpretationService.ByLanguage(
                language,
                "أي منتج تقصدين؟ قولي الأول أو الثاني أو اذكري اسم المنتج.",
                "Which product do you mean? Say first, second, or mention the product name."
            );

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = reply,
                ScreenText = reply,
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "AddToCart",
                SuggestedQuery = ""
            };
        }

        var qty = interpretation.Quantity.HasValue &&
                  interpretation.Quantity.Value > 0
            ? interpretation.Quantity.Value
            : 1;

        await _cartService.AddToCartAsync(
            new AddToCartRequest
            {
                ProductId = selectedProduct.Id,
                Count = qty
            },
            userId ?? ""
        );

        var successReply = _interpretationService.ByLanguage(
            language,
            $"تمت إضافة {qty} من {selectedProduct.Name} إلى السلة.",
            $"Added {qty} item(s) of {selectedProduct.Name} to your cart."
        );

        return new VoiceResponseDto
        {
            Action = "AddToCart",
            ReplyText = successReply,
            ScreenText = successReply,
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            SuggestedAction = "",
            SuggestedQuery = "",
            Data = selectedProduct
        };
    }
    private ProductUserResponse? ResolveProductFromAiSelection(
    CommandInterpretation interpretation,
    List<ProductUserResponse> products)
{
    if (products == null || products.Count == 0)
        return null;

    if (interpretation.SelectedProductId.HasValue)
    {
        var byId = products.FirstOrDefault(p => p.Id == interpretation.SelectedProductId.Value);
        if (byId != null)
            return byId;
    }

    if (interpretation.SelectedProductIndex.HasValue)
    {
        var index = interpretation.SelectedProductIndex.Value - 1;
        if (index >= 0 && index < products.Count)
            return products[index];
    }

    if (!string.IsNullOrWhiteSpace(interpretation.SelectedProductName))
    {
        var byName = products.FirstOrDefault(p =>
            string.Equals(p.Name?.Trim(), interpretation.SelectedProductName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (byName != null)
            return byName;
    }

    if (!string.IsNullOrWhiteSpace(interpretation.ProductName))
    {
        var byProductName = products.FirstOrDefault(p =>
            p.Name != null &&
            p.Name.Contains(interpretation.ProductName, StringComparison.OrdinalIgnoreCase));

        if (byProductName != null)
            return byProductName;
    }

    return null;
}
    private ProductUserResponse? ResolveProductFromUserText(string text, List<ProductUserResponse> products)
    {
        var lower = text.Trim().ToLowerInvariant();

        if (products == null || products.Count == 0)
            return null;

        if (products.Count == 1)
        {
            if (lower.Contains("it") ||
                lower.Contains("this") ||
                lower.Contains("this one") ||
                lower.Contains("that") ||
                lower.Contains("that one") ||
                lower.Contains("أضفه") ||
                lower.Contains("ضيفه") ||
                lower.Contains("هذا") ||
                lower.Contains("هاد") ||
                lower.Contains("هاذ") ||
                lower.Contains("هاي") ||
                lower.Contains("هي") ||
                lower.Contains("هذي") ||
                lower.Contains("هذه") ||
                lower.Contains("إياه"))
            {
                return products[0];
            }
        }

        if (lower.Contains("الأول") || lower.Contains("first"))
            return products.ElementAtOrDefault(0);

        if (lower.Contains("الثاني") || lower.Contains("second"))
            return products.ElementAtOrDefault(1);

        if (lower.Contains("الثالث") || lower.Contains("third"))
            return products.ElementAtOrDefault(2);

        foreach (var product in products)
        {
            if (!string.IsNullOrWhiteSpace(product.Name) &&
                lower.Contains(product.Name.ToLowerInvariant()))
            {
                return product;
            }
        }

        return null;
    }
    public ProductUserResponse? ResolveProductFromAiOrLastShown(
    CommandInterpretation interpretation,
    string text,
    string sessionId)
{
    var state = GetState(sessionId);

    var selectableProducts =
        state.LastShownProducts != null && state.LastShownProducts.Count > 0
            ? state.LastShownProducts
            : state.LastMatchedProducts ?? new List<ProductUserResponse>();

    return ResolveProductFromAiSelection(interpretation, selectableProducts)
           ?? ResolveProductFromUserText(text, selectableProducts);
}
    public List<ProductUserResponse> GetLastShownProducts(string sessionId)
{
    var state = GetState(sessionId);
    return state.LastShownProducts ?? new List<ProductUserResponse>();
}
    public ProductUserResponse? ResolveProductFromLastShown(string text, string sessionId)
    {
        var state = GetState(sessionId);

        var selectableProducts =
            state.LastShownProducts != null && state.LastShownProducts.Count > 0
                ? state.LastShownProducts
                : state.LastMatchedProducts ?? new List<ProductUserResponse>();

        return ResolveProductFromUserText(text, selectableProducts);
    }
    public async Task<VoiceResponseDto> HandleSearchAndRecommendAsync(
    string sessionId,
    string text,
    string? language,
    string? correctedText,
    string queryText,
    CommandInterpretation interpretation)
    {
        var searchResult = await HandleSearchProductAsync(
            sessionId,
            text,
            language,
            correctedText,
            queryText,
            interpretation);

        var state = GetState(sessionId);

        var candidates =
            state.LastMatchedProducts != null && state.LastMatchedProducts.Count > 0
                ? state.LastMatchedProducts
                : state.LastShownProducts ?? new List<ProductUserResponse>();

        if (candidates.Count == 0)
            return searchResult;

        return await HandleRecommendProductAsync(
            sessionId,
            text,
            language,
            correctedText,
            interpretation);
    }
public void SetCurrentProductDetails(
    string sessionId,
    ProductUserResponse product,
    string assistantReply)
{
    var state = GetState(sessionId);

    state.LastAction = "OpenProductDetails";

    state.LastShownProducts = new List<ProductUserResponse>
    {
        product
    };

    state.LastAssistantReply = assistantReply;

    SaveState(sessionId, state);
}
    public async Task<VoiceResponseDto> HandleRecommendProductAsync(
        string sessionId,
        string text,
        string? language,
        string? correctedText,
        CommandInterpretation interpretation)
    {
        var state = GetState(sessionId);

        var candidates =
            state.LastShownProducts != null && state.LastShownProducts.Count > 0
                ? state.LastShownProducts
                : state.LastMatchedProducts ?? new List<ProductUserResponse>();

        if (candidates.Count == 0)
        {
            var reply = _interpretationService.ByLanguage(
                language,
                "عن أي نوع منتج بتحب أنصحك؟",
                "What type of product would you like me to recommend?"
            );

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = reply,
                ScreenText = reply,
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "SearchAndRecommend",
                SuggestedQuery = ""
            };
        }

        var aiRecommendation = await _openAiService.RecommendBestProductAsync(
            candidates,
            interpretation,
            language);

        if (aiRecommendation == null || aiRecommendation.ProductId == null)
        {
            var reply = _interpretationService.ByLanguage(
                language,
                "لم أستطع تحديد أفضل منتج من النتائج.",
                "I could not determine the best product from the results."
            );

            return new VoiceResponseDto
            {
                Action = "RecommendProduct",
                ReplyText = reply,
                ScreenText = reply,
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        var selectedProduct = candidates
            .FirstOrDefault(p => p.Id == aiRecommendation.ProductId.Value);

        if (selectedProduct == null)
        {
            var reply = _interpretationService.ByLanguage(
                language,
                "المنتج الذي تم اختياره غير موجود ضمن النتائج الحالية.",
                "The selected product was not found in the current results."
            );

            return new VoiceResponseDto
            {
                Action = "RecommendProduct",
                ReplyText = reply,
                ScreenText = reply,
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        var replyText = _interpretationService.ByLanguage(
            language,
            $"بنصحك بـ {selectedProduct.Name}. {aiRecommendation.Reason}",
            $"I recommend {selectedProduct.Name}. {aiRecommendation.Reason}"
        );

        state.LastShownProducts = new List<ProductUserResponse> { selectedProduct };
        state.LastAssistantReply = replyText;
        SaveState(sessionId, state);

        return new VoiceResponseDto
        {
            Action = "RecommendProduct",
            ReplyText = replyText,
            ScreenText = _interpretationService.ByLanguage(
                language,
                $"{selectedProduct.Name} - {selectedProduct.Price} شيكل",
                $"{selectedProduct.Name} - {selectedProduct.Price} NIS"
            ),
            RecognizedText = text,
            CorrectedText = correctedText ?? text,
            NeedsConfirmation = false,
            SuggestedAction = "AddToCart",
            SuggestedQuery = "",
            Data = selectedProduct
        };
    }
    public string GetLastAction(string sessionId) => GetState(sessionId).LastAction;
    public string GetLastQuery(string sessionId) => GetState(sessionId).LastQuery;
    public string GetPendingSlot(string sessionId) => GetState(sessionId).PendingSlot;
    public int GetShownProductsCount(string sessionId) => GetState(sessionId).LastShownIndex;
    public int GetTotalMatchedProductsCount(string sessionId) => GetState(sessionId).LastMatchedProducts?.Count ?? 0;
    public ProductSearchContext? GetSearchContext(string sessionId) => GetState(sessionId).SearchContext;
    public string GetLastAssistantReply(string sessionId) => GetState(sessionId).LastAssistantReply;
    public bool GetAwaitingResultRefinement(string sessionId)
        => GetState(sessionId).AwaitingResultRefinement;
    public List<ProductUserResponse> LastShownProducts { get; set; } = new();
    public void SetLastAction(string sessionId, string action)
    {
        var state = GetState(sessionId);
        state.LastAction = action;
        SaveState(sessionId, state);
    }

    public void SetLastAssistantReply(string sessionId, string reply)
    {
        var state = GetState(sessionId);
        state.LastAssistantReply = reply;
        SaveState(sessionId, state);
    }

    public void ClearLastShownProducts(string sessionId)
    {
        var state = GetState(sessionId);
        state.LastShownProducts = new List<ProductUserResponse>();
        state.LastMatchedProducts = new List<ProductUserResponse>();
        state.LastShownIndex = 0;
        SaveState(sessionId, state);
    }
    private static List<string> ExtractImportantAttributeTokens(string attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
            return new List<string>();

        var ignoredWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "اللون",
        "لون",
        "يكون",
        "بكون",
        "بدّي",
        "بدي",
        "with",
        "color",
        "colour",
        "size",
        "the",
        "is",
        "new",
        "brand",
        "جديد",
        "جديدة"
    };

        return attributes
            .Replace("،", " ")
            .Replace(".", " ")
            .Replace("-", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => !ignoredWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    private static bool IsGenericSearchWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return true;

        var genericWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "product",
        "item",
        "thing",
        "منتج",
        "شي",
        "اشي",
        "حاجة",
        "clothing"
    };

        return genericWords.Contains(word.Trim());
    }
    private static bool IsProceedOnlyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Trim()
            .Replace(".", "")
            .Replace("،", "")
            .Replace("!", "")
            .Replace("؟", "")
            .Replace("?", "")
            .ToLowerInvariant();

        var proceedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "لا",
        "لا لا",
        "no",
        "nope",
        "تمام",
        "اوك",
        "ok",
        "خلص",
        "اعرض",
        "اعرض النتائج",
        "show",
        "show results"
    };

        return proceedWords.Contains(cleaned);
    }
private bool MatchesVisualSearch(
    ProductUserResponse product,
    CommandInterpretation interpretation,
    string cleanQuery)
{
    var visualText = product.VisualSearchText?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(visualText))
        return false;

    var nameAndDescription =
        $"{product.Name ?? ""} {product.Description ?? ""}";

    var brand = interpretation.Brand?.Trim() ?? "";
    var model = interpretation.Model?.Trim() ?? "";

    
    if (!string.IsNullOrWhiteSpace(brand) &&
        !nameAndDescription.Contains(
            brand,
            StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

   
    if (!string.IsNullOrWhiteSpace(model) &&
        !nameAndDescription.Contains(
            model,
            StringComparison.OrdinalIgnoreCase) &&
        !visualText.Contains(
            model,
            StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var visualTokens = BuildVisualSearchTokens(
        interpretation,
        cleanQuery
    );

    if (visualTokens.Count == 0)
        return false;

    return visualTokens.Any(token =>
        visualText.Contains(
            token,
            StringComparison.OrdinalIgnoreCase));
}
private List<string> BuildVisualSearchTokens(
    CommandInterpretation interpretation,
    string cleanQuery)
{
    var source = string.Join(" ", new[]
    {
        interpretation.Attributes,
        interpretation.Keywords,
        interpretation.ProductCategory,
        interpretation.ProductName,
        interpretation.Model,
        interpretation.Query,
        cleanQuery
    }
    .Where(value => !string.IsNullOrWhiteSpace(value)));

    var cleaned = _interpretationService.CleanSearchQuery(source);

    return cleaned
        .Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries)
        .Where(token => token.Length > 1)
        .Where(token => !IsGenericSearchWord(token))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
private bool MatchesHardSearchConstraints(
    ProductUserResponse product,
    CommandInterpretation interpretation)
{
    var name = NormalizeSearchText(
        product.Name ?? "");

    var description = NormalizeSearchText(
        product.Description ?? "");

    var visualText = NormalizeSearchText(
        product.VisualSearchText ?? "");

    var nameAndDescription =
        $"{name} {description}";

    var allSearchableText =
        $"{visualText} {name} {description}";

  
    var brand = NormalizeSearchText(
        interpretation.Brand ?? "");

    if (!string.IsNullOrWhiteSpace(brand) &&
        !nameAndDescription.Contains(
            brand,
            StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

   
    var model = NormalizeSearchText(
        interpretation.Model ?? "");

    if (!string.IsNullOrWhiteSpace(model) &&
        !allSearchableText.Contains(
            model,
            StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (interpretation.MinPrice.HasValue &&
        product.Price < interpretation.MinPrice.Value)
    {
        return false;
    }

    /*
     * الحد الأعلى شامل:
     * إذا طلب 100، منتج سعره 100 يجب أن يظهر.
     */
    if (interpretation.MaxPrice.HasValue &&
        product.Price > interpretation.MaxPrice.Value)
    {
        return false;
    }

    return true;
}
}