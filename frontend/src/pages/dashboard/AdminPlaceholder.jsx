import React from "react";

export default function AdminPlaceholder({
  title,
  description,
}) {
  return (
    <section>
      <div className="admin-page-heading">
        <div>
          <span>Administration</span>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
      </div>

      <div className="admin-dashboard-panel">
        <div className="admin-empty-state">
          <h4>{title} page is ready</h4>
          <p>
            The API and management table will be added in the
            next step.
          </p>
        </div>
      </div>
    </section>
  );
}