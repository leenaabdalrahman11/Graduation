using System.Linq;
using MyApi.DAL.DTO.Requests;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using MyApi.DAL.Repository;
using MyApiProject.MyApi.BLL.Service;

namespace MyApi.BLL.Service;

public class VoiceAssistantService : IVoiceAssistantService
{

    private readonly IStoreRepository _storeRepository;
    private readonly IOpenAiService _openAiService;
    private readonly IVoiceInterpretationService _interpretationService;
    private readonly IVoiceProductSearchService _voiceProductSearchService;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly ICheckoutService _checkoutService;
    private readonly IOrderService _orderService;
    private readonly IVoiceTextCorrectionService _voiceTextCorrectionService;
    
    public VoiceAssistantService(
        IStoreRepository storeRepository,
        IOpenAiService openAiService,
        IVoiceInterpretationService interpretationService,
        IVoiceProductSearchService voiceProductSearchService,
        ICartService cartService,
        IProductService productService,
        ICheckoutService checkoutService,
        IOrderService orderService,
        IVoiceTextCorrectionService voiceTextCorrectionService
        )
    {
        _storeRepository = storeRepository;
        _openAiService = openAiService;
        _interpretationService = interpretationService;
        _voiceProductSearchService = voiceProductSearchService;
        _cartService = cartService;
        _productService = productService;
        _checkoutService = checkoutService;
        _orderService = orderService;
        _voiceTextCorrectionService = voiceTextCorrectionService;
        
    }
   public async Task<VoiceResponseDto> ExecuteCommandAsync(
    string text,
    string? language,
    string userId,
    int? selectedProductIndex = null)
    {
        var sessionId = $"voice-session-{userId}";
        var catalogProductNames = _storeRepository
            .GetProducts()
            .SelectMany(p => p.Translations ?? Enumerable.Empty<ProductTranslation>())
            .Select(t => t.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();
        var correctionResult = _voiceTextCorrectionService.CorrectText(
    text,
    catalogProductNames,
    language);

var currentCart = await _cartService.GetUserCartAsync(userId, language ?? "ar");
var currentCartItems = currentCart?.Items ?? new List<CartResponse>();
        var textForAi = correctionResult.ShouldAutoCorrect
            ? correctionResult.CorrectedText
            : text;
        var lastAction = _voiceProductSearchService.GetLastAction(sessionId);

var currentScreen = lastAction == "OpenProductDetails"
    ? "ProductDetails"
    : "";
var interpretationResult = await _openAiService.InterpretCommandAsync(
    textForAi,
    language,
    currentScreen: currentScreen,
    lastAction: lastAction,
    lastProductQuery: _voiceProductSearchService.GetLastQuery(sessionId),
    pendingSlot: _voiceProductSearchService.GetPendingSlot(sessionId),
    lastAssistantReply: _voiceProductSearchService.GetLastAssistantReply(sessionId),
    shownProductsCount: _voiceProductSearchService.GetShownProductsCount(sessionId),
    totalMatchedProducts: _voiceProductSearchService.GetTotalMatchedProductsCount(sessionId),
    searchContext: _voiceProductSearchService.GetSearchContext(sessionId),
    catalogProductNames: catalogProductNames,
    shownProducts: _voiceProductSearchService.GetLastShownProducts(sessionId),
    awaitingResultRefinement:
        _voiceProductSearchService.GetAwaitingResultRefinement(sessionId),
    awaitingPaymentConfirmation:
        _voiceProductSearchService.GetAwaitingPaymentConfirmation(sessionId),
    cartItems: currentCartItems
);
        Console.WriteLine("===== VOICE TEXT CORRECTION =====");
        Console.WriteLine($"OriginalText: {correctionResult.OriginalText}");
        Console.WriteLine($"CorrectedText: {correctionResult.CorrectedText}");
        Console.WriteLine($"BestCatalogMatch: {correctionResult.BestCatalogMatch}");
        Console.WriteLine($"Confidence: {correctionResult.Confidence}");
        Console.WriteLine($"ShouldAutoCorrect: {correctionResult.ShouldAutoCorrect}");
        Console.WriteLine($"NeedsConfirmation: {correctionResult.NeedsConfirmation}");
        Console.WriteLine("===== END VOICE TEXT CORRECTION =====");
        CommandInterpretation interpretation = _interpretationService.BuildInterpretation(
            interpretationResult,
            textForAi,
            language);
   /*         bool isAddToCartText =
    textForAi.Contains("ضيف", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("أضف", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("اضف", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("السلة", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("الكارت", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("add", StringComparison.OrdinalIgnoreCase) ||
    textForAi.Contains("cart", StringComparison.OrdinalIgnoreCase);

if (selectedProductIndex.HasValue && isAddToCartText)
{
    interpretation.Action = "AddToCart";
    interpretation.SelectedProductIndex = selectedProductIndex.Value;
    interpretation.Confidence = 0.99;
    interpretation.NeedsConfirmation = false;
    interpretation.ConversationMode = "KeyboardProductSelection";
}*/
            if (selectedProductIndex.HasValue)
{
    interpretation.SelectedProductIndex = selectedProductIndex.Value;
}
        if (correctionResult.ShouldAutoCorrect &&
        !string.IsNullOrWhiteSpace(correctionResult.BestCatalogMatch))
        {
            interpretation.CorrectedText = correctionResult.CorrectedText;
            interpretation.MatchedCatalogName = correctionResult.BestCatalogMatch;

            if (string.IsNullOrWhiteSpace(interpretation.Query))
                interpretation.Query = correctionResult.BestCatalogMatch;

            if (string.IsNullOrWhiteSpace(interpretation.ProductName))
                interpretation.ProductName = correctionResult.BestCatalogMatch;
        }
        if (correctionResult.NeedsConfirmation &&
            !string.IsNullOrWhiteSpace(correctionResult.BestCatalogMatch))
        {
          var reply = _interpretationService.ByLanguage(
    language,
    $"هَلْ تَقْصِدِينَ الْبَحْثَ عَنْ {correctionResult.BestCatalogMatch}؟",
    $"Do you mean search for {correctionResult.BestCatalogMatch}?"
);

return new VoiceResponseDto
{
    Action = "Confirm",
    ReplyText = reply,
    ScreenText = RemoveArabicDiacritics(reply),
    RecognizedText = text,
    CorrectedText = correctionResult.BestCatalogMatch,
    NeedsConfirmation = true,
    SuggestedAction = "SearchProduct",
    SuggestedQuery = correctionResult.BestCatalogMatch
};
        }

        Console.WriteLine("===== AFTER BUILD INTERPRETATION =====");
        Console.WriteLine($"Input text: {text}");
        Console.WriteLine($"Action: {interpretation.Action}");
        Console.WriteLine($"ConversationMode: {interpretation.ConversationMode}");
        Console.WriteLine($"ShouldSearchNow: {interpretation.ShouldSearchNow}");
        Console.WriteLine($"ReplyText: {interpretation.ReplyText}");
        Console.WriteLine($"Query: {interpretation.Query}");
        Console.WriteLine($"Confidence: {interpretation.Confidence}");
        Console.WriteLine("===== END AFTER BUILD INTERPRETATION =====");

        var pendingResult = await _voiceProductSearchService.HandlePendingSearchAsync(sessionId, textForAi, interpretation);
        Console.WriteLine("===== PENDING RESULT =====");
        Console.WriteLine($"Pending Action: {pendingResult.Action}");
        Console.WriteLine($"Pending QueryText: {pendingResult.QueryText}");
        Console.WriteLine($"Pending Confidence: {pendingResult.Confidence}");
        Console.WriteLine($"Pending NeedsConfirmation: {pendingResult.NeedsConfirmation}");
        Console.WriteLine("===== END PENDING RESULT =====");

        string action = pendingResult.Action;

        var queryText = pendingResult.QueryText;
        var correctedText = !string.IsNullOrWhiteSpace(interpretation.CorrectedText)
    ? interpretation.CorrectedText.Trim()
    : correctionResult.ShouldAutoCorrect
        ? correctionResult.CorrectedText
        : text;
        var confidence = pendingResult.Confidence;
        var needsConfirmation = pendingResult.NeedsConfirmation;
        var lowerText = (correctedText ?? text ?? "").Trim().ToLowerInvariant();

        if (
            action == "OpenProductDetails" &&
            (
                lowerText.Contains("الطلب") ||
                lowerText.Contains("order")
            )
        )
        {
            action = "OpenOrderDetails";
        }
        _voiceProductSearchService.ResetPendingSearchIfNeeded(sessionId, action);
        var mode = _interpretationService.DecideExecutionMode(new CommandInterpretation
        {
            Action = action,
            Query = queryText,
            CorrectedText = correctedText,
            Confidence = confidence,
            NeedsConfirmation = needsConfirmation
        });

string confirmReply = action switch
{
    "ViewCart" => _interpretationService.ByLanguage(
        language,
        "هَلْ تَقْصِدِينَ عَرْضَ السَّلَّةِ؟",
        "Do you mean view the cart?"
    ),

    "TrackLatestOrder" => _interpretationService.ByLanguage(
        language,
        "هَلْ تَقْصِدِينَ تَتَبُّعَ آخِرِ طَلَبٍ؟",
        "Do you mean track your latest order?"
    ),

    "ViewOrders" => _interpretationService.ByLanguage(
        language,
        "هَلْ تَقْصِدِينَ عَرْضَ الطَّلَبَاتِ؟",
        "Do you mean view your orders?"
    ),

    "SearchProduct" => _interpretationService.ByLanguage(
        language,
        $"هَلْ تَقْصِدِينَ الْبَحْثَ عَنْ {queryText}؟",
        $"Do you mean search for {queryText}?"
    ),

    "OpenProductDetails" => _interpretationService.ByLanguage(
        language,
        "هَلْ تَقْصِدِينَ عَرْضَ تَفَاصِيلِ هَذَا الْمُنْتَجِ؟",
        "Do you mean show this product details?"
    ),

    _ => _interpretationService.ByLanguage(
        language,
        "هَلْ تَقْصِدِينَ هَذَا الطَّلَبَ؟",
        "Do you mean this request?"
    )
};
        if (mode == "Confirm")
        {
            return new VoiceResponseDto
            {
                Action = "Confirm",
              ReplyText = confirmReply,
ScreenText = RemoveArabicDiacritics(confirmReply),
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = true,
                SuggestedAction = action,
                SuggestedQuery = queryText
            };
        }

     string clarifyReply = !string.IsNullOrWhiteSpace(interpretation.ReplyText)
    ? interpretation.ReplyText
    : string.IsNullOrWhiteSpace(queryText)
        ? _interpretationService.ByLanguage(
            language,
            "مَا اسْمُ الْمُنْتَجِ الَّذِي تُرِيدِينَ الْبَحْثَ عَنْهُ؟",
            "What product would you like to search for?"
        )
        : _interpretationService.ByLanguage(
            language,
            "مُمْكِنٌ أَنْ تُوَضِّحِي أَكْثَرَ مَاذَا تَقْصِدِينَ؟",
            "Could you clarify what you mean?"
        );

        if (mode == "Clarify")
        {
            if (
                interpretation.Action == "Clarify" &&
                interpretation.ConversationMode == "AskFinalDetails" &&
                interpretation.EnoughInfoToSearch
            )
            {
                return await _voiceProductSearchService.HandleSearchProductAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText,
                    queryText,
                    interpretation);
            }

            return new VoiceResponseDto
            {
                Action = "Clarify",
                ReplyText = clarifyReply,
ScreenText = RemoveArabicDiacritics(clarifyReply),
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = false,
                SuggestedAction = action,
                SuggestedQuery = queryText
            };
        }

        if (mode == "Repeat")
        {
            return new VoiceResponseDto
            {
                Action = "Repeat",
                ReplyText = _interpretationService.ByLanguage(language, "ما فهمت ماذا قلتِ. من فضلك أعيدي الطلب.", "I did not understand what you said. Please repeat your request."),
                ScreenText = _interpretationService.ByLanguage(language, "لم أفهم. من فضلك قوليها مرة أخرى.", "I did not understand. Please say it again."),
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = true,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        if (string.IsNullOrWhiteSpace(correctedText))
            correctedText = text;


        var orders = await _orderService.GetMyOrdersAsync(userId);

        var allowedActions = new[]
        {
    "SearchProduct",
    "SearchAndRecommend",
    "RecommendProduct",
    "ShowMoreProducts",
    "ViewCart",
    "TrackLatestOrder",
    "ViewOrders",
    "Repeat",
    "GoBack",
    "Exit",
    "GeneralConversation",
    "Clarify",
    "OpenProductDetails",
    "AddToCart",
    "RemoveFromCart",
    "SortByPrice",
    "FilterByCategory",
    "OpenOrderDetails",
    "OpenPayment"
};

        if (!allowedActions.Contains(action))
        {
            return new VoiceResponseDto
            {
                Action = "Repeat",
                ReplyText = _interpretationService.ByLanguage(language, "طلبك ليس من الخيارات المتاحة. من فضلك أعيدي الطلب.", "Your request is not one of the available options. Please repeat your request."),
                ScreenText = _interpretationService.ByLanguage(language, "هذا الخيار غير متاح. من فضلك قوليها مرة أخرى.", "That option is not available. Please say it again."),
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = true,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }
        Console.WriteLine("===== BEFORE SWITCH =====");
        Console.WriteLine($"Final action before switch: {action}");
        Console.WriteLine($"Final queryText before switch: {queryText}");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine("===== END BEFORE SWITCH =====");
        if (action != "OpenPayment" && action != "ViewCart")
        {
            _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, false);
        }
        Console.WriteLine("===== ORDER DEBUG =====");
        Console.WriteLine($"User text: {text}");
        Console.WriteLine($"Corrected text: {correctedText}");
        Console.WriteLine($"Final action: {action}");
        Console.WriteLine("===== END ORDER DEBUG =====");
        switch (action)
        {
            case "RemoveFromCart":
    return await _voiceProductSearchService.HandleRemoveFromCartAsync(
        sessionId,
        textForAi,
        language,
        correctedText,
        interpretation,
        userId
    );

            case "SearchProduct":
                return await _voiceProductSearchService.HandleSearchProductAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText,
                    queryText,
                    interpretation);

            case "SearchAndRecommend":
                return await _voiceProductSearchService.HandleSearchAndRecommendAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText,
                    queryText,
                    interpretation);

            case "RecommendProduct":
                return await _voiceProductSearchService.HandleRecommendProductAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText,
                    interpretation);
        case "GeneralConversation":
{
    var naturalReply = _interpretationService.GetNaturalReply(
        correctedText ?? text,
        language
    );

    return new VoiceResponseDto
    {
        Action = "GeneralConversation",
        ReplyText = naturalReply,
        ScreenText = RemoveArabicDiacritics(naturalReply),
        RecognizedText = text,
        CorrectedText = correctedText ?? text,
        NeedsConfirmation = false,
        SuggestedAction = "",
        SuggestedQuery = ""
    };
}

            case "ViewCart":
                {
                    var userCart = await _cartService.GetUserCartAsync(userId);

                    if (userCart == null || userCart.Items == null || userCart.Items.Count == 0)
                    {
                        _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, false);

                        return new VoiceResponseDto
                        {
                            Action = "ViewCart",
                            ReplyText = _interpretationService.ByLanguage(language, "سلة التسوق فارغة.", "Your cart is empty."),
                            ScreenText = _interpretationService.ByLanguage(language, "السلة فارغة.", "Cart is empty."),
                            RecognizedText = text,
                            CorrectedText = correctedText,
                            NeedsConfirmation = false,
                            SuggestedAction = "",
                            SuggestedQuery = "",
                            Data = userCart
                        };
                    }

                   var spokenCart = string.Join(" . ", userCart.Items.Select(x =>
    _interpretationService.ByLanguage(
        language,
        $"{x.ProductName}، الْكَمِّيَّةُ {x.Count}، السِّعْرُ {x.Price} شِيكَلًا، الْمَجْمُوعُ {x.TotalPrice} شِيكَلًا",
        $"{x.ProductName}, quantity {x.Count}, price {x.Price} shekels, total {x.TotalPrice} shekels"
    )));

                    var screenCart = string.Join(" | ", userCart.Items.Select(x =>
                        _interpretationService.ByLanguage(
                            language,
                            $"{x.ProductName} × {x.Count} = {x.TotalPrice} شيكل",
                            $"{x.ProductName} x{x.Count} = {x.TotalPrice} NIS"
                        )));

                    _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, true);

                  var reviewReply = _interpretationService.ByLanguage(
    language,
    $"هَذِهِ مُرَاجَعَةُ السَّلَّةِ قَبْلَ الدَّفْعِ. لَدَيْكِ {userCart.Items.Count} عُنْصُرًا. {spokenCart}. الْمَجْمُوعُ الْكُلِّيُّ {userCart.CartTotal} شِيكَلًا. إِذَا كُنْتِ جَاهِزَةً، فَقُولِي: ادْفَعِي الْآنَ.",
    $"Here is your cart review before payment. You have {userCart.Items.Count} item(s). {spokenCart}. Cart total is {userCart.CartTotal} shekels. If you are ready, say: pay now."
);

                    return new VoiceResponseDto
                    {
                        Action = "ViewCart",
                        ReplyText = reviewReply,
                        ScreenText = _interpretationService.ByLanguage(
                            language,
                            $"{screenCart} | الإجمالي = {userCart.CartTotal} شيكل",
                            $"{screenCart} | Total = {userCart.CartTotal} NIS"
                        ),
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "OpenPayment",
                        SuggestedQuery = "",
                        Data = userCart
                    };
                }
            case "TrackLatestOrder":
                {
                    var myOrders = await _orderService.GetMyOrdersAsync(userId);

                    var latestOrder = myOrders
                        .OrderByDescending(o => o.Id)
                        .FirstOrDefault();

                    if (latestOrder is null)
                    {
                        return new VoiceResponseDto
                        {
                            Action = "TrackLatestOrder",
                           ReplyText = _interpretationService.ByLanguage(
    language,
    "لَمْ يَتِمَّ الْعُثُورُ عَلَى أَيِّ طَلَبَاتٍ.",
    "No orders were found."
),
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                "لم يتم العثور على آخر طلب.",
                                "No latest order found."
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText,
                            NeedsConfirmation = false,
                            SuggestedAction = "",
                            SuggestedQuery = ""
                        };
                    }

                    var spokenItems = latestOrder.OrderItems == null || latestOrder.OrderItems.Count == 0
                        ? _interpretationService.ByLanguage(
                            language,
                            "لا توجد منتجات داخل هذا الطلب",
                            "There are no products inside this order"
                        )
                        : string.Join(" . ", latestOrder.OrderItems.Select(item =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{item.ProductName}، الكمية {item.Count}، السعر {item.Price} شيكل، المجموع {item.TotalPrice} شيكل",
                                $"{item.ProductName}, quantity {item.Count}, price {item.Price} shekels, total {item.TotalPrice} shekels"
                            )));

                    var screenItems = latestOrder.OrderItems == null || latestOrder.OrderItems.Count == 0
                        ? _interpretationService.ByLanguage(
                            language,
                            "لا توجد منتجات داخل الطلب",
                            "No products inside the order"
                        )
                        : string.Join(" | ", latestOrder.OrderItems.Select(item =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{item.ProductName} × {item.Count} = {item.TotalPrice} شيكل",
                                $"{item.ProductName} x{item.Count} = {item.TotalPrice} NIS"
                            )));

                    return new VoiceResponseDto
                    {
                        Action = "TrackLatestOrder",
                        ReplyText = _interpretationService.ByLanguage(
                            language,
                            $"آخر طلب لديك هو رقم {latestOrder.Id}. حالته {latestOrder.OrderStatus}. حالة الدفع {latestOrder.PaymentStatus}. المنتجات: {spokenItems}.",
                            $"Your latest order is number {latestOrder.Id}. Status is {latestOrder.OrderStatus}. Payment status is {latestOrder.PaymentStatus}. Products: {spokenItems}."
                        ),
                        ScreenText = _interpretationService.ByLanguage(
                            language,
                            $"آخر طلب: {latestOrder.Id} - {latestOrder.OrderStatus} - {latestOrder.PaymentStatus} | {screenItems}",
                            $"Latest order: {latestOrder.Id} - {latestOrder.OrderStatus} - {latestOrder.PaymentStatus} | {screenItems}"
                        ),
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = "",
                        Data = latestOrder
                    };
                }
            case "ViewOrders":
                {
                    var myOrders = await _orderService.GetMyOrdersAsync(userId);

                    var latestThreeOrders = myOrders
                        .OrderByDescending(o => o.Id)
                        .Take(3)
                        .ToList();

                    if (latestThreeOrders.Count == 0)
                    {
                        return new VoiceResponseDto
                        {
                            Action = "ViewOrders",
                           ReplyText = _interpretationService.ByLanguage(
    language,
    "لَيْسَ لَدَيْكِ أَيُّ طَلَبَاتٍ.",
    "You do not have any orders."
),
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                "لم يتم العثور على طلبات.",
                                "No orders found."
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText,
                            NeedsConfirmation = false,
                            SuggestedAction = "",
                            SuggestedQuery = ""
                        };
                    }

                    var spokenOrders = string.Join(" . ", latestThreeOrders.Select((o, index) =>
                        _interpretationService.ByLanguage(
                            language,
                            $"الطلب {index + 1}: رقم الطلب {o.Id}، حالته {o.OrderStatus}، وحالة الدفع {o.PaymentStatus}",
                            $"Order {index + 1}: order id {o.Id}, status {o.OrderStatus}, payment status {o.PaymentStatus}"
                        )));

                    var screenOrders = string.Join(" | ", latestThreeOrders.Select((o, index) =>
                        _interpretationService.ByLanguage(
                            language,
                            $"{index + 1}) طلب {o.Id} - {o.OrderStatus} - {o.PaymentStatus}",
                            $"{index + 1}) Order {o.Id} - {o.OrderStatus} - {o.PaymentStatus}"
                        )));
                   var replyText = _interpretationService.ByLanguage(
    language,
    $"هَذِهِ آخِرُ {latestThreeOrders.Count} طَلَبَاتٍ لَدَيْكِ. {spokenOrders}. اخْتَارِي طَلَبًا لِعَرْضِ التَّفَاصِيلِ، مِثْلَ: الطَّلَبِ الْأَوَّلِ، أَوِ الثَّانِي، أَوِ الثَّالِثِ.",
    $"Here are your latest {latestThreeOrders.Count} orders. {spokenOrders}. Choose an order to see details, like first, second, or third."
);

                    _voiceProductSearchService.SetLastAction(sessionId, "ViewOrders");
                    _voiceProductSearchService.SetLastAssistantReply(sessionId, replyText);
                    _voiceProductSearchService.ClearLastShownProducts(sessionId);

                    return new VoiceResponseDto
                    {
                        Action = "ViewOrders",
                        ReplyText = replyText,
                        ScreenText = screenOrders,
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "OpenOrderDetails",
                        SuggestedQuery = "",
                        Data = latestThreeOrders
                    };
                }

            case "OpenOrderDetails":
                {
                    var myOrders = await _orderService.GetMyOrdersAsync(userId);

                    var latestThreeOrders = myOrders
                        .OrderByDescending(o => o.Id)
                        .Take(3)
                        .ToList();

                    if (latestThreeOrders.Count == 0)
                    {
                        return new VoiceResponseDto
                        {
                            Action = "OpenOrderDetails",
                            ReplyText = _interpretationService.ByLanguage(
                                language,
                                "لَيْسَ لَدَيْكِ أَيُّ طَلَبَاتٍ.",
                                "You do not have any orders."
                            ),
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                "لا توجد طلبات.",
                                "No orders found."
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText,
                            NeedsConfirmation = false,
                            SuggestedAction = "",
                            SuggestedQuery = ""
                        };
                    }

                    var lower = (correctedText ?? text).Trim().ToLowerInvariant();

                    OrderResponse? selectedOrder = null;

                    if (lower.Contains("الأول") || lower.Contains("اول") || lower.Contains("first"))
                        selectedOrder = latestThreeOrders.ElementAtOrDefault(0);
                    else if (lower.Contains("الثاني") || lower.Contains("second"))
                        selectedOrder = latestThreeOrders.ElementAtOrDefault(1);
                    else if (lower.Contains("الثالث") || lower.Contains("third"))
                        selectedOrder = latestThreeOrders.ElementAtOrDefault(2);
                    else
                        selectedOrder = latestThreeOrders
                            .FirstOrDefault(o => lower.Contains(o.Id.ToString()));

                    if (selectedOrder is null)
                    {
                        return new VoiceResponseDto
                        {
                            Action = "Clarify",
                          ReplyText = _interpretationService.ByLanguage(
    language,
    "أَيَّ طَلَبٍ تَقْصِدِينَ؟ قُولِي: الطَّلَبَ الْأَوَّلَ، أَوِ الطَّلَبَ الثَّانِيَ، أَوِ الطَّلَبَ الثَّالِثَ.",
    "Which order do you mean? Say first, second, or third."
),
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                "اختاري: الأول، الثاني، أو الثالث.",
                                "Choose: first, second, or third."
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText,
                            NeedsConfirmation = false,
                            SuggestedAction = "OpenOrderDetails",
                            SuggestedQuery = ""
                        };
                    }

                    var spokenItems = selectedOrder.OrderItems == null || selectedOrder.OrderItems.Count == 0
                        ? _interpretationService.ByLanguage(
                            language,
                            "لا توجد منتجات داخل هذا الطلب",
                            "There are no products inside this order"
                        )
                        : string.Join(" . ", selectedOrder.OrderItems.Select(item =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{item.ProductName}، الكمية {item.Count}، السعر {item.Price} شيكل، المجموع {item.TotalPrice} شيكل",
                                $"{item.ProductName}, quantity {item.Count}, price {item.Price} shekels, total {item.TotalPrice} shekels"
                            )));

                    var screenItems = selectedOrder.OrderItems == null || selectedOrder.OrderItems.Count == 0
                        ? _interpretationService.ByLanguage(
                            language,
                            "لا توجد منتجات داخل الطلب",
                            "No products inside the order"
                        )
                        : string.Join(" | ", selectedOrder.OrderItems.Select(item =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{item.ProductName} × {item.Count} = {item.TotalPrice} شيكل",
                                $"{item.ProductName} x{item.Count} = {item.TotalPrice} NIS"
                            )));

                    return new VoiceResponseDto
                    {
                        Action = "OpenOrderDetails",
                      ReplyText = _interpretationService.ByLanguage(
    language,
    $"تَفَاصِيلُ الطَّلَبِ رَقْمَ {selectedOrder.Id}. حَالَتُهُ {selectedOrder.OrderStatus}. حَالَةُ الدَّفْعِ {selectedOrder.PaymentStatus}. الْمُنْتَجَاتُ: {spokenItems}.",
    $"Details for order number {selectedOrder.Id}. Status is {selectedOrder.OrderStatus}. Payment status is {selectedOrder.PaymentStatus}. Products: {spokenItems}."
),
                        ScreenText = _interpretationService.ByLanguage(
                            language,
                            $"طلب {selectedOrder.Id} - {selectedOrder.OrderStatus} - {selectedOrder.PaymentStatus} | {screenItems}",
                            $"Order {selectedOrder.Id} - {selectedOrder.OrderStatus} - {selectedOrder.PaymentStatus} | {screenItems}"
                        ),
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = "",
                        Data = selectedOrder
                    };
                }
            case "Repeat":
                return new VoiceResponseDto
                {
                 Action = "Repeat",
ReplyText = _interpretationService.ByLanguage(
    language,
    "سَأُعِيدُ الْخِيَارَاتِ الْمُتَاحَةَ. اِبْحَثِي عَنْ مُنْتَجٍ، اعْرِضِي عَنَاصِرَ السَّلَّةِ، تَتَبَّعِي آخِرَ طَلَبٍ، أَوِ اعْرِضِي طَلَبَاتِي.",
    "Repeating the available options. Search for a product, view cart items, track your latest order, or view my order."
),
ScreenText = _interpretationService.ByLanguage(
    language,
    "تمت إعادة الخيارات.",
    "Options repeated."
),
RecognizedText = text,
CorrectedText = correctedText,
NeedsConfirmation = false,
SuggestedAction = "",
SuggestedQuery = ""
};

            case "GoBack":
                return new VoiceResponseDto
                {
                    Action = "GoBack",
                    ReplyText = _interpretationService.ByLanguage(language, "العودة إلى القائمة الرئيسية.", "Going back to the main menu."),
                    ScreenText = _interpretationService.ByLanguage(language, "رجوع إلى القائمة الرئيسية.", "Back to main menu."),
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };
            case "ShowMoreProducts":
                return await _voiceProductSearchService.HandleShowMoreProductsAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText);
            case "Exit":
                return new VoiceResponseDto
                {
                    Action = "Exit",
                    ReplyText = _interpretationService.ByLanguage(language, "جارٍ إنهاء المساعد. مع السلامة.", "Exiting the assistant. Goodbye."),
                    ScreenText = _interpretationService.ByLanguage(language, "تم إنهاء الجلسة.", "Session ended."),
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };
            case "OpenProductDetails":
                {
var selectedProduct = _voiceProductSearchService.ResolveProductFromAiOrLastShown(
    interpretation,
    textForAi,
    sessionId
);
                    if (selectedProduct == null)
                    {
                    var reply = _interpretationService.ByLanguage(
    language,
    "أَيَّ مُنْتَجٍ تَقْصِدِينَ؟ قُولِي الْأَوَّلَ، أَوِ الثَّانِيَ، أَوِ الثَّالِثَ، أَوِ اذْكُرِي اسْمَ الْمُنْتَجِ.",
    "Which product do you mean? Say first, second, third, or mention the product name."
);

return new VoiceResponseDto
{
    Action = "Clarify",
    ReplyText = reply,
    ScreenText = RemoveArabicDiacritics(reply),
    RecognizedText = text,
    CorrectedText = correctedText ?? text,
    NeedsConfirmation = false,
    SuggestedAction = "OpenProductDetails",
    SuggestedQuery = ""
};
                    }

                    var details = await _productService.GetProductsDetailsForUser(selectedProduct.Id, language ?? "en");

                    var response = details;

                    var spokenReply = _interpretationService.ByLanguage(
                        language,
                        $"تفاصيل المنتج: {response.Name}. الوصف: {response.Description}. السعر {response.Price} شيكل. الكمية المتوفرة {response.Quantity}. هل تريدين إضافته إلى السلة أم نكمل البحث؟",
                        $"Product details: {response.Name}. Description: {response.Description}. Price {response.Price} shekels. Available quantity {response.Quantity}. Would you like to add it to the cart or continue searching?"
                    );
_voiceProductSearchService.SetCurrentProductDetails(
    sessionId,
    selectedProduct,
    spokenReply
);
                    var screenReply = _interpretationService.ByLanguage(
                        language,
                        $"الاسم: {response.Name} | السعر: {response.Price} شيكل | الكمية: {response.Quantity} | الخصم: {response.Discount}%",
                        $"Name: {response.Name} | Price: {response.Price} NIS | Quantity: {response.Quantity} | Discount: {response.Discount}%"
                    );

                    return new VoiceResponseDto
                    {
                        Action = "OpenProductDetails",
                        ReplyText = spokenReply,
                        ScreenText = screenReply,
                        RecognizedText = text,
                        CorrectedText = correctedText ?? text,
                        NeedsConfirmation = false,
                        SuggestedAction = "AddToCart",
                        SuggestedQuery = "",
                        Data = response
                    };
                }
            case "OpenPayment":
                {
                    var userCart = await _cartService.GetUserCartAsync(userId);

                    if (userCart == null || userCart.Items == null || userCart.Items.Count == 0)
                    {

                        _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, false);

                        return new VoiceResponseDto
                        {

                            Action = "OpenPayment",
                            ReplyText = _interpretationService.ByLanguage(
                                language,
                                "السلة فارغة، لا يمكن المتابعة إلى الدفع.",
                                "Your cart is empty, so payment cannot continue."
                            ),
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                "السلة فارغة.",
                                "Cart is empty."
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText ?? text,
                            NeedsConfirmation = false,
                            SuggestedAction = "SearchProduct",
                            SuggestedQuery = ""
                        };
                    }

                    if (!_voiceProductSearchService.GetAwaitingPaymentConfirmation(sessionId))
                    {
                        var spokenCart = string.Join(" . ", userCart.Items.Select(x =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{x.ProductName}، الكمية {x.Count}، السعر {x.Price} شيكل، المجموع {x.TotalPrice} شيكل",
                                $"{x.ProductName}, quantity {x.Count}, price {x.Price} shekels, total {x.TotalPrice} shekels"
                            )));

                        var screenCart = string.Join(" | ", userCart.Items.Select(x =>
                            _interpretationService.ByLanguage(
                                language,
                                $"{x.ProductName} × {x.Count} = {x.TotalPrice} شيكل",
                                $"{x.ProductName} x{x.Count} = {x.TotalPrice} NIS"
                            )));

                        _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, true);

                        var reviewReply = _interpretationService.ByLanguage(
                            language,
                            $"قبل الدفع، هذه مراجعة السلة. لديك {userCart.Items.Count} عنصر. {spokenCart}. المجموع الكلي {userCart.CartTotal} شيكل. إذا أردتِ المتابعة، قولي: ادفع الآن.",
                            $"Before payment, here is your cart review. You have {userCart.Items.Count} item(s). {spokenCart}. Cart total is {userCart.CartTotal} shekels. If you want to continue, say: pay now."
                        );

                        return new VoiceResponseDto
                        {
                            Action = "ViewCart",
                            ReplyText = reviewReply,
                            ScreenText = _interpretationService.ByLanguage(
                                language,
                                $"{screenCart} | Total = {userCart.CartTotal} NIS",
                                $"{screenCart} | Total = {userCart.CartTotal} NIS"
                            ),
                            RecognizedText = text,
                            CorrectedText = correctedText ?? text,
                            NeedsConfirmation = false,
                            SuggestedAction = "OpenPayment",
                            SuggestedQuery = "",
                            Data = userCart
                        };
                    }

                    _voiceProductSearchService.SetAwaitingPaymentConfirmation(sessionId, false);
                    Console.WriteLine("===== RETURNING REQUEST CARD DETAILS - NO PAYMENT EXECUTED HERE =====");
                    return new VoiceResponseDto
                    {
                        Action = "RequestCardDetails",
                        ReplyText = _interpretationService.ByLanguage(
                            language,
                            "تمام، من فضلك أدخلي معلومات البطاقة لإكمال الدفع.",
                            "Okay, please enter your card details to complete the payment."
                        ),
                        ScreenText = _interpretationService.ByLanguage(
                            language,
                            "أدخلي معلومات البطاقة لإكمال الدفع.",
                            "Enter card details to complete payment."
                        ),
                        RecognizedText = text,
                        CorrectedText = correctedText ?? text,
                        NeedsConfirmation = false,
                        SuggestedAction = "EnterCardDetails",
                        SuggestedQuery = "",
                        Data = userCart
                    };
                }

            case "AddToCart":
                return await _voiceProductSearchService.HandleAddToCartAsync(
                    sessionId,
                    textForAi,
                    language,
                    correctedText,
                    interpretation,
                    userId
                );
            default:
                return new VoiceResponseDto
                {
                    Action = "Repeat",
                    ReplyText = _interpretationService.ByLanguage(language, "ما فهمت ماذا قلتِ. من فضلك أعيدي الطلب.", "I did not understand what you said. Please repeat your request."),
                    ScreenText = _interpretationService.ByLanguage(language, "لم أفهم. من فضلك قوليها مرة أخرى.", "I did not understand. Please say it again."),
                    RecognizedText = text,
                    CorrectedText = correctedText ?? text,
                    NeedsConfirmation = true,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };
        }
    }
    private string GetPreferredProductName(Product p, string? language)
    {
        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.ToLowerInvariant();

        return p.Translations?
            .FirstOrDefault(t => t.Language == selectedLanguage)?.Name
            ?? p.Translations?.FirstOrDefault(t => t.Language == "en")?.Name
            ?? p.Translations?.FirstOrDefault()?.Name
            ?? $"Product {p.Id}";
    }
    private static string RemoveArabicDiacritics(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
        return text ?? string.Empty;

    return System.Text.RegularExpressions.Regex.Replace(
        text,
        @"[\u0610-\u061A\u064B-\u065F\u0670\u06D6-\u06ED]",
        string.Empty
    );
}

}