const PhaseButton = ({
  onClick,
  label,
  color,
  disabled = false,
  type = "button",
}: {
  onClick?: () => void;
  label: string;
  color: string;
  disabled?: boolean;
  type?: "button" | "submit";
}) => (
  <div className="flex justify-center my-3">
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`px-8 py-3 text-xs font-black text-white rounded-lg shadow-md tracking-wide uppercase transition-transform ${
        disabled
          ? "opacity-80 cursor-default"
          : "hover:scale-105 active:scale-95"
      }`}
      style={{ backgroundColor: color }}
    >
      {label}
    </button>
  </div>
);

export default PhaseButton;