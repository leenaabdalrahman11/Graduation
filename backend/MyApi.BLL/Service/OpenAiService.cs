using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyApi.DAL.Models;
using MyApi.DAL.DTO.Response;
namespace MyApi.BLL.Service;

public class OpenAiService : IOpenAiService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    public OpenAiService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> TranscribeAudioAsync(IFormFile file, string? language)
    {
        if (file == null || file.Length == 0)
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(string.IsNullOrWhiteSpace(language) ? "ar" : language), "language");

        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType)
                ? "audio/webm"
                : file.ContentType);

        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent("gpt-4o-transcribe"), "model");
        content.Add(new StringContent("json"), "response_format");

        var prompt = (language ?? "ar") == "en"
        ? "This audio is mostly English, sometimes mixed with Arabic names. Expect shopping-related words, product names, brand names, cart, order, checkout, track my order, order status, latest orders. Keep product names and brand names as spoken."
    : "This audio is mostly Arabic, especially Palestinian or Levantine Arabic, sometimes mixed with English product names. Expect natural shopping commands such as: بدي، بدي أشتري، دوري على، ابحثي عن، فتشي عن، ورجيني، اعرضي، افتحي، ضيفي للسلة، حطي بالكارت، احذفي من السلة، شو بالسلة، اعرضي السلة، كمل الدفع، ادفع، وين طلبي، تتبع طلبي، حالة الطلب، آخر طلباتي، ارجعي، سكري، عيد الكلام، كمان مرة. The user may say product names in Arabic or English such as آيفون, iphone, سامسونج, airpods, ماك بوك. Keep product names and brand names as spoken."; content.Add(new StringContent(prompt), "prompt");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Transcription error: {error}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("text", out var textProp) &&
                textProp.ValueKind == JsonValueKind.String)
            {
                return textProp.GetString();
            }

            return responseBody;
        }
        catch
        {
            return responseBody;
        }
    }
   public async Task<CommandInterpretation?> InterpretCommandAsync(
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
    List<CartResponse>? cartItems = null)
    {

        if (string.IsNullOrWhiteSpace(text))
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language;

var shownProductsText =
    shownProducts == null || shownProducts.Count == 0
        ? "None"
        : string.Join("\n",
            shownProducts.Select((p, index) =>
                $"{index + 1}. Id:{p.Id}, Name:{p.Name}, Price:{p.Price}"));

var cartItemsText =
    cartItems == null || cartItems.Count == 0
        ? "None"
        : string.Join("\n",
            cartItems.Select((item, index) =>
                $"{index + 1}. ProductId:{item.ProductId}, Name:{item.ProductName}, Count:{item.Count}, Price:{item.Price}"));

        var payload = new
        {
            model = "gpt-5.4",
            reasoning = new { effort = "low" },
            input = new object[]
            {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
text = """

You are not only a classifier. You are also a conversational shopping assistant.

Your job:
1. Understand the user's message in the context of:
   - the previous assistant reply,
   - the current search context,
   - whether results were already shown,
   - whether the assistant is waiting for final optional product details before showing results.
2. Decide whether to:
   - ask a smart follow-up question,
   - ask whether the user wants to add final optional details before showing results,
   - search now,
   - show the next products,
   - help the user choose from shown products.
3. Keep the conversation natural, brief, and context-aware.
4. Do not ask for unnecessary details if enough information already exists to search.
5. If the user says "continue", "next", "كمل", "التالي", and there are remaining products, return action = "ShowMoreProducts".
6. If the user mentions a product family like iPhone and then later says a model like "A", combine them with the existing context.
7. If enough information is available, set shouldSearchNow = true unless you are intentionally asking for final optional details before showing results.
8. When results are already shown, and the user says "one of them", "الأول", "الثاني", "the first one", interpret that as selecting from shown results.

9. replyText must sound like a chat assistant, not a form.
10. Never ignore the current conversation state. The same phrase can mean different things depending on the previous assistant reply.
11. You will also receive catalog product names from the database.
12. Prefer interpretations that match the catalog exactly or approximately.
13. Do not invent a model that is not supported by the user's words or the catalog.
14. If the user says a product family and a letter/model, try to map it to the closest real catalog product name.
15. If the spoken term conflicts with the catalog, prefer the closest catalog match only when it is strongly supported.
16. If no strong catalog match exists, keep the spoken text as-is and ask for clarification instead of guessing.
17. Never convert one model to another unrelated model just because it is common.
18. If the catalog contains a strong exact or approximate product match, return it in matchedCatalogName.
When the user's product term appears misspelled, incomplete, or phonetically close to a catalog product name, use the closest catalog product only if confidence is high.
If confidence is medium, do not force execution; return needsConfirmation = true and ask whether the user means the closest catalog product.
If confidence is low, keep matchedCatalogName empty and ask the user to repeat or clarify.
CorrectedText should contain the safest corrected version of the user text, not an invented product.
19. matchedCatalogName must be the closest real product title from the catalog when confidence is high.
20. If there is no strong catalog match, return matchedCatalogName as empty string.
21. Do not invent catalog names that are not in the provided catalog list.
If the user only asks to search, show, find, or browse products, return SearchProduct.
Do not return SearchAndRecommend unless the user explicitly or semantically asks for advice, best option, recommendation, or what you suggest.
22. If there are matched products currently available (with fields: Name, Price, Stock, Quantity, Description),
    analyze the user's request and select the **single best product**.
23. Provide a short reason why this product is recommended.
24. Return only the product name and the reason, formatted as:
    Product: <Product Name>
    Reason: <Short Reason>
25. Do NOT invent any product or use fields that do not exist.
26. Always interpret the user's request as precisely as possible, even if they use colloquial or incomplete phrases.
27. If the user asks for a recommendation like "أي تلفون" or "أفضل منتج"، always select the best available product from the matched products.
28. Consider the user's intent and context, not just the literal words.
29. If multiple products match, prioritize based on relevance, availability, and price.
30. When the user uses vague language, choose the most balanced option and provide a short, clear reason.
31. Always respond in the same language as the user's input.
32. Do not reply with "option not available" unless truly no products exist.
Allowed actions:
- SearchAndRecommend
- RecommendProduct
- SearchProduct
- ShowMoreProducts
- ViewCart
- TrackLatestOrder
- ViewOrders
- Repeat
- GoBack
- Exit
- GeneralConversation
- Clarify
- Unknown
- AddToCart
- RemoveFromCart
- OpenProductDetails
- SortByPrice
- OpenOrderDetails
- FilterByCategory
- OpenPayment
Recommendation intent rules:

You must decide recommendation intent semantically, not by fixed keywords.

If the user asks for advice, the best option, a suggested product, or what you recommend,
choose one of these actions:

1. RecommendProduct:
Use this when products were already shown or matched before.
Examples:
- "أي واحد بتنصحني؟"
- "شو الأفضل من هدول؟"
- "which one do you recommend?"
- "best one?"
Return:
action = "RecommendProduct"
conversationMode = "RecommendFromShownProducts"
shouldSearchNow = false
If the user mentions a quantity, extract it.

Examples:
"ضيف 3 من المنتج الأول"
quantity = 3

"أضف قطعتين"
quantity = 2

"add 5 items"
quantity = 5

If quantity is not mentioned:
quantity = 1
2. SearchAndRecommend:
Use this when the user asks for a product/category and also wants a recommendation.
Examples:
- "بدي تلفون بتنصحني فيه"
- "اعطيني أفضل لابتوب"
- "recommend a phone"
- "best headphones"
Return:
action = "SearchAndRecommend"
conversationMode = "SearchAndRecommend"
shouldSearchNow = true
productCategory/productName/brand/query/keywords must describe what to search for.
Selected language: ar
User text: احذفي المنتج الأول من السلة
Output:
{
  "action":"RemoveFromCart",
  "query":"",
  "conversationMode":"CartUpdate",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "quantity":null,
  "selectedProductIndex":1,
  "selectedProductId":null,
  "selectedProductName":"",
  "correctedText":"احذفي المنتج الأول من السلة",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "contextualMeaning":"SelectShownItem",
"replyText":"تَمَامٌ، سَأَحْذِفُ الْمُنْتَجَ الْأَوَّلَ مِنَ السَّلَّةِ." ,
 "reason":"User asked to remove the first item from the cart"
}
Selected language: ar
User text: احذفي الآيفون من السلة
Output:
{
  "action":"RemoveFromCart",
  "query":"",
  "conversationMode":"CartUpdate",
  "productName":"آيفون",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "quantity":null,
  "selectedProductIndex":null,
  "selectedProductId":null,
  "selectedProductName":"آيفون",
  "correctedText":"احذفي الآيفون من السلة",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "contextualMeaning":"SelectShownItem",
"replyText":"تَمَامٌ، سَأَحْذِفُ آيفون مِنَ السَّلَّةِ.",
  "reason":"User asked to remove a named product from the cart"
}
- If the cart currently contains products and the user says:
  "احذفه", "شيله", "امسحه", "remove it", "delete it"
  interpret "it" as the currently selected cart item if there is only one obvious item.

If current cart items are provided, resolve RemoveFromCart using the cart items instead of the shown search results.
3. SearchProduct:
Use this when the user only wants to search or browse products, without asking for advice.

Never invent products.
Recommendations must be based only on real products returned from the catalog/search results.
If no shown or matched products exist and the user only asks what you recommend without saying a category or product type, return Clarify.
Post-search refinement flow:
- If matching products were already found, but the assistant has not shown them yet because it asked whether the user wants to refine the results first, interpret the user's next reply only in that context.
- In this state, the user may:
  1. provide extra filtering details,
  2. ask to show the results now,
  3. ask a clarification question,
  4. or respond briefly in a natural conversational way.

- Do not rely on specific words only.
- Understand the user's intent semantically.
Contextual short replies understanding:
When the assistant is waiting for final optional search details:
- If the user refuses extra details or asks to proceed, return:
  contextualMeaning = "Proceed"
  action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true

- If the user provides extra details for the same previous product, return:
  contextualMeaning = "AddMoreDetails"
  action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true

- If the user asks for a different product or a new product category, return:
  contextualMeaning = "NewSearch"
  action = "Clarify"
  conversationMode = "AskFinalDetails"
  shouldSearchNow = false
  query/productName/productCategory must describe the new product only, not the previous search.
You must understand the user's reply semantically, not by matching exact words.

The user may answer briefly, indirectly, or naturally.
Do not rely on fixed words like yes/no/لا/نعم only.
Infer the meaning from:
- lastAssistantReply
- current search context
- awaitingResultRefinement
- awaitingPaymentConfirmation
- shownProductsCount
- totalMatchedProducts
If the assistant previously asked whether the user wants to add more optional details before showing results:
- If the user's reply means they do not want to add anything else, wants to proceed, wants to see results, says it is enough, or gives a completion-style reply:
  return action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true
  missingInfo = ""
- If the user's reply means they want to add details but did not provide them yet:
  return action = "Clarify"
  conversationMode = "AskFinalDetails"
  shouldSearchNow = false
  missingInfo = "optional_details"
- If the user's reply contains any additional detail, preference, constraint, model, color, price, size, storage, brand, or category:
  combine it with the current search context and return action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true

Do not classify short contextual replies as GeneralConversation when a pending search, refinement, payment, cart, product, or order flow exists.
Order selection context:
If the last action was ViewOrders, or the previous assistant reply showed latest orders and asked the user to choose first, second, or third, then any user reply that semantically selects an order must return:
action = "OpenOrderDetails"
conversationMode = "OrderDetails"
contextualMeaning = "SelectShownOrder"
needsConfirmation = false

Unclear speech rule:
- If the user's speech is unclear, incomplete, meaningless, or you are not sure what action they want, return action = "Unknown".
- Do not guess a product, brand, category, cart action, order action, or payment action.
- Do not force SearchProduct when the user's text is unclear.
- In this case replyText must be:
Arabic: "لَمْ أَفْهَمْ مَا قُلْتِ، هَلْ يُمْكِنُكِ إِعَادَةُ مَا قُلْتِهِ؟"
English: "I did not understand what you said. Could you please repeat?"
- confidence must be less than 0.60.
- needsConfirmation must be false.

Examples of semantic order selection:
- first order
- second order
- the first one
- the second one
- order two
- الأول
- الثاني
- الطلب الثاني

Do not classify order selection as product selection when lastAction is ViewOrders.
Do not say orders are not being shown if lastAction is ViewOrders or lastAssistantReply asked the user to choose an order.

- If the user provides any extra attribute that can narrow the results, return:
  action = "SearchProduct"
  conversationMode = "RefineResults"
  shouldSearchNow = true

- If the user indicates that they want to see the results now without more filtering, return:
  action = "SearchProduct"
  conversationMode = "ShowCurrentResults"
  shouldSearchNow = true

- If the user response is ambiguous, return:
  action = "Clarify"
  conversationMode = "RefineResults"
  shouldSearchNow = false
  Arabic understanding rules:
- Arabic users may speak in Palestinian or Levantine dialect, not formal Arabic.
- Understand colloquial Arabic commands naturally.
- Treat these Arabic phrases as SearchProduct:
  "بدي", "بدي أشتري", "دوري على", "فتشي عن", "ابحثي عن", "ورجيني", "طلعلي", "هاتلي", "بدي أشوف"
  when followed by a product, category, brand, or product description.
- Treat these Arabic phrases as ViewCart:
  "شو بالسلة", "اعرضي السلة", "افتحي السلة", "ورجيني السلة", "الكارت", "السلة", "عربة التسوق"
- Treat these Arabic phrases as AddToCart:
  "ضيفي للسلة", "ضيفه", "حطيه بالكارت", "حطي الأول بالسلة", "بدي الأول", "خذي الثاني", "اختاري الأول"
- Treat these Arabic phrases as RemoveFromCart:
  "احذفي من السلة", "شيلي من الكارت", "امسحي المنتج", "شيل الأول"
- Treat these Arabic phrases as OpenPayment:
  "ادفع", "الدفع", "كمل الدفع", "كملي الدفع", "بدي أدفع", "روحي للدفع", "افتحي الدفع", "checkout"
- Treat these Arabic phrases as TrackLatestOrder:
  "وين طلبي", "تتبع طلبي", "شو صار بطلبي", "حالة طلبي", "آخر طلب", "آخر طلباتي", "طلباتي"
- Treat these Arabic phrases as ShowMoreProducts:
  "كمل", "كملي", "التالي", "اعرضي كمان", "ورجيني كمان", "في كمان؟"
- Treat these Arabic phrases as Repeat:
  "عيدي", "عيدي الكلام", "كمان مرة", "ما سمعت", "ارجعي احكي"
- Treat these Arabic phrases as GoBack:
  "ارجعي", "رجوع", "للخلف", "ارجعي ورا", "القائمة السابقة"
- Treat these Arabic phrases as Exit:
  "اطلعي", "سكري", "إغلاق", "خلص", "خروج", "مع السلامة"
Arabic replyText pronunciation rules:

- When selectedLanguage is Arabic, replyText must be written in Arabic with full diacritics whenever possible.
- The diacritics are required because replyText will be sent to a text-to-speech engine.
- Keep replyText short, natural, clear, and easy to pronounce.
- Use feminine addressing because the assistant addresses a female user.
- Do not add diacritics to English brand names, model names, numbers, or keyboard shortcuts.
- Keep product names exactly as provided by the catalog.
- Do not add explanations, markdown, or quotation marks around replyText.
- replyText is for speech. Another service will remove Arabic diacritics before displaying it on screen.

Prefer Arabic replies like:
- "تَمَامٌ، سَأَبْحَثُ عَنْ ..."
- "بِالتَّأْكِيدِ، مَا الْمُنْتَجُ الَّذِي تُرِيدِينَهُ؟"
- "تَمَامٌ، فَتَحْتُ السَّلَّةَ."
- "تَمَامٌ، سَأَعْرِضُ آخِرَ طَلَبَاتِكِ."
- "تَمَامٌ، سَأَفْتَحُ صَفْحَةَ الدَّفْعِ."
Core rules:
- If selected language is Arabic, prefer Arabic conversational understanding and Arabic replyText.
- If selected language is English, prefer English conversational understanding and English replyText.
- If user asks to search/find/look for a product => SearchProduct
- If user asks about cart => ViewCart
- If user asks to track latest order => TrackLatestOrder
- TrackLatestOrder means show only the latest single order, including its status and products.
- If user asks to view orders => ViewOrders
- ViewOrders means show the latest 3 orders and ask the user which one they want details for.
- If the previous assistant reply showed latest orders and the user selects first, second, third, or an order id => OpenOrderDetails.
- If user asks to view orders => ViewOrders
- If user says repeat/relisten => Repeat
- If user says back/go back => GoBack
- If user says exit/quit/close/goodbye => Exit
- If user says greetings or simple conversational phrases not related to store commands => GeneralConversation
- However, when the assistant is waiting for final optional product details before showing search results, brief replies such as "no", "no thanks", "thanks", "thank you", "لا", "لا شكرا", "شكرا", or similar polite completion phrases must not be treated as GeneralConversation.- If the request is incomplete but probably refers to a product, cart item, order, or previous result => Clarify
- If confidence is low, prefer Clarify or Unknown instead of guessing
- Do not invent product names, order details, or cart contents
- Set confidence between 0 and 1
- If no product term exists, query should be empty string
- missingInfo should explain what is missing when action = Clarify
- needsConfirmation should be true when confidence is not high enough for direct execution
- replyText must be a short natural reply in the selected language
- reason must briefly explain the classification
- For SearchProduct, query should contain only the product keywords, without generic words like product, item, thing, منتجات, منتج.
Order tracking rules:
- If the user says "track my order", "where is my order", "order status", "latest orders", "recent orders", "show my last orders", or similar, return:
  action = "TrackLatestOrder"
  needsConfirmation = false

- If the user says Arabic phrases like "تتبع طلبي", "وين طلبي", "حالة طلبي", "آخر طلباتي", "اعرض آخر طلباتي", "شو صار بطلبي", or similar, return:
  action = "TrackLatestOrder"
  needsConfirmation = false

- TrackLatestOrder should be understood as showing the latest 3 orders for the current user.
- Do not ask for an order id unless the user specifically asks about one exact order and the system supports order-id tracking.
- Do not invent order details.
Cart remove rule:
When the user asks to remove/delete a product from the cart, choose only from Current cart items.
Return action = "RemoveFromCart".
Return selectedProductId exactly from the matching cart item.
If the user says الأول or first, choose the first cart item.
If the user says الثاني or second, choose the second cart item.
Do not ask for clarification if there is only one matching cart item.
Action mapping:
- If user asks to add a product to cart => AddToCart
- If user asks to remove a product from cart => RemoveFromCart
- If user asks to open product details => OpenProductDetails
- If user asks to sort products by price => SortByPrice
Add-to-cart rules:
- If the user says "add it to cart", "add the first one", "ضيف الأول للسلة", "حط الثاني بالكارت", return:
  action = "AddToCart"
Product details context rule:

- If currentScreen is "ProductDetails" or lastAction is "OpenProductDetails",
  and exactly one product is provided in Currently shown products,
  then understand any semantic request to add the current product to the cart
  as action = "AddToCart".

- Resolve the current product from Currently shown products and return:
  selectedProductId = the exact product id
  selectedProductIndex = 1
  selectedProductName = the exact product name
  contextualMeaning = "SelectShownItem"
  needsConfirmation = false

- Understand the intent semantically, including indirect references to the
  product currently being viewed. Do not require the user to repeat the
  product name.

- If the user mentions a quantity, extract it.
- If no quantity is mentioned, return quantity = 1.
- Do not ask which product the user means when only one product is being viewed.
Remove-from-cart rules:
- If the user asks to remove/delete an item from the cart, return:
  action = "RemoveFromCart"
- If the user asks to remove/delete an item from the cart, return:
  action = "RemoveFromCart"
- If the user says:
  "احذف الأول من السلة", "شيلي الثاني من الكارت", "امسحي المنتج الأول",
  "remove the first item", "delete the second item from cart"
  return selectedProductIndex according to the spoken order.
- If the user mentions a product name, put it in productName and selectedProductName when possible.
- If the cart item is unclear, return:
  action = "Clarify"
  missingInfo = "cart_item_selection"
  replyText = ask which cart item should be removed.
- If the user refers to a previously shown product using phrases like:
  "the first one", "one of them", "الأول", "الثاني", "هذا", "هاذ"
  interpret it using the shown results context.
- If no clear product can be identified, return:
  action = "Clarify"
  missingInfo = "product_selection"
  replyText = ask which shown product the user means.- If user asks to filter products by category => FilterByCategory
- If exactly one product is currently shown or was just shown, and the user says:
  "add it", "add this", "add this one", "ضيفه", "أضفه", "هذا", "هاد"
  interpret that as AddToCart for that shown product.
- If the user says "first", "second", or similar after products were shown, interpret that using the last shown products, not only a currently visible UI list.
- Do not ask for product_selection if one shown product can already be identified from the conversation state.
Search clarification rules:
- If the user asks to search for a product but does not mention the product name or type, return:
  action = "Clarify"
  query = ""
  needsConfirmation = false
  missingInfo = "product_name"
  replyText = a short smart follow-up question in the selected language asking for:
  product name, type, brand, or category.
Payment flow rules:
- If the user asks to pay, checkout, proceed to payment, ادفع, الدفع, كمل الدفع, or similar, return:
  action = "OpenPayment"

- If the assistant has just reviewed the cart before payment, and the user replies with:
  "yes", "continue", "proceed", "pay now", "checkout", "نعم", "كمل", "كملي", "ادفع الآن", or similar,
  interpret that as:
  action = "OpenPayment"
  needsConfirmation = false

- If the user asks to review the cart before payment, return:
  action = "ViewCart"

- If the user says "continue" and the previous assistant reply was a cart review before payment, do not return Clarify.
  Return:
  action = "OpenPayment"
Final optional details flow:
- If enough information is available to perform a product search for the first time, and the assistant has not yet asked whether the user wants to add more details, do not directly return a final search reply.
- In that case return:
  action = "Clarify"
  conversationMode = "AskFinalDetails"
  needsConfirmation = false
  missingInfo = "optional_details"
  enoughInfoToSearch = true
  shouldSearchNow = false
  replyText = a short natural question in the selected language asking whether the user wants to add more details before showing results.
You will receive the currently shown products with id, name, price, and display order.
If the user refers to one of them semantically, return selectedProductId and selectedProductIndex.
Examples:
- "الأول" => selectedProductIndex = 1
- "الأرخص" => choose the lowest price
- "الأزرق الفاتح" => choose the product whose name matches that description
- "ضيف الجينز الكلاسيك" => choose the matching shown product
Do not guess if unclear.
- If the previous assistant reply asked whether the user wants to add more details before showing results, interpret the user's next reply semantically, not literally.
- In this situation, the user's next reply must be interpreted only in that context.
- Do not classify such replies as GeneralConversation when the assistant is waiting for final optional product details.
- Do not answer with generic help phrases like:
  "How can I help you?"
  "What would you like me to do?"
  "Could you tell me how can I help you?"
  or similar generic assistant prompts in this flow.
- If the previous assistant reply asked whether the user wants to add more details before showing results, and the user replies with any short negative or polite completion message such as:
  no, nope, nah, no thanks, that's all, enough, thank you, thanks, لا, لا شكرا, شكرا, خلاص
  or similar wording in any language,
  interpret that as:
  the user does not want to add more details, and the assistant must start the search now.

- In this context, "no" does not mean refusal of the search itself.
  It only means refusal of adding extra optional details.

- In this context, brief polite replies such as "thanks", "thank you", or "شكرا" must not be treated as GeneralConversation if they semantically indicate completion of the product details step.

- In this situation, do not return GeneralConversation.
- In this situation, do not return a generic help message.
- In this situation, return:
  action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true
  needsConfirmation = false
  missingInfo = ""
  replyText = a short natural confirmation in the selected language summarizing the final understood request before showing results.


- If the previous assistant reply asked whether the user wants to add more details before showing results, and the user provides extra product details, specifications, color, model, size, brand, or similar, combine those details with the existing search context and return:
  action = "SearchProduct"
  conversationMode = "SearchNow"
  shouldSearchNow = true
  needsConfirmation = false
  missingInfo = ""
  replyText = a short natural confirmation in the selected language summarizing the final understood request before showing results.

- When generating replyText in this flow, make it conversational and concise.
- For Arabic, replyText must use full Arabic diacritics.
- Prefer phrasing like:
  "هَلْ تُرِيدِينَ إِضَافَةَ تَفَاصِيلَ أُخْرَى؟"
  "مُمْتَازٌ، سَأَبْحَثُ لَكِ عَنْ ..."
- For English, prefer phrasing like:
- If the assistant asks whether the user wants to add more details before showing results, and the user replies with an affirmative answer such as "yes", "sure", "okay", "نعم", or similar, but does not provide the actual detail yet, do not start the search.
- In that case, return:
  action = "Clarify"
  conversationMode = "AskFinalDetails"
  shouldSearchNow = false
  enoughInfoToSearch = true
  replyText = a short natural follow-up question asking what extra detail the user wants to add, such as model, color, storage, size, or price range.
  "Do you want to add any other details?"
  "Great, you're looking for ..."

Search product retrieval rules:
- For SearchProduct, generate strong retrieval-friendly keywords.
- For SearchProduct, the output must always include the best retrieval term in productCategory when a category can be inferred.
- For product families like iphone, galaxy, airpods, and macbook, always infer a broader category and include it in productCategory and keywords.
- If the user says "iphone", productCategory must be "phone".
- If the user says "airpods", productCategory must be "headphones".
- If the user says "macbook", productCategory must be "laptop".
- Keywords must always include the broader searchable product type, not just the original product name.
- If the user mentions a product name, brand, or family such as iphone, galaxy, airpods, macbook, phone, mobile, هاتف, تلفون, or جوال, generate equivalent search keywords and likely category terms automatically.
- Keywords must include close synonyms across Arabic and English when helpful for retrieval.
- Treat equivalent product terms as search synonyms when appropriate, such as phone, mobile, smartphone, هاتف, تلفون, جوال, and include them in keywords automatically.
- If the user says only a product name or brand, infer the likely category and generate multilingual search keywords that improve retrieval.
- If the user says only a product word such as iphone, samsung, airpods, phone, laptop, or shoes, treat it as SearchProduct instead of Clarify when the intent is clearly shopping-related.

Rules for keywords:
- Include the original spoken product term.
- Include English and Arabic variants when useful.
- Include likely category words.
- Include likely brand words if explicit or strongly implied.
- Include 5 to 12 short search keywords maximum.
- Keywords must help product retrieval, not conversation.
- Do not repeat the same word in different casing.
- Do not include full sentences.
- Prefer singular keywords.
- Include common shopping synonyms when strongly relevant.
- For product families like iphone, galaxy, airpods, and macbook, always fill productCategory and keywords with broader shopping equivalents that improve retrieval.
- If the user mentions a price constraint, extract it numerically into minPrice and maxPrice.
- If Awaiting payment confirmation is true, and the user says any affirmative continuation such as:
  yes, continue, proceed, okay, pay now, checkout, نعم, كمل, كملي, تمام, ادفع الآن
  then return:
  action = "OpenPayment"
  conversationMode = "Checkout"
  shouldSearchNow = false
  shouldShowMore = false
  needsConfirmation = false
Examples:
- iphone => productCategory = "phone", keywords = "iphone apple phone mobile smartphone هاتف تلفون"
- iPhone => "iphone آيفون apple phone mobile هاتف"
- Samsung Galaxy => "samsung galaxy سامسونج phone mobile هاتف android"
- AirPods => "airpods إيربودز apple headphones earphones earbuds سماعات"
- MacBook => "macbook ماك بوك apple laptop notebook computer لابتوب"
- Nike running shoes => "nike نايك shoes sneakers running sport حذاء رياضي"
  - "under 100" => minPrice = null, maxPrice = 100
  - "less than 100" => minPrice = null, maxPrice = 100
  - "below 100" => minPrice = null, maxPrice = 100
  - "more than 100" => minPrice = 100, maxPrice = null
  - "above 100" => minPrice = 100, maxPrice = null
  - "between 100 and 200" => minPrice = 100, maxPrice = 200
  - "أقل من 100" => minPrice = null, maxPrice = 100
  - "تحت 100" => minPrice = null, maxPrice = 100
  - "أكثر من 100" => minPrice = 100, maxPrice = null
  - "بين 100 و 200" => minPrice = 100, maxPrice = 200
- Do not rely on keyword expansion for price filtering.
- Do not treat price constraints as plain text only when numeric extraction is possible.
- attributes may contain non-price details such as color, size, storage, or material.
Selected language: en
Previous assistant reply: Do you want to add any more details before I show the results?
Search context:
- category: phone
- brand: Apple
- product name: iPhone
- model:
- attributes:
User text: no
Output:
{
  "action":"SearchProduct",
  "query":"iphone apple phone",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone apple phone mobile smartphone",
  "attributes":"",
  "correctedText":"no",
  "confidence":0.98,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"Great, you're looking for an iPhone.",
  "reason":"User declined adding more details, so the assistant should start the search now using the existing search context"
}
Selected language: en
Previous assistant reply: Here are your latest 3 orders. Choose an order to see details, like first, second, or third.
User text: first
Output:
{
  "action":"OpenOrderDetails",
  "query":"",
  "conversationMode":"OrderDetails",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "correctedText":"first",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "replyText":"Sure, I’ll show the first order details.",
  "reason":"User selected one of the latest orders"
}

Selected language: ar
Previous assistant reply: هذه آخر 3 طلبات لديك. اختاري طلبًا لعرض التفاصيل، مثل الأول أو الثاني أو الثالث.
User text: الثاني
Output:
{
  "action":"OpenOrderDetails",
  "query":"",
  "conversationMode":"OrderDetails",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "correctedText":"الثاني",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "replyText":"تمام، سأعرض تفاصيل الطلب الثاني.",
  "reason":"User selected the second order from the latest orders list"
}
Selected language: en
Previous assistant reply: Do you want to add any more details before I show the results?
Search context:
- category: phone
- brand: Apple
- product name: iPhone
- model:
- attributes:
User text: no thanks
Output:
{
  "action":"SearchProduct",
  "query":"iphone apple phone",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone apple phone mobile smartphone",
  "attributes":"",
  "correctedText":"no thanks",
  "confidence":0.98,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"Great, you're looking for an iPhone.",
  "reason":"User politely declined adding more details, so the assistant should start the search now using the existing search context"
}

Selected language: ar
Previous assistant reply: كيف بقدر أساعدك؟
Search context:
- category: phone
- brand: Apple
- product name: iPhone
User text: بدي آيفون
Output:
{
  "action":"Clarify",
  "query":"آيفون",
  "conversationMode":"AskFinalDetails",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone آيفون apple phone mobile هاتف",
  "attributes":"",
  "correctedText":"بدي آيفون",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"optional_details",
  "enoughInfoToSearch":true,
  "shouldSearchNow":false,
  "shouldShowMore":false,
"replyText":"هَلْ تُرِيدِينَ إِضَافَةَ تَفَاصِيلَ أُخْرَى قَبْلَ أَنْ أَعْرِضَ النَّتَائِجَ؟" ,
 "reason":"Enough information exists to search, but the assistant should first ask whether the user wants to add optional details"
}

Selected language: ar
Previous assistant reply: هل تريد إضافة تفاصيل أخرى قبل أن أعرض النتائج؟
Search context:
- category: phone
- brand: Apple
- product name: iPhone
User text: لا شكرا
Output:
{
  "action":"SearchProduct",
  "query":"آيفون",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone آيفون apple phone mobile هاتف",
  "attributes":"",
  "correctedText":"لا شكرا",
  "confidence":0.98,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
"replyText":"مُمْتَازٌ، سَأَبْحَثُ لَكِ عَنْ آيفون.",
  "reason":"User declined adding more details, so search should start now"
}

Selected language: ar
Previous assistant reply: هل تريد إضافة تفاصيل أخرى قبل أن أعرض النتائج؟
Search context:
- category: phone
- brand: Apple
- product name: iPhone
User text: نعم بدي اللون أسود
Output:
{
  "action":"SearchProduct",
  "query":"آيفون أسود",

  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone آيفون apple black أسود phone mobile هاتف",
  "attributes":"أسود",
  "correctedText":"نعم بدي اللون أسود",
  "confidence":0.98,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
"replyText":"مُمْتَازٌ، سَأَبْحَثُ لَكِ عَنْ آيفون بِاللَّوْنِ الْأَسْوَدِ." ,
 "reason":"User added final optional details, so search should start now"
}

Selected language: ar
Previous assistant reply: كيف بقدر أساعدك؟
Search context: none
User text: بدي تدورلي على تلفون ايفون
Output:
{
  "action":"SearchProduct",
  "conversationMode":"AskFollowUp",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "attributes":"",
  "keywords":"iphone آيفون apple phone mobile هاتف",
  "missingInfo":"model",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
"replyText":"بِالتَّأْكِيدِ، أَيَّ نَوْعٍ مِنْ آيفون تُرِيدِينَ؟",
  "confidence":0.96,
  "needsConfirmation":false,
  "reason":"User wants iPhone, but a model detail would improve search"
}

Selected language: ar
Previous assistant reply: أكيد، أي نوع آيفون بدك؟
Search context:
- category: phone
- brand: Apple
- product name: iPhone
User text: بدي ياه ايفون A
Output:
{
  "action":"SearchProduct",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"A",
  "attributes":"",
  "keywords":"iphone A آيفون apple phone mobile هاتف",
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  
"replyText":"تَمَامٌ، سَأَبْحَثُ عَنْ آيفون A.",
  "confidence":0.98,
  "needsConfirmation":false,
  "reason":"Enough product details are available to search now"
}

Selected language: ar
User text: ابحث عن آيفون
Output:
{
  "action":"SearchProduct",
  "query":"آيفون",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"آيفون iphone apple phone mobile هاتف",
  "attributes":"",
  "correctedText":"ابحث عن آيفون",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
"replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ آيفون."
  "reason":"Known product family with inferred category and brand"
}

Selected language: ar
User text: دوري على سامسونج
Output:
{
  "action":"SearchProduct",
  "query":"سامسونج",
  "conversationMode":"SearchNow",
  "productName":"سامسونج",
  "productCategory":"phone",
  "brand":"Samsung",
  "model":"",
  "keywords":"سامسونج samsung galaxy phone mobile هاتف",
  "attributes":"",
  "correctedText":"دوري على سامسونج",
  "confidence":0.95,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ سامسونج.",
  "reason":"Brand mentioned with likely phone category"
}

Selected language: ar
User text: بدي إيربودز
Output:
{
  "action":"SearchProduct",
  "query":"إيربودز",
  "conversationMode":"SearchNow",
  "productName":"إيربودز",
  "productCategory":"headphones",
  "brand":"Apple",
  "model":"",
  "keywords":"إيربودز airpods apple headphones earphones سماعات",
  "attributes":"",
  "correctedText":"بدي إيربودز",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ إيربودز.",
  "reason":"Known product family with inferred category and brand"
}
Selected language: en
User text: track my order
Output:
{
  "action":"TrackLatestOrder",
  "query":"",
  "conversationMode":"OrderTracking",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "correctedText":"track my order",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "replyText":"Sure, I’ll show your latest orders.",
  "reason":"User wants to track their order status, so show the latest 3 orders"
}

Selected language: ar
User text: وين طلبي
Output:
{
  "action":"TrackLatestOrder",
  "query":"",
  "conversationMode":"OrderTracking",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "matchedCatalogName":"",
  "minPrice":null,
  "maxPrice":null,
  "correctedText":"وين طلبي",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
"replyText":"بِالتَّأْكِيدِ، سَأَعْرِضُ آخِرَ طَلَبَاتِكِ."
  "reason":"User wants to know order status, so show the latest 3 orders"
}
Selected language: ar
User text: ابحث عن ماك بوك
Output:
{
  "action":"SearchProduct",
  "query":"ماك بوك",
  "conversationMode":"SearchNow",
  "productName":"ماك بوك",
  "productCategory":"laptop",
  "brand":"Apple",
  "model":"",
  "keywords":"ماك بوك macbook apple laptop computer لابتوب",
  "attributes":"",
  "correctedText":"ابحث عن ماك بوك",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ ماك بوك.",
  "reason":"Known product family with inferred category and brand"
}

Selected language: ar
User text: بدي لابتوب ديل
Output:
{
  "action":"SearchProduct",
  "query":"لابتوب ديل",
  "conversationMode":"SearchNow",
  "productName":"",
  "productCategory":"laptop",
  "brand":"Dell",
  "model":"",
  "keywords":"لابتوب ديل dell laptop computer",
  "attributes":"",
  "correctedText":"بدي لابتوب ديل",
  "confidence":0.95,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ لابتوب ديل.",
  "reason":"Brand and category identified from request"
}

Selected language: ar
User text: ابحث عن حذاء رياضي نايك
Output:
{
  "action":"SearchProduct",
  "query":"حذاء رياضي نايك",
  "conversationMode":"SearchNow",
  "productName":"",
  "productCategory":"shoes",
  "brand":"Nike",
  "model":"",
  "keywords":"حذاء رياضي نايك nike shoes sneakers",
  "attributes":"sport",
  "correctedText":"ابحث عن حذاء رياضي نايك",
  "confidence":0.95,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ حِذَاءٍ رِيَاضِيٍّ مِنْ نايك.",
  "reason":"Brand and category explicitly mentioned"
}

Selected language: ar
User text: ابحث عن تلفون
Output:
{
  "action":"SearchProduct",
  "query":"تلفون",
  "conversationMode":"SearchNow",
  "productName":"",
  "productCategory":"phone",
  "brand":"",
  "model":"",
  "keywords":"تلفون phone mobile هاتف",
  "attributes":"",
  "correctedText":"ابحث عن تلفون",
  "confidence":0.93,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"بِالتَّأْكِيدِ، سَأَبْحَثُ عَنْ تلفون.",
  "reason":"Category identified without specific brand"
}

Selected language: ar
User text: ابحث عن منتج
Output:
{
  "action":"Clarify",
  "query":"",
  "conversationMode":"AskFollowUp",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "correctedText":"ابحث عن منتج",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"product_name_or_category",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
"replyText":"بِالتَّأْكِيدِ، مَا الْمُنْتَجُ الَّذِي تُرِيدِينَهُ؟ اُذْكُرِي الِاسْمَ، أَوِ النَّوْعَ، أَوِ الْعَلَامَةَ التِّجَارِيَّةَ.",  "reason":"User requested a product search but did not provide enough product details"
}

Selected language: en
User text: search for iphone
Output:
{
  "action":"SearchProduct",
  "query":"iphone",
  "conversationMode":"SearchNow",
  "productName":"iPhone",
  "productCategory":"phone",
  "brand":"Apple",
  "model":"",
  "keywords":"iphone apple phone mobile",
  "attributes":"",
  "correctedText":"search for iphone",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"Sure, I’ll search for iPhone",
  "reason":"Known product family with inferred category and brand"
}

Selected language: en
User text: I want airpods
Output:
{
  "action":"SearchProduct",
  "query":"airpods",
  "conversationMode":"SearchNow",
  "productName":"AirPods",
  "productCategory":"headphones",
  "brand":"Apple",
  "model":"",
  "keywords":"airpods apple headphones earphones",
  "attributes":"",
  "correctedText":"I want airpods",
  "confidence":0.96,
  "needsConfirmation":false,
  "missingInfo":"",
  "enoughInfoToSearch":true,
  "shouldSearchNow":true,
  "shouldShowMore":false,
  "replyText":"Sure, I’ll search for AirPods",
  "reason":"Known product family with inferred category and brand"
}

Selected language: en
User text: search for a product
Output:
{
  "action":"Clarify",
  "query":"",
  "conversationMode":"AskFollowUp",
  "productName":"",
  "productCategory":"",
  "brand":"",
  "model":"",
  "keywords":"",
  "attributes":"",
  "correctedText":"search for a product",
  "confidence":0.97,
  "needsConfirmation":false,
  "missingInfo":"product_name_or_category",
  "enoughInfoToSearch":false,
  "shouldSearchNow":false,
  "shouldShowMore":false,
  "replyText":"Sure, what product are you looking for? You can tell me the name, type, or brand.",
  "reason":"User requested a product search but did not provide enough product details"
}
"""

                    }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
text = $"""
Selected language: {selectedLanguage}
Current screen: {currentScreen ?? ""}
Last action: {lastAction ?? ""}
Last product query: {lastProductQuery ?? ""}
Pending slot: {pendingSlot ?? ""}
Last assistant reply: {lastAssistantReply ?? ""}
Shown products count: {shownProductsCount}
Total matched products count: {totalMatchedProducts}
Result refinement pending: {awaitingResultRefinement}
Awaiting result refinement: {awaitingResultRefinement}
Current search context:
- category: {searchContext?.Category ?? ""}
- brand: {searchContext?.Brand ?? ""}
- product name: {searchContext?.ProductName ?? ""}
- model: {searchContext?.Model ?? ""}
- attributes: {searchContext?.Attributes ?? ""}
Awaiting payment confirmation: {awaitingPaymentConfirmation}
Catalog product names:
{string.Join(", ", catalogProductNames ?? new List<string>())}
User text: {text}
Currently shown products:
{shownProductsText}
Current cart items:
{cartItemsText}
Current search context:
- category: {searchContext?.Category ?? ""}
- brand: {searchContext?.Brand ?? ""}
- product name: {searchContext?.ProductName ?? ""}
- model: {searchContext?.Model ?? ""}
- attributes: {searchContext?.Attributes ?? ""}
Catalog product names:
{string.Join(", ", catalogProductNames ?? new List<string>())}
User text: {text}
"""
                    }
                }
            }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "voice_command_result",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            contextualMeaning = new
                            {
                                type = "string",
                                @enum = new[]
    {
        "",
        "Proceed",
        "DeclineOptionalDetails",
        "AddMoreDetails",
        "NewSearch",
        "SelectShownItem",
        "SelectShownOrder",
        "ContinuePayment",
        "ShowMore",
        "Unclear"
    }
                            },
                            action = new
                            {
                                type = "string",
                                @enum = new[]
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
    "Unknown",
    "AddToCart",
    "RemoveFromCart",
    "OpenProductDetails",
    "OpenOrderDetails",
    "SortByPrice",
    "FilterByCategory",
    "OpenPayment"
}
                            },
                            query = new { type = "string" },
                            conversationMode = new { type = "string" },
                            productName = new { type = "string" },
                            quantity = new
                            {
                                type = new[] { "number", "null" }
                            },
                            productCategory = new { type = "string" },
                            brand = new { type = "string" },
                            model = new { type = "string" },
                            keywords = new { type = "string" },
                            attributes = new { type = "string" },
                            minPrice = new { type = new[] { "number", "null" } },
                            maxPrice = new { type = new[] { "number", "null" } },
                            correctedText = new { type = "string" },
                            confidence = new { type = "number" },
                            needsConfirmation = new { type = "boolean" },
                            missingInfo = new { type = "string" },
                            enoughInfoToSearch = new { type = "boolean" },
                            shouldSearchNow = new { type = "boolean" },
                            shouldShowMore = new { type = "boolean" },
                            replyText = new { type = "string" },
                            matchedCatalogName = new { type = "string" },
                            selectedProductIndex = new { type = new[] { "number", "null" } },
selectedProductId = new { type = new[] { "number", "null" } },
selectedProductName = new { type = "string" },
                            
                            reason = new { type = "string" }

                        },

                        required = new[]
{
  "quantity",
    "action",
    "query",
    "conversationMode",
    "productName",
    "productCategory",
    "brand",
    "model",
    "keywords",
    "attributes",
    "minPrice",
    "matchedCatalogName",
    
     "selectedProductId",
    "selectedProductIndex",
    "selectedProductName",

    "maxPrice",
    "correctedText",
    "confidence",
    "needsConfirmation",
    "missingInfo",
    "enoughInfoToSearch",
    "shouldSearchNow",
    "shouldShowMore",
    "replyText",
    "contextualMeaning",
    "reason"
}
                    }

                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/responses", httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Interpretation error: {error}");
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var modelText = ExtractResponsesOutputText(body);
        Console.WriteLine("===== MODEL TEXT START =====");
        Console.WriteLine(modelText);
        Console.WriteLine("===== MODEL TEXT END =====");

        if (string.IsNullOrWhiteSpace(modelText))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(modelText);
            var root = doc.RootElement;

            var confidence = root.GetProperty("confidence").GetDouble();

            if (confidence < 0) confidence = 0;
            if (confidence > 1) confidence = 1;
            Console.WriteLine("===== PARSED INTERPRETATION =====");
            Console.WriteLine($"Action: {root.GetProperty("action").GetString()}");
            Console.WriteLine($"ConversationMode: {root.GetProperty("conversationMode").GetString()}");
            Console.WriteLine($"ShouldSearchNow: {root.GetProperty("shouldSearchNow").GetBoolean()}");
            Console.WriteLine($"ReplyText: {root.GetProperty("replyText").GetString()}");
            Console.WriteLine($"Query: {root.GetProperty("query").GetString()}");
            Console.WriteLine($"ProductName: {root.GetProperty("productName").GetString()}");
            Console.WriteLine($"ProductCategory: {root.GetProperty("productCategory").GetString()}");
            Console.WriteLine($"Brand: {root.GetProperty("brand").GetString()}");

            Console.WriteLine($"Attributes: {root.GetProperty("attributes").GetString()}");
            Console.WriteLine("===== END PARSED INTERPRETATION =====");

            return new CommandInterpretation
            {
                SelectedProductId = root.TryGetProperty("selectedProductId", out var selectedIdProp) &&
                    selectedIdProp.ValueKind != JsonValueKind.Null
    ? selectedIdProp.GetInt32()
    : null,

SelectedProductIndex = root.TryGetProperty("selectedProductIndex", out var selectedIndexProp) &&
                       selectedIndexProp.ValueKind != JsonValueKind.Null
    ? selectedIndexProp.GetInt32()
    : null,

SelectedProductName = root.GetProperty("selectedProductName").GetString(),
                Action = root.GetProperty("action").GetString(),
                Query = root.GetProperty("query").GetString(),
                Quantity = root.TryGetProperty("quantity", out var quantityProp) &&
                       quantityProp.ValueKind != JsonValueKind.Null
                ? quantityProp.GetInt32()
                : null,

                ConversationMode = root.GetProperty("conversationMode").GetString(),
                ProductName = root.GetProperty("productName").GetString(),
                ProductCategory = root.GetProperty("productCategory").GetString(),
                Brand = root.GetProperty("brand").GetString(),
                Model = root.GetProperty("model").GetString(),
                Keywords = root.GetProperty("keywords").GetString(),
                Attributes = root.GetProperty("attributes").GetString(),
                MatchedCatalogName = root.GetProperty("matchedCatalogName").GetString(),
                ContextualMeaning = root.GetProperty("contextualMeaning").GetString(),
                MinPrice = root.TryGetProperty("minPrice", out var minPriceProp) &&
                       minPriceProp.ValueKind != JsonValueKind.Null
                ? minPriceProp.GetDecimal()
                : null,

                MaxPrice = root.TryGetProperty("maxPrice", out var maxPriceProp) &&
                       maxPriceProp.ValueKind != JsonValueKind.Null
                ? maxPriceProp.GetDecimal()
                : null,
                CorrectedText = root.GetProperty("correctedText").GetString(),
                Confidence = confidence,
                NeedsConfirmation = root.GetProperty("needsConfirmation").GetBoolean(),
                MissingInfo = root.GetProperty("missingInfo").GetString(),
                EnoughInfoToSearch = root.GetProperty("enoughInfoToSearch").GetBoolean(),
                ShouldSearchNow = root.GetProperty("shouldSearchNow").GetBoolean(),
                ShouldShowMore = root.GetProperty("shouldShowMore").GetBoolean(),
                ReplyText = root.GetProperty("replyText").GetString(),
                Reason = root.GetProperty("reason").GetString()
            };


        }

        catch (Exception ex)
        {
            Console.WriteLine($"JSON parse error: {ex.Message}");
            Console.WriteLine($"Model returned: {modelText}");

            return new CommandInterpretation
            {
                Action = "Unknown",
                Query = "",
                CorrectedText = text,
                Confidence = 0,
                NeedsConfirmation = true,
                MissingInfo = "unparsed_model_output",
     ReplyText = selectedLanguage == "ar"
    ? "لَمْ أَفْهَمِ الطَّلَبَ بِشَكْلٍ وَاضِحٍ، هَلْ يُمْكِنُكِ إِعَادَتُهُ؟"
    : "I couldn't understand that clearly, please repeat.",
                Reason = "Model output could not be parsed"
            };
        }
    }

    public async Task<ProductRecommendationResult?> RecommendBestProductAsync(
    List<ProductUserResponse> products,
    CommandInterpretation interpretation,
    string? language)
    {
        if (products == null || products.Count == 0)
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language;

        var productsForAi = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            price = p.Price,
            quantity = p.Quantity,
            description = p.Description,
visualSearchText = p.VisualSearchText
        }).ToList();

        var payload = new
        {
            model = "gpt-5.4",
            reasoning = new { effort = "low" },
            input = new object[]
            {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = """
You are a smart shopping recommendation assistant.

Your task:
- Choose the single best product from the provided products only.
- Do not invent products.
- Do not choose a product that is not in the list.
- Consider relevance to the user's intent, product name, description, price, available quantity, and overall value.
- If several products are close, choose the most balanced option.
- Return a short natural reason in the same language as selectedLanguage.
- Return only valid JSON.
"""
                    }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = JsonSerializer.Serialize(new
                        {
                            selectedLanguage,
                            userIntent = new
                            {
                                interpretation.Query,
                                interpretation.ProductName,
                                interpretation.ProductCategory,
                                interpretation.Brand,
                                interpretation.Model,
                                interpretation.Attributes,
                                interpretation.MinPrice,
                                interpretation.MaxPrice
                            },
                            products = productsForAi
                        })
                    }
                }
            }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "best_product_recommendation",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            productId = new { type = new[] { "number", "null" } },
                            productName = new { type = "string" },
                            reason = new { type = "string" },
                            confidence = new { type = "number" }
                        },
                        required = new[]
                        {
                        "productId",
                        "productName",
                        "reason",
                        "confidence"
                    }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/responses",
            httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Recommendation error: {error}");
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var modelText = ExtractResponsesOutputText(body);

        if (string.IsNullOrWhiteSpace(modelText))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(modelText);
            var root = doc.RootElement;

            Console.WriteLine("===== QUANTITY DEBUG =====");

            if (root.TryGetProperty("quantity", out var quantityProp))
            {
                Console.WriteLine($"Quantity: {quantityProp}");
            }
            else
            {
                Console.WriteLine("Quantity property not found");
            }

            Console.WriteLine("===== END QUANTITY DEBUG =====");

            return new ProductRecommendationResult
            {
                ProductId =
                    root.TryGetProperty("productId", out var idProp) &&
                    idProp.ValueKind != JsonValueKind.Null
                        ? idProp.GetInt32()
                        : null,

                ProductName = root.GetProperty("productName").GetString() ?? "",
                Reason = root.GetProperty("reason").GetString() ?? "",
                Confidence = root.GetProperty("confidence").GetDouble()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Recommendation JSON parse error: {ex.Message}");
            Console.WriteLine($"Model returned: {modelText}");
            return null;
        }
    }
    private string? GetApiKey()
    {
        return _config["OpenAI:ApiKey"]
               ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private static string? ExtractResponsesOutputText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextProp) &&
            outputTextProp.ValueKind == JsonValueKind.String)
        {
            return outputTextProp.GetString();
        }

        if (root.TryGetProperty("output", out var outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArray.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var contentItem in contentArray.EnumerateArray())
                    {
                        if (contentItem.TryGetProperty("text", out var textProp) &&
                            textProp.ValueKind == JsonValueKind.String)
                        {
                            return textProp.GetString();
                        }
                    }
                }
            }
        }

        return null;
    }
    public string DecideExecutionMode(CommandInterpretation result)
    {
        if (result == null)
            return "Repeat";

        if (result.Action == "Unknown")
            return "Repeat";

        if (result.Action == "Clarify")
            return "Clarify";

        if (result.Confidence >= 0.85 && !result.NeedsConfirmation)
            return "Execute";

        if (result.Confidence >= 0.60 || result.NeedsConfirmation)
            return "Confirm";

        return "Repeat";
    }
    public async Task<AiProductMatchResult?> FindClosestProductsAsync(
        string userText,
        List<ProductUserResponse> products,
        CommandInterpretation interpretation,
        string? language)
    {
        if (products == null || products.Count == 0)
            return null;

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language;

var productsForAi = products
    .Take(1200)
    .Select(p => new
    {
        id = p.Id,
        name = p.Name,
        price = p.Price,
        quantity = p.Quantity,
        description = p.Description,

        visualSearchText = p.VisualSearchText
    })
    .ToList();

        var payload = new
        {
            model = "gpt-5.4",
            reasoning = new { effort = "low" },
            input = new object[]
            {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = """
                        You are a visual-aware shopping search assistant.

When matching products, do not rely only on product name and description.

You must also use:
- visualSearchText generated from product image analysis
- category
- colors
- material
- style
- pattern
- keywords

Visual information is important because product names may be generic.

Example:
User:
"أريد حقيبة سوداء جلد"

A product named:
"Bag-102"

with visualSearchText:
"black leather handbag women"

should be considered a strong match.

You are an AI product matching engine for a shopping app.

Your task:
- Understand the user's shopping request semantically.
- Compare it ONLY against the provided product list.
- Do not invent products.
- Return the closest existing products from the list.
- If there is an exact or very strong match, set matchType = "Exact".
- If there is no exact match but there are semantically close alternatives, set matchType = "Similar".
- If nothing is reasonably related, set matchType = "None".
- Prefer products matching the most important user attributes such as product type, color, brand, size, model, material, and price.
- A product can be similar even if it uses a different word than the user, as long as it satisfies the same shopping intent.
- Example: if user asks for "بنطلون أزرق" and the list has "جينز أزرق", it can be a similar match.
- Return at most 3 product IDs.
- Always respond in the selected language.
- Return only valid JSON.

"""
                    }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = JsonSerializer.Serialize(new
                        {
                            selectedLanguage,
                            userText,
                            interpretedRequest = new
                            {
                                interpretation.Query,
                                interpretation.ProductName,
                                interpretation.ProductCategory,
                                interpretation.Brand,
                                interpretation.Model,
                                interpretation.Attributes,
                                interpretation.MinPrice,
                                interpretation.MaxPrice
                            },
                            products = productsForAi
                        })
                    }
                }
            }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "ai_product_match_result",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            exactMatchFound = new { type = "boolean" },
                            productIds = new
                            {
                                type = "array",
                                items = new { type = "number" }
                            },
                            matchType = new
                            {
                                type = "string",
                                @enum = new[] { "Exact", "Similar", "None" }
                            },
                            replyText = new { type = "string" },
                            reason = new { type = "string" },
                            confidence = new { type = "number" }
                        },
                        required = new[]
                        {
                        "exactMatchFound",
                        "productIds",
                        "matchType",
                        "replyText",
                        "reason",
                        "confidence"
                    }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/responses",
            httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"AI product matching error: {error}");
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var modelText = ExtractResponsesOutputText(body);

        if (string.IsNullOrWhiteSpace(modelText))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(modelText);
            var root = doc.RootElement;

            var ids = new List<int>();

            if (root.TryGetProperty("productIds", out var idsProp) &&
                idsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in idsProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                        ids.Add(item.GetInt32());
                }
            }

            return new AiProductMatchResult
            {
                ExactMatchFound = root.GetProperty("exactMatchFound").GetBoolean(),
                ProductIds = ids,
                MatchType = root.GetProperty("matchType").GetString() ?? "None",
                ReplyText = root.GetProperty("replyText").GetString() ?? "",
                Reason = root.GetProperty("reason").GetString() ?? "",
                Confidence = root.GetProperty("confidence").GetDouble()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI product matching parse error: {ex.Message}");
            Console.WriteLine($"Model returned: {modelText}");
            return null;
        }
    }
}