import { useTranslation } from "react-i18next";
import type { BudgetFormValues } from "../../types/inputsheet";

type Props = {
  values: BudgetFormValues;
};

const EndScreen = ({ values }: Props) => {
  const { t: tCommon, i18n } = useTranslation();
  const { t: tInput } = useTranslation("input_sheet");
  const locale = i18n.resolvedLanguage ?? "en";
  const currencyFormatter = new Intl.NumberFormat(locale, {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 2,
  });
  const signedCurrencyFormatter = new Intl.NumberFormat(locale, {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 2,
    signDisplay: "always",
  });
  const formatCurrency = (value: string, showSign = false) => {
    if (value.trim() === "") return "—";
    const number = Number(value);
    if (!Number.isFinite(number)) return "—";
    return (showSign ? signedCurrencyFormatter : currencyFormatter).format(number);
  };
  const phase1Budget = Number(values.remaining_budget_phase1);
  const phase2Budget = Number(values.remaining_budget_phase2);
  const budgetChange =
    values.remaining_budget_phase1.trim() !== "" &&
    values.remaining_budget_phase2.trim() !== "" &&
    Number.isFinite(phase1Budget) &&
    Number.isFinite(phase2Budget)
      ? String(phase2Budget - phase1Budget)
      : "";
  const eventImpacts = [
    values.calc_p2_event1,
    values.calc_p2_event2,
    values.calc_p2_event3,
  ];
  const totalEventImpact =
    eventImpacts.every((impact) => impact.trim() !== "") &&
    eventImpacts.every((impact) => Number.isFinite(Number(impact)))
      ? String(eventImpacts.reduce((total, impact) => total + Number(impact), 0))
      : "";

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="end-screen-title"
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
              {tCommon("endScreen.stats.budgetChange")}
            </h2>
            <p className="mt-2 text-xl font-black text-slate-800">
              {formatCurrency(budgetChange, true)}
            </p>
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
