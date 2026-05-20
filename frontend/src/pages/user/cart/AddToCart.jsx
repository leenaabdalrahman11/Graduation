import React from 'react'

export async function addToCart(baseUrl, productId, count = 1) {
  const token = localStorage.getItem("token");

  console.log("BASE URL:", baseUrl);
  console.log("TOKEN EXISTS:", !!token);
  console.log("TOKEN VALUE:", token);

  if (!token) {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    throw new Error("You are not logged in");
  }

  const response = await fetch(`${baseUrl}/api/Cart`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
      productId,
      count,
    }),
  });

  console.log("ADD TO CART STATUS:", response.status);

  const rawText = await response.text();
  console.log("RAW RESPONSE TEXT:", rawText);

  let data = null;
  try {
    data = rawText ? JSON.parse(rawText) : null;
  } catch {
    data = { message: rawText };
  }

  if (response.status === 401) {
    console.log("401 UNAUTHORIZED FROM BACKEND");
    throw new Error("Session expired. Please log in again.");
  }

  if (!response.ok) {
    throw new Error(data?.message || "Failed to add product to cart");
  }

  return data;
}