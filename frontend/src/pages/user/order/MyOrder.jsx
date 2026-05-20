import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

export default function MyOrders() {
  const baseUrl = import.meta.env.VITE_API_URL;
  const token = localStorage.getItem("token");
  const navigate = useNavigate();

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  const getMyOrders = async () => {
    try {
      const response = await fetch(`${baseUrl}/api/Order/my-orders`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.status === 401) {
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        navigate("/login");
        return;
      }

      if (!response.ok) {
        throw new Error("Failed to fetch orders");
      }

      const data = await response.json();
      console.log("Orders data:", data);
      setOrders(data || []);
    } catch (error) {
      console.error("Error fetching orders:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    getMyOrders();
  }, []);

  const getStatusStyle = (status) => {
    switch (status) {
      case "Pending":
        return "bg-yellow-50 text-yellow-700 border-yellow-200";
      case "Shipped":
        return "bg-blue-50 text-blue-700 border-blue-200";
      case "Delivered":
        return "bg-green-50 text-green-700 border-green-200";
      case "Cancelled":
        return "bg-red-50 text-red-700 border-red-200";
      default:
        return "bg-gray-50 text-gray-700 border-gray-200";
    }
  };

  const getPaymentStyle = (status) => {
    switch (status) {
      case "Paid":
        return "bg-green-50 text-green-700 border-green-200";
      case "Unpaid":
        return "bg-red-50 text-red-700 border-red-200";
      case "Pending":
        return "bg-yellow-50 text-yellow-700 border-yellow-200";
      default:
        return "bg-gray-50 text-gray-700 border-gray-200";
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center text-base sm:text-lg text-gray-500 px-4 text-center">
        Loading...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#f8f8f8] px-3 sm:px-4 md:px-8 lg:px-16 xl:px-20 py-6 sm:py-8 md:py-10">
      <div className="max-w-7xl mx-auto flex flex-col justify-center">
        <h1 className="text-2xl sm:text-3xl md:text-4xl font-semibold text-gray-800 mb-6 sm:mb-8 text-center">
          My Orders
        </h1>

        {orders.length === 0 ? (
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 sm:p-8 md:p-10 text-center">
            <p className="text-base sm:text-lg text-gray-500">
              You don't have any orders yet
            </p>

            <button
              onClick={() => navigate("/")}
              className="mt-6 bg-[#bc4c2a] text-white px-6 py-3 rounded-xl hover:bg-[#a03e22] transition duration-300 text-sm sm:text-base"
            >
              Start Shopping
            </button>
          </div>
        ) : (
          <div className="space-y-5 sm:space-y-6">
            {orders.map((order) => (
              <div
                key={order.id}
                className="bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-xl hover:-translate-y-1 hover:border-[#bc4c2a]/30 hover:bg-[#fffaf7] transition-all duration-300 p-4 sm:p-5 md:p-6"
              >
                <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-5">
                  <div>
                    <h2 className="text-lg sm:text-xl md:text-2xl font-semibold text-gray-800">
                      Order #{order.id}
                    </h2>

                    <p className="text-xs sm:text-sm text-gray-500 mt-1">
                      Track your order status and details
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2 sm:gap-3">
                    <span
                      className={`px-3 sm:px-4 py-2 rounded-full border text-xs sm:text-sm font-medium ${getStatusStyle(
                        order.orderStatus
                      )}`}
                    >
                      {order.orderStatus}
                    </span>

                    <span
                      className={`px-3 sm:px-4 py-2 rounded-full border text-xs sm:text-sm font-medium ${getPaymentStyle(
                        order.paymentStatus
                      )}`}
                    >
                      {order.paymentStatus}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-2 sm:gap-3 mb-5">
                  <div className="bg-gray-50 rounded-xl px-2 sm:px-4 py-2 sm:py-3 text-center transition-all duration-300 hover:bg-orange-50 hover:shadow-md hover:-translate-y-1">
                    <p className="text-xs text-gray-500 mb-1">Order ID</p>
                    <p className="font-medium text-gray-800 text-sm sm:text-base">
                      #{order.id}
                    </p>
                  </div>

                  <div className="bg-gray-50 rounded-xl px-2 sm:px-4 py-2 sm:py-3 text-center transition-all duration-300 hover:bg-orange-50 hover:shadow-md hover:-translate-y-1">
                    <p className="text-xs text-gray-500 mb-1">Items</p>
                    <p className="font-medium text-gray-800 text-sm sm:text-base">
                      {order.orderItems?.length || 0}
                    </p>
                  </div>

                  <div className="bg-gray-50 rounded-xl px-2 sm:px-4 py-2 sm:py-3 text-center transition-all duration-300 hover:bg-orange-50 hover:shadow-md hover:-translate-y-1">
                    <p className="text-xs text-gray-500 mb-1">Order Status</p>
                    <p className="font-medium text-gray-800 text-sm sm:text-base">
                      {order.orderStatus}
                    </p>
                  </div>

                  <div className="bg-gray-50 rounded-xl px-2 sm:px-4 py-2 sm:py-3 text-center transition-all duration-300 hover:bg-orange-50 hover:shadow-md hover:-translate-y-1">
                    <p className="text-xs text-gray-500 mb-1">Payment</p>
                    <p className="font-medium text-gray-800 text-sm sm:text-base">
                      {order.paymentStatus}
                    </p>
                  </div>
                </div>

                {order.orderItems && order.orderItems.length > 0 && (
                  <div className="border-t border-gray-100 pt-4">
                    <h3 className="text-base sm:text-lg font-semibold text-gray-800 mb-3">
                      Products
                    </h3>

                    <div className="space-y-3">
                      {order.orderItems.map((item, index) => (
                        <div
                          key={index}
                          onClick={() =>
                            navigate(`/productDetails/${item.productId}`)
                          }
                          className="bg-gray-50 rounded-xl p-3 sm:p-4 cursor-pointer hover:bg-orange-50 hover:shadow-md hover:-translate-y-1 transition-all duration-300"
                        >
                          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                            <div>
                              <h4 className="font-medium text-gray-800 text-sm sm:text-base">
                                {item.productName}
                              </h4>

                              <p className="text-xs sm:text-sm text-gray-500 mt-1">
                                Tap to view product details
                              </p>
                            </div>

                            <div className="grid grid-cols-3 gap-2 sm:gap-3 text-center">
                              <div>
                                <p className="text-xs text-gray-500">Price</p>
                                <p className="font-medium text-gray-800 text-sm">
                                  ${item.price}
                                </p>
                              </div>

                              <div>
                                <p className="text-xs text-gray-500">
                                  Quantity
                                </p>
                                <p className="font-medium text-gray-800 text-sm">
                                  {item.count}
                                </p>
                              </div>

                              <div>
                                <p className="text-xs text-gray-500">Total</p>
                                <p className="font-medium text-[#bc4c2a] text-sm">
                                  ${item.totalPrice}
                                </p>
                              </div>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                <div className="flex justify-end mt-5 border-t border-gray-100 pt-4">
                  <div className="text-right">
                    <p className="text-xs sm:text-sm text-gray-500">
                      Order Total
                    </p>
                    <p className="text-lg sm:text-xl font-semibold text-[#bc4c2a]">
                      ${order.totalPrice || order.orderTotal || order.total || 0}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}