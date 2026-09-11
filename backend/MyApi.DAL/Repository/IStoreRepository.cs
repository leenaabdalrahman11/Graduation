using System.Collections.Generic;
using MyApi.DAL.Models;

namespace MyApi.DAL.Repository;

public interface IStoreRepository
{
    List<Product> GetProducts();
    List<OrderItem> GetCartItems();
    List<Order> GetOrders();
}