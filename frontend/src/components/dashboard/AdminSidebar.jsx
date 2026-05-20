import React from "react";
import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  Package,
  Tags,
  ShoppingCart,
  Users,
  Star,
  X,
} from "lucide-react";

const links = [
  {
    to: "/admin",
    label: "Dashboard",
    icon: LayoutDashboard,
    end: true,
  },
  {
    to: "/admin/products",
    label: "Products",
    icon: Package,
  },
  {
    to: "/admin/categories",
    label: "Categories",
    icon: Tags,
  },
  {
    to: "/admin/orders",
    label: "Orders",
    icon: ShoppingCart,
  },
  {
    to: "/admin/users",
    label: "Users",
    icon: Users,
  },
 
];

export default function AdminSidebar({
  isOpen,
  onClose,
}) {
  return (
    <>
      {isOpen && (
        <button
          type="button"
          className="admin-sidebar-overlay"
          onClick={onClose}
          aria-label="Close sidebar"
        />
      )}

      <aside
        className={`admin-sidebar ${
          isOpen ? "admin-sidebar-open" : ""
        }`}
      >
        <div className="admin-sidebar-brand">
          <div>
            <span className="admin-brand-small">
              Administration
            </span>

            <h2>Admin Panel</h2>
          </div>

          <button
            type="button"
            className="admin-sidebar-close"
            onClick={onClose}
            aria-label="Close sidebar"
          >
            <X size={22} />
          </button>
        </div>

        <nav className="admin-sidebar-nav">
          {links.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              onClick={onClose}
              className={({ isActive }) =>
                `admin-sidebar-link ${
                  isActive ? "active" : ""
                }`
              }
            >
              <Icon size={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="admin-sidebar-footer">
          <p>Store Management</p>
          <span>Admin Dashboard</span>
        </div>
      </aside>
    </>
  );
}