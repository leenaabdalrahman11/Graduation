import React from "react";
import ReactDOM from "react-dom/client";
import HomePageBlindExtension from "./pages/HomePageBlindExtension";
import "./overlay.css";
console.log("HomePageBlindExtension NEW BUILD LOADED");
const existingRoot = document.getElementById("voice-extension-root");

if (!existingRoot) {
  const rootElement = document.createElement("div");
  rootElement.id = "voice-extension-root";
  rootElement.style.display = "none";
  document.body.appendChild(rootElement);

  ReactDOM.createRoot(rootElement).render(
    <React.StrictMode>
      <HomePageBlindExtension />
    </React.StrictMode>
  );
}

chrome.runtime.onMessage.addListener((message) => {
  if (message.type === "TOGGLE_VOICE_EXTENSION") {
    const root = document.getElementById("voice-extension-root");
    if (!root) return;

    const isClosed = root.style.display === "none";

    root.style.display = isClosed ? "block" : "none";

    if (isClosed) {
      window.dispatchEvent(new Event("VOICE_EXTENSION_OPENED"));
    }
  }
});