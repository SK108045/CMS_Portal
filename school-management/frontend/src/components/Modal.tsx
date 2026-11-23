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
  const [closing, setClosing] = useState(false);

  useEffect(() => {
    ref.current?.showModal();
    return () => {
      if (closingTimer.current !== null) window.clearTimeout(closingTimer.current);
      ref.current?.close();
    };
  }, []);

  function closeSmoothly() {
    if (closing) return;
    setClosing(true);
    closingTimer.current = window.setTimeout(() => onClose(), 180);
  }

  return (
    <dialog
      ref={ref}
      className={[className, closing ? "dialog-closing" : ""]
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
