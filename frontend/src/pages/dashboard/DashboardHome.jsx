import React from "react";
import {
  DollarSign,
  Package,
  ShoppingCart,
  Users,
  ArrowUpRight,
} from "lucide-react";
import "./DashboardHome.css";

const cards = [
  {
    title: "Total Sales",
    value: "$0.00",
    description: "Total completed sales",
    icon: DollarSign,
  },
  {
    title: "Products",
    value: "0",
    description: "Products in the store",
    icon: Package,
  },
  {
    title: "Orders",
    value: "0",
    description: "All customer orders",
    icon: ShoppingCart,
  },
  {
    title: "Users",
    value: "0",
    description: "Registered customers",
    icon: Users,
  },
];

export default function DashboardHome() {
  return (
    <section>
      <div className="admin-page-heading">
        <div>
          <span>Overview</span>
          <h2>Dashboard</h2>
          <p>
            Track store activity, orders, products, and sales.
          </p>
        </div>

        <div className="admin-page-date">
          {new Date().toLocaleDateString("en-US", {
            weekday: "long",
            month: "long",
            day: "numeric",
            year: "numeric",
          })}
        </div>
      </div>

      <div className="admin-stat-grid">
        {cards.map(
          ({
            title,
            value,
            description,
            icon: Icon,
          }) => (
            <article className="admin-stat-card" key={title}>
              <div className="admin-stat-card-top">
                <div className="admin-stat-icon">
                  <Icon size={23} />
                </div>

                <ArrowUpRight
                  className="admin-stat-arrow"
                  size={18}
                />
              </div>

              <p>{title}</p>
              <h3>{value}</h3>
              <span>{description}</span>
            </article>
          )
        )}
      </div>

      <div className="admin-dashboard-grid">
        <section className="admin-dashboard-panel">
          <div className="admin-panel-heading">
            <div>
              <h3>Recent Orders</h3>
              <p>Latest orders placed by customers.</p>
            </div>
          </div>

          <div className="admin-empty-state">
            <ShoppingCart size={38} />
            <h4>No orders loaded yet</h4>
            <p>
              We will connect this section to the orders API.
            </p>
          </div>
        </section>

        <section className="admin-dashboard-panel">
          <div className="admin-panel-heading">
            <div>
              <h3>Low Stock</h3>
              <p>Products that need more quantity.</p>
            </div>
          </div>

          <div className="admin-empty-state">
            <Package size={38} />
            <h4>No products loaded yet</h4>
            <p>
              Low-stock products will appear here.
            </p>
          </div>
        </section>
      </div>
    </section>
  );
}