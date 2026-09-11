import React, { useEffect, useState } from "react";
import { authorizedFetch } from "../../utils/authorizedFetch";

export default function AdminOrders() {

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState("Pending");


  const getOrders = async () => {
    try {

      setLoading(true);

      const response = await authorizedFetch(
        `${import.meta.env.VITE_API_URL}/api/admin/Orders?status=${status}`
      );


      const text = await response.text();

      console.log("STATUS:", response.status);
      console.log("RESPONSE:", text);


      if (!response.ok) {
        throw new Error(text);
      }


      const data = JSON.parse(text);

      setOrders(data.orders || data.Orders || []);


    } catch(error) {

      console.log("GET ORDERS ERROR:", error);
      setOrders([]);

    } finally {

      setLoading(false);

    }
  };



  useEffect(() => {
    getOrders();
  }, [status]);





  const updateStatus = async (orderId, newStatus) => {

    try {

      const response = await authorizedFetch(
        `${import.meta.env.VITE_API_URL}/api/admin/Orders/${orderId}`,
        {
          method: "PATCH",

          headers: {
            "Content-Type": "application/json"
          },

          body: JSON.stringify({
            status: newStatus
          })
        }
      );


      const data = await response.json();

      console.log("UPDATE RESPONSE:", data);


      if(response.ok){

        getOrders();

      }


    } catch(error){

      console.log("UPDATE STATUS ERROR:", error);

    }

  };




  if (loading)
    return <h3>Loading...</h3>;



  return (

    <div className="admin-page">


      <h1>Orders</h1>



      <select
        value={status}
        onChange={(e)=>setStatus(e.target.value)}
      >

        <option value="Pending">
          Pending
        </option>

        <option value="Approved">
          Approved
        </option>

        <option value="Shipped">
          Shipped
        </option>

        <option value="Delivered">
          Delivered
        </option>

        <option value="Cancelled">
          Cancelled
        </option>


      </select>




      <table className="table">

        <thead>

          <tr>

            <th>ID</th>
            <th>User</th>
            <th>Status</th>
            <th>Payment</th>
            <th>Amount</th>
            <th>Date</th>

          </tr>

        </thead>



        <tbody>


        {
          orders.map(order => (

            <tr key={order.id}>


              <td>
                {order.id}
              </td>



              <td>
                {order.userId}
              </td>




              <td>

                <select

                  value={order.orderStatus}

                  onChange={(e)=>
                    updateStatus(
                      order.id,
                      e.target.value
                    )
                  }

                >

                  <option value="Pending">
                    Pending
                  </option>


                  <option value="Approved">
                    Approved
                  </option>


                  <option value="Shipped">
                    Shipped
                  </option>


                  <option value="Delivered">
                    Delivered
                  </option>


                  <option value="Cancelled">
                    Cancelled
                  </option>


                </select>


              </td>





              <td>
                {order.paymentStatus}
              </td>



              <td>
                {order.amountPaid}
              </td>



              <td>
                {order.orderDate}
              </td>



            </tr>

          ))
        }


        </tbody>


      </table>



    </div>

  );

}