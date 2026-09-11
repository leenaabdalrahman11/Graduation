using System.Collections.Generic;
using MyApi.DAL.Models;

namespace MyApi.DAL.Repository;

public class StoreRepository : IStoreRepository
{
    private readonly List<Product> _products = new();
    private readonly List<OrderItem> _cartItems = new();
    private readonly List<Order> _orders = new();

    public List<Product> GetProducts() => _products;
    public List<OrderItem> GetCartItems() => _cartItems;
    public List<Order> GetOrders() => _orders;
}