import React, { useState, useRef, useEffect } from 'react';
import AudioRecorder from '../components/AudioRecorder.jsx';
import MainLayout from '../layout/MainLayout.jsx';

const baseUrl = import.meta.env.VITE_API_URL;

export default function HomePageBlind() {
  const [status, setStatus] = useState("Ready");
  const [recognizedText, setRecognizedText] = useState("-");
  const [screenText, setScreenText] = useState("-");
  const [isMuted, setIsMuted] = useState(false);

  const [message, setMessage] = useState('');
  const [options, setOptions] = useState([]);
  const [voiceStartStatus, setVoiceStartStatus] = useState('Loading...');
  const [selectedLanguage, setSelectedLanguage] = useState("");
  const [languageStep, setLanguageStep] = useState(true);
  const [lastSpokenText, setLastSpokenText] = useState("");
  const [products, setProducts] = useState([]);
  const isMutedRef = useRef(false);
  const recorderRef = useRef(null);
  const streamRef = useRef(null);
  const chunksRef = useRef([]);
  const uiText = {
  ar: {
    ready: "جاهز",
    loading: "جاري التحميل...",
    chooseLanguage: "اختيار اللغة",
    waitingLanguageSelection: "بانتظار اختيار اللغة",
    userSaid: "قال المستخدم:",
    noSpeechDetected: "لم يتم التقاط أي كلام بعد",
    voiceCommands: "الأوامر الصوتية",
    selectLanguageOrVoice: "اختاري اللغة أو قوليها بالصوت",
    availableOptions: "اختاري أحد الخيارات المتاحة",
    arabic: "العربية",
    english: "الإنجليزية",
    pleaseSayLanguage: "من فضلك قولي عربي أو إنجليزي.",
    arabicSelected: "تم اختيار العربية",
    englishSelected: "تم اختيار الإنجليزية",
    languageSelectedStatus: "تم اختيار اللغة",
    mutedRecordingDisabled: "تم الكتم - التسجيل متوقف",
    listening: "أستمع الآن",
    recordingError: "خطأ في التسجيل",
    uploadingAudio: "جاري رفع الصوت",
    transcriptionError: "خطأ في تحويل الصوت إلى نص",
    couldNotTranscribe: "تعذر تحويل الصوت إلى نص.",
    noValidInput: "لم يتم استلام إدخال صحيح",
    pleaseSaySomething: "من فضلك قولي شيئًا للبحث.",
    processing: "جاري المعالجة",
    readyAfterResponse: "جاهز",
    sendCommandError: "خطأ في إرسال الطلب",
    couldNotSendCommand: "تعذر إرسال الطلب إلى الـ API.",
    muted: "تم الكتم",
    unmuted: "تم إلغاء الكتم",
    replayingAudio: "إعادة تشغيل الصوت",
    skippedAudio: "تم تخطي الصوت",
    exited: "تم الخروج",
    goingBack: "الرجوع إلى القائمة الرئيسية",
      welcomeMessage: "مرحبًا، كيف أستطيع مساعدتك؟ يمكنك قول: ابحث عن منتج، اعرض عناصر السلة، تتبع آخر طلب، أو اعرض طلباتي."

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
    arabicSelected: "Arabic selected",
    englishSelected: "English selected",
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
      welcomeMessage: "Hi, how can I help you? You can say: Search for a product, View cart items, Track your latest order, or View my order."}
};
const currentLang = selectedLanguage === "en" ? "en" : "ar";
const t = uiText[currentLang];

  useEffect(() => {
    if (languageStep && !isMutedRef.current) {
      window.speechSynthesis.cancel();
      const promptText = selectedLanguage === "en"
  ? uiText.en.pleaseSayLanguage
  : uiText.ar.pleaseSayLanguage;

const utterance = new SpeechSynthesisUtterance(promptText);
    //  const utterance = new SpeechSynthesisUtterance("Please say Arabic or English.");
      window.speechSynthesis.speak(utterance);
    }
  }, [languageStep]);
useEffect(() => {
  const loadVoices = () => {
    window.speechSynthesis.getVoices();
  };

  loadVoices();

  if (window.speechSynthesis.onvoiceschanged !== undefined) {
    window.speechSynthesis.onvoiceschanged = loadVoices;
  }

  return () => {
    window.speechSynthesis.onvoiceschanged = null;
  };
}, []);
  useEffect(() => {
    const fetchVoiceStartData = async () => {
      try {
        const response = await fetch(`${baseUrl}/api/voice/start?language=en`);
        const data = await response.json();

        setMessage(data.message || '');
        setOptions(data.options || []);
        setVoiceStartStatus(data.status || 'Ready');
      } catch (error) {
        setVoiceStartStatus('Error fetching data');
        console.error('Error fetching data:', error);
      }
    };

    fetchVoiceStartData();
  }, []);

const speakText = (text) => {
  if (!text || isMutedRef.current) return;
  if (!("speechSynthesis" in window)) return;

  const synth = window.speechSynthesis;
  synth.cancel();
  synth.resume();

  const speakNow = () => {
    const utterance = new SpeechSynthesisUtterance(text);
    const hasArabic = /[\u0600-\u06FF]/.test(text);
    const voices = synth.getVoices();

    console.log("Available voices:", voices.map(v => `${v.name} - ${v.lang}`));
    console.log("Text to speak:", text);

    let selectedVoice = null;

    if (hasArabic || selectedLanguage === "ar") {
      utterance.lang = "ar";
      selectedVoice =
        voices.find((v) => v.lang && v.lang.toLowerCase().startsWith("ar")) ||
        voices[0] ||
        null;
    } else {
      utterance.lang = "en-US";
      selectedVoice =
        voices.find((v) => v.lang && v.lang.toLowerCase().startsWith("en")) ||
        voices[0] ||
        null;
    }

    if (selectedVoice) {
      utterance.voice = selectedVoice;
      console.log("Selected voice:", selectedVoice.name, selectedVoice.lang);
    }

    utterance.rate = 1;
    utterance.pitch = 1;
    utterance.volume = 1;

    utterance.onstart = () => console.log("Speech started");
    utterance.onend = () => console.log("Speech ended");
    utterance.onerror = (e) => console.error("Speech error:", e);

    synth.speak(utterance);
  };
console.log(window.speechSynthesis.getVoices());
  setTimeout(speakNow, 300);
};

  const handleLanguageSelect = (lang) => {
    const isArabic = lang === "ar";
   setLanguageStep(false);
    setSelectedLanguage(lang);
const langText = isArabic ? uiText.ar : uiText.en;

setStatus(langText.languageSelectedStatus);
setRecognizedText(isArabic ? langText.arabic : langText.english);
setScreenText(langText.welcomeMessage);
setLastSpokenText(langText.welcomeMessage);

speakText(langText.welcomeMessage);
  };

  const startRecording = async () => {
    if (isMutedRef.current) {
      setStatus(t.mutedRecordingDisabled);
  //    setStatus("Muted - recording disabled");
      return;
    }

    try {
      window.speechSynthesis.cancel();
      await new Promise((resolve) => setTimeout(resolve, 400));

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;
      chunksRef.current = [];

      const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
      recorderRef.current = recorder;
      recorder.onstart = () => setStatus(t.listening);

     // recorder.onstart = () => setStatus("Listening");

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunksRef.current.push(event.data);
        }
      };

      recorder.onstop = async () => {
        const blob = new Blob(chunksRef.current, { type: 'audio/webm' });
        await transcribeAndSend(blob);
      };

      recorder.start();
    } catch (error) {
      console.error(error);
      setStatus(t.recordingError);
    //  setStatus("Recording error");
    }
  };

  const stopRecording = () => {
    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      recorderRef.current.stop();
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
    }
  };

  const transcribeAndSend = async (audioBlob) => {
    try {
      setStatus(t.uploadingAudio);
     // setStatus("Uploading audio");

      const file = new File([audioBlob], "voice.webm", { type: "audio/webm" });
      const formData = new FormData();
      formData.append("file", file);
      formData.append("language", selectedLanguage || "en");

const token = localStorage.getItem("token");

const transcribeRes = await fetch(`${baseUrl}/api/voice/transcribe`, {
  method: "POST",
  headers: {
    Authorization: `Bearer ${token}`,
  },
  body: formData,
});

      if (!transcribeRes.ok) {
        throw new Error("Failed to transcribe audio");
      }

      const transcribeData = await transcribeRes.json();
      const text = transcribeData.text || "";

      setRecognizedText(text || "-");
      await sendCommandText(text);
    } catch (error) {
      console.error(error);
      setStatus(t.transcriptionError);
      setScreenText(t.couldNotTranscribe);
     // setStatus("Transcription error");
      //setScreenText("Could not transcribe audio.");
    }
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
speakText(t.pleaseSayLanguage);

    //  setScreenText("Please say Arabic or English.");
      setRecognizedText(text || "-");
    //  speakText("Please say Arabic or English.");
      return;
    }

    if (!text || text.trim() === "") {
      setStatus(t.noValidInput);
setScreenText(t.pleaseSaySomething);
   //   setStatus("No valid input received");
     // setScreenText("Please say something for the search.");
      return;
    }
setStatus(t.processing);
   // setStatus("Processing");

const token = localStorage.getItem("token");

const res = await fetch(`${baseUrl}/api/voice/command`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({
    text,
    language: selectedLanguage,
  }),
});

    if (!res.ok) {
      throw new Error("Failed to send command");
    }

    const data = await res.json();
    if (
  (data.action === "SearchProduct" || data.action === "ShowMoreProducts") &&
  Array.isArray(data.data)
) {
  setProducts(data.data);
} else if (
  data.action !== "SearchProduct" &&
  data.action !== "ShowMoreProducts"
) {
  setProducts([]);
}

    setRecognizedText(data.correctedText || data.recognizedText || text || "-");
    setScreenText(data.screenText || "-");
    setStatus(t.readyAfterResponse);
  //  setStatus("Ready");

const textToSpeak = data.replyText || data.screenText || "";
setLastSpokenText(textToSpeak);

if (!isMutedRef.current && textToSpeak) {
  speakText(textToSpeak);
}

  } catch (error) {
    console.error(error);
    setStatus(t.sendCommandError);
setScreenText(t.couldNotSendCommand);
   // setStatus("Error sending command");
   // setScreenText("Could not send command to API.");
  }
};

  const handleMute = () => {
    const nextMutedState = !isMutedRef.current;

    isMutedRef.current = nextMutedState;
    setIsMuted(nextMutedState);

    if (nextMutedState) {
      window.speechSynthesis.cancel();
      setStatus(t.muted);
     // setStatus("Muted");
    } else {
      setStatus(t.unmuted);
      //setStatus("Unmuted");
    }
  };

const handleReListen = () => {
  if (!isMutedRef.current && lastSpokenText) {
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
    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      recorderRef.current.stop();
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
    }

    setStatus(t.exited);
    setRecognizedText("-");
    setScreenText("-");
    setSelectedLanguage("");
    setLanguageStep(true);
  };

  const handleGoBack = () => {
    window.speechSynthesis.cancel();
    setProducts([]);
    setStatus(t.goingBack);
  };
  const localizedOptions =
  currentLang === "ar"
    ? [
        "ابحث عن منتج",
        "اعرض عناصر السلة",
        "تتبع آخر طلب",
        "اعرض طلباتي"
      ]
    : [
        "Search for a product",
        "View cart items",
        "Track your latest order",
        "View my order"
      ];

  return (
    <MainLayout>
      <div className="min-h-screen bg-[#f6efe7] px-4 py-6 md:px-8">
        <div className="mx-auto max-w-7xl rounded-[32px] border-2 border-[#b08968] bg-[#efe3d3] p-6 shadow-lg md:p-8">
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">

            <div className="rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-6 shadow-sm">
<h1 className="mb-3 text-3xl font-bold leading-snug text-[#6b4226] md:text-5xl">
  {languageStep
    ? t.chooseLanguage
    : (screenText && screenText !== "-" ? screenText : t.welcomeMessage)}
</h1>

              <p className="mb-5 text-base font-semibold text-[#8b5e3c] md:text-lg">
                {currentLang === "ar" ? "الحالة:" : "Status:"} {languageStep ? t.waitingLanguageSelection : voiceStartStatus}
              </p>

              <div className="rounded-[20px] border border-[#d6bda7] bg-[#fffaf6] px-4 py-3">
                <p className="text-sm font-semibold text-[#8b5e3c] md:text-base">
                  {t.userSaid}
                </p>
                <p className="mt-1 break-words text-lg font-bold text-[#6b4226] md:text-2xl">
                  {recognizedText && recognizedText !== "-" ? recognizedText : t.noSpeechDetected}</p>
              </div>
            </div>

            <div className="rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-5 shadow-sm">
              <div className="mb-4">
                <h2 className="text-2xl font-bold text-[#6b4226]">
{languageStep ? t.chooseLanguage : t.voiceCommands}                </h2>
                <p className="mt-1 text-sm text-[#8b5e3c]">
                  {languageStep
                    ? t.selectLanguageOrVoice
                    : t.availableOptions}
                </p>
              </div>

           {languageStep ? (
  <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
    <button
      onClick={() => handleLanguageSelect("ar")}
      className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-5 text-center text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
    >
      {t.arabic}
    </button>

    <button
      onClick={() => handleLanguageSelect("en")}
      className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-5 text-center text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
    >
      {t.english}
    </button>
  </div>
) : products.length > 0 ? (
  <div className="flex flex-col gap-4">
    <h3 className="text-xl font-bold text-[#6b4226]">
      {currentLang === "ar" ? "المنتجات" : "Products"}
    </h3>

    {products.map((product, index) => (
      <div
        key={product.id || index}
        className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-left"
      >
        <div className="text-xl font-bold text-[#6b4226]">
          {product.name}
        </div>

        <div className="mt-2 text-lg text-[#8b5e3c]">
          {currentLang === "ar"
            ? `السعر: ${product.price} شيكل`
            : `Price: ${product.price} NIS`}
        </div>
      </div>
    ))}
  </div>
) : (
  <div className="flex flex-col gap-4">
    {localizedOptions.map((option, index) => (
      <button
        key={index}
        onClick={() => sendCommandText(option)}
        className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-left text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
      >
        {option}
      </button>
    ))}
  </div>
)}
            </div>

            <div className="lg:col-span-2 rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-4 shadow-sm">
              <AudioRecorder
                startRecording={startRecording}
                stopRecording={stopRecording}
                handleMute={handleMute}
                handleReListen={handleReListen}
                handleSkip={handleSkip}
                handleExit={handleExit}
                handleGoBack={handleGoBack}
                isMuted={isMuted}
              />
            </div>

          </div>
        </div>
      </div>
    </MainLayout>
  );
}