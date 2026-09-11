using System;

namespace MyApi.DAL.DTO.Response;

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public int Count { get; set; }
    public decimal TotalPrice { get; set; }
}
