import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import type { BudgetFormValues } from "../../types/inputsheet";
import { formatCurrency as formatEuro } from "../../utils/formatters";

type Props = {
  values: BudgetFormValues;
  onClose: () => void;
};

const EndScreen = ({ values, onClose }: Props) => {
  const { t: tCommon } = useTranslation();
  const { t: tInput } = useTranslation("input_sheet");

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose]);
  const toNumber = (value: unknown): number | null => {
    if (typeof value === "number") {
      return Number.isFinite(value) ? value : null;
    }
    if (typeof value !== "string" || value.trim() === "") return null;
    const number = Number(value);
    return Number.isFinite(number) ? number : null;
  };
  const formatCurrency = (value: unknown, showSign = false) => {
    const number = toNumber(value);
    if (number === null) return "—";
    return formatEuro(number, { showSign });
  };
  const phase1Budget = toNumber(values.remaining_budget_phase1);
  const phase2Budget = toNumber(values.remaining_budget_phase2);

  const eventImpacts = [
    values.calc_p2_event1,
    values.calc_p2_event2,
    values.calc_p2_event3,
  ].map(toNumber);
  const totalEventImpact = eventImpacts.every((impact) => impact !== null)
    ? eventImpacts.reduce<number>((total, impact) => total + (impact ?? 0), 0)
    : null;

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/50 backdrop-blur-sm"
      onClick={onClose}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="end-screen-title"
        onClick={(e) => e.stopPropagation()}
        className="flex h-1/2 w-1/2 min-h-80 min-w-80 max-w-4xl flex-col items-center overflow-y-auto rounded-3xl bg-slate-50 p-6 shadow-2xl"
      >
        <h1
          id="end-screen-title"
          className="text-center text-2xl font-black uppercase tracking-widest text-teal-700"
        >
          {tCommon("endScreen.title")}
        </h1>
        <div className="mt-6 grid w-full flex-1 grid-cols-1 gap-3 sm:grid-cols-2">
          <article className="rounded-2xl bg-white p-4 shadow-sm">
            <h2 className="text-sm font-bold uppercase tracking-wide text-slate-500">
              {tCommon("endScreen.stats.finalTotal")}
            </h2>
            <p className="mt-2 text-2xl font-black text-teal-700">
              {formatCurrency(values.calc_p2_total5)}
            </p>
          </article>

          <article className="rounded-2xl bg-white p-4 shadow-sm">
            <h2 className="text-sm font-bold uppercase tracking-wide text-slate-500">
              {tCommon("endScreen.stats.allocation")}
            </h2>
            <dl className="mt-2 space-y-1 text-sm">
              <div className="flex justify-between gap-2">
                <dt>{tInput("checking_account")}</dt>
                <dd className="font-bold">{formatCurrency(values.calc_p2_checking)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>{tInput("savings_account")}</dt>
                <dd className="font-bold">{formatCurrency(values.calc_p2_savings)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>{tInput("investment")}</dt>
                <dd className="font-bold">{formatCurrency(values.calc_p2_investment)}</dd>
              </div>
            </dl>
          </article>

                   <article className="rounded-2xl bg-white p-4 shadow-sm">
            <h2 className="text-sm font-bold uppercase tracking-wide text-slate-500">
              {tCommon("endScreen.stats.monthlyTotal")}
            </h2>
            <div className="mt-2 grid grid-cols-2 gap-4">
              <div>
                <h3 className="text-xs font-bold uppercase tracking-wide text-slate-400">
                  {tCommon("endScreen.stats.Phase1")}
                </h3>
                <p className="mt-1 text-xl font-black text-slate-800">
                  {formatCurrency(phase1Budget)}
                </p>
              </div>
              <div>
                <h3 className="text-xs font-bold uppercase tracking-wide text-slate-400">
                  {tCommon("endScreen.stats.Phase2")}
                </h3>
                <p className="mt-1 text-xl font-black text-slate-800">
                  {formatCurrency(phase2Budget)}
                </p>
              </div>
            </div>
          </article>



          <article className="rounded-2xl bg-white p-4 shadow-sm">
            <h2 className="text-sm font-bold uppercase tracking-wide text-slate-500">
              {tCommon("endScreen.stats.eventImpact")}
            </h2>
            <p className="mt-2 text-xl font-black text-slate-800">
              {formatCurrency(totalEventImpact, true)}
            </p>
            <ul className="mt-2 space-y-1 text-xs text-slate-600">
              {eventImpacts.map((impact, index) => (
                <li key={index} className="flex justify-between gap-2">
                  <span>{tCommon("endScreen.stats.event", { number: index + 1 })}</span>
                  <span>{formatCurrency(impact, true)}</span>
                </li>
              ))}
            </ul>
          </article>
        </div>
      </section>
    </div>
  );
};

export default EndScreen;
