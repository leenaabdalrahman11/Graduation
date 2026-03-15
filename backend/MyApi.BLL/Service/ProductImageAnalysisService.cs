using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyApi.DAL.DTO.Response;

namespace MyApi.BLL.Service;

public class ProductImageAnalysisService : IProductImageAnalysisService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductImageAnalysisService> _logger;

    public ProductImageAnalysisService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<ProductImageAnalysisService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProductImageAnalysisResult?> AnalyzeAsync(
        IFormFile image,
        CancellationToken cancellationToken = default)
    {
        if (image == null || image.Length == 0)
            return null;

        if (string.IsNullOrWhiteSpace(image.ContentType) ||
            !image.ContentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "The uploaded file is not an image: {ContentType}",
                image.ContentType);

            return null;
        }

        var apiKey =
            _config["OpenAI:ApiKey"] ??
            Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "OpenAI API key was not found."
    );
}

var model =
    _config["OpenAI:VisionModel"]
    ?? "gpt-4o-mini";
        try
        {
            var imageDataUrl = await ConvertImageToDataUrlAsync(
                image,
                cancellationToken);

            using var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,

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
You are a detailed visual product analyzer for an e-commerce search system.

Analyze the main product visible in the image carefully and generate highly detailed,
atomic search keywords in Arabic and English.

The keywords must describe every clearly visible and searchable characteristic.

Include when visibly supported:

1. Main product type:
   bracelet, necklace, shoes, bag, shirt, phone, watch, decoration, etc.

2. Product subtype:
   beaded bracelet, braided bracelet, sports shoes, shoulder bag, etc.

3. Visible components:
   beads, chain, stones, pearls, leaves, flowers, straps, buttons, zipper, buckle, etc.

4. Material or apparent material:
   metal, fabric, leather, plastic, glass, wood, beads, thread, etc.
   Only include material when visually reasonable.

5. Main and secondary colors:
   gold, silver, black, red, transparent, multicolor, etc.

6. Surface and finish:
   shiny, glossy, matte, metallic, reflective, transparent, sparkling, smooth, rough, etc.

7. Shape and construction:
   round, rectangular, thin, wide, layered, braided, twisted, woven,
   beaded, chained, adjustable, open, closed, etc.

8. Decorative details and motifs:
   leaf motif, floral design, geometric pattern, heart, star,
   engraved decoration, plant decoration, etc.

9. Style:
   elegant, casual, classic, modern, luxury, minimal, bohemian,
   sporty, feminine, handmade-looking, etc.

10. Search synonyms:
   Include common alternative shopping terms that users may naturally say.

Keyword rules:
- Return keywords as separate short tags, not sentences.
- Each keyword should normally contain between one and three words.
- Generate approximately 12 to 30 useful Arabic keywords.
- Generate approximately 12 to 30 useful English keywords.
- Do not repeat the same keyword.
- Include both general and specific terms.
- Do not invent a brand, model, size, price, age group, or hidden specification.
- Do not claim real gold, silver, diamonds, leather, or precious material unless certain.
- If an item only appears gold-colored, describe it as gold-colored or golden, not real gold.
- Describe only what is clearly or reasonably visible.
- If a detail is uncertain, omit it.
- Captions should be detailed but concise.

Example for an image of a golden braided beaded bracelet with leaf decorations:

Arabic keywords:
أسوارة، إكسسوار، أسوارة خرز، خرز دائري، لون ذهبي، ذهبي لامع،
مظهر معدني، بتلمع، مجدلة، نسيج مضفور، زخرفة ورق شجر،
ورق شجر ذهبي، زخرفة نباتية، تصميم أنيق، أسوارة نسائية، إكسسوار كاجوال

English keywords:
bracelet, accessory, beaded bracelet, round beads, golden color,
shiny, metallic appearance, sparkling, braided bracelet, woven design,
leaf decoration, golden leaves, botanical motif, elegant accessory,
feminine bracelet, casual jewelry

Return Arabic and English values that describe the same visible characteristics.
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
                                text = "Analyze this product image for visual search metadata."
                            },
                            new
                            {
                                type = "input_image",
                                image_url = imageDataUrl,
                                detail = "auto"
                            }
                        }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "product_visual_analysis",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,

                            properties = new
                            {
                                categoryAr = new
                                {
                                    type = "string"
                                },
                                categoryEn = new
                                {
                                    type = "string"
                                },
                                subCategoryAr = new
                                {
                                    type = "string"
                                },
                                subCategoryEn = new
                                {
                                    type = "string"
                                },
                                colorsAr = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "string"
                                    }
                                },
                                colorsEn = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "string"
                                    }
                                },
                                materialAr = new
                                {
                                    type = "string"
                                },
                                materialEn = new
                                {
                                    type = "string"
                                },
                                styleAr = new
                                {
                                    type = "string"
                                },
                                styleEn = new
                                {
                                    type = "string"
                                },
                                patternAr = new
                                {
                                    type = "string"
                                },
                                patternEn = new
                                {
                                    type = "string"
                                },
                                captionAr = new
                                {
                                    type = "string"
                                },
                                captionEn = new
                                {
                                    type = "string"
                                },
keywordsAr = new
{
    type = "array",
    items = new
    {
        type = "string"
    }
},
keywordsEn = new
{
    type = "array",
    items = new
    {
        type = "string"
    }
},
                                confidence = new
                                {
                                    type = "number",
                                    minimum = 0,
                                    maximum = 1
                                }
                            },

                            required = new[]
                            {
                                "categoryAr",
                                "categoryEn",
                                "subCategoryAr",
                                "subCategoryEn",
                                "colorsAr",
                                "colorsEn",
                                "materialAr",
                                "materialEn",
                                "styleAr",
                                "styleEn",
                                "patternAr",
                                "patternEn",
                                "captionAr",
                                "captionEn",
                                "keywordsAr",
                                "keywordsEn",
                                "confidence"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var httpContent = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                "https://api.openai.com/v1/responses",
                httpContent,
                cancellationToken);

if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadAsStringAsync(
        cancellationToken);

    _logger.LogError(
        "Product image analysis failed. Status: {Status}. Error: {Error}",
        response.StatusCode,
        error);

    throw new InvalidOperationException(
        $"OpenAI image analysis failed " +
        $"({(int)response.StatusCode}): {error}"
    );
}

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            var modelText = ExtractResponsesOutputText(responseBody);

            if (string.IsNullOrWhiteSpace(modelText))
                return null;

            using var document = JsonDocument.Parse(modelText);
            var root = document.RootElement;

            var confidence = root.GetProperty("confidence").GetDouble();

            return new ProductImageAnalysisResult
            {
                CategoryAr =
                    root.GetProperty("categoryAr").GetString() ?? "",

                CategoryEn =
                    root.GetProperty("categoryEn").GetString() ?? "",

                SubCategoryAr =
                    root.GetProperty("subCategoryAr").GetString() ?? "",

                SubCategoryEn =
                    root.GetProperty("subCategoryEn").GetString() ?? "",

                ColorsAr = ReadStringArray(root, "colorsAr"),
                ColorsEn = ReadStringArray(root, "colorsEn"),

                MaterialAr =
                    root.GetProperty("materialAr").GetString() ?? "",

                MaterialEn =
                    root.GetProperty("materialEn").GetString() ?? "",

                StyleAr =
                    root.GetProperty("styleAr").GetString() ?? "",

                StyleEn =
                    root.GetProperty("styleEn").GetString() ?? "",

                PatternAr =
                    root.GetProperty("patternAr").GetString() ?? "",

                PatternEn =
                    root.GetProperty("patternEn").GetString() ?? "",

                CaptionAr =
                    root.GetProperty("captionAr").GetString() ?? "",

                CaptionEn =
                    root.GetProperty("captionEn").GetString() ?? "",

KeywordsAr = ReadStringArray(root, "keywordsAr"),
KeywordsEn = ReadStringArray(root, "keywordsEn"),

                Confidence = Math.Clamp(confidence, 0, 1),

                ModelName = model
            };
        }
catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Unexpected error while analyzing product image.");

    throw;
}
    }

    private static async Task<string> ConvertImageToDataUrlAsync(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        await using var inputStream = image.OpenReadStream();
        using var memoryStream = new MemoryStream();

        await inputStream.CopyToAsync(
            memoryStream,
            cancellationToken);

        var base64 = Convert.ToBase64String(
            memoryStream.ToArray());

        var contentType = string.IsNullOrWhiteSpace(image.ContentType)
            ? "image/jpeg"
            : image.ContentType;

        return $"data:{contentType};base64,{base64}";
    }

    private static List<string> ReadStringArray(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return property
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ExtractResponsesOutputText(
        string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty(
                "output_text",
                out var outputTextProperty) &&
            outputTextProperty.ValueKind == JsonValueKind.String)
        {
            return outputTextProperty.GetString();
        }

        if (!root.TryGetProperty(
                "output",
                out var outputArray) ||
            outputArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty(
                    "content",
                    out var contentArray) ||
                contentArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (contentItem.TryGetProperty(
                        "text",
                        out var textProperty) &&
                    textProperty.ValueKind == JsonValueKind.String)
                {
                    return textProperty.GetString();
                }
            }
        }

        return null;
    }
}