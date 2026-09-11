import React from 'react';

const AudioRecorder = ({
  startRecording,
  stopRecording,
  handleMute,
  handleReListen,
  handleSkip,
  handleExit,
  handleGoBack,
  isMuted
}) => {
  return (
    <div className="flex flex-wrap justify-center gap-4">
      <button
        onClick={handleSkip}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        Skip
      </button>

      <button
        onClick={startRecording}
        disabled={isMuted}
        className={`min-w-[140px] rounded-[26px] border-[3px] px-6 py-4 text-2xl font-bold shadow-sm transition ${
          isMuted
            ? 'cursor-not-allowed border-[#b7a79a] bg-[#ddd4cc] text-[#8c7b6d]'
            : 'border-[#8b5e3c] bg-[#c9976b] text-[#fffaf5] hover:bg-[#b98558]'
        }`}
      >
        Speak
      </button>

      <button
        onClick={handleMute}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        {isMuted ? "Unmute" : "Mute"}
      </button>

      <button
        onClick={handleReListen}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        Re listen
      </button>

      <button
        onClick={handleExit}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        Exit
      </button>

      <button
        onClick={handleGoBack}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        Go Back
      </button>

      <button
        onClick={stopRecording}
        className="min-w-[140px] rounded-[26px] border-[3px] border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-2xl font-bold text-[#6b4226] shadow-sm transition hover:bg-[#c9ae95]"
      >
        Stop
      </button>
    </div>
  );
};

export default AudioRecorder;