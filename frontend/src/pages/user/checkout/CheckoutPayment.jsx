import { useEffect, useMemo, useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";

function PaymentForm({ baseUrl, token }) {
  const stripe = useStripe();
  const elements = useElements();

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!stripe || !elements) return;

    setLoading(true);
    setMessage("");

    const { error, paymentIntent } = await stripe.confirmPayment({
      elements,
      redirect: "if_required",
    });

    if (error) {
      setMessage(error.message || "Payment failed.");
      setLoading(false);
      return;
    }

    if (paymentIntent?.status === "succeeded") {
      const confirmRes = await fetch(`${baseUrl}/api/Checkout/confirm-payment`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          paymentIntentId: paymentIntent.id,
        }),
      });

      const confirmData = await confirmRes.json();

      if (!confirmRes.ok) {
        setMessage(confirmData?.message || "Payment succeeded, but order confirmation failed.");
        setLoading(false);
        return;
      }

      setMessage("Payment completed successfully.");
    } else {
      setMessage("Payment is processing.");
    }

    setLoading(false);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <PaymentElement />
      <button type="submit" disabled={!stripe || loading}>
        {loading ? "Processing..." : "Pay now"}
      </button>
      {message && <p>{message}</p>}
    </form>
  );
}

export default function CheckoutPayment() {
  const baseUrl = import.meta.env.VITE_API_URL;
  const token = localStorage.getItem("token");

  const [stripePromise, setStripePromise] = useState(null);
  const [clientSecret, setClientSecret] = useState("");
  const [amount, setAmount] = useState(null);
  const [pageError, setPageError] = useState("");

  useEffect(() => {
    const loadPayment = async () => {
      try {
        const configRes = await fetch(`${baseUrl}/api/Checkout/config`);
        const configData = await configRes.json();

        if (!configRes.ok || !configData.publishableKey) {
          setPageError("Failed to load Stripe config.");
          return;
        }

        setStripePromise(loadStripe(configData.publishableKey));

const intentRes = await authorizedFetch(
 "/api/Checkout/create-intent",
 {
   method:"POST",
   headers:{
     "Content-Type":"application/json"
   },
   body:JSON.stringify({
     paymentMethod:"Visa"
   })
 }
);


const intentData = await intentRes.json();

setClientSecret(intentData.clientSecret);
setPaymentIntentId(intentData.paymentIntentId);

        if (!intentRes.ok || !intentData.clientSecret) {
          setPageError(intentData?.message || "Failed to create payment intent.");
          return;
        }

        setClientSecret(intentData.clientSecret);
        setAmount(intentData.amount);
      } catch {
        setPageError("Something went wrong while loading the payment form.");
      }
    };

    loadPayment();
  }, [baseUrl, token]);

  const options = useMemo(() => ({
    clientSecret,
  }), [clientSecret]);

  if (pageError) return <p>{pageError}</p>;
  if (!stripePromise || !clientSecret) return <p>Loading payment form...</p>;

  return (
    <div>
      <h2>Complete your payment</h2>
      {amount !== null && <p>Total: {amount} NIS</p>}

      <Elements stripe={stripePromise} options={options}>
        <PaymentForm baseUrl={baseUrl} token={token} />
      </Elements>
    </div>
  );
}