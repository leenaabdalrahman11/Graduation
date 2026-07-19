export async function authorizedFetch(url, options = {}) {
  const token =
    localStorage.getItem("accessToken") ||
    localStorage.getItem("token");

  const isFormData = options.body instanceof FormData;

  const headers = new Headers(options.headers || {});

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  if (!isFormData && options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    localStorage.removeItem("token");
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");

    window.location.href = "/login";
    return null;
  }

  return response;
}