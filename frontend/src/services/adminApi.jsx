import { authorizedFetch } from "../utils/authorizedFetch";

const baseUrl = (import.meta.env.VITE_API_URL || "").replace(/\/$/, "");

async function request(endpoint, options = {}) {
  const response = await authorizedFetch(
    `${baseUrl}${endpoint}`,
    options
  );

  if (!response) {
    throw new Error("You are not authorized. Please sign in again.");
  }

  const contentType = response.headers.get("content-type") || "";

  let result = null;

  if (contentType.includes("application/json")) {
    result = await response.json();
  } else {
    const text = await response.text();
    result = text ? { message: text } : null;
  }

  if (!response.ok) {
    const message =
      result?.message ||
      result?.Message ||
      result?.title ||
      result?.Title ||
      "The request failed.";

    throw new Error(message);
  }

  return result;
}

function findArray(value, visited = new Set()) {
  if (Array.isArray(value)) {
    return value;
  }

  if (!value || typeof value !== "object") {
    return null;
  }

  if (visited.has(value)) {
    return null;
  }

  visited.add(value);

  const possibleKeys = [
    "response",
    "Response",
    "data",
    "Data",
    "products",
    "Products",
    "orders",
    "Orders",
    "users",
    "Users",
    "categories",
    "Categories",
    "items",
    "Items",
    "result",
    "Result",
  ];

  for (const key of possibleKeys) {
    if (!(key in value)) continue;

    const found = findArray(value[key], visited);

    if (found !== null) {
      return found;
    }
  }

  return null;
}

export function extractList(payload) {
  return findArray(payload) ?? [];
}

export const adminApi = {
  getProducts() {
    return request("/api/Admin/Product");
  },
createProduct: async (formData) => {
  const token =
    localStorage.getItem("token") ||
    localStorage.getItem("accessToken");

  const response = await fetch(
    `${baseUrl}/api/Admin/Product`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
      body: formData,
    }
  );

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    const validationErrors = data?.errors
      ? Object.values(data.errors).flat().join("\n")
      : "";

    throw new Error(
      validationErrors ||
        data?.message ||
        data?.Message ||
        data?.title ||
        "Failed to create product"
    );
  }

  return data;
},
  createProduct(formData) {
    return request("/api/Admin/Product", {
      method: "POST",
      body: formData,
    });
  },

  updateProduct(id, formData) {
    return request(`/api/Admin/Product/${id}`, {
      method: "PUT",
      body: formData,
    });
  },

  deleteProduct(id) {
    return request(`/api/Admin/Product/${id}`, {
      method: "DELETE",
    });
  },

  getCategories() {
    return request("/api/Categories");
  },

  createCategory(category) {
    return request("/api/Categories", {
      method: "POST",
      body: JSON.stringify(category),
    });
  },

  updateCategory(id, category) {
    return request(`/api/Categories/${id}`, {
      method: "PATCH",
      body: JSON.stringify(category),
    });
  },

  toggleCategoryStatus(id) {
    return request(`/api/Categories/toggle-status/${id}`, {
      method: "PATCH",
    });
  },

  deleteCategory(id) {
    return request(`/api/Categories/${id}`, {
      method: "DELETE",
    });
  },

  getOrders(status = 0) {
    return request(`/api/admin/Orders?status=${status}`);
  },

  updateOrderStatus(orderId, status) {
    return request(`/api/admin/Orders/${orderId}`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    });
  },

  getUsers() {
    return request("/api/admin/Manage/users");
  },

  blockUser(id) {
    return request(`/api/admin/Manage/block/${id}`, {
      method: "PATCH",
    });
  },

  unblockUser(id) {
    return request(`/api/admin/Manage/unblock/${id}`, {
      method: "PATCH",
    });
  },

  changeUserRole(requestBody) {
    return request("/api/admin/Manage/change-role", {
      method: "PATCH",
      body: JSON.stringify(requestBody),
    });
  },
};