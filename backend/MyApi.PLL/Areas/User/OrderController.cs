using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DAL.DTO.Response;
using MyApiProject.MyApi.BLL.Service;

namespace MyApiProject.MyApi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("my-orders")]
    public async Task<ActionResult<List<OrderResponse>>> GetMyOrders()
    {
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new BaseResponse
            {
                IsSuccess = false,
                Message = "User is not authenticated"
            });
        }

        var orders = await _orderService.GetMyOrdersAsync(userId);

        return Ok(orders);
    }
}