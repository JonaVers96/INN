import type { ReactNode } from "react";

const CalcRow = ({
  label,
  sub,
  children,
}: {
  label: string;
  sub?: string;
  children: ReactNode;
}) => (
  <div className="flex gap-0 border-b border-white text-xs">
    <div className="flex-1 px-2 py-2 pr-2 border-r border-white">
      <p className="font-bold">{label}</p>
      {sub && <p className="text-slate-500">{sub}</p>}
    </div>
    <span className="w-1/3 px-2 py-2 bg-white/30 flex items-center justify-end gap-1">
      {children}
    </span>
  </div>
);

export default CalcRow;
