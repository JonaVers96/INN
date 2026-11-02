import { useTranslation } from "react-i18next";

const EndScreen = () => {
  const { t } = useTranslation();

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="end-screen-title"
        className="flex h-1/2 w-1/2 min-h-64 min-w-80 max-w-4xl flex-col items-center rounded-3xl bg-slate-50 px-6 py-12 shadow-2xl"
      >
        <h1
          id="end-screen-title"
          className="text-3xl font-black uppercase tracking-widest text-teal-700"
        >
          {t("endScreen.title")}
        </h1>
        <div className="mt-8 w-full flex-1" />
      </section>
    </div>
  );
};

export default EndScreen;
