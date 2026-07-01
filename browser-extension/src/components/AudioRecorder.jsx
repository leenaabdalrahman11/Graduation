import React, { useEffect, useRef } from "react";
import { motion } from "framer-motion";
import {
  FaVolumeUp,
  FaMicrophone,
  FaRedoAlt,
  FaPowerOff,
  FaArrowLeft,
  FaStop,
} from "react-icons/fa";

export default function AudioControls({
  startRecording,
  stopRecording,
  handleMute,
  handleReListen,
  handleSkip,
  handleExit,
  handleGoBack,
  isMuted,
  announce,
  speakThenRun,
  typedCommand,
  setTypedCommand,
  handleTypedCommandSubmit,
  currentLang = "ar",
}) {
  const inputRef = useRef(null);

  const focusCommandInput = () => {
    setTimeout(() => {
      inputRef.current?.focus({ preventScroll: true });
    }, 0);
  };

  useEffect(() => {
    const returnFocusToInput = (event) => {
      const target = event.target;

      if (target?.id === "typed-command-input") return;

      setTimeout(() => {
        inputRef.current?.focus({ preventScroll: true });
      }, 80);
    };

    document.addEventListener("click", returnFocusToInput);

    return () => {
      document.removeEventListener("click", returnFocusToInput);
    };
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      inputRef.current?.focus();
    }, 150);

    return () => clearTimeout(timer);
  }, []);

  const buttonBase =
    "rounded-[22px] font-bold transition flex items-center justify-center gap-3";

  const activeStyle =
    "bg-[#1b1b2e] border-[2px] border-[#0094FF] text-[#ffffff] hover:bg-[#222244]";

  const buttonStyle = {
    minWidth: "140px",
    height: "80px",
    padding: "0 16px",
    fontSize: "22px",
    boxShadow: "0 0 15px rgba(0,148,255,0.5)",
  };

  const buttons = [
    {
      label: "Skip >>",
      icon: null,
      aria: "تخطي الصوت",
      focusText: "زر تخطي الصوت",
      onClick: () => speakThenRun?.("تم تخطي الصوت", handleSkip),
    },
    {
      label: "Speak",
      icon: <FaVolumeUp size={24} />,
      aria: "بدء التسجيل",
      onClick: () => {
        startRecording?.();
        focusCommandInput();
      },
      pulse: true,
    },
    {
      label: isMuted ? "Unmute" : "Mute",
      icon: <FaMicrophone size={24} />,
      aria: isMuted ? "إلغاء الكتم" : "كتم الصوت",
      focusText: isMuted ? "زر إلغاء الكتم" : "زر كتم الصوت",
      onClick: () =>
        speakThenRun?.(
          isMuted ? "تم إلغاء الكتم" : "تم كتم الصوت",
          handleMute
        ),
    },
    {
      label: "Re-listen",
      icon: <FaRedoAlt size={24} />,
      aria: "إعادة الاستماع",
      focusText: "زر إعادة الاستماع",
      onClick: () => speakThenRun?.("تمت إعادة الاستماع", handleReListen),
    },
    {
      label: "Exit",
      icon: <FaPowerOff size={24} />,
      aria: "إغلاق المساعد",
      focusText: "زر إغلاق المساعد",
      onClick: () => speakThenRun?.("تم إغلاق المساعد", handleExit),
    },
    {
      label: "Go Back",
      icon: <FaArrowLeft size={24} />,
      aria: "رجوع",
      focusText: "زر الرجوع",
      onClick: () => {
        speakThenRun?.("تم الرجوع", handleGoBack);
        focusCommandInput();
      },
    },
    {
      label: "Stop",
      icon: <FaStop size={22} />,
      aria: "إيقاف التسجيل",
      onClick: () => {
        stopRecording?.();
        focusCommandInput();
      },
    },
  ];

  return (
    <motion.div
      initial={{ opacity: 0, y: 25 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35 }}
      style={{
        padding: "18px 20px",
        display: "flex",
        flexDirection: "column",
        gap: "18px",
        width: "100%",
      }}
    >
      <div
        className="flex justify-center items-center gap-4"
        style={{
          overflowX: "auto",
          overflowY: "hidden",
          whiteSpace: "nowrap",
          width: "100%",
        }}
      >
        {buttons.map((btn, index) => (
          <motion.button
            key={btn.label}
            onClick={btn.onClick}
            onFocus={() => btn.focusText && announce?.(btn.focusText)}
            aria-label={btn.aria}
            className={`${buttonBase} ${activeStyle}`}
            style={buttonStyle}
            initial={{ opacity: 0, y: 18 }}
            animate={
              btn.pulse
                ? {
                    opacity: 1,
                    y: 0,
                    boxShadow: [
                      "0 0 15px rgba(0,148,255,0.5)",
                      "0 0 28px rgba(0,148,255,0.9)",
                      "0 0 15px rgba(0,148,255,0.5)",
                    ],
                  }
                : { opacity: 1, y: 0 }
            }
            transition={
              btn.pulse
                ? {
                    opacity: { duration: 0.25, delay: index * 0.06 },
                    y: { duration: 0.25, delay: index * 0.06 },
                    boxShadow: {
                      duration: 1.2,
                      repeat: Infinity,
                      ease: "easeInOut",
                    },
                  }
                : { duration: 0.25, delay: index * 0.06 }
            }
            whileHover={{ scale: 1.08, y: -4 }}
            whileTap={{ scale: 0.94 }}
          >
            {btn.label} {btn.icon}
          </motion.button>
        ))}
      </div>

      <motion.form
        onSubmit={(e) => {
          e.preventDefault();
          handleTypedCommandSubmit?.();

          setTimeout(() => {
            inputRef.current?.focus();
          }, 100);
        }}
        initial={{ opacity: 0, scale: 0.97 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.3, delay: 0.15 }}
        style={{
          display: "flex",
          alignItems: "center",
          gap: "12px",
          width: "100%",
        }}
      >
        <motion.input
          ref={inputRef}
          autoFocus
          id="typed-command-input"
          type="text"
          value={typedCommand || ""}
          onChange={(e) => setTypedCommand?.(e.target.value)}
          placeholder={
            currentLang === "ar"
              ? "اكتبي الطلب هنا بدل الصوت..."
              : "Type your command instead of voice..."
          }
          aria-label={
            currentLang === "ar" ? "حقل كتابة الطلب" : "Command input field"
          }
          whileFocus={{
            scale: 1.01,
            boxShadow: "0 0 25px rgba(0,148,255,0.75)",
          }}
          style={{
            flex: 1,
            width: "100%",
            height: "50px",
            borderRadius: "18px",
            border: "2px solid #0094FF",
            background: "#1b1b2e",
            color: "#ffffff",
            padding: "0 18px",
            fontSize: "18px",
            fontWeight: "600",
            outline: "none",
            boxShadow: "0 0 15px rgba(0,148,255,0.35)",
            boxSizing: "border-box",
          }}
        />

        <motion.button
          type="submit"
          whileHover={{ scale: 1.07 }}
          whileTap={{ scale: 0.94 }}
          animate={{
            boxShadow: [
              "0 0 15px rgba(0,148,255,0.5)",
              "0 0 26px rgba(0,148,255,0.85)",
              "0 0 15px rgba(0,148,255,0.5)",
            ],
          }}
          transition={{
            boxShadow: {
              duration: 1.4,
              repeat: Infinity,
              ease: "easeInOut",
            },
          }}
          style={{
            width: "120px",
            minWidth: "120px",
            height: "50px",
            borderRadius: "18px",
            border: "2px solid #0094FF",
            background: "#0094FF",
            color: "#ffffff",
            fontSize: "18px",
            fontWeight: "800",
            cursor: "pointer",
            boxShadow: "0 0 15px rgba(0,148,255,0.5)",
          }}
        >
          {currentLang === "ar" ? "إرسال" : "Send"}
        </motion.button>
      </motion.form>
    </motion.div>
  );
}