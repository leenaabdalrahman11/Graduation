using Microsoft.AspNetCore.Http;
using MyApi.DAL.DTO.Response;

namespace MyApi.BLL.Service;

public interface IProductImageAnalysisService
{
    Task<ProductImageAnalysisResult?> AnalyzeAsync(
        IFormFile image,
        CancellationToken cancellationToken = default);
}