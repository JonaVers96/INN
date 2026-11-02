import type { ReactNode } from "react";

type Props = { onClose: () => void; children: ReactNode };

const ModalOverlay = ({ onClose, children }: Props) => (
  <div className="fixed inset-0 bg-black/50 z-50 overflow-y-auto" onClick={onClose}>
    <div className="flex items-start justify-center min-h-full py-8">
      <div
        className="bg-white rounded-2xl p-6 max-w-5xl w-full mx-4"
        onClick={(e) => e.stopPropagation()}
      >
        <button onClick={onClose} className="float-right text-slate-400 hover:text-slate-600">
          x
        </button>
        {children}
      </div>
    </div>
  </div>
);

export default ModalOverlay;
