const CategoryHeader = ({ icon, label }: { icon: string; label: string }) => (
  <div
    className="flex items-center gap-2 mb-1 mt-2 rounded px-2 py-1"
    style={{ backgroundColor: "#F9BC2F" }}
  >
    <img src={icon} alt="" className="w-4 h-4" />
    <span className="text-xs font-black text-white uppercase tracking-wide">
      {label}
    </span>
  </div>
);

export default CategoryHeader;