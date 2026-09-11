const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export function getStoredToken() {
  return (
    localStorage.getItem("accessToken") ||
    localStorage.getItem("token") ||
    ""
  );
}

export function decodeToken(token = getStoredToken()) {
  if (!token) return null;

  try {
    const payloadPart = token.split(".")[1];

    if (!payloadPart) return null;

    const base64 = payloadPart
      .replace(/-/g, "+")
      .replace(/_/g, "/");

    const paddedBase64 = base64.padEnd(
      Math.ceil(base64.length / 4) * 4,
      "="
    );

    const jsonPayload = decodeURIComponent(
      atob(paddedBase64)
        .split("")
        .map(
          (character) =>
            `%${character.charCodeAt(0).toString(16).padStart(2, "0")}`
        )
        .join("")
    );

    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error("Failed to decode token:", error);
    return null;
  }
}

export function getCurrentUserRole(token = getStoredToken()) {
  const payload = decodeToken(token);

  if (!payload) return null;

  const role =
    payload.role ||
    payload.roles ||
    payload[ROLE_CLAIM] ||
    null;

  if (Array.isArray(role)) {
    return role;
  }

  return role ? [role] : [];
}

export function isAdmin(token = getStoredToken()) {
  const roles = getCurrentUserRole(token);

  if (!roles) return false;

  return roles.some(
    (role) => String(role).toLowerCase() === "admin"
  );
}

export function logout() {
  localStorage.removeItem("token");
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("user");
}