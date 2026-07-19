import React from "react";
import { LogOut, Menu, UserRound } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { logout } from "../../utils/auth";

export default function AdminHeader({ onOpenSidebar }) {
  const navigate = useNavigate();

  let currentUser = {};

  try {
    currentUser = JSON.parse(
      localStorage.getItem("user") || "{}"
    );
  } catch {
    currentUser = {};
  }

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <header className="admin-header">
      <div className="admin-header-left">
        <button
          type="button"
          className="admin-menu-button"
          onClick={onOpenSidebar}
          aria-label="Open sidebar"
        >
          <Menu size={23} />
        </button>

        <div>
          <h1>Welcome back</h1>
          <p>Manage your store from one place.</p>
        </div>
      </div>

      <div className="admin-header-actions">
        <div className="admin-user-information">
          <div className="admin-user-icon">
            <UserRound size={19} />
          </div>

          <div className="admin-user-text">
            <strong>
              {currentUser.fullName || "Administrator"}
            </strong>

            <span>
              {currentUser.email || "Admin account"}
            </span>
          </div>
        </div>

        <button
          type="button"
          className="admin-logout-button"
          onClick={handleLogout}
        >
          <LogOut size={18} />
          <span>Logout</span>
        </button>
      </div>
    </header>
  );
}