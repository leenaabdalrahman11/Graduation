using Mapster;
using Microsoft.EntityFrameworkCore;
using MyApi.DAL.DTO.Requests;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using MyApi.DAL.Repository;
using Microsoft.AspNetCore.Http;
namespace MyApi.BLL.Service;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IFileService _fileService;
    
private readonly IProductImageAnalysisService _productImageAnalysisService;

public ProductService(
    IProductRepository productRepository,
    IFileService fileService,
    IProductImageAnalysisService productImageAnalysisService)
{
    _productRepository = productRepository;
    _fileService = fileService;
    _productImageAnalysisService = productImageAnalysisService;
}

    public Task CreateAsync(ProductRequest request)
    {
        throw new NotImplementedException();
    }

public async Task<ProductResponse> CreateProduct(
    ProductRequest request,
    string? userId)
{
    var product = request.Adapt<Product>();

    product.CreatedAt = DateTime.UtcNow;
    product.CreatedBy = userId;

    if (request.MainImage is not null)
    {
        var imageAnalysis =
            await _productImageAnalysisService.AnalyzeAsync(
                request.MainImage);

        if (imageAnalysis is null)
        {
            throw new Exception(
                "OpenAI image analysis returned null."
            );
        }

        product.VisualMetadata =
            BuildVisualMetadata(imageAnalysis);

        product.MainImage =
            await _fileService.UploadAsync(
                request.MainImage);
    }

    if (request.SubImages is not null &&
        request.SubImages.Any())
    {
        product.SubImages = new List<ProductImage>();

        foreach (var image in request.SubImages)
        {
            var imageUrl =
                await _fileService.UploadAsync(image);

            product.SubImages.Add(new ProductImage
            {
                ImageName = imageUrl
            });
        }
    }

    var savedProduct =
        await _productRepository.AddAsync(product);

    return savedProduct.Adapt<ProductResponse>();
}
private static ProductVisualMetadata BuildVisualMetadata(
    ProductImageAnalysisResult result)
{
    return new ProductVisualMetadata
    {
        CategoryAr = result.CategoryAr,
        CategoryEn = result.CategoryEn,

        SubCategoryAr = result.SubCategoryAr,
        SubCategoryEn = result.SubCategoryEn,

        ColorsAr = string.Join(
            " ",
            result.ColorsAr.Distinct(
                StringComparer.OrdinalIgnoreCase)),

        ColorsEn = string.Join(
            " ",
            result.ColorsEn.Distinct(
                StringComparer.OrdinalIgnoreCase)),

        MaterialAr = result.MaterialAr,
        MaterialEn = result.MaterialEn,

        StyleAr = result.StyleAr,
        StyleEn = result.StyleEn,

        PatternAr = result.PatternAr,
        PatternEn = result.PatternEn,

        CaptionAr = result.CaptionAr,
        CaptionEn = result.CaptionEn,

KeywordsAr = string.Join(
    " ",
    result.KeywordsAr
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)),

KeywordsEn = string.Join(
    " ",
    result.KeywordsEn
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)),

        Confidence = (decimal)result.Confidence,
        AnalyzedAt = DateTime.UtcNow,
        ModelName = result.ModelName
    };
}
public async Task<List<ProductResponse>>
    GetAllProductsForAdmin()
{
    var products = await _productRepository
        .Query()
        .Include(p => p.Translations)
        .Include(p => p.SubImages)
        .Include(p => p.VisualMetadata)
        .ToListAsync();

    return products.Adapt<List<ProductResponse>>();
}
public async Task AnalyzeMissingProducts()
{
    var products = await _productRepository
        .Query()
        .Include(p => p.VisualMetadata)
        .Where(p =>
            p.MainImage != null &&
            p.VisualMetadata == null)
        .ToListAsync();


    foreach (var product in products)
    {
        try
        {
            var imagePath = Path.Combine(
                @"C:\Users\leena\Downloads\graduation (43)\graduation (42)\graduation\API_ASP\Asp.net_Api\MyApi.PLL\wwwroot",
                product.MainImage.TrimStart('/')
                    .Replace("/", "\\")
            );


            if (!File.Exists(imagePath))
            {
                continue;
            }


            await using var stream =
                new FileStream(
                    imagePath,
                    FileMode.Open,
                    FileAccess.Read);


            var file = new FormFile(
                stream,
                0,
                stream.Length,
                "image",
                Path.GetFileName(imagePath))
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/webp"
            };


            var result =
                await _productImageAnalysisService
                    .AnalyzeAsync(file);


            if (result == null)
                continue;


            product.VisualMetadata =
                BuildVisualMetadata(result);

            product.VisualMetadata.ProductId =
                product.Id;


            await _productRepository.UpdateAsync(product);


        }
        catch(Exception ex)
        {
            Console.WriteLine(
                $"Failed Product {product.Id}: {ex.Message}");
        }
    }
}
public async Task<PaginatResponse<ProductUserResponse>> GetAllProductsForUser(
    string lang = "en",
    int page = 1,
    int limit = 3,
    string? search = null,
    int? categoryId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    decimal? minRate = null,
    decimal? maxRate = null,
    string? sortBy = null,
    bool asc = true,
    ProductSearchMode searchMode = ProductSearchMode.NameAndDescription
    )
{
    var query = _productRepository.Query();

if (!string.IsNullOrWhiteSpace(search))
{
    var searchValue = search.Trim();

    if (searchMode == ProductSearchMode.NameAndDescription)
    {
        query = query.Where(product =>
            product.Translations.Any(translation =>
                (translation.Name != null &&
                 translation.Name.Contains(searchValue)) ||

                (translation.Description != null &&
                 translation.Description.Contains(searchValue))
            )
        );
    }
    else if (searchMode == ProductSearchMode.VisualOnly)
    {
        query = query.Where(product =>
            product.VisualMetadata != null &&
            (
                (product.VisualMetadata.CategoryAr != null &&
                 product.VisualMetadata.CategoryAr.Contains(searchValue)) ||

                (product.VisualMetadata.CategoryEn != null &&
                 product.VisualMetadata.CategoryEn.Contains(searchValue)) ||

                (product.VisualMetadata.SubCategoryAr != null &&
                 product.VisualMetadata.SubCategoryAr.Contains(searchValue)) ||

                (product.VisualMetadata.SubCategoryEn != null &&
                 product.VisualMetadata.SubCategoryEn.Contains(searchValue)) ||

                (product.VisualMetadata.ColorsAr != null &&
                 product.VisualMetadata.ColorsAr.Contains(searchValue)) ||

                (product.VisualMetadata.ColorsEn != null &&
                 product.VisualMetadata.ColorsEn.Contains(searchValue)) ||

                (product.VisualMetadata.MaterialAr != null &&
                 product.VisualMetadata.MaterialAr.Contains(searchValue)) ||

                (product.VisualMetadata.MaterialEn != null &&
                 product.VisualMetadata.MaterialEn.Contains(searchValue)) ||

                (product.VisualMetadata.StyleAr != null &&
                 product.VisualMetadata.StyleAr.Contains(searchValue)) ||

                (product.VisualMetadata.StyleEn != null &&
                 product.VisualMetadata.StyleEn.Contains(searchValue)) ||

                (product.VisualMetadata.PatternAr != null &&
                 product.VisualMetadata.PatternAr.Contains(searchValue)) ||

                (product.VisualMetadata.PatternEn != null &&
                 product.VisualMetadata.PatternEn.Contains(searchValue)) ||

                (product.VisualMetadata.CaptionAr != null &&
                 product.VisualMetadata.CaptionAr.Contains(searchValue)) ||

                (product.VisualMetadata.CaptionEn != null &&
                 product.VisualMetadata.CaptionEn.Contains(searchValue)) ||

                (product.VisualMetadata.KeywordsAr != null &&
                 product.VisualMetadata.KeywordsAr.Contains(searchValue)) ||

                (product.VisualMetadata.KeywordsEn != null &&
                 product.VisualMetadata.KeywordsEn.Contains(searchValue))
            )
        );
    }
    else
    {
        query = query.Where(product =>
            product.Translations.Any(translation =>
                (translation.Name != null &&
                 translation.Name.Contains(searchValue)) ||

                (translation.Description != null &&
                 translation.Description.Contains(searchValue))
            )
            ||
            (
                product.VisualMetadata != null &&
                (
                    (product.VisualMetadata.CategoryAr != null &&
                     product.VisualMetadata.CategoryAr.Contains(searchValue)) ||

                    (product.VisualMetadata.CategoryEn != null &&
                     product.VisualMetadata.CategoryEn.Contains(searchValue)) ||

                    (product.VisualMetadata.SubCategoryAr != null &&
                     product.VisualMetadata.SubCategoryAr.Contains(searchValue)) ||

                    (product.VisualMetadata.SubCategoryEn != null &&
                     product.VisualMetadata.SubCategoryEn.Contains(searchValue)) ||

                    (product.VisualMetadata.ColorsAr != null &&
                     product.VisualMetadata.ColorsAr.Contains(searchValue)) ||

                    (product.VisualMetadata.ColorsEn != null &&
                     product.VisualMetadata.ColorsEn.Contains(searchValue)) ||

                    (product.VisualMetadata.MaterialAr != null &&
                     product.VisualMetadata.MaterialAr.Contains(searchValue)) ||

                    (product.VisualMetadata.MaterialEn != null &&
                     product.VisualMetadata.MaterialEn.Contains(searchValue)) ||

                    (product.VisualMetadata.StyleAr != null &&
                     product.VisualMetadata.StyleAr.Contains(searchValue)) ||

                    (product.VisualMetadata.StyleEn != null &&
                     product.VisualMetadata.StyleEn.Contains(searchValue)) ||

                    (product.VisualMetadata.PatternAr != null &&
                     product.VisualMetadata.PatternAr.Contains(searchValue)) ||

                    (product.VisualMetadata.PatternEn != null &&
                     product.VisualMetadata.PatternEn.Contains(searchValue)) ||

                    (product.VisualMetadata.CaptionAr != null &&
                     product.VisualMetadata.CaptionAr.Contains(searchValue)) ||

                    (product.VisualMetadata.CaptionEn != null &&
                     product.VisualMetadata.CaptionEn.Contains(searchValue)) ||

                    (product.VisualMetadata.KeywordsAr != null &&
                     product.VisualMetadata.KeywordsAr.Contains(searchValue)) ||

                    (product.VisualMetadata.KeywordsEn != null &&
                     product.VisualMetadata.KeywordsEn.Contains(searchValue))
                )
            )
        );
    }
}
    if (categoryId is not null)
    {
        query = query.Where(p => p.CategoryId == categoryId);
    }

    if (minPrice is not null)
    {
        query = query.Where(p => p.Price >= minPrice);
    }

    if (maxPrice is not null)
    {
        query = query.Where(p => p.Price <= maxPrice);
    }

    if (minRate is not null)
    {
        query = query.Where(p => p.Rate >= minRate);
    }

    if (maxRate is not null)
    {
        query = query.Where(p => p.Rate <= maxRate);
    }

    if (sortBy is not null)
    {
        sortBy = sortBy.ToLower();

        if (sortBy == "price")
        {
            query = asc ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
        }
        else if (sortBy == "name")
        {
            query = asc
                ? query.OrderBy(p => p.Translations.FirstOrDefault(t => t.Language == lang)!.Name)
                : query.OrderByDescending(p => p.Translations.FirstOrDefault(t => t.Language == lang)!.Name);
        }
        else if (sortBy == "rate")
        {
            query = asc ? query.OrderBy(p => p.Rate) : query.OrderByDescending(p => p.Rate);
        }
        else if (sortBy == "createdAt")
        {
            query = asc ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
        }
    }

    var totalCount = await query.CountAsync();

    var response = await query
        .Skip((page - 1) * limit)
        .Take(limit)
        .Select(p => new ProductUserResponse
        {
            
            Id = p.Id,
            Name = p.Translations
                .Where(t => t.Language == lang)
                .Select(t => t.Name)
                .FirstOrDefault() ?? "",
            Description = p.Translations
                .Where(t => t.Language == lang)
                .Select(t => t.Description)
                .FirstOrDefault(),
            Price = p.Price,
            Stock = p.Stock,
            MainImage = p.MainImage,
            Quantity = p.Quantity,
            VisualSearchText = p.VisualMetadata == null
    ? ""
    : (p.VisualMetadata.CategoryAr ?? "") + " " +
      (p.VisualMetadata.CategoryEn ?? "") + " " +
      (p.VisualMetadata.SubCategoryAr ?? "") + " " +
      (p.VisualMetadata.SubCategoryEn ?? "") + " " +
      (p.VisualMetadata.ColorsAr ?? "") + " " +
      (p.VisualMetadata.ColorsEn ?? "") + " " +
      (p.VisualMetadata.MaterialAr ?? "") + " " +
      (p.VisualMetadata.MaterialEn ?? "") + " " +
      (p.VisualMetadata.StyleAr ?? "") + " " +
      (p.VisualMetadata.StyleEn ?? "") + " " +
      (p.VisualMetadata.PatternAr ?? "") + " " +
      (p.VisualMetadata.PatternEn ?? "") + " " +
      (p.VisualMetadata.CaptionAr ?? "") + " " +
      (p.VisualMetadata.CaptionEn ?? "") + " " +
      (p.VisualMetadata.KeywordsAr ?? "") + " " +
      (p.VisualMetadata.KeywordsEn ?? "")
        })
        .ToListAsync();

    return new PaginatResponse<ProductUserResponse>
    {
        TotalCount = totalCount,
        Page = page,
        Limit = limit,
        Data = response
    };
}
    public async Task<BaseResponse> DeleteProductAsync(int id)
    {
        var product = await _productRepository.FindByIdAsync(id);
        if (product == null)
        {
            return new BaseResponse
            {
                IsSuccess = false,
                Message = "Product not found"
            };
        }
        foreach (var image in product.SubImages)
        {
            await _fileService.DeleteAsync(image.ImageName);
        }
        if (product.MainImage != null)
        {
            await _fileService.DeleteAsync(product.MainImage);
        }
        return await _productRepository.DeleteAsync(product);
    }
public async Task<BaseResponse> UpdateProductAsync(int id, ProductRequest request)
{
var product = await _productRepository.Query()
    .Include(p => p.SubImages)
    .Include(p => p.Translations)
    .Include(p => p.VisualMetadata)
    .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
        return new BaseResponse { IsSuccess = false, Message = "Product not found" };

    var productTranslation = product.Translations?
        .FirstOrDefault(t => t.Language == "en");

    var requestTranslation = request.Translations?
        .FirstOrDefault(t => t.Language == "en");

    if (productTranslation != null && requestTranslation != null)
    {
        productTranslation.Name = requestTranslation.Name;
        productTranslation.Description = requestTranslation.Description;
    }
if (request.Price > 0)
    product.Price = request.Price;

if (request.CategoryId > 0)
    product.CategoryId = request.CategoryId;

if (request.MainImage != null)
{
    var imageUrl = await _fileService.UploadAsync(
        request.MainImage);

    product.MainImage = imageUrl;

    var imageAnalysis =
        await _productImageAnalysisService.AnalyzeAsync(
            request.MainImage);

    if (imageAnalysis != null)
    {
        ApplyVisualMetadata(
            product,
            imageAnalysis);
    }
    else if (product.VisualMetadata != null)
    {
        ClearVisualMetadata(product.VisualMetadata);
    }
}

    if (request.SubImages != null && request.SubImages.Any())
    {
        foreach (var image in product.SubImages)
            await _fileService.DeleteAsync(image.ImageName);

        product.SubImages.Clear();

        foreach (var image in request.SubImages)
        {
            var imageUrl = await _fileService.UploadAsync(image);
            product.SubImages.Add(new ProductImage { ImageName = imageUrl });
        }
    }

    await _productRepository.UpdateAsync(product);

    return new BaseResponse
    {
        IsSuccess = true,
        Message = "Product updated successfully"
    };
}
private static void ApplyVisualMetadata(
    Product product,
    ProductImageAnalysisResult result)
{
    product.VisualMetadata ??= new ProductVisualMetadata
    {
        ProductId = product.Id
    };

    var metadata = product.VisualMetadata;

    metadata.CategoryAr = result.CategoryAr;
    metadata.CategoryEn = result.CategoryEn;

    metadata.SubCategoryAr = result.SubCategoryAr;
    metadata.SubCategoryEn = result.SubCategoryEn;

    metadata.ColorsAr = string.Join(
        " ",
        result.ColorsAr.Distinct(
            StringComparer.OrdinalIgnoreCase));

    metadata.ColorsEn = string.Join(
        " ",
        result.ColorsEn.Distinct(
            StringComparer.OrdinalIgnoreCase));

    metadata.MaterialAr = result.MaterialAr;
    metadata.MaterialEn = result.MaterialEn;

    metadata.StyleAr = result.StyleAr;
    metadata.StyleEn = result.StyleEn;

    metadata.PatternAr = result.PatternAr;
    metadata.PatternEn = result.PatternEn;

    metadata.CaptionAr = result.CaptionAr;
    metadata.CaptionEn = result.CaptionEn;

metadata.KeywordsAr = string.Join(
    " ",
    result.KeywordsAr
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase));

metadata.KeywordsEn = string.Join(
    " ",
    result.KeywordsEn
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    metadata.Confidence = (decimal)result.Confidence;
    metadata.AnalyzedAt = DateTime.UtcNow;
    metadata.ModelName = result.ModelName;
}

private static void ClearVisualMetadata(
    ProductVisualMetadata metadata)
{
    metadata.CategoryAr = "";
    metadata.CategoryEn = "";

    metadata.SubCategoryAr = "";
    metadata.SubCategoryEn = "";

    metadata.ColorsAr = "";
    metadata.ColorsEn = "";

    metadata.MaterialAr = "";
    metadata.MaterialEn = "";

    metadata.StyleAr = "";
    metadata.StyleEn = "";

    metadata.PatternAr = "";
    metadata.PatternEn = "";

    metadata.CaptionAr = "";
    metadata.CaptionEn = "";

    metadata.KeywordsAr = "";
    metadata.KeywordsEn = "";

    metadata.Confidence = 0;
    metadata.AnalyzedAt = DateTime.UtcNow;
}
    public Task<List<ProductResponse>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse> ToggleStatus(int Id)
    {
        throw new NotImplementedException();
    }

    public async Task<ProductUserDetails> GetProductsDetailsForUser(int id, string lang = "en")
    {
        var products = await _productRepository.FindByIdAsync(id);
        var response = products.BuildAdapter().AddParameters("lang", lang).AdaptToType<ProductUserDetails>();
        return response;

    }
}
