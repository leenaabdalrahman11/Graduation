import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { getStoredToken, isAdmin } from "../utils/auth";

export default function AdminRoute({ children }) {
  const location = useLocation();
  const token = getStoredToken();

  if (!token) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname }}
      />
    );
  }

  if (!isAdmin(token)) {
    return <Navigate to="/" replace />;
  }

  return children;
}