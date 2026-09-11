import React from 'react';

const MainLayout = ({ children }) => {
  return (
    <div className="min-h-screen bg-[#d9d7d3] p-6 md:p-10">
      <div className="mx-auto max-w-7xl rounded-[28px]">{children}</div>
    </div>
  );
};

export default MainLayout;