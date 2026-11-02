const SectionLabel = ({ label }: { label: string }) => (
  <div
    className="px-2 py-1 mb-3 mt-3 text-xs font-bold tracking-[0.3em] text-center border border-slate-300 "
    style={{ backgroundColor: "#FEF9E7", color: "#8B7A3A" }}
  >
    {label}
  </div>
);

export default SectionLabel;