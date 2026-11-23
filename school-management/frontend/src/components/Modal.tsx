import { useEffect, useRef, useState } from "react";

export function Modal({
  title,
  onClose,
  children,
  className = "",
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  className?: string;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const closingTimer = useRef<number | null>(null);
  const frame = useRef<number | null>(null);
  const [entered, setEntered] = useState(false);
  const [closing, setClosing] = useState(false);

  useEffect(() => {
    ref.current?.showModal();
    frame.current = window.requestAnimationFrame(() => {
      frame.current = window.requestAnimationFrame(() => setEntered(true));
    });
    return () => {
      if (frame.current !== null) window.cancelAnimationFrame(frame.current);
      if (closingTimer.current !== null) window.clearTimeout(closingTimer.current);
      ref.current?.close();
    };
  }, []);

  function closeSmoothly() {
    if (closing) return;
    setClosing(true);
    setEntered(false);
    closingTimer.current = window.setTimeout(() => onClose(), 220);
  }

  return (
    <dialog
      ref={ref}
      className={[
        className,
        entered ? "dialog-entered" : "",
        closing ? "dialog-closing" : "",
      ]
        .filter(Boolean)
        .join(" ")}
      onCancel={(event) => {
        event.preventDefault();
        closeSmoothly();
      }}
    >
      <div className="modal-head">
        <h2>{title}</h2>
        <button onClick={closeSmoothly} aria-label="Close dialog">
          ✕
        </button>
      </div>
      <div className="modal-content">{children}</div>
    </dialog>
  );
}
