import React, { useState, useRef, useEffect } from "react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  CardNumberElement,
  CardExpiryElement,
  CardCvcElement,
  useStripe,
  useElements
} from "@stripe/react-stripe-js";

const baseUrl = "https://localhost:7291";
const stripePromise = loadStripe(
  "pk_test_51T2uQtLrcSbFJSjbNhEtB1VeiUnh2MIIfEU6PCqWdWj34UeBMAKMJ0pRsYcy4vLkijnS2nSqUHlgpyFP56w8PSTi00BiqfhzOA"
);


export default function ExtensionPaymentPage() {
  const params = new URLSearchParams(window.location.search);

const clientSecret = params.get("clientSecret");
const paymentIntentId = params.get("paymentIntentId");
const lang = params.get("lang") === "ar" ? "ar" : "en";
const isAr = lang === "ar";

  if (!clientSecret || !paymentIntentId) {
    return (
      <div
  style={{
    ...pageStyle,
    direction: isAr ? "rtl" : "ltr"
  }}
  lang={lang}
>
        <h2 style={titleStyle}>Payment error</h2>
        <p style={textStyle}>Missing payment information.</p>
      </div>
    );
  }

  return (
    <div
  style={{
    ...pageStyle,
    direction: isAr ? "rtl" : "ltr"
  }}
  lang={lang}
>
      <Elements stripe={stripePromise} options={{ clientSecret, locale: lang }}>
       <StripePaymentForm
  clientSecret={clientSecret}
  paymentIntentId={paymentIntentId}
  lang={lang}
/>
      </Elements>
    </div>
  );
}

function StripePaymentForm({ clientSecret, paymentIntentId, lang }) {
  const isAr = lang === "ar";

  const labels = isAr
    ? {
        title: "الدفع",
        cardNumber: "رقم البطاقة",
        expiry: "تاريخ الانتهاء",
        cvc: "رمز الأمان",
        confirm: "تأكيد الدفع",
        processing: "جاري معالجة الدفع...",
        hint: "استخدمي زر Tab للانتقال بين الحقول، و Shift + Tab للرجوع للحقل السابق.",
        testCard: "بطاقة تجريبية: 4242424242424242",
        cardNotReady: "حقل البطاقة غير جاهز.",
        paymentFailed: "فشل الدفع.",
        tokenMissing: "لم يتم العثور على بيانات المستخدم.",
        confirmationFailed: "فشل تأكيد الدفع.",
        success: "تم الدفع بنجاح."
      }
    : {
        title: "Payment",
        cardNumber: "Card number",
        expiry: "Expiry",
        cvc: "CVC",
        confirm: "Confirm payment",
        processing: "Processing...",
        hint: "Use Tab to move between fields, and Shift + Tab to go back.",
        testCard: "Test card: 4242424242424242",
        cardNotReady: "Card field is not ready.",
        paymentFailed: "Payment failed.",
        tokenMissing: "User token not found.",
        confirmationFailed: "Payment confirmation failed.",
        success: "Payment completed successfully."
      };
  const stripe = useStripe();
  const elements = useElements();
const cardNumberRef = useRef(null);
const cardExpiryRef = useRef(null);
const cardCvcRef = useRef(null);
const formRef = useRef(null);
const confirmButtonRef = useRef(null);
const pendingFocusRef = useRef("cardNumber");

const focusStripeField = (field) => {
  pendingFocusRef.current = field;

  setTimeout(() => {
    if (field === "cardNumber" && cardNumberRef.current) {
      cardNumberRef.current.focus();
      pendingFocusRef.current = "";
    }

    if (field === "expiry" && cardExpiryRef.current) {
      cardExpiryRef.current.focus();
      pendingFocusRef.current = "";
    }

    if (field === "cvc" && cardCvcRef.current) {
      cardCvcRef.current.focus();
      pendingFocusRef.current = "";
    }

    if (field === "confirm" && confirmButtonRef.current) {
      confirmButtonRef.current.focus();
      pendingFocusRef.current = "";
    }
  }, 250);
};
useEffect(() => {
  const handlePaymentInputMessage = (event) => {
    if (event.data?.type === "PAYMENT_FOCUS_FIELD") {
      focusStripeField(event.data.field || "cardNumber");
    }

    if (event.data?.type === "PAYMENT_SUBMIT") {
      confirmButtonRef.current?.click();
    }
  };

  window.addEventListener("message", handlePaymentInputMessage);

  return () => {
    window.removeEventListener("message", handlePaymentInputMessage);
  };
}, []);
useEffect(() => {
  const timer = setTimeout(() => {
    if (cardNumberRef.current) {
      cardNumberRef.current.focus();
    }
  }, 700);

  return () => clearTimeout(timer);
}, []);
  const [isPaying, setIsPaying] = useState(false);
  const [message, setMessage] = useState("");

  const [complete, setComplete] = useState({
    number: false,
    expiry: false,
    cvc: false
  });

  const paymentDisabled =
    !stripe || isPaying || !complete.number || !complete.expiry || !complete.cvc;

  const handleSubmit = async (e) => {
    
    e.preventDefault();

    if (!stripe || !elements) {
      return;
    }
if (paymentDisabled) {
  const message = isAr
    ? "أكملي رقم البطاقة، تاريخ الانتهاء، ورمز الأمان أولًا."
    : "Please complete card number, expiry date, and CVC first.";

  setMessage(message);

  window.parent.postMessage(
    {
      type: "PAYMENT_FAILED",
      message
    },
    "*"
  );

  return;
}
    setIsPaying(true);
    setMessage("");

    const cardNumberElement = elements.getElement(CardNumberElement);

    if (!cardNumberElement) {
      setMessage(labels.cardNotReady);
      setIsPaying(false);
      return;
    }

    const { error, paymentIntent } = await stripe.confirmCardPayment(
      clientSecret,
      {
        payment_method: {
          card: cardNumberElement
        }
      }
    );

    if (error) {
      const errorMessage = error.message || labels.paymentFailed;

      setMessage(errorMessage);
      setIsPaying(false);

      window.parent.postMessage(
        {
          type: "PAYMENT_FAILED",
          message: errorMessage
        },
        "*"
      );

      return;
    }

    const token = localStorage.getItem("token");

    if (!token) {
      const errorMessage = labels.tokenMissing;

      setMessage(errorMessage);
      setIsPaying(false);

      window.parent.postMessage(
        {
          type: "PAYMENT_FAILED",
          message: errorMessage
        },
        "*"
      );

      return;
    }

    const finalPaymentIntentId = paymentIntent?.id || paymentIntentId;

    const confirmRes = await fetch(`${baseUrl}/api/checkout/confirm-payment`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify({
        paymentIntentId: finalPaymentIntentId
      })
    });

    const confirmData = await confirmRes.json().catch(() => null);

    if (!confirmRes.ok) {
      const errorMessage =
        confirmData?.message ||
        confirmData?.Message ||
        labels.confirmationFailed;

      setMessage(errorMessage);
      setIsPaying(false);

      window.parent.postMessage(
        {
          type: "PAYMENT_FAILED",
          message: errorMessage
        },
        "*"
      );

      return;
    }

    const successMessage =
      confirmData?.message ||
      confirmData?.Message ||
      labels.success;

    setMessage(successMessage);
    setIsPaying(false);

    window.parent.postMessage(
      {
        type: "PAYMENT_SUCCESS",
        message: successMessage
      },
      "*"
    );
  };

  return (
      <form ref={formRef} onSubmit={handleSubmit} style={formStyle}>
      <h2 style={titleStyle}>{labels.title}</h2>

<p style={hintStyle}>{labels.hint}</p>

      <label style={labelStyle}>{labels.cardNumber}</label>
      <div style={inputBoxStyle}>
<CardNumberElement
  onReady={(element) => {
    cardNumberRef.current = element;

    setTimeout(() => {
      element.focus();
    }, 500);
  }}
  onChange={(event) => {
    setComplete((prev) => ({
      ...prev,
      number: event.complete
    }));

    if (event.complete) {
      focusStripeField("expiry");

      window.parent.postMessage(
        {
          type: "PAYMENT_STEP_MESSAGE",
          message: isAr
            ? "تم إدخال رقم البطاقة. الآن أدخلي تاريخ الانتهاء."
            : "Card number entered. Now enter the expiry date."
        },
        "*"
      );
    }
  }}
  options={{
    showIcon: true,
    style: stripeElementStyle
  }}
/>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "12px" }}>
        <div>
          <label style={labelStyle}>{labels.expiry}</label>
          <div style={inputBoxStyle}>
<CardExpiryElement
  onReady={(element) => {
    cardExpiryRef.current = element;
  }}
  onChange={(event) => {
    setComplete((prev) => ({
      ...prev,
      expiry: event.complete
    }));

    if (event.complete) {
      focusStripeField("cvc");

      window.parent.postMessage(
        {
          type: "PAYMENT_STEP_MESSAGE",
          message: isAr
            ? "تم إدخال تاريخ الانتهاء. الآن أدخلي رمز الأمان."
            : "Expiry date entered. Now enter the security code."
        },
        "*"
      );
    }
  }}
  options={{
    style: stripeElementStyle
  }}
/>
          </div>
        </div>

        <div>
          <label style={labelStyle}>{labels.cvc}</label>
          <div style={inputBoxStyle}>
<CardCvcElement
  onReady={(element) => {
    cardCvcRef.current = element;
  }}
  onChange={(event) => {
    setComplete((prev) => ({
      ...prev,
      cvc: event.complete
    }));

    if (event.complete) {
      focusStripeField("confirm");

      window.parent.postMessage(
        {
          type: "PAYMENT_STEP_MESSAGE",
          message: isAr
            ? "تم إدخال كل بيانات الدفع. اضغطي Enter أو زر تأكيد الدفع."
            : "All payment information has been entered. Press Enter or confirm payment."
        },
        "*"
      );
    }
  }}
  options={{
    style: stripeElementStyle
  }}
/>
          </div>
        </div>
      </div>

<button
  ref={confirmButtonRef}
  type="submit"
  disabled={paymentDisabled}
  style={{
    ...buttonStyle,
    opacity: paymentDisabled ? 0.6 : 1,
    cursor: paymentDisabled ? "not-allowed" : "pointer"
  }}
>
  {isPaying ? labels.processing : labels.confirm}
</button>
     <p style={hintStyle}>{labels.testCard}</p>

      {message && <p style={messageStyle}>{message}</p>}
    </form>
  );
}
const pageStyle = {
  width: "100%",
  minHeight: "100vh",
  boxSizing: "border-box",
  padding: "24px",
  background: "#1b1b2e",
  fontFamily: "Arial, sans-serif",
  color: "#ffffff"
};

const formStyle = {
  display: "flex",
  flexDirection: "column",
  gap: "16px"
};

const titleStyle = {
  margin: 0,
  fontSize: "32px",
  fontWeight: "800",
  color: "#ffffff"
};

const textStyle = {
  fontSize: "18px",
  color: "#A0CFFF"
};

const labelStyle = {
  fontSize: "18px",
  fontWeight: "700",
  color: "#A0CFFF"
};

const inputBoxStyle = {
  width: "100%",
  borderRadius: "18px",
  border: "2px solid #0094FF",
  background: "#1f1f2e",
  padding: "18px",
  minHeight: "60px",
  boxSizing: "border-box",
  boxShadow: "0 0 12px rgba(0,148,255,0.35)"
};

const buttonStyle = {
  width: "100%",
  borderRadius: "22px",
  border: "2px solid #0094FF",
  background: "#0094FF",
  padding: "18px 24px",
  textAlign: "center",
  fontSize: "24px",
  fontWeight: "800",
  color: "#ffffff",
  boxShadow: "0 0 18px rgba(0,148,255,0.45)"
};

const hintStyle = {
  fontSize: "16px",
  color: "#A0CFFF",
  fontWeight: "600"
};

const messageStyle = {
  fontSize: "18px",
  color: "#ffffff",
  fontWeight: "700"
};

const stripeElementStyle = {
  base: {
    fontSize: "20px",
    color: "#ffffff",
    fontFamily: "Arial, sans-serif",
    lineHeight: "28px",
    "::placeholder": {
      color: "#A0CFFF"
    }
  },
  invalid: {
    color: "#ff6b6b"
  }
};