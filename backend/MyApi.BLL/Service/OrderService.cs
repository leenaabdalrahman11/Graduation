using System;
using Mapster;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using MyApi.DAL.Repository;

namespace MyApiProject.MyApi.BLL.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _orderRepository.GetOrderByIdAsync(orderId);
    }

    public async Task<List<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var order = await _orderRepository.GetOrdersByStatusAsync(status);
        return order.Adapt<List<OrderResponse>>();
    }

    public async Task<BaseResponse> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return new BaseResponse
            {
                IsSuccess = false,
                Message = "Order not found"
            };
        }
        order.OrderStatus = newStatus;
        if (newStatus == OrderStatus.Delivered)
        {
            order.PaymentStatus = PaymentStatus.Paid;
        }
        else if (newStatus == OrderStatus.Cancelled)
        {
            if(order.OrderStatus == OrderStatus.Shipped)
            {
                return new BaseResponse
                {
                    IsSuccess = false,
                    Message = "Cannot cancel an order that has already been shipped"
                };
            }
        }
        await _orderRepository.UpdateAsync(order);
        return new BaseResponse
        {
            IsSuccess = true,
            Message = "Order status updated successfully"
        };
    }
public async Task<List<OrderResponse>> GetMyOrdersAsync(string userId)
{
    var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);

    return orders.Select(order => new OrderResponse
    {
        Id = order.Id,
OrderStatus = order.OrderStatus.ToString(),
PaymentStatus = order.PaymentStatus.ToString(),
        AmountPaid = order.AmountPaid,
        UserName = order.User?.UserName,

OrderItems = order.OrderItems.Select(item => new OrderItemResponse
{
    ProductId = item.ProductId,
    ProductName = item.product != null
        ? item.product.Translations.FirstOrDefault().Name
        : null,
    Price = item.UnitPrice,
    Count = item.Quantity,
    TotalPrice = item.TotalPrice
}).ToList()
    }).ToList();
}
}
