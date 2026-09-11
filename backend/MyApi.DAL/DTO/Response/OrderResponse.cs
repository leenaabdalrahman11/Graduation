using System;
using System.Text.Json.Serialization;
using MyApi.DAL.Models;

namespace MyApi.DAL.DTO.Response;

public class OrderResponse
{
    public int Id { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string? UserName { get; set; }

    public List<OrderItemResponse> OrderItems { get; set; } = new();
}
