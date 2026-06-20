chrome.action.onClicked.addListener(async (tab) => {
  if (!tab.id) return;

  try {
    await chrome.tabs.sendMessage(tab.id, {
      type: "TOGGLE_VOICE_EXTENSION"
    });
  } catch (error) {
    console.warn("Content script not ready. Injecting content script...", error);

    try {
      await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        files: ["content.js"]
      });

      await chrome.tabs.sendMessage(tab.id, {
        type: "TOGGLE_VOICE_EXTENSION"
      });
    } catch (injectError) {
      console.error("Could not inject content script:", injectError);
    }
  }
});