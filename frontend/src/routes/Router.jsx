import React from "react";
import { createBrowserRouter } from "react-router-dom";
import Home from "../pages/user/home/Home";
import Layout from "../layouts/Layout";
import Products from "../components/user/products/Products";
import Collection from "../pages/user/collection/Collection";
import ProductDetails from "../pages/productDetails/ProductDetails";
import OurStory from "../pages/user/Our/OurStory";
import OurCraft from "../pages/user/Our/OurCraft";
import Contact from "../pages/user/contact/Contact";
import Search from "../components/user/search/Search";
import Register from "../pages/user/register/Register";
import Login from "../pages/user/login/Login";
import Cart from "../pages/user/cart/Cart";
import CheckOut from "../pages/user/checkout/CheckOut";
import ResetPassword from "../pages/user/login/ResetPassword";
import HomePageBlind from "../blind/pages/HomePageBlind";
import CheckoutPayment from "../pages/user/checkout/CheckoutPayment";
import PaymentCancel from "../pages/user/checkout/PaymentCancel";
import PaymentSuccess from "../pages/user/checkout/PaymentSuccess";
import ExtensionPaymentPage from "../pages/user/checkout/ExtensionPaymentPage";
import MyOrders from "../pages/user/order/MyOrder";
import AdminRoute from "./AdminRoute";
import AdminLayout from "../layouts/AdminLayout";
import DashboardHome from "../pages/dashboard/DashboardHome";
import AdminPlaceholder from "../pages/dashboard/AdminPlaceholder";
import AdminProducts from "../pages/dashboard/AdminProducts";
import AdminCategories from "../pages/dashboard/AdminCategories";
import AdminUsers from "../pages/dashboard/AdminUsers";
import AdminOrders from "../pages/dashboard/AdminOrders";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Home />,
      },
      {
        path: "/products",
        element: <Collection />,
      },
      {
        path: "/productDetails/:id",
        element: <ProductDetails />,
      },
      {
        path: "/ourStory",
        element: <OurStory />,
      },
      {
        path: "/ourCraft",
        element: <OurCraft />,
      },
      {
        path: "/contact",
        element: <Contact />,
      },
      {
        path: "/search",
        element: <Search />,
      },
      {
        path: "/register",
        element:<Register />
      },
      {
        path:"/login",
        element:<Login />
      },{
        path:"/cart",
        element:<Cart />
      },{
        path:"/checkout",
        element:<CheckOut />
      },{
        path:"/reset-password",
        element:<ResetPassword />
      },{
        path:"/homeBlind",
        element: <HomePageBlind />
      },{
        path:"/checkout/payment",
         element:<CheckoutPayment />
      }
      ,{
        path:"/payment-success",
        element:<PaymentSuccess />
      },{
        path:"/payment-cancel",
        element:<PaymentCancel />
      },{
        path:"/my-orders",
        element:<MyOrders/>
      }
    ],
  },
  {
  path: "/admin",
  element: (
    <AdminRoute>
      <AdminLayout />
    </AdminRoute>
  ),
  children: [
    {
      index: true,
      element: <DashboardHome />,
    },
{
  path: "products",
  element: <AdminProducts />,
    },
{
  path: "categories",
  element: <AdminCategories />,
},
{
  path: "orders",
  element: <AdminOrders />,
},
{
  path: "users",
  element: <AdminUsers />,
},
    {
      path: "reviews",
      element: (
        <AdminPlaceholder
          title="Reviews"
          description="Review and moderate product ratings and comments."
        />
      ),
    },
  ],
},
  {
  path: "/extension-payment",
  element: <ExtensionPaymentPage />
}
]);
