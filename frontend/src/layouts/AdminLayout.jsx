import React, { useState } from "react";
import { Outlet } from "react-router-dom";
import AdminSidebar from "../components/dashboard/AdminSidebar";
import AdminHeader from "../components/dashboard/AdminHeader";
import "./AdminLayout.css";

export default function AdminLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="admin-layout">
      <AdminSidebar
        isOpen={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
      />

      <div className="admin-main">
        <AdminHeader
          onOpenSidebar={() => setSidebarOpen(true)}
        />

        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}