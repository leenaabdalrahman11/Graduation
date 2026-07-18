import React, { useState, useRef, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import AudioRecorder from "../components/AudioRecorder.jsx";
import {
  FaSearch,
  FaShoppingCart,
  FaCube,
  FaClipboard,
  FaQuestionCircle,
} from "react-icons/fa";
console.log("HomePageBlindExtension LOADED");
const baseUrl = import.meta.env.VITE_API_URL || "https://localhost:7291";
export default function HomePageBlindExtension() {
  const [voiceLevel, setVoiceLevel] = useState(0);
  const [isListeningVisual, setIsListeningVisual] = useState(false);
  const [showHelpTooltip, setShowHelpTooltip] = useState(false);
 const speakHelp = () => {

  if (window.speechSynthesis.speaking) {
    window.speechSynthesis.cancel();
    setIsSpeakingVisual(false);
    setSpeakingLevel(0);
    return;
  }

  const helpText =
    currentLang === "ar"
      ? "الاختصارات المتاحة هي: Alt زائد A للعربية، Alt زائد E للإنجليزية، Alt زائد S لبدء التسجيل، Alt زائد P للإيقاف، Alt زائد M لكتم الصوت، Alt زائد R لإعادة الاستماع، Alt زائد K للتخطي، Alt زائد B للرجوع، Alt زائد X للخروج، Alt زائد واحد إلى ثلاثة لاختيار المنتج."
      : "Available shortcuts are: Alt plus A for Arabic, Alt plus E for English, Alt plus S to start recording, Alt plus P to stop, Alt plus M to mute, Alt plus R to replay, Alt plus K to skip, Alt plus B to go back, Alt plus X to exit, Alt plus one to three to select products.";

  speakText(helpText);
};
  const paymentFields = [
    {
      key: "cardNumber",
      ar: "رقم البطاقة",
      en: "card number",
    },
    {
      key: "expiry",
      ar: "تاريخ الانتهاء",
      en: "expiry date",
    },
    {
      key: "cvc",
      ar: "رمز الأمان",
      en: "security code",
    },
  ];

const handlePaymentTypedInput = (value) => {
  const cleanValue = value.trim();

  if (!cleanValue) {
    const screenMessage =
      currentLang === "ar"
        ? "من فضلك اكتب أمرًا مثل: تأكيد الدفع، أو اكتب البيانات داخل حقول الدفع."
        : "Please type a command like confirm payment, or enter the payment data inside the payment fields.";

    const speechMessage =
      currentLang === "ar"
        ? "مِنْ فَضْلِكَ اُكْتُب أَمْرًا مِثْلَ: تَأْكِيدِ الدَّفْعِ، أَوِ اُكْتُب الْبَيَانَاتِ دَاخِلَ حُقُولِ الدَّفْعِ."
        : screenMessage;

    setScreenText(screenMessage);
    setLastSpokenText(speechMessage);
    speakText(speechMessage);
    return;
  }

  const lower = cleanValue.toLowerCase();

  const isConfirmPayment =
    lower.includes("تأكيد الدفع") ||
    lower.includes("تاكيد الدفع") ||
    lower.includes("ادفع") ||
    lower.includes("confirm") ||
    lower.includes("pay");

  if (isConfirmPayment) {
    paymentIframeRef.current?.contentWindow?.postMessage(
      {
        type: "PAYMENT_SUBMIT",
      },
      "http://localhost:5174",
    );

    const screenMessage =
      currentLang === "ar"
        ? "جاري محاولة تأكيد الدفع."
        : "Trying to confirm payment.";

    const speechMessage =
      currentLang === "ar"
        ? "جَارِي مُحَاوَلَةُ تَأْكِيدِ الدَّفْعِ."
        : screenMessage;

    setScreenText(screenMessage);
    setLastSpokenText(speechMessage);
    speakText(speechMessage);
    return;
  }

  const currentField =
    paymentFields[paymentInputStep] || paymentFields[0];

  paymentIframeRef.current?.contentWindow?.postMessage(
    {
      type: "PAYMENT_FOCUS_FIELD",
      field: currentField.key,
    },
    "http://localhost:5174",
  );

  const screenMessage =
    currentLang === "ar"
      ? `تم تحديد حقل ${currentField.ar}. اكتب البيانات داخل حقل الدفع نفسه.`
      : `${currentField.en} field is focused. Type the payment data inside the payment field.`;

  const speechMessage =
    currentLang === "ar"
      ? `تَمَّ تَحْدِيدُ حَقْلِ ${currentField.ar}. اُكْتُب الْبَيَانَاتِ دَاخِلَ حَقْلِ الدَّفْعِ نَفْسِهِ.`
      : screenMessage;

  setScreenText(screenMessage);
  setLastSpokenText(speechMessage);
  speakText(speechMessage);
};
  const [selectedProductShortcutIndex, setSelectedProductShortcutIndex] =
    useState(null);
  const [status, setStatus] = useState("Ready");
  const [recognizedText, setRecognizedText] = useState("-");
  const [screenText, setScreenText] = useState("-");
  const [isMuted, setIsMuted] = useState(false);
  const [availableVoices, setAvailableVoices] = useState([]);
  const [voiceStartStatus, setVoiceStartStatus] = useState("Loading...");
  const [selectedLanguage, setSelectedLanguage] = useState("");
  const [languageStep, setLanguageStep] = useState(true);
  const [lastSpokenText, setLastSpokenText] = useState("");
  const [orders, setOrders] = useState([]);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [products, setProducts] = useState([]);
  const [cartItems, setCartItems] = useState([]);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [rightPanelMode, setRightPanelMode] = useState("commands");
  const [isSpeakingVisual, setIsSpeakingVisual] = useState(false);
  const [speakingLevel, setSpeakingLevel] = useState(0);
  const [clientSecret, setClientSecret] = useState("");
  const [paymentIntentId, setPaymentIntentId] = useState("");
  const [paymentFrameUrl, setPaymentFrameUrl] = useState("");
  const [typedCommand, setTypedCommand] = useState("");
  const [loginEmail, setLoginEmail] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState("");
  const [authMode, setAuthMode] = useState("login");

  const [registerFullName, setRegisterFullName] = useState("");
  const [registerEmail, setRegisterEmail] = useState("");
  const [registerPassword, setRegisterPassword] = useState("");
  const [registerConfirmPassword, setRegisterConfirmPassword] = useState("");

  const [registerLoading, setRegisterLoading] = useState(false);
  const [registerError, setRegisterError] = useState("");
  const [registerMessage, setRegisterMessage] = useState("");
  const isMutedRef = useRef(false);
  const recorderRef = useRef(null);
  const streamRef = useRef(null);
  const mainHeadingRef = useRef(null);
  const chunksRef = useRef([]);
  const audioContextRef = useRef(null);
  const analyserRef = useRef(null);
  const vadAnimationRef = useRef(null);
  const hasSpeechRef = useRef(false);
  const lastVoiceTimeRef = useRef(null);
  const isStoppingRef = useRef(false);
  const paymentIframeRef = useRef(null);
  const [paymentInputStep, setPaymentInputStep] = useState(0);
 const uiText = {
  ar: {
    ready: "جاهز",
    loading: "جاري التحميل...",
    chooseLanguage: "اختيار اللغة",
    waitingLanguageSelection: "بانتظار اختيار اللغة",
    userSaid: "قال المستخدم:",
    noSpeechDetected: "لم يتم التقاط أي كلام بعد.",
    voiceCommands: "الأوامر الصوتية",
    selectLanguageOrVoice: "اختيار اللغة أو قولها بالصوت",
    availableOptions: "اختر واحدًا من الخيارات المتاحة",
    arabic: "العربية",
    english: "الإنجليزية",
    pleaseSayLanguage:
      "من فضلك، قول: عربي أو إنجليزي، أو استخدم Alt زائد A للعربية وAlt زائد E للإنجليزية.",
    languageSelectedStatus: "تم اختيار اللغة.",
    mutedRecordingDisabled: "تم الكتم، والتسجيل متوقف.",
    listening: "أستمع الآن.",
    recordingError: "خطأ في التسجيل.",
    uploadingAudio: "جاري رفع الصوت",
    transcriptionError: "خطأ في تحويل الصوت إلى نص.",
    couldNotTranscribe: "تعذر تحويل الصوت إلى نص.",
    noValidInput: "لم يتم استلام إدخال صحيح.",
    pleaseSaySomething: "من فضلك، قول شيئًا للبحث.",
    processing: "جاري المعالجة.",
    readyAfterResponse: "جاهز",
    sendCommandError: "خطأ في إرسال الطلب.",
    couldNotSendCommand: "تعذر إرسال الطلب إلى الـ API.",
    muted: "تم كتم الصوت.",
    unmuted: "تم إلغاء الكتم.",
    replayingAudio: "إعادة تشغيل الصوت",
    skippedAudio: "تم تخطي الصوت",
    exited: "تم الإغلاق",
    goingBack: "الرجوع إلى القائمة الرئيسية",
    loginFirst: "يجب تسجيل الدخول أولًا.",
    productsTitle: "المنتجات",
    statusLabel: "الحالة:",
    paymentIntro:
      "سأفتح الآن نموذج الدفع داخل الموقع. ستجد حقول رقم البطاقة، وتاريخ الانتهاء، ورمز الأمان.",
    paymentStepTitle: "الدفع",
    paymentCardNumber: "رقم البطاقة",
    paymentExpiry: "تاريخ الانتهاء",
    paymentCvc: "رمز الأمان",
    paymentSubmit: "تأكيد الدفع",
    paymentSuccess: "تم الدفع بنجاح",
    paymentFailed: "فشل الدفع",
    welcomeMessage:
      "مرحبًا، كيف أستطيع مساعدتك؟ يمكنك أن تقول: ابحث عن منتج، اعرض عناصر السلة، تتبع آخر طلب، أو اعرض طلباتي.",
  },

  en: {
    ready: "Ready",
    loading: "Loading...",
    chooseLanguage: "Choose language",
    waitingLanguageSelection: "Waiting for language selection",
    userSaid: "User said:",
    noSpeechDetected: "No speech detected yet",
    voiceCommands: "Voice Commands",
    selectLanguageOrVoice: "Select a language or say it by voice",
    availableOptions: "Choose one of the available options",
    arabic: "Arabic",
    english: "English",
    pleaseSayLanguage: "Please say Arabic or English.",
    languageSelectedStatus: "Language selected",
    mutedRecordingDisabled: "Muted - recording disabled",
    listening: "Listening",
    recordingError: "Recording error",
    uploadingAudio: "Uploading audio",
    transcriptionError: "Transcription error",
    couldNotTranscribe: "Could not transcribe audio.",
    noValidInput: "No valid input received",
    pleaseSaySomething: "Please say something for the search.",
    processing: "Processing",
    readyAfterResponse: "Ready",
    sendCommandError: "Error sending command",
    couldNotSendCommand: "Could not send command to API.",
    muted: "Muted",
    unmuted: "Unmuted",
    replayingAudio: "Replaying audio",
    skippedAudio: "Skipped audio",
    exited: "Exited",
    goingBack: "Going back to the main menu",
    loginFirst: "You need to log in first.",
    productsTitle: "Products",
    statusLabel: "Status:",
    paymentIntro:
      "I will now open the payment form inside the website. You will find card number, expiry date, and security code fields.",
    paymentStepTitle: "Payment",
    paymentCardNumber: "Card number",
    paymentExpiry: "Expiry date",
    paymentCvc: "Security code",
    paymentSubmit: "Confirm payment",
    paymentSuccess: "Payment completed successfully",
    paymentFailed: "Payment failed",
    welcomeMessage:
      "Hi, how can I help you? You can say: Search for a product, View cart items, Track your latest order, or View my orders.",
  },
};
const speechText = {
  ar: {
    ready: "جَاهِزٌ",
    loading: "جَارِي التَّحْمِيلِ.",
    chooseLanguage: "اخْتِيَارُ اللُّغَةِ",
    waitingLanguageSelection: "بِانْتِظَارِ اخْتِيَارِ اللُّغَةِ",
    userSaid: "قَالَ الْمُسْتَخْدِمُ:",
    noSpeechDetected: "لَمْ يَتِمَّ الْتِقَاطُ أَيِّ كَلَامٍ بَعْدُ.",
    voiceCommands: "الْأَوَامِرُ الصَّوْتِيَّةُ",
    selectLanguageOrVoice: "اِخْتَرْ اللُّغَةَ أَوْ قُول هَا بِالصَّوْتِ.",
    availableOptions: "اِخْتَرْ وَاحِدًا مِنَ الْخِيَارَاتِ الْمُتَاحَةِ.",
    arabic: "الْعَرَبِيَّةُ",
    english: "الْإِنْجِلِيزِيَّةُ",
    pleaseSayLanguage:
      "مِنْ فَضْلِكَ  قُول : عَرَبِيٌّ أَوْ إِنْجِلِيزِيٌّ، أَوِ اسْتَخْدِم  Alt زَائِد A لِلْعَرَبِيَّةِ، وَAlt زَائِد E لِلْإِنْجِلِيزِيَّةِ.",
    languageSelectedStatus: "تَمَّ اخْتِيَارُ اللُّغَةِ.",
    mutedRecordingDisabled: "تَمَّ الْكَتْمُ، وَالتَّسْجِيلُ مُتَوَقِّفٌ.",
    listening: "أَسْتَمِعُ الْآنَ.",
    recordingError: "خَطَأٌ فِي التَّسْجِيلِ.",
    uploadingAudio: "جَارِي رَفْعِ الصَّوْتِ.",
    transcriptionError: "خَطَأٌ فِي تَحْوِيلِ الصَّوْتِ إِلَى نَصٍّ.",
    couldNotTranscribe: "تَعَذَّرَ تَحْوِيلُ الصَّوْتِ إِلَى نَصٍّ.",
    noValidInput: "لَمْ يَتِمَّ اسْتِلَامُ إِدْخَالٍ صَحِيحٍ.",
    pleaseSaySomething: "مِنْ فَضْلِكَ قُول  شَيْئًا لِلْبَحْثِ.",
    processing: "جَارِي الْمُعَالَجَةِ.",
    readyAfterResponse: "جَاهِزٌ",
    sendCommandError: "خَطَأٌ فِي إِرْسَالِ الطَّلَبِ.",
    couldNotSendCommand: "تَعَذَّرَ إِرْسَالُ الطَّلَبِ إِلَى الْخَادِمِ.",
    muted: "تَمَّ كَتْمُ الصَّوْتِ.",
    unmuted: "تَمَّ إِلْغَاءُ الْكَتْمِ.",
    replayingAudio: "إِعَادَةُ تَشْغِيلِ الصَّوْتِ.",
    skippedAudio: "تَمَّ تَخَطِّي الصَّوْتِ.",
    exited: "تَمَّ الْإِغْلَاقُ.",
    goingBack: "الرُّجُوعُ إِلَى الْقَائِمَةِ الرَّئِيسِيَّةِ.",
    loginFirst: "يَجِبُ تَسْجِيلُ الدُّخُولِ أَوَّلًا.",
    paymentIntro:
      "سَأَفْتَحُ الْآنَ نَمُوذَجَ الدَّفْعِ دَاخِلَ الْمَوْقِعِ. سَتَجِد حُقُولَ رَقْمِ الْبِطَاقَةِ، وَتَارِيخِ الِانْتِهَاءِ، وَرَمْزِ الْأَمَانِ.",
    paymentSuccess: "تَمَّ الدَّفْعُ بِنَجَاحٍ.",
    paymentFailed: "فَشِلَ الدَّفْعُ.",
    welcomeMessage:
      "مَرْحَبًا، كَيْفَ أَسْتَطِيعُ مُسَاعَدَتَكَ؟ يُمْكِنُكَ أَنْ تَقُول : اِبْحَث عَنْ مُنْتَجٍ، اِعْرِض عَنَاصِرَ السَّلَّةِ، تَتَبَّع آخِرَ طَلَبٍ، أَوِ اعْرِضِي طَلَبَاتِي.",
  },

  en: {
    ...uiText.en,
  },
};
  const currentLang = selectedLanguage === "en" ? "en" : "ar";

const t = uiText[currentLang];       
const speech = speechText[currentLang]; 
  const getAccessToken = () => {
    return localStorage.getItem("token") || localStorage.getItem("accessToken");
  };
 const showLoginPanel = () => {
  const screenMessage =
    currentLang === "ar"
      ? "يجب تسجيل الدخول أولًا. أدخل البريد الإلكتروني، ثم كلمة المرور. إذا لم يكن لديك حساب، اختار إنشاء حساب جديد."
      : "You need to log in first. Enter your email, then your password. If you do not have an account, choose create account.";

  const speechMessage =
    currentLang === "ar"
      ? "يَجِبُ تَسْجِيلُ الدُّخُولِ أَوَّلًا. أَدْخِل الْبَرِيدَ الْإِلِكْتُرُونِيَّ، ثُمَّ كَلِمَةَ الْمُرُورِ. إِذَا لَمْ يَكُنْ لَدَيْكَ حِسَابٌ، اِخْتَرْ إِنْشَاءَ حِسَابٍ جَدِيدٍ."
      : screenMessage;

  setAuthMode("login");
  setRightPanelMode("login");
  setStatus(t.loginFirst);

  setScreenText(screenMessage);
  setLastSpokenText(speechMessage);

  if (!isMutedRef.current) {
    speakText(speechMessage);
  }

  setTimeout(() => {
    document.getElementById("inline-login-email")?.focus();
  }, 900);
};
const showLoginForm = () => {
  const screenMessage =
    currentLang === "ar"
      ? "تم فتح نموذج تسجيل الدخول. أدخل البريد الإلكتروني ثم كلمة المرور. اضغط Tab للانتقال بين الحقول."
      : "Login form opened. Enter your email, then password. Press Tab to move between fields.";

  const speechMessage =
    currentLang === "ar"
      ? "تَمَّ فَتْحُ نَمُوذَجِ تَسْجِيلِ الدُّخُولِ. أَدْخِل الْبَرِيدَ الْإِلِكْتُرُونِيَّ، ثُمَّ كَلِمَةَ الْمُرُورِ. اضْغَط  Tab لِلِانْتِقَالِ بَيْنَ الْحُقُولِ."
      : screenMessage;

  setAuthMode("login");
  setRightPanelMode("login");

  setScreenText(screenMessage);
  setLastSpokenText(speechMessage);
  speakText(speechMessage);
};

 const showRegisterForm = () => {
  const screenMessage =
    currentLang === "ar"
      ? "تم فتح نموذج إنشاء حساب جديد. أدخل الاسم الكامل، ثم البريد الإلكتروني، ثم كلمة المرور، ثم تأكيد كلمة المرور. اضغط Tab للانتقال بين الحقول."
      : "Create account form opened. Enter full name, email, password, then confirm password. Press Tab to move between fields.";

  const speechMessage =
    currentLang === "ar"
      ? "تَمَّ فَتْحُ نَمُوذَجِ إِنْشَاءِ حِسَابٍ جَدِيدٍ. أَدْخِل الِاسْمَ الْكَامِلَ، ثُمَّ الْبَرِيدَ الْإِلِكْتُرُونِيَّ، ثُمَّ كَلِمَةَ الْمُرُورِ، ثُمَّ تَأْكِيدَ كَلِمَةِ الْمُرُورِ. اضْغَط Tab لِلِانْتِقَالِ بَيْنَ الْحُقُولِ."
      : screenMessage;

  setAuthMode("register");
  setRightPanelMode("login");

  setScreenText(screenMessage);
  setLastSpokenText(speechMessage);
  speakText(speechMessage);
};

  const handleInlineLogin = async () => {
    setLoginError("");

    if (!loginEmail.trim()) {
      const message =
        currentLang === "ar"
          ? "من فضلك أدخل البريد الإلكتروني."
          : "Please enter your email.";

      setLoginError(message);
      speakText(message);
      return;
    }

    if (!loginPassword.trim()) {
      const message =
        currentLang === "ar"
          ? "من فضلك أدخل كلمة المرور."
          : "Please enter your password.";

      setLoginError(message);
      speakText(message);
      return;
    }

    try {
      setLoginLoading(true);

      const response = await fetch(`${baseUrl}/api/auth/Account/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: loginEmail,
          password: loginPassword,
        }),
      });

      const data = await response.json().catch(() => null);

      const accessToken = data?.accessToken || data?.AccessToken;
      const refreshToken = data?.refreshToken || data?.RefreshToken;
      const isSuccess = data?.isSuccess ?? data?.IsSuccess;

      if (!response.ok || isSuccess === false || !accessToken) {
        const message =
          data?.message ||
          data?.Message ||
          (currentLang === "ar"
            ? "فشل تسجيل الدخول. تأكد من البريد وكلمة المرور."
            : "Login failed. Please check your email and password.");

        setLoginError(message);
        speakText(message);
        return;
      }

      localStorage.setItem("token", accessToken);
      localStorage.setItem("accessToken", accessToken);

      if (refreshToken) {
        localStorage.setItem("refreshToken", refreshToken);
      }

      localStorage.setItem(
        "user",
        JSON.stringify({
          userId: data?.userId || data?.UserId,
          email: data?.email || data?.Email || loginEmail,
          fullName: data?.fullName || data?.FullName || "",
        }),
      );

      const successMessage =
        currentLang === "ar"
          ? "تم تسجيل الدخول بنجاح. مرحبًا بك في المساعد الصوتي. يمكنك الآن استخدام الأوامر الصوتية: ابحث عن منتج، اعرض عناصر السلة، تتبع آخر طلب، أو اعرض طلباتي."
          : "Login successful. Welcome to the voice assistant. You can now use the voice commands: search for a product, view cart items, track your latest order, or view my orders.";

      setLoginPassword("");
      setAuthMode("login");
      setRightPanelMode("commands");
      setStatus(t.readyAfterResponse);
      setScreenText(successMessage);
      setLastSpokenText(successMessage);
      setRecognizedText("-");

      speakText(successMessage);
    } catch (error) {
      console.error("INLINE LOGIN ERROR:", error);

      const message =
        currentLang === "ar"
          ? "حدث خطأ في الاتصال بالسيرفر."
          : "A server connection error occurred.";

      setLoginError(message);
      speakText(message);
    } finally {
      setLoginLoading(false);
    }
  };
  const handleInlineRegister = async () => {
    setRegisterError("");
    setRegisterMessage("");

    if (!registerFullName.trim()) {
      const message =
        currentLang === "ar"
          ? "من فضلك أدخل الاسم الكامل."
          : "Please enter your full name.";

      setRegisterError(message);
      speakText(message);
      return;
    }

    if (!registerEmail.trim()) {
      const message =
        currentLang === "ar"
          ? "من فضلك أدخل البريد الإلكتروني."
          : "Please enter your email.";

      setRegisterError(message);
      speakText(message);
      return;
    }

    if (!registerPassword.trim()) {
      const message =
        currentLang === "ar"
          ? "من فضلك أدخل كلمة المرور."
          : "Please enter your password.";

      setRegisterError(message);
      speakText(message);
      return;
    }

    if (registerPassword !== registerConfirmPassword) {
      const message =
        currentLang === "ar"
          ? "كلمة المرور وتأكيد كلمة المرور غير متطابقين."
          : "Password and confirm password do not match.";

      setRegisterError(message);
      speakText(message);
      return;
    }

    try {
      setRegisterLoading(true);

      const response = await fetch(`${baseUrl}/api/auth/Account/register`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },

        body: JSON.stringify({
          fullName: registerFullName,
          FullName: registerFullName,

          email: registerEmail,
          Email: registerEmail,

          password: registerPassword,
          Password: registerPassword,

          confirmPassword: registerConfirmPassword,
          ConfirmPassword: registerConfirmPassword,
        }),
      });

      const data = await response.json().catch(() => null);

      const isSuccess = data?.isSuccess ?? data?.IsSuccess;

      if (!response.ok || isSuccess === false) {
        const errors = data?.errors || data?.Errors;
        const errorText = Array.isArray(errors) ? errors.join(" - ") : "";

        const message =
          data?.message ||
          data?.Message ||
          errorText ||
          (currentLang === "ar"
            ? "فشل إنشاء الحساب."
            : "Account creation failed.");

        setRegisterError(message);
        speakText(message);
        return;
      }

      const successMessage =
        data?.message ||
        data?.Message ||
        (currentLang === "ar"
          ? "تم إنشاء الحساب بنجاح. من فضلك افحص بريدك الإلكتروني لتأكيد الحساب، ثم سجل الدخول."
          : "Account created successfully. Please check your email to confirm your account, then log in.");

      setRegisterMessage(successMessage);
      setScreenText(successMessage);
      setLastSpokenText(successMessage);

      setLoginEmail(registerEmail);
      setAuthMode("login");

      setRegisterFullName("");
      setRegisterPassword("");
      setRegisterConfirmPassword("");

      speakText(successMessage);
    } catch (error) {
      console.error("INLINE REGISTER ERROR:", error);

      const message =
        currentLang === "ar"
          ? "حدث خطأ في الاتصال بالسيرفر أثناء إنشاء الحساب."
          : "A server connection error occurred while creating the account.";

      setRegisterError(message);
      speakText(message);
    } finally {
      setRegisterLoading(false);
    }
  };

  const refreshAccessToken = async () => {
    const accessToken =
      localStorage.getItem("token") || localStorage.getItem("accessToken");
    const refreshToken = localStorage.getItem("refreshToken");

    if (!accessToken || !refreshToken) {
      return null;
    }

    const response = await fetch(`${baseUrl}/api/auth/Account/refreshToken`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        accessToken,
        refreshToken,
      }),
    });

    const data = await response.json().catch(() => null);

    if (!response.ok || !data) {
      localStorage.removeItem("token");
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("user");
      return null;
    }

    const newAccessToken = data.accessToken || data.AccessToken;
    const newRefreshToken = data.refreshToken || data.RefreshToken;

    if (!newAccessToken) {
      return null;
    }

    localStorage.setItem("token", newAccessToken);
    localStorage.setItem("accessToken", newAccessToken);

    if (newRefreshToken) {
      localStorage.setItem("refreshToken", newRefreshToken);
    }

    return newAccessToken;
  };

  const authFetch = async (url, options = {}) => {
    let token = getAccessToken();

    if (!token) {
      throw new Error(t.loginFirst);
    }

    let response = await fetch(url, {
      ...options,
      headers: {
        ...(options.headers || {}),
        Authorization: `Bearer ${token}`,
      },
    });

    if (response.status !== 401) {
      return response;
    }

    const newToken = await refreshAccessToken();

    if (!newToken) {
      throw new Error(t.loginFirst);
    }

    response = await fetch(url, {
      ...options,
      headers: {
        ...(options.headers || {}),
        Authorization: `Bearer ${newToken}`,
      },
    });

    return response;
  };
  const getAssistantTheme = () => {
    if (rightPanelMode === "payment" || rightPanelMode === "paymentSuccess") {
      return {
        main: "#ffb300",
        glow: "rgba(255,179,0,0.65)",
        background: "rgba(255,179,0,0.10)",
      };
    }

    if (
      status === t.processing ||
      status === "Processing" ||
      status === "جاري المعالجة"
    ) {
      return {
        main: "#9b5cff",
        glow: "rgba(155,92,255,0.65)",
        background: "rgba(155,92,255,0.10)",
      };
    }

    if (isListeningVisual) {
      return {
        main: "#00ff88",
        glow: "rgba(0,255,136,0.65)",
        background: "rgba(0,255,136,0.10)",
      };
    }

    if (isSpeakingVisual) {
      return {
        main: "#00c8ff",
        glow: "rgba(0,200,255,0.65)",
        background: "rgba(0,200,255,0.10)",
      };
    }

    if (
      status === t.sendCommandError ||
      status === t.recordingError ||
      status === t.transcriptionError
    ) {
      return {
        main: "#ff3b3b",
        glow: "rgba(255,59,59,0.65)",
        background: "rgba(255,59,59,0.10)",
      };
    }

    return {
      main: "#0094FF",
      glow: "rgba(0,148,255,0.55)",
      background: "rgba(0,148,255,0.08)",
    };
  };

  const assistantTheme = getAssistantTheme();
useEffect(() => {
  const handleExtensionOpened = () => {
    setTimeout(() => {
      const welcomeSpeechText =
        selectedLanguage === "en"
          ? "Welcome to the voice assistant. Please choose your language. Press Alt plus A for Arabic, or Alt plus E for English."
          : "مَرْحَبًا بِكَ فِي الْمُسَاعِدِ الصَّوْتِيِّ. مِنْ فَضْلِكَ اِخْتَرْ اللُّغَةَ. اضْغَط Alt زَائِد A لِلُّغَةِ الْعَرَبِيَّةِ، أَوْ Alt زَائِد E لِلُّغَةِ الْإِنْجِلِيزِيَّةِ.";

      const welcomeScreenText =
        selectedLanguage === "en"
          ? "Welcome to the voice assistant. Please choose your language."
          : "مرحبًا بك في المساعد الصوتي. من فضلك اختار اللغة.";

      setScreenText(welcomeScreenText);
      setLastSpokenText(welcomeSpeechText);
      speakText(welcomeSpeechText);
    }, 500);
  };

  window.addEventListener(
    "VOICE_EXTENSION_OPENED",
    handleExtensionOpened,
  );

  return () => {
    window.removeEventListener(
      "VOICE_EXTENSION_OPENED",
      handleExtensionOpened,
    );
  };
}, [selectedLanguage]);

useEffect(() => {
  if (rightPanelMode === "login" || rightPanelMode === "payment") {
    return;
  }

  const input = document.getElementById("typed-command-input");

  if (input) {
    setTimeout(() => {
      input.focus({ preventScroll: true });
    }, 100);
    return;
  }

  if (mainHeadingRef.current) {
    mainHeadingRef.current.focus();
  }
}, [languageStep, screenText, rightPanelMode]);
  useEffect(() => {
    const handlePaymentMessage = (event) => {
      if (event.origin !== "http://localhost:5174") {
        return;
      }
      if (event.data?.type === "PAYMENT_STEP_MESSAGE") {
        const stepMessage = event.data?.message || "";

        setScreenText(stepMessage);
        setLastSpokenText(stepMessage);

        if (!isMutedRef.current) {
          speakText(stepMessage);
        }
      }
      if (event.data?.type === "PAYMENT_SUCCESS") {
        const successText =
          currentLang === "ar"
            ? "تم الدفع بنجاح."
            : "Payment completed successfully.";

        setStatus(t.readyAfterResponse);
        setScreenText(successText);
        setLastSpokenText(successText);
        setRightPanelMode("paymentSuccess");

        if (!isMutedRef.current) {
          speakText(successText);
        }
      }

      if (event.data?.type === "PAYMENT_FAILED") {
        const failText =
          event.data?.message ||
          (currentLang === "ar" ? "فشل الدفع." : "Payment failed.");

        setStatus(t.paymentFailed);
        setScreenText(failText);

        if (!isMutedRef.current) {
          speakText(failText);
        }
      }
    };

    window.addEventListener("message", handlePaymentMessage);

    return () => {
      window.removeEventListener("message", handlePaymentMessage);
    };
  }, [currentLang, t]);
  useEffect(() => {
    const loadVoices = () => {
      const voices = window.speechSynthesis.getVoices();

      console.log(
        "AVAILABLE VOICES:",
        voices.map((v) => ({
          name: v.name,
          lang: v.lang,
        })),
      );

      setAvailableVoices(voices);
    };

    loadVoices();

    window.speechSynthesis.onvoiceschanged = loadVoices;

    const timer = setTimeout(loadVoices, 1000);

    return () => {
      window.speechSynthesis.onvoiceschanged = null;
      clearTimeout(timer);
    };
  }, []);
  useEffect(() => {
    const fetchVoiceStartData = async () => {
      try {
        const response = await fetch(`${baseUrl}/api/voice/start?language=en`);
        const data = await response.json();
        setVoiceStartStatus(data.status || "Ready");
      } catch (error) {
        setVoiceStartStatus("Error fetching data");
        console.error("Error fetching data:", error);
      }
    };

    fetchVoiceStartData();
  }, []);
  const speakThenRun = (text, callback) => {
    if (!text) {
      callback?.();
      return;
    }

    if (isMutedRef.current) {
      callback?.();
      return;
    }

    speakText(text);

    const waitTime = text.length * 120 + 500;

    setTimeout(() => {
      callback?.();
    }, waitTime);
  };
  const speakText = (text) => {
    if (!text || isMutedRef.current) return;
    if (!("speechSynthesis" in window)) return;

    const synth = window.speechSynthesis;
    synth.cancel();
    synth.resume();

    const speakNow = () => {
      const utterance = new SpeechSynthesisUtterance(text);
      let speakingAnimation = null;

      utterance.onstart = () => {
        setIsSpeakingVisual(true);

        speakingAnimation = setInterval(() => {
          setSpeakingLevel(Math.random() * 35 + 10);
        }, 100);
      };

      utterance.onend = () => {
        setIsSpeakingVisual(false);
        setSpeakingLevel(0);

        clearInterval(speakingAnimation);
      };

      utterance.onerror = () => {
        setIsSpeakingVisual(false);
        setSpeakingLevel(0);

        clearInterval(speakingAnimation);
      };
      const hasArabic = /[\u0600-\u06FF]/.test(text);

      const voices =
        availableVoices.length > 0 ? availableVoices : synth.getVoices();

      console.log(
        "SPEAKING WITH VOICES:",
        voices.map((v) => ({
          name: v.name,
          lang: v.lang,
        })),
      );

      let selectedVoice = null;

      if (hasArabic || selectedLanguage === "ar") {
        selectedVoice =
          voices.find((v) => v.lang?.toLowerCase() === "ar-eg") ||
          voices.find((v) => v.lang?.toLowerCase() === "ar-ae") ||
          voices.find((v) => v.lang?.toLowerCase() === "ar-jo") ||
          voices.find(
            (v) =>
              v.lang?.toLowerCase().startsWith("ar") &&
              !v.name?.toLowerCase().includes("naayf"),
          ) ||
          voices.find((v) => v.lang?.toLowerCase().startsWith("ar")) ||
          null;

        utterance.lang = selectedVoice?.lang || "ar-SA";
      } else {
        selectedVoice =
          voices.find((v) => v.lang?.toLowerCase() === "en-us") ||
          voices.find((v) => v.lang?.toLowerCase().startsWith("en")) ||
          null;

        utterance.lang = selectedVoice?.lang || "en-US";
      }

      if (selectedVoice) {
        utterance.voice = selectedVoice;
        console.log("SELECTED VOICE:", selectedVoice.name, selectedVoice.lang);
      } else {
        console.warn("NO MATCHING VOICE FOUND, USING BROWSER DEFAULT");
      }

      utterance.rate = hasArabic || selectedLanguage === "ar" ? 0.85 : 1;
      utterance.pitch = 1;
      utterance.volume = 1;

      synth.speak(utterance);
    };

    setTimeout(speakNow, 500);
  };
  const playRecordSound = (type) => {
    return new Promise((resolve) => {
      const AudioContextClass =
        window.AudioContext || window.webkitAudioContext;

      if (!AudioContextClass) {
        resolve();
        return;
      }

      const audioCtx = new AudioContextClass();
      const oscillator = audioCtx.createOscillator();
      const gainNode = audioCtx.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(audioCtx.destination);

      if (type === "start") {
        oscillator.type = "sine";
        oscillator.frequency.value = 950;
      } else {
        oscillator.type = "triangle";
        oscillator.frequency.value = 420;
      }

      gainNode.gain.value = 0.25;

      oscillator.start();

      setTimeout(
        () => {
          oscillator.stop();
          audioCtx.close().catch(() => {});
          resolve();
        },
        type === "start" ? 180 : 260,
      );
    });
  };
  const startRecording = async () => {
    if (recorderRef.current && recorderRef.current.state === "recording") {
      return;
    }

    try {
      window.speechSynthesis.cancel();

      await playRecordSound("start");

      await new Promise((resolve) => setTimeout(resolve, 200));

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;
      chunksRef.current = [];

      hasSpeechRef.current = false;
      lastVoiceTimeRef.current = null;
      isStoppingRef.current = false;

      const recorder = new MediaRecorder(stream, { mimeType: "audio/webm" });
      recorderRef.current = recorder;

      recorder.onstart = () => {
        setStatus(t.listening);
        setIsListeningVisual(true);
        startSilenceDetection(stream);
      };

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunksRef.current.push(event.data);
        }
      };

      recorder.onstop = async () => {
        const blob = new Blob(chunksRef.current, { type: "audio/webm" });

        recorderRef.current = null;

        playRecordSound("stop");

        if (blob.size > 0) {
          await transcribeAndSend(blob);
        }
      };

      recorder.start();
    } catch (error) {
      console.error(error);
      setStatus(t.recordingError);
      isStoppingRef.current = false;
    }
  };
 const handleLanguageSelect = (lang) => {
  const isArabic = lang === "ar";

  setLanguageStep(false);
  setSelectedLanguage(lang);

  const langText = isArabic ? uiText.ar : uiText.en;          
  const langSpeech = isArabic ? speechText.ar : speechText.en; 

  setStatus(langText.languageSelectedStatus);
  setRecognizedText(isArabic ? langText.arabic : langText.english);

  const welcomeWithHelp =
    isArabic
      ? `${langSpeech.welcomeMessage} لِلْمُسَاعَدَةِ وَسَمَاعِ جَمِيعِ الْأَوَامِرِ وَالِاخْتِصَارَاتِ الْمُتَاحَةِ، اضْغَط Alt زَائِد H فِي أَيِّ وَقْتٍ.`
      : `${langSpeech.welcomeMessage} For help and to hear all available commands and shortcuts, press Alt plus H at any time.`;

  if (!getAccessToken()) {
    const loginScreenMessage =
      lang === "ar"
        ? "تم اختيار اللغة العربية. يجب تسجيل الدخول أولًا.أدخل البريد الإلكتروني في الحقل الأول، ثم كلمة المرور في الحقل الثاني. إذا لم يكن لديك حساب، اختار إنشاء حساب جديد."
        : "English selected. You need to log in first. Enter your email in the first field, then your password in the second field. If you do not have an account, choose create account.";

    const loginSpeechMessage =
      lang === "ar"
        ? "تَمَّ اخْتِيَارُ اللُّغَةِ الْعَرَبِيَّةِ. يَجِبُ تَسْجِيلُ الدُّخُولِ أَوَّلًا. أَدْخِل الْبَرِيدَ الْإِلِكْتُرُونِيَّ فِي الْحَقْلِ الْأَوَّلِ، ثُمَّ كَلِمَةَ الْمُرُورِ فِي الْحَقْلِ الثَّانِي. إِذَا لَمْ يَكُنْ لَدَيْكَ حِسَابٌ، اِخْتَرْ إِنْشَاءَ حِسَابٍ جَدِيدٍ."
        : loginScreenMessage;

    setAuthMode("login");
    setRightPanelMode("login");
    setStatus(langText.loginFirst || "Login required");

    setScreenText(loginScreenMessage);
    setLastSpokenText(loginSpeechMessage);
    speakText(loginSpeechMessage);

    setTimeout(() => {
      document.getElementById("inline-login-email")?.focus();
    }, 900);

    return;
  }

  setRightPanelMode("commands");

  setScreenText(langText.welcomeMessage);

  setLastSpokenText(welcomeWithHelp);
  speakText(welcomeWithHelp);
};
  const startSilenceDetection = (stream) => {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    const audioContext = new AudioContextClass();

    const source = audioContext.createMediaStreamSource(stream);
    const analyser = audioContext.createAnalyser();

    analyser.fftSize = 2048;
    source.connect(analyser);

    audioContextRef.current = audioContext;
    analyserRef.current = analyser;

    const dataArray = new Uint8Array(analyser.fftSize);

    const requiredSilenceMs = 3000; 
    const startSpeechThreshold = 8; 
    const continueSpeechThreshold = 3;
    const minSpeechFrames = 4; 

    let speechFrames = 0;

    const detect = () => {
      if (isStoppingRef.current) return;

      analyser.getByteTimeDomainData(dataArray);

      let sum = 0;

      for (let i = 0; i < dataArray.length; i++) {
        const value = dataArray[i] - 128;
        sum += value * value;
      }

      const volume = Math.sqrt(sum / dataArray.length);
      setVoiceLevel(Math.min(volume, 40));
      const now = performance.now();

      const threshold = hasSpeechRef.current
        ? continueSpeechThreshold
        : startSpeechThreshold;

      const isVoice = volume > threshold;

      if (isVoice) {
        speechFrames++;

        if (speechFrames >= minSpeechFrames) {
          hasSpeechRef.current = true;
          lastVoiceTimeRef.current = now;
        }
      } else {
        speechFrames = 0;

        if (hasSpeechRef.current && lastVoiceTimeRef.current) {
          const silenceDuration = now - lastVoiceTimeRef.current;

          if (silenceDuration >= requiredSilenceMs) {
            isStoppingRef.current = true;
            stopRecording();
            return;
          }
        }
      }

      vadAnimationRef.current = requestAnimationFrame(detect);
    };

    detect();
  };

  const stopRecording = () => {
    if (vadAnimationRef.current) {
      cancelAnimationFrame(vadAnimationRef.current);
      vadAnimationRef.current = null;
    }

    if (audioContextRef.current) {
      audioContextRef.current.close().catch(() => {});
      audioContextRef.current = null;
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    }

    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      recorderRef.current.stop();
    }
    setIsListeningVisual(false);
    setVoiceLevel(0);
  };

  const transcribeAndSend = async (audioBlob) => {
    let text = "";

    try {
      setStatus(t.uploadingAudio);
      const token = getAccessToken();

      if (!token) {
        setStatus(t.transcriptionError);
        setScreenText(t.loginFirst);
        return;
      }

      const file = new File([audioBlob], "voice.webm", { type: "audio/webm" });

      const formData = new FormData();
      formData.append("file", file);
      formData.append("language", selectedLanguage || "en");
      const transcribeRes = await authFetch(`${baseUrl}/api/voice/transcribe`, {
        method: "POST",
        body: formData,
      });

      if (!transcribeRes.ok) {
        const errorText = await transcribeRes.text();
        console.error("TRANSCRIBE API ERROR:", errorText);
        throw new Error(
          `Transcribe failed with status ${transcribeRes.status}`,
        );
      }

      const transcribeData = await transcribeRes.json();
      text = transcribeData.text || "";

      setRecognizedText(text || "-");
    } catch (error) {
      console.error("REAL TRANSCRIPTION ERROR:", error);
      setStatus(t.transcriptionError);
      setScreenText(t.couldNotTranscribe);
      return;
    }

    try {
      if (!text.trim()) {
        setStatus(t.noValidInput);
        setScreenText(t.pleaseSaySomething);
        return;
      }

      const getProductSelectionText = (index) => {
        if (currentLang === "ar") {
          if (index === 1) return "المنتج الأول";
          if (index === 2) return "المنتج الثاني";
          if (index === 3) return "المنتج الثالث";
        }

        if (index === 1) return "the first product";
        if (index === 2) return "the second product";
        if (index === 3) return "the third product";

        return "";
      };

      await sendCommandText(text);
    } catch (error) {
      console.error("COMMAND ERROR AFTER TRANSCRIPTION:", error);
      setStatus(t.sendCommandError);
      setScreenText(error.message || t.couldNotSendCommand);
    }
  };
  const handleTypedCommandSubmit = async () => {
    const command = typedCommand.trim();

    if (!command) {
      setStatus(t.noValidInput);
      setScreenText(
        currentLang === "ar"
          ? "من فضلك اكتب طلبًا أولاً."
          : "Please type a command first.",
      );
      return;
    }

    setRecognizedText(command);
    setTypedCommand("");
    if (rightPanelMode === "payment") {
      setTypedCommand("");
      handlePaymentTypedInput(command);
      return;
    }
    await sendCommandText(command);
  };
  const sendCommandText = async (text) => {
    try {
      if (languageStep) {
        const lowerText = text.toLowerCase().trim();

        if (
          lowerText.includes("arabic") ||
          lowerText.includes("عربي") ||
          lowerText.includes("العربية")
        ) {
          handleLanguageSelect("ar");
          return;
        }

        if (
          lowerText.includes("english") ||
          lowerText.includes("انجليزي") ||
          lowerText.includes("english language")
        ) {
          handleLanguageSelect("en");
          return;
        }

    setScreenText(t.pleaseSayLanguage);
setLastSpokenText(speech.pleaseSayLanguage);
speakText(speech.pleaseSayLanguage);
        setRecognizedText(text || "-");
        return;
      }

      if (!text || text.trim() === "") {
        setStatus(t.noValidInput);
        setScreenText(t.pleaseSaySomething);
        return;
      }

      const token = getAccessToken();
      console.log("EXT TOKEN:", token);

      if (!token) {
        showLoginPanel();
        return;
      }

      setStatus(t.processing);

      const res = await authFetch(`${baseUrl}/api/voice/command`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          text,
          language: selectedLanguage,
          selectedProductIndex: selectedProductShortcutIndex,
        }),
      });
      setSelectedProductShortcutIndex(null);

      const data = await res.json().catch(() => null);
      console.log("VOICE API RAW RESPONSE =", data);
      if (!res.ok) {
        throw new Error(
          data?.message ||
            data?.Message ||
            `Request failed with status ${res.status}`,
        );
      }

      const rawAction = data?.action || data?.Action || "";
      const action = String(rawAction).trim();
      const actionKey = action.toLowerCase();

      const responsePayload = data?.data || data?.Data || {};

      console.log("RAW ACTION =", rawAction);
      console.log("ACTION KEY =", actionKey);
      console.log("RESPONSE PAYLOAD =", responsePayload);
      const responseOrders = Array.isArray(responsePayload)
        ? responsePayload
        : responsePayload?.orders || responsePayload?.Orders || [];
      if (
        action === "RequestCardDetails" ||
        action === "OpenPayment" ||
        data?.suggestedAction === "EnterCardDetails" ||
        data?.SuggestedAction === "EnterCardDetails"
      ) {
        const intentRes = await authFetch(
          `${baseUrl}/api/checkout/create-intent`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              paymentMethod: "visa",
            }),
          },
        );

        const intentData = await intentRes.json().catch(() => null);

        if (!intentRes.ok || !intentData?.clientSecret) {
          throw new Error(intentData?.message || "Could not start payment.");
        }

        setClientSecret(intentData.clientSecret);
        setPaymentIntentId(intentData.paymentIntentId);

        const paymentUrl = new URL("http://localhost:5174/extension-payment");
        paymentUrl.searchParams.set("clientSecret", intentData.clientSecret);
        paymentUrl.searchParams.set(
          "paymentIntentId",
          intentData.paymentIntentId,
        );
        paymentUrl.searchParams.set("lang", currentLang);
        setPaymentFrameUrl(paymentUrl.toString());

const textToSpeak =
  currentLang === "ar"
    ? "سأفتح نموذج الدفع الآن. تم تعطيل شريط الأوامر مؤقتًا. أدخل رقم البطاقة داخل أول حقل، ثم تاريخ الانتهاء، ثم رمز الأمان."
    : "I will open the payment form now. The command input bar is temporarily disabled. Enter the card number in the first field, then expiry date, then security code.";
     setProducts([]);
        setCartItems([]);
        setSelectedProduct(null);

        window.speechSynthesis.cancel();

        if (recorderRef.current && recorderRef.current.state !== "inactive") {
          recorderRef.current.stop();
        }

        if (streamRef.current) {
          streamRef.current.getTracks().forEach((track) => track.stop());
        }
        setPaymentInputStep(0);
        setRightPanelMode("payment");
        setScreenText(textToSpeak);
        setLastSpokenText(textToSpeak);
        setStatus(t.readyAfterResponse);

        setTimeout(() => {
          paymentIframeRef.current?.contentWindow?.postMessage(
            {
              type: "PAYMENT_FOCUS_FIELD",
              field: "cardNumber",
            },
            "http://localhost:5174",
          );
        }, 1000);
        if (!isMutedRef.current) {
          speakText(textToSpeak);
        }

        return;
      }

      console.log("RESPONSE PAYLOAD =", responsePayload);
      console.log("PAYLOAD KEYS =", Object.keys(responsePayload || {}));
      console.log("VOICE API FULL RESPONSE =", data);
      console.log("VOICE ACTION =", action);
      const getArrayFromPayload = (payload) => {
        if (!payload) return [];

        if (Array.isArray(payload)) return payload;

        if (Array.isArray(payload.$values)) return payload.$values;

        const possibleArrays = [
          payload.items,
          payload.Items,
          payload.items?.$values,
          payload.Items?.$values,

          payload.cartItems,
          payload.CartItems,
          payload.cartItems?.$values,
          payload.CartItems?.$values,

          payload.products,
          payload.Products,
          payload.products?.$values,
          payload.Products?.$values,
        ];

        for (const value of possibleArrays) {
          if (Array.isArray(value)) {
            return value;
          }
        }

        return [];
      };
      const responseProducts = getArrayFromPayload(responsePayload);

      console.log("VOICE DATA =", responseProducts);
      console.log("IS ARRAY =", Array.isArray(responseProducts));

      const correctedText =
        data?.correctedText ||
        data?.CorrectedText ||
        data?.recognizedText ||
        data?.RecognizedText ||
        text ||
        "-";

      const responseReplyText = data?.replyText || data?.ReplyText || "";

      const responseScreenText =
        data?.screenText || data?.ScreenText || responseReplyText || "-";

      if (
        actionKey === "searchproduct" ||
        actionKey === "searchandrecommend" ||
        actionKey === "recommendproduct" ||
        actionKey === "showmoreproducts"
      ) {
        setProducts(responseProducts);
        setCartItems([]);
        setOrders([]);
        setSelectedOrder(null);

        if (action === "RecommendProduct" && responseProducts.length > 0) {
          setSelectedProduct(responseProducts[0]);
        } else {
          setSelectedProduct(null);
        }

        setRightPanelMode("products");
      } else if (actionKey === "viewcart") {
        const cartItemsFromPayload = getArrayFromPayload(responsePayload);

        console.log("CART ITEMS FROM PAYLOAD =", cartItemsFromPayload);

        setCartItems(cartItemsFromPayload);
        setProducts([]);
        setOrders([]);
        setSelectedProduct(null);
        setSelectedOrder(null);
        setRightPanelMode("cart");
      } else if (actionKey === "vieworders" && Array.isArray(responseOrders)) {
        setOrders(responseOrders);
        setProducts([]);
        setCartItems([]);
        setSelectedProduct(null);
        setSelectedOrder(null);
        setRightPanelMode("orders");
      }  else if (actionKey === "openproductdetails") {
  setSelectedProduct(responsePayload);


  setCartItems([]);
  setOrders([]);
  setSelectedOrder(null);
  setRightPanelMode("products");
} else if (
        actionKey === "tracklatestorder" ||
        actionKey === "openorderdetails"
      ) {
        setSelectedOrder(responsePayload);
        setOrders([]);
        setProducts([]);
        setCartItems([]);
        setSelectedProduct(null);
        setRightPanelMode("orderDetails");
      } else {
        setProducts([]);
        setCartItems([]);
        setOrders([]);
        setSelectedProduct(null);
        setSelectedOrder(null);
        setRightPanelMode("commands");
      }

      setRecognizedText(correctedText);
      setScreenText(responseScreenText);
      setStatus(t.readyAfterResponse);

      const textToSpeak = responseReplyText || responseScreenText || "";
      setLastSpokenText(textToSpeak);

      if (!isMutedRef.current && textToSpeak) {
        speakText(textToSpeak);
      }
    } catch (error) {
      console.error("SEND COMMAND ERROR:", error);
      setStatus(t.sendCommandError);
      setScreenText(error.message || t.couldNotSendCommand);
      throw error;
    }
  };

  const handleMute = () => {
    const nextMutedState = !isMutedRef.current;

    isMutedRef.current = nextMutedState;
    setIsMuted(nextMutedState);

    if (nextMutedState) {
      window.speechSynthesis.cancel();
      setStatus(t.muted);
    } else {
      setStatus(t.unmuted);
    }
  };
  const announce = (text) => {
    if (!text || isMutedRef.current) return;

    window.speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = selectedLanguage === "en" ? "en-US" : "ar-SA";

    window.speechSynthesis.speak(utterance);
  };
  const handleReListen = () => {
    if (lastSpokenText) {
      isMutedRef.current = false;
      setIsMuted(false);

      speakText(lastSpokenText);
      setStatus(t.replayingAudio);
    }
  };

  const handleSkip = () => {
    window.speechSynthesis.cancel();
    setStatus(t.skippedAudio);
  };

  const handleExit = () => {
    window.speechSynthesis.cancel();

    setProducts([]);
    setCartItems([]);
    setSelectedProduct(null);
    setRightPanelMode("commands");

    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      isStoppingRef.current = true;
      stopRecording();
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
    }

    setStatus(t.exited);
    setRecognizedText("-");
    setScreenText("-");
    setSelectedLanguage("");
    setLanguageStep(true);

    const root = document.getElementById("voice-extension-root");
    if (root) {
      root.style.display = "none";
    }
  };

const handleGoBack = () => {
  window.speechSynthesis.cancel();

  if (selectedProduct) {
    setSelectedProduct(null);
    setRightPanelMode("products");
    return;
  }

  setProducts([]);
  setCartItems([]);
  setSelectedProduct(null);
  setRightPanelMode("commands");
  setStatus(t.goingBack);
};
  const localizedOptions =
    currentLang === "ar"
      ? ["ابحث عن منتج", "اعرض عناصر السلة", "تتبع آخر طلب", "اعرض طلباتي"]
      : [
          "Search for a product",
          "View cart items",
          "Track your latest order",
          "View my orders",
        ];
  const iconMap = {
    "ابحث عن منتج": <FaSearch size={20} />,
    "اعرض عناصر السلة": <FaShoppingCart size={20} />,
    "تتبع آخر طلب": <FaCube size={20} />,
    "اعرض طلباتي": <FaClipboard size={20} />,
    "Search for a product": <FaSearch size={20} />,
    "View cart items": <FaShoppingCart size={20} />,
    "Track your latest order": <FaCube size={20} />,
    "View my orders": <FaClipboard size={20} />,
  };

  useEffect(() => {
    const handleKeyboardShortcuts = (event) => {
      if (!event.altKey) return;

      const code = event.code;

      switch (code) {
        case "KeyS":
          event.preventDefault();
          startRecording();
          break;

        case "KeyP":
          event.preventDefault();
          isStoppingRef.current = true;
          stopRecording();
          break;

        case "KeyM":
          event.preventDefault();
          speakThenRun(
            isMutedRef.current ? "تم إلغاء الكتم" : "تم كتم الصوت",
            handleMute,
          );
          break;

        case "KeyR":
          event.preventDefault();
          speakThenRun("تمت إعادة الاستماع", handleReListen);
          break;

        case "KeyK":
          event.preventDefault();
          speakThenRun("تم تخطي الصوت", handleSkip);
          break;

        case "KeyB":
          event.preventDefault();
          speakThenRun("تم الرجوع", handleGoBack);
          break;

        case "KeyX":
          event.preventDefault();
          speakThenRun("تم إغلاق المساعد", handleExit);
          break;
        case "KeyN":
          event.preventDefault();
          showRegisterForm();
          break;

        case "KeyL":
          event.preventDefault();
          showLoginForm();
          break;
        case "Digit1":
          event.preventDefault();
          if (rightPanelMode === "products" && products.length >= 1) {
            setSelectedProduct(products[0]);
            setSelectedProductShortcutIndex(1);
          }
          break;

        case "Digit2":
          event.preventDefault();
          if (rightPanelMode === "products" && products.length >= 2) {
            setSelectedProduct(products[1]);
            setSelectedProductShortcutIndex(2);
          }
          break;

        case "Digit3":
          event.preventDefault();
          if (rightPanelMode === "products" && products.length >= 3) {
            setSelectedProduct(products[2]);
            setSelectedProductShortcutIndex(3);
          }
          break;
        case "KeyA":
          if (languageStep) {
            event.preventDefault();
            handleLanguageSelect("ar");
          }
          break;

        case "KeyE":
          if (languageStep) {
            event.preventDefault();
            handleLanguageSelect("en");
          }
          break;
        case "KeyH":
          event.preventDefault();
          speakHelp();
          break;
        default:
          break;
      }
    };

    window.addEventListener("keydown", handleKeyboardShortcuts);

    return () => {
      window.removeEventListener("keydown", handleKeyboardShortcuts);
    };
  }, [
    lastSpokenText,
    selectedProduct,
    currentLang,
    languageStep,
    rightPanelMode,
    products,
    selectedProductShortcutIndex,
  ]);
const focusTypedCommandInput = (event) => {
  if (rightPanelMode === "payment") {
    return;
  }

  const target = event?.target;

  if (
    target?.closest?.("[data-skip-autofocus='true']") ||
    target?.tagName === "INPUT" ||
    target?.tagName === "TEXTAREA" ||
    target?.tagName === "BUTTON"
  ) {
    return;
  }

  setTimeout(() => {
    const input = document.getElementById("typed-command-input");
    input?.focus({ preventScroll: true });
  }, 100);
};
  return (
    <div
      id="voice-extension-overlay"
      role="dialog"
      onClickCapture={focusTypedCommandInput}
      onMouseUpCapture={focusTypedCommandInput}
      aria-modal="true"
      aria-label={currentLang === "ar" ? "المساعد الصوتي" : "Voice assistant"}
      style={{
        position: "fixed",
        inset: "0",
        zIndex: 999999,
        background: `linear-gradient(135deg, rgba(0,0,0,0.78), ${assistantTheme.background})`,
        transition: "background 0.5s ease",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        padding: "24px",
        pointerEvents: "auto",
        overflowY: "auto",
      }}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.92 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.45 }}
        style={{
          width: "95%",
          maxWidth: "1400px",
          position: "relative",
          minHeight: "650px",
          padding: "32px",
          borderRadius: "32px",
          border: `2px solid ${assistantTheme.main}`,
          background: "#1f1f2e",
          boxShadow: `0 0 35px ${assistantTheme.glow}, 0 20px 50px rgba(0,0,0,0.7)`,
          transition:
            "border 0.5s ease, box-shadow 0.5s ease, background 0.5s ease",
          fontFamily: "Arial, sans-serif",
          color: "#ffffff",
          display: "flex",
          flexDirection: "column",
          gap: "24px",
        }}
      >
      <motion.button
  onMouseEnter={() => setShowHelpTooltip(true)}
  onMouseLeave={() => setShowHelpTooltip(false)}
  onClick={speakHelp}
  aria-label={currentLang === "ar" ? "المساعدة" : "Help"}
  style={{
    position: "absolute",
    top: "20px",
    right: "20px",
    width: "60px",
    height: "60px",
    borderRadius: "50%",
    border: "2px solid #0094FF",
    background: "#1b1b2e",
    color: "#ffffff",
    cursor: "pointer",
    boxShadow: "0 0 18px rgba(0,148,255,0.6)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: 999,
  }}
>
  <FaQuestionCircle size={28}/>

  {showHelpTooltip && (
    <div
      style={{
        position: "absolute",
        top: "70px",
        right: "0",
        width: "200px",
        padding: "15px",
        background: "#1b1b2e",
        border: "1px solid #0094FF",
        borderRadius: "12px",
        color: "white",
        textAlign: "right",
        lineHeight: "1.8",
        fontSize: "14px",
      }}
    >
      <div>Alt + A → العربية</div>
      <div>Alt + E → الإنجليزية</div>
      <div>Alt + S → تسجيل</div>
      <div>Alt + P → إيقاف</div>
      <div>Alt + M → كتم</div>
      <div>Alt + R → إعادة</div>
      <div>Alt + K → تخطي</div>
      <div>Alt + B → رجوع</div>
      <div>Alt + X → خروج</div>
      <div>Alt + 1-3 → اختيار منتج</div>
    </div>
  )}
</motion.button>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "24px",
            minHeight: "560px",
          }}
        >
          <div
            style={{
              borderRadius: "24px",
              border: "2px solid #0094FF",
              background: "#1b1b2e",
              padding: "24px",
              boxShadow: "0 10px 30px rgba(0,148,255,0.3)",
            }}
          >
            <motion.h1
              initial={{ opacity: 0, x: -25 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.35 }}
              ref={mainHeadingRef}
              tabIndex={-1}
              style={{
                marginBottom: "12px",
                fontSize: "40px",
                fontWeight: "700",
                lineHeight: "1.1",
                color: "#ffffff",
                fontFamily: "Arial, sans-serif",
              }}
            >
              {languageStep
                ? t.chooseLanguage
                : screenText && screenText !== "-"
                  ? screenText
                  : t.welcomeMessage}
            </motion.h1>

            <p
              style={{
                marginBottom: "20px",
                fontSize: "20px",
                color: "#A0CFFF",
                fontWeight: "600",
                fontFamily: "Arial, sans-serif",
                lineHeight: "1.3",
              }}
            >
              {t.statusLabel}{" "}
              {languageStep ? t.waitingLanguageSelection : status}
            </p>
            {isListeningVisual && (
              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: [1, 1.15, 1], opacity: [0.7, 1, 0.7] }}
                transition={{ duration: 1, repeat: Infinity }}
                style={{
                  margin: "16px auto",
                  width: "90px",
                  height: "90px",
                  borderRadius: "50%",
                  border: "3px solid #0094FF",
                  boxShadow: "0 0 30px rgba(0,148,255,0.8)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  fontSize: "32px",
                }}
              >
                
              </motion.div>
            )}
            <div
              style={{
                borderRadius: "20px",
                border: "1px solid #0094FF",
                boxShadow: "0 0 10px rgba(0,148,255,0.3)",
                background: "#1b1b2e",
                padding: "16px",
              }}
            >
  

             
              <VoiceWave active={isSpeakingVisual} level={speakingLevel} />
            </div>
          </div>

          <div
            style={{
              borderRadius: "24px",
              border: "2px solid #0094FF",
              background: "#1b1b2e",
              padding: "20px",
              boxShadow: "0 0 10px rgba(0,148,255,0.3)",
            }}
          >
            <div style={{ marginBottom: "16px" }}>
              <h2
                style={{
                  fontSize: "32px",
                  fontWeight: "700",
                  color: "#ffffff",
                  fontFamily: "Arial, sans-serif",
                }}
              >
                {languageStep
                  ? t.chooseLanguage
                  : rightPanelMode === "login"
                    ? authMode === "register"
                      ? currentLang === "ar"
                        ? "إنشاء حساب جديد"
                        : "Create Account"
                      : currentLang === "ar"
                        ? "تسجيل الدخول"
                        : "Login"
                    : t.voiceCommands}
              </h2>

              <p
                style={{
                  marginTop: "6px",
                  fontSize: "18px",
                  color: "#A0CFFF",
                  fontFamily: "Arial, sans-serif",
                  textShadow: "0 0 4px rgba(0,176,255,0.5)",
                }}
              >
                {languageStep ? t.selectLanguageOrVoice : t.availableOptions}
              </p>
            </div>
            <AnimatePresence mode="wait">
              <motion.div
                key={`${languageStep}-${rightPanelMode}-${selectedProduct ? "details" : "list"}`}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                transition={{ duration: 0.3 }}
              >
                {languageStep ? (
                  <div
                    style={{
                      display: "grid",
                      gridTemplateColumns: "1fr 1fr",
                      gap: "16px",
                    }}
                  >
                    <motion.button
                      whileHover={{ scale: 1.06 }}
                      whileTap={{ scale: 0.95 }}
                      onClick={() => handleLanguageSelect("ar")}
                      style={buttonStyle}
                      aria-label="اختيار اللغة العربية"
                    >
                      {t.arabic}
                    </motion.button>

                    <motion.button
                      whileHover={{ scale: 1.06 }}
                      whileTap={{ scale: 0.95 }}
                      onClick={() => handleLanguageSelect("en")}
                      style={buttonStyle}
                      aria-label="Choose English language"
                    >
                      {t.english}
                    </motion.button>
                  </div>
                ) : rightPanelMode === "login" ? (
                  <InlineLoginPanel
                    currentLang={currentLang}
                    authMode={authMode}
                    setAuthMode={setAuthMode}
                    showLoginForm={showLoginForm}
                    showRegisterForm={showRegisterForm}
                    loginEmail={loginEmail}
                    setLoginEmail={setLoginEmail}
                    loginPassword={loginPassword}
                    setLoginPassword={setLoginPassword}
                    loginLoading={loginLoading}
                    loginError={loginError}
                    handleInlineLogin={handleInlineLogin}
                    registerFullName={registerFullName}
                    setRegisterFullName={setRegisterFullName}
                    registerEmail={registerEmail}
                    setRegisterEmail={setRegisterEmail}
                    registerPassword={registerPassword}
                    setRegisterPassword={setRegisterPassword}
                    registerConfirmPassword={registerConfirmPassword}
                    setRegisterConfirmPassword={setRegisterConfirmPassword}
                    registerLoading={registerLoading}
                    registerError={registerError}
                    registerMessage={registerMessage}
                    handleInlineRegister={handleInlineRegister}
                    speakText={speakText}
                  />
                ) : rightPanelMode === "products" ? (
                  selectedProduct ? (
                    <ProductDetailsPanel
                      selectedProduct={selectedProduct}
                      currentLang={currentLang}
                      setSelectedProduct={setSelectedProduct}
                    />
                  ) : (
                    <ProductsPanel
                      products={products}
                      currentLang={currentLang}
                      t={t}
                      setSelectedProduct={setSelectedProduct}
                    />
                  )
                ) : rightPanelMode === "orders" ? (
                  <OrdersPanel
                    orders={orders}
                    currentLang={currentLang}
                    sendCommandText={sendCommandText}
                  />
                ) : rightPanelMode === "orderDetails" ? (
                  <OrderDetailsPanel
                    order={selectedOrder}
                    currentLang={currentLang}
                  />
                ) : rightPanelMode === "cart" ? (
                  <CartPanel cartItems={cartItems} currentLang={currentLang} />
                ) : rightPanelMode === "payment" ? (
                  paymentFrameUrl ? (
<iframe
  ref={paymentIframeRef}
  title="Payment"
  src={paymentFrameUrl}
  allow="payment *"
  onLoad={() => {
    setTimeout(() => {
      paymentIframeRef.current?.contentWindow?.postMessage(
        {
          type: "PAYMENT_FOCUS_FIELD",
          field: "cardNumber",
        },
        "http://localhost:5174"
      );
    }, 1000);
  }}
  style={{
    width: "100%",
    height: "620px",
    border: "none",
    borderRadius: "22px",
    background: "#1b1b2e",
    boxShadow: "0 0 15px rgba(0,148,255,0.3)",
  }}
/>
                  ) : (
                    <div
                      style={{
                        fontSize: "20px",
                        fontWeight: "700",
                        color: "#A0CFFF",
                        fontFamily: "Arial, sans-serif",
                        textShadow: "0 0 4px rgba(0,176,255,0.5)",
                      }}
                    >
                      {currentLang === "ar"
                        ? "جاري تجهيز الدفع..."
                        : "Preparing payment..."}
                    </div>
                  )
                ) : rightPanelMode === "paymentSuccess" ? (
                  <PaymentSuccessPanel
                    currentLang={currentLang}
                    screenText={screenText}
                    setRightPanelMode={setRightPanelMode}
                  />
                ) : (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.96 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ duration: 0.3 }}
                    style={{
                      display: "flex",
                      flexDirection: "column",
                      gap: "16px",
                    }}
                  >
                    {localizedOptions.map((option, index) => (
                      <motion.button
                        initial={{ opacity: 0, x: 30 }}
                        animate={{ opacity: 1, x: 0 }}
                        whileHover={{ scale: 1.04 }}
                        whileTap={{ scale: 0.96 }}
                        transition={{ duration: 0.25, delay: index * 0.08 }}
                        key={index}
                        onClick={() => sendCommandText(option)}
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          alignItems: "center",
                          padding: "16px 24px",
                          borderRadius: "16px",
                          border: "2px solid #0094FF",
                          background: "#1b1b2e",
                          color: "#ffffff",
                          fontWeight: "700",
                          fontSize: "18px",
                          cursor: "pointer",
                          boxShadow: "0 0 10px rgba(0,148,255,0.5)",
                        }}
                        onMouseOver={(e) =>
                          (e.currentTarget.style.boxShadow =
                            "0 0 25px rgba(0,148,255,0.7)")
                        }
                        onMouseOut={(e) =>
                          (e.currentTarget.style.boxShadow =
                            "0 0 10px rgba(0,148,255,0.5)")
                        }
                        aria-label={option}
                      >
                        <span>{option}</span>
                        {iconMap[option]}
                      </motion.button>
                    ))}
                  </motion.div>
                )}
              </motion.div>
            </AnimatePresence>
          </div>

          <div
            style={{
              gridColumn: "1 / -1",
              borderRadius: "24px",
              border: "2px solid #0094FF",
              background: "#1b1b2e",
              padding: "16px",
              marginTop: "8px",
              boxShadow: "0 0 10px rgba(0,148,255,0.3)",
            }}
          >
{rightPanelMode !== "login" && rightPanelMode !== "payment" && (
  <div
    style={{
      gridColumn: "1 / -1",
      borderRadius: "24px",
      border: "2px solid #0094FF",
      background: "#1b1b2e",
      padding: "16px",
      marginTop: "8px",
      boxShadow: "0 0 10px rgba(0,148,255,0.3)",
    }}
  >
    <AudioRecorder
      startRecording={startRecording}
      stopRecording={stopRecording}
      handleMute={handleMute}
      handleReListen={handleReListen}
      handleSkip={handleSkip}
      handleExit={handleExit}
      handleGoBack={handleGoBack}
      isMuted={isMuted}
      announce={announce}
      speakThenRun={speakThenRun}
      typedCommand={typedCommand}
      setTypedCommand={setTypedCommand}
      handleTypedCommandSubmit={handleTypedCommandSubmit}
      currentLang={currentLang}
    />
  </div>
)}
          </div>
        </div>
      </motion.div>
    </div>
  );
}

function VoiceWave({ active, level }) {
  const bars = Array.from({ length: 32 }, (_, index) => {
    const pattern = [20, 35, 55, 40, 70, 95, 60, 45];
    return pattern[index % pattern.length];
  });

  return (
    <div
      style={{
        width: "100%",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: "6px",
        height: "140px",
        marginTop: "18px",
      }}
    >
      {bars.map((baseHeight, index) => {
        const animatedHeight = active
          ? Math.max(8, baseHeight + level * ((index % 4) + 1) * 0.35)
          : baseHeight;

        return (
          <div
            key={index}
            style={{
              flex: 1,
              maxWidth: "10px",
              height: `${animatedHeight}px`,
              borderRadius: "999px",
              background: "#1a9cff",
              transition: "height 0.08s ease",
              boxShadow: active ? "0 0 10px rgba(26,156,255,0.8)" : "none",
              opacity: active ? 1 : 0.45,
            }}
          />
        );
      })}
    </div>
  );
}

function ProductsPanel({ products, currentLang, t, setSelectedProduct }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "24px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {t.productsTitle}
      </h3>
      {products.length === 0 && (
        <div
          style={{
            fontSize: "20px",
            fontWeight: "600",
            color: "#A0CFFF",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {currentLang === "ar"
            ? "تم تنفيذ البحث لكن لم تصل منتجات للواجهة."
            : "Search completed, but no products reached the interface."}
        </div>
      )}
      {products.map((product, index) => (
        <motion.button
          initial={{ opacity: 0, y: 18 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.25, delay: index * 0.08 }}
          key={product.id || index}
          onClick={() => {
            console.log("SELECTED PRODUCT =", product);
            setSelectedProduct(product);
          }}
          style={{
            width: "100%",
            borderRadius: "22px",
            border: "2px solid #0094FF",
            background: "#1b1b2e",
            padding: "16px 24px",
            textAlign: "left",
            cursor: "pointer",
            boxShadow: "0 0 10px rgba(0,148,255,0.5)",
            transition: "box-shadow 0.2s ease",
          }}
        >
          <div
            style={{
              fontSize: "24px",
              fontWeight: "700",
              color: "#ffffff",
              fontFamily: "Arial, sans-serif",
            }}
          >
            {product.name || product.Name}
          </div>

          <div
            style={{
              marginTop: "8px",
              fontSize: "20px",
              color: "#A0CFFF",
              fontFamily: "Arial, sans-serif",
              textShadow: "0 0 4px rgba(0,176,255,0.5)",
            }}
          >
            {currentLang === "ar"
              ? `السعر: ${product.price ?? product.Price} شيكل`
              : `Price: ${product.price ?? product.Price} NIS`}
          </div>
        </motion.button>
      ))}
    </div>
  );
}

function ProductDetailsPanel({
  selectedProduct,
  currentLang,
  setSelectedProduct,
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "24px",
          fontWeight: "700",
          color: "#6b4226",
        }}
      >
        {currentLang === "ar" ? "تفاصيل المنتج" : "Product Details"}
      </h3>

      <div
        style={{
          width: "100%",
          borderRadius: "22px",
          border: "2px solid #8b5e3c",
          background: "#1b1b2e",
          padding: "20px 24px",
          textAlign: "left",
        }}
      >
        <div
          style={{
            fontSize: "28px",
            fontWeight: "700",
            color: "#ffffff",
            fontFamily: "Arial, sans-serif",
          }}
        >
          {selectedProduct.name}
        </div>

        <div
          style={{
            marginTop: "12px",
            fontSize: "20px",
            color: "#A0CFFF",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {currentLang === "ar"
            ? `السعر: ${selectedProduct.price} شيكل`
            : `Price: ${selectedProduct.price} NIS`}
        </div>

        <div
          style={{
            marginTop: "12px",
            fontSize: "18px",
            color: "#A0CFFF",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {selectedProduct.description ||
            (currentLang === "ar"
              ? "لا يوجد وصف متاح"
              : "No description available")}
        </div>

        <motion.button
          whileHover={{ scale: 1.06 }}
          whileTap={{ scale: 0.95 }}
          onClick={() => setSelectedProduct(null)}
          style={{
            marginTop: "16px",
            borderRadius: "16px",
            border: "2px solid #0094FF",
            background: "#1b1b2e",
            padding: "12px 18px",
            fontSize: "18px",
            fontWeight: "700",
            color: "#ffffff",
            fontFamily: "Arial, sans-serif",
            cursor: "pointer",
          }}
        >
          {currentLang === "ar" ? "رجوع للمنتجات" : "Back to products"}
        </motion.button>
      </div>
    </div>
  );
}

function CartPanel({ cartItems, currentLang }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "24px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {currentLang === "ar" ? "عناصر السلة" : "Cart Items"}
      </h3>

      {cartItems.length > 0 ? (
        cartItems.map((item, index) => (
          <motion.div
            key={
              item.id || item.Id || item.productId || item.ProductId || index
            }
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.25, delay: index * 0.08 }}
            style={{
              width: "100%",
              borderRadius: "22px",
              border: "2px solid #0094FF",
              background: "#1b1b2e",
              padding: "16px 24px",
              textAlign: "left",
            }}
          >
            <div
              style={{
                fontSize: "24px",
                fontWeight: "700",
                color: "#ffffff",
                fontFamily: "Arial, sans-serif",
              }}
            >
              {item.productName || item.ProductName || item.name || item.Name}
            </div>

            <div
              style={{
                marginTop: "8px",
                fontSize: "20px",
                color: "#A0CFFF",
                fontFamily: "Arial, sans-serif",
                textShadow: "0 0 4px rgba(0,176,255,0.5)",
              }}
            >
              {currentLang === "ar"
                ? `الكمية: ${item.count ?? item.Count ?? item.quantity ?? item.Quantity ?? 1} | المجموع: ${
                    item.totalPrice ??
                    item.TotalPrice ??
                    item.price ??
                    item.Price
                  } شيكل`
                : `Qty: ${item.count ?? item.Count ?? item.quantity ?? item.Quantity ?? 1} | Total: ${
                    item.totalPrice ??
                    item.TotalPrice ??
                    item.price ??
                    item.Price
                  } NIS`}
            </div>
          </motion.div>
        ))
      ) : (
        <div
          style={{
            fontSize: "20px",
            color: "#A0CFFF",
            fontWeight: "600",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {currentLang === "ar" ? "السلة فارغة" : "Cart is empty"}
        </div>
      )}
    </div>
  );
}
function OrdersPanel({ orders, currentLang, sendCommandText }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "24px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {currentLang === "ar" ? "آخر الطلبات" : "Latest Orders"}
      </h3>

      {orders.length > 0 ? (
        orders.map((order, index) => (
          <motion.button
            whileHover={{ scale: 1.06 }}
            whileTap={{ scale: 0.95 }}
            key={order.id || index}
            onClick={() => {
              const command =
                currentLang === "ar"
                  ? index === 0
                    ? "الطلب الأول"
                    : index === 1
                      ? "الطلب الثاني"
                      : "الطلب الثالث"
                  : index === 0
                    ? "first order"
                    : index === 1
                      ? "second order"
                      : "third order";

              sendCommandText(command);
            }}
            style={{
              width: "100%",
              borderRadius: "22px",
              border: "2px solid #0094FF",
              background: "#1b1b2e",
              padding: "16px 24px",
              textAlign: "left",
              cursor: "pointer",
            }}
          >
            <div
              style={{
                fontSize: "24px",
                fontWeight: "700",
                color: "#ffffff",
                fontFamily: "Arial, sans-serif",
              }}
            >
              {currentLang === "ar"
                ? `الطلب ${index + 1}: رقم ${order.id}`
                : `Order ${index + 1}: #${order.id}`}
            </div>

            <div
              style={{
                marginTop: "8px",
                fontSize: "20px",
                color: "#A0CFFF",
                fontFamily: "Arial, sans-serif",
                textShadow: "0 0 4px rgba(0,176,255,0.5)",
              }}
            >
              {currentLang === "ar"
                ? `الحالة: ${order.orderStatus} | الدفع: ${order.paymentStatus}`
                : `Status: ${order.orderStatus} | Payment: ${order.paymentStatus}`}
            </div>
          </motion.button>
        ))
      ) : (
        <div
          style={{
            fontSize: "20px",
            color: "#A0CFFF",
            fontWeight: "600",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {currentLang === "ar" ? "لا توجد طلبات" : "No orders found"}
        </div>
      )}
    </div>
  );
}
function OrderDetailsPanel({ order, currentLang }) {
  if (!order) {
    return (
      <div style={{ fontSize: "20px", color: "#A0CFFF", fontWeight: "700" }}>
        {currentLang === "ar" ? "لا توجد تفاصيل طلب" : "No order details"}
      </div>
    );
  }

  const items = order.orderItems || order.OrderItems || [];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "24px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {currentLang === "ar" ? "تفاصيل الطلب" : "Order Details"}
      </h3>

      <div
        style={{
          width: "100%",
          borderRadius: "22px",
          border: "2px solid #0094FF",
          background: "#1b1b2e",
          padding: "20px 24px",
          textAlign: "left",
        }}
      >
        <div
          style={{
            fontSize: "26px",
            fontWeight: "700",
            color: "#ffffff",
            fontFamily: "Arial, sans-serif",
          }}
        >
          {currentLang === "ar"
            ? `رقم الطلب: ${order.id}`
            : `Order ID: ${order.id}`}
        </div>

        <div
          style={{
            marginTop: "10px",
            fontSize: "20px",
            color: "#A0CFFF",
            fontFamily: "Arial, sans-serif",
            textShadow: "0 0 4px rgba(0,176,255,0.5)",
          }}
        >
          {currentLang === "ar"
            ? `الحالة: ${order.orderStatus} | الدفع: ${order.paymentStatus}`
            : `Status: ${order.orderStatus} | Payment: ${order.paymentStatus}`}
        </div>

        <div
          style={{
            marginTop: "18px",
            display: "flex",
            flexDirection: "column",
            gap: "10px",
          }}
        >
          {items.length > 0 ? (
            items.map((item, index) => (
              <div
                key={item.productId || index}
                style={{
                  borderRadius: "16px",
                  border: "1px solid #0094FF",
                  background: "#1b1b2e",
                  padding: "12px 16px",
                }}
              >
                <div
                  style={{
                    fontSize: "20px",
                    fontWeight: "700",
                    color: "#ffffff",
                    fontFamily: "Arial, sans-serif",
                  }}
                >
                  {item.productName}
                </div>

                <div
                  style={{
                    marginTop: "6px",
                    fontSize: "18px",
                    color: "#A0CFFF",
                    fontFamily: "Arial, sans-serif",
                    textShadow: "0 0 4px rgba(0,176,255,0.5)",
                  }}
                >
                  {currentLang === "ar"
                    ? `الكمية: ${item.count} | السعر: ${item.price} | المجموع: ${item.totalPrice}`
                    : `Qty: ${item.count} | Price: ${item.price} | Total: ${item.totalPrice}`}
                </div>
              </div>
            ))
          ) : (
            <div
              style={{
                fontSize: "18px",
                color: "#A0CFFF",
                fontWeight: "600",
                fontFamily: "Arial, sans-serif",
                textShadow: "0 0 4px rgba(0,176,255,0.5)",
              }}
            >
              {currentLang === "ar"
                ? "لا توجد منتجات داخل هذا الطلب"
                : "No products inside this order"}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
function PaymentSuccessPanel({ currentLang, screenText, setRightPanelMode }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <h3
        style={{
          fontSize: "28px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {currentLang === "ar" ? "نتيجة الدفع" : "Payment Result"}
      </h3>

      <div
        style={{
          width: "100%",
          borderRadius: "22px",
          border: "2px solid #0094FF",
          background: "#1b1b2e",
          padding: "20px 24px",
          fontSize: "24px",
          fontWeight: "700",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {screenText}
      </div>

      <motion.button
        whileHover={{ scale: 1.06 }}
        whileTap={{ scale: 0.95 }}
        type="button"
        onClick={() => setRightPanelMode("commands")}
        style={buttonStyle}
      >
        {currentLang === "ar" ? "الرجوع للأوامر" : "Back to commands"}
      </motion.button>
    </div>
  );
}
function InlineLoginPanel({
  currentLang,
  authMode,
  showLoginForm,
  showRegisterForm,

  loginEmail,
  setLoginEmail,
  loginPassword,
  setLoginPassword,
  loginLoading,
  loginError,
  handleInlineLogin,

  registerFullName,
  setRegisterFullName,
  registerEmail,
  setRegisterEmail,
  registerPassword,
  setRegisterPassword,
  registerConfirmPassword,
  setRegisterConfirmPassword,
  registerLoading,
  registerError,
  registerMessage,
  handleInlineRegister,

  speakText,
}) {
  const isAr = currentLang === "ar";
  const isRegister = authMode === "register";

  const fieldStyle = {
    width: "100%",
    borderRadius: "18px",
    border: "2px solid #0094FF",
    background: "#101020",
    color: "#ffffff",
    padding: "16px 18px",
    fontSize: "20px",
    outline: "none",
  };

  const labelStyle = {
    fontSize: "18px",
    fontWeight: "700",
    color: "#ffffff",
  };

  return (
    <div
      data-skip-autofocus="true"
      style={{
        display: "flex",
        flexDirection: "column",
        gap: "16px",
      }}
    >
      <h3
        style={{
          fontSize: "28px",
          fontWeight: "800",
          color: "#ffffff",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {isRegister
          ? isAr
            ? "إنشاء حساب جديد"
            : "Create Account"
          : isAr
            ? "تسجيل الدخول"
            : "Login"}
      </h3>

      <p
        style={{
          fontSize: "18px",
          color: "#A0CFFF",
          lineHeight: "1.5",
          fontFamily: "Arial, sans-serif",
        }}
      >
        {isRegister
          ? isAr
            ? "أدخل البيانات التالية لإنشاء حساب جديد. اضغط Tab للانتقال بين الحقول، و Enter في آخر حقل لإنشاء الحساب."
            : "Enter the following information to create a new account. Press Tab to move between fields, and Enter in the last field to create the account."
          : isAr
            ? "أدخل البريد الإلكتروني، ثم كلمة المرور. اضغط Tab للانتقال بين الحقول، أو Enter بعد البريد الإلكتروني للانتقال إلى كلمة المرور. إذا لم يكن لديك حساب، اختار إنشاء حساب جديد."
            : "Enter your email, then your password. Press Tab to move between fields, or press Enter after the email to move to password. If you do not have an account, choose create account."}
      </p>

      <div style={{ display: "flex", gap: "12px" }}>
        <button
          type="button"
          onClick={showLoginForm}
          style={{
            ...buttonStyle,
            borderColor: !isRegister ? "#00ff88" : "#0094FF",
          }}
        >
          {isAr ? "لدي حساب - تسجيل الدخول" : "I have an account - Login"}
        </button>

        <button
          type="button"
          onClick={showRegisterForm}
          style={{
            ...buttonStyle,
            borderColor: isRegister ? "#00ff88" : "#0094FF",
          }}
        >
          {isAr
            ? "ليس لدي حساب - إنشاء حساب جديد"
            : "I do not have an account - Create account"}
        </button>
      </div>

      {!isRegister ? (
        <>
          <label style={labelStyle}>
            {isAr ? "البريد الإلكتروني" : "Email"}
          </label>
          <input
            id="inline-login-email"
            type="email"
            value={loginEmail}
            onChange={(e) => setLoginEmail(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل البريد الإلكتروني. أدخل بريدك الإلكتروني."
                  : "Email field. Enter your email.",
              )
            }
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                document.getElementById("inline-login-password")?.focus();
              }
            }}
            placeholder={isAr ? "أدخل البريد الإلكتروني" : "Enter email"}
            style={fieldStyle}
          />

          <label style={labelStyle}>{isAr ? "كلمة المرور" : "Password"}</label>

          <input
            id="inline-login-password"
            type="password"
            value={loginPassword}
            onChange={(e) => setLoginPassword(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل كلمة المرور. أدخل كلمة المرور، ثم اضغط Enter لتسجيل الدخول."
                  : "Password field. Enter your password, then press Enter to log in.",
              )
            }
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleInlineLogin();
              }
            }}
            placeholder={isAr ? "أدخل كلمة المرور" : "Enter password"}
            style={fieldStyle}
          />

          {loginError && (
            <div role="alert" style={errorBoxStyle}>
              {loginError}
            </div>
          )}

          <button
            type="button"
            onClick={handleInlineLogin}
            disabled={loginLoading}
            style={buttonStyle}
          >
            {loginLoading
              ? isAr
                ? "جاري تسجيل الدخول..."
                : "Logging in..."
              : isAr
                ? "تسجيل الدخول"
                : "Login"}
          </button>
        </>
      ) : (
        <>
          <label style={labelStyle}>
            {isAr ? "الاسم الكامل" : "Full Name"}
          </label>

          <input
            type="text"
            id="inline-register-fullname"
            value={registerFullName}
            onChange={(e) => setRegisterFullName(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل الاسم الكامل. أدخل اسمك الكامل. للانتقال إلى البريد الإلكتروني اضغط Tab."
                  : "Full name field. Enter your full name. Press Tab to move to email.",
              )
            }
            placeholder={isAr ? "أدخل الاسم الكامل" : "Enter full name"}
            style={fieldStyle}
          />

          <label style={labelStyle}>
            {isAr ? "البريد الإلكتروني" : "Email"}
          </label>

          <input
            type="email"
            value={registerEmail}
            onChange={(e) => setRegisterEmail(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل البريد الإلكتروني. أدخل بريدك الإلكتروني. للانتقال إلى كلمة المرور اضغط Tab."
                  : "Email field. Enter your email. Press Tab to move to password.",
              )
            }
            placeholder={isAr ? "أدخل البريد الإلكتروني" : "Enter email"}
            style={fieldStyle}
          />

          <label style={labelStyle}>{isAr ? "كلمة المرور" : "Password"}</label>

          <input
            type="password"
            value={registerPassword}
            onChange={(e) => setRegisterPassword(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل كلمة المرور. أدخل كلمة مرور قوية. للانتقال إلى تأكيد كلمة المرور اضغط Tab."
                  : "Password field. Enter a strong password. Press Tab to move to confirm password.",
              )
            }
            placeholder={isAr ? "أدخل كلمة المرور" : "Enter password"}
            style={fieldStyle}
          />

          <label style={labelStyle}>
            {isAr ? "تأكيد كلمة المرور" : "Confirm Password"}
          </label>

          <input
            type="password"
            value={registerConfirmPassword}
            onChange={(e) => setRegisterConfirmPassword(e.target.value)}
            onFocus={() =>
              speakText(
                isAr
                  ? "حقل تأكيد كلمة المرور. أعيد إدخال كلمة المرور. لإنشاء الحساب اضغط Enter."
                  : "Confirm password field. Re-enter your password. Press Enter to create the account.",
              )
            }
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleInlineRegister();
              }
            }}
            placeholder={isAr ? "أعيد كلمة المرور" : "Confirm password"}
            style={fieldStyle}
          />

          {registerError && (
            <div role="alert" style={errorBoxStyle}>
              {registerError}
            </div>
          )}

          {registerMessage && (
            <div role="status" style={successBoxStyle}>
              {registerMessage}
            </div>
          )}

          <button
            type="button"
            onClick={handleInlineRegister}
            disabled={registerLoading}
            style={buttonStyle}
          >
            {registerLoading
              ? isAr
                ? "جاري إنشاء الحساب..."
                : "Creating account..."
              : isAr
                ? "إنشاء الحساب"
                : "Create Account"}
          </button>
        </>
      )}
    </div>
  );
}

const errorBoxStyle = {
  borderRadius: "16px",
  border: "1px solid #ff3b3b",
  background: "rgba(255,59,59,0.12)",
  color: "#ffb3b3",
  padding: "12px 16px",
  fontSize: "18px",
  fontWeight: "700",
};

const successBoxStyle = {
  borderRadius: "16px",
  border: "1px solid #00ff88",
  background: "rgba(0,255,136,0.12)",
  color: "#b7ffd8",
  padding: "12px 16px",
  fontSize: "18px",
  fontWeight: "700",
};
const srOnly = {
  position: "absolute",
  width: "1px",
  height: "1px",
  padding: 0,
  margin: "-1px",
  overflow: "hidden",
  clip: "rect(0,0,0,0)",
  whiteSpace: "nowrap",
  border: 0,
};

const buttonStyle = {
  minWidth: "145px",
  height: "58px",
  borderRadius: "18px",
  border: "2px solid #0094FF",
  background: "#1b1b2e",
  padding: "16px 26px",
  textAlign: "center",
  fontSize: "18px",
  fontWeight: "800",
  color: "#ffffff",
  fontFamily: "Arial, sans-serif",
  cursor: "pointer",
  boxShadow: "0 0 16px rgba(0,148,255,0.45)",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  gap: "10px",
};
