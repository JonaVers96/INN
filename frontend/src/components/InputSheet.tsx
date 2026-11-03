import {Fragment, useState} from "react";
import { useTranslation } from "react-i18next";
import { useFormik} from "formik";
import { useCharacter } from "../hooks/useCharacter";
import type {  BudgetFormValues,  FlowStep} from "../types/inputsheet";
import PartnerWheelOverlay from "./PartnerWheelOverlay";
import ChildrenWheelOverlay from "./ChildrenWheelOverlay";
import {
  InvalidFieldsContext,
  errorStyle,
  totalErrorStyle,
  col2Class,
} from "./inputSheet/invalidFields";
import SectionLabel from "./inputSheet/SectionLabel";
import CategoryHeader from "./inputSheet/CategoryHeader";
import InputRow from "./inputSheet/InputRow";
import CalcInput from "./inputSheet/CalcInput";
import CalcRow from "./inputSheet/CalcRow";
import PhaseButton from "./inputSheet/PhaseButton";
import { useDossierSubmit } from "../hooks/useDosierSubmit";
import {
  INPUT_SECTIONS,
  P1_CALC_ROWS,
  P2_CALC_ROWS_TOP,
  P2_CALC_ROWS_BOTTOM,
  P2_ACCOUNT_COLUMNS,
  type CalcRowConfig,
} from "./inputSheet/inputSheetConfig";
const InputSheet = ({
  onPhase2Complete,
  onGameComplete,
}: {
  onPhase2Complete?: () => void;
  onGameComplete?: (values: BudgetFormValues) => void;
}) => {
  const { t } = useTranslation("input_sheet");

  const [step, setStep] = useState<FlowStep>("PHASE1_INPUT");
  const [phase1Collapsed, setPhase1Collapsed] = useState(false);
  const [showPartnerWheel, setShowPartnerWheel] = useState(false);
  const [showChildrenWheel, setShowChildrenWheel] = useState(false);

  const col1ReadOnly = step !== "PHASE1_INPUT";
  const col2ReadOnly = step !== "PHASE2_INPUT";
  const col2Blurred = step === "PHASE1_INPUT" || step === "PHASE1_CALC";

  // klaarzetten children en partner
  const { character, revealLivingSituation } = useCharacter();

  // bepalen of berekeningsvelden readonly zijn
  const p1CalcReadOnly = step !== "PHASE1_CALC";
  const p2CalcReadOnly = step !== "PHASE2_CALC";
 
  const { onSubmit, topBtn, hasError, isChecking, invalidFields, clearField } =
    useDossierSubmit({
      step,
      setStep,
      onPhase1Valid: () => setShowPartnerWheel(true),
      onPhase2Complete,
      onGameComplete,
    });

  const formik = useFormik<
    BudgetFormValues & {
      remaining_budget_phase1: string;
      remaining_budget_phase2: string;
    }
  >({
    initialValues: {
      net_salary_phase1: "",
      net_salary_phase2: "",
      partner_salary_phase1: "",
      partner_salary_phase2: "",
      monthly_rent_phase1: "",
      monthly_rent_phase2: "",
      living_costs_phase1: "",
      living_costs_phase2: "",
      purchase_monthly_phase1: "",
      purchase_monthly_phase2: "",
      monthly_cost_phase1: "",
      monthly_cost_phase2: "",
      car_insurance_phase1: "",
      car_insurance_phase2: "",
      fire_insurance_phase1: "",
      fire_insurance_phase2: "",
      family_insurance_phase1: "",
      family_insurance_phase2: "",
      hospital_insurance_phase1: "",
      hospital_insurance_phase2: "",
      accident_insurance_phase1: "",
      accident_insurance_phase2: "",
      remaining_budget_phase1: "",
      remaining_budget_phase2: "",

      // fase 1 Berekeningen
      calc_p1_remaining: "",
      calc_p1_student: "",
      calc_p1_parental: "",
      calc_p1_housing: "",
      calc_p1_factor: "",
      calc_p1_total1: "",

      // fase 2 Berekeningen
      calc_p2_total1_carry: "",
      calc_p2_partner_savings: "",
      calc_p2_total2: "",
      calc_p2_housing: "",
      calc_p2_total3: "",
      calc_p2_checking: "",
      calc_p2_savings: "",
      calc_p2_investment: "",
      calc_p2_total3_age50: "",
      calc_p2_remaining: "",
      calc_p2_event1: "",
      calc_p2_event2: "",
      calc_p2_event3: "",
      calc_p2_total4: "",
      calc_p2_return_savings: "",
      calc_p2_return_investment: "",
      calc_p2_inflation: "",
      calc_p2_total5: "",
    },
  onSubmit,
  });

  if (!character)
    return <div className="p-10 text-center">{t("general.no_role")}</div>;

  const partnerIndex = character.livingSituation.allPartners.findIndex(
    (p) => p.id === character.livingSituation.partner?.id,
  );
  const chosenPartnerIndex = partnerIndex >= 0 ? partnerIndex : 0;
  const chosenChildrenIndex = character.livingSituation.children ?? 0;

  const handlePartnerAssigned = () => {
    setShowPartnerWheel(false);
    setShowChildrenWheel(true);
  };

  const handleChildrenAssigned = () => {
    setShowChildrenWheel(false);
    revealLivingSituation();
    setStep("PHASE2_INPUT");
    setPhase1Collapsed(true);
  };

    const renderCalcRows = (rows: CalcRowConfig[], readOnly: boolean) =>
    rows.map((row) => (
      <CalcRow
        key={row.name}
        label={t(row.labelKey)}
        sub={row.subKey ? t(row.subKey) : undefined}
      >
        <CalcInput
          name={row.name}
          formik={formik}
          readOnly={row.alwaysReadOnly || readOnly}
          prefix={row.prefix}
        />
      </CalcRow>
    ));

  return (
    <InvalidFieldsContext.Provider value={{ invalidFields, clearField }}>
    <div className="flex">
      <div className="flex-1">
        <form onSubmit={formik.handleSubmit} className="h-full overflow-y-auto">
          <div className="flex justify-start mb-1">
            <img
              src="/Logo_Langwerpig.svg"
              alt="Live Your Life"
              className="w-60 opacity-60"
            />
          </div>

          <div className="flex mb-2 sticky top-0 bg-white pb-1 border-b border-slate-100 z-10">
            <div className="flex-1" />
            <div
              className={`w-32 text-center text-xs font-black text-white py-1 ${col1ReadOnly ? "opacity-70 pointer-events-none" : ""}`}
              style={{ backgroundColor: "#DE7862" }}
            >
              {t("work_phase_1")}
            </div>
            <div
              className={`w-32 text-center text-xs font-black text-white py-1 ${col2Blurred ? "blur-[1px] pointer-events-none" : ""}`}
              style={{ backgroundColor: "#11B0A5" }}
            >
              {t("work_phase_2")}
            </div>
          </div>
          {INPUT_SECTIONS.map((section) => (
            <Fragment key={section.labelKey}>
              <SectionLabel label={t(section.labelKey)} />
              {section.categories.map((category) => (
                <Fragment key={category.labelKey}>
                  <CategoryHeader icon={category.icon} label={t(category.labelKey)} />
                  {category.rows.map((row) => (
                    <InputRow
                      key={row.field}
                      label={t(row.labelKey)}
                      namePhase1={`${row.field}_phase1`}
                      namePhase2={`${row.field}_phase2`}
                      formik={formik}
                      col1ReadOnly={row.col1Locked || col1ReadOnly}
                      col2ReadOnly={col2ReadOnly}
                      col2Blurred={col2Blurred}
                      placeholder={row.placeholder}
                    />
                  ))}
                </Fragment>
              ))}
            </Fragment>
          ))}

          <div
            className="flex gap-0 mt-4 text-xs font-black uppercase items-stretch"
            style={{ backgroundColor: "#F9BC2F" }}
          >
            <span className="flex-1 text-slate-600 px-2 py-2 flex items-center">
              {t("remaining_budget")}
            </span>
            <div
              className="w-32 flex items-center justify-between px-2 py-1"
              style={{ backgroundColor: "#FDE8DF" }}
            >
              <span>=</span>
              <input
                name="remaining_budget_phase1"
                type="number"
                onChange={(e) => {
                  clearField("remaining_budget_phase1");
                  formik.handleChange(e);
                }}
                value={formik.values.remaining_budget_phase1}
                readOnly={col1ReadOnly}
                style={errorStyle(
                  invalidFields.includes("remaining_budget_phase1"),
                )}
                className={`w-full mx-1 bg-transparent text-center outline-none font-black ${
                  col1ReadOnly ? "opacity-70 pointer-events-none" : ""
                }`}
              />
              <span>▲</span>
            </div>
            <div
              className="w-32 flex items-center justify-between px-2 py-1"
              style={{ backgroundColor: "#E8F7F5" }}
            >
              <span>=</span>
              <input
                name="remaining_budget_phase2"
                type="number"
                onChange={(e) => {
                  clearField("remaining_budget_phase2");
                  formik.handleChange(e);
                }}
                value={formik.values.remaining_budget_phase2}
                readOnly={col2ReadOnly}
                style={errorStyle(
                  invalidFields.includes("remaining_budget_phase2"),
                )}
                className={`w-full mx-1 bg-transparent text-center outline-none font-black ${col2Class(col2Blurred, col2ReadOnly)}`}
              />
              <span>■</span>
            </div>
          </div>

          <PhaseButton
            type="submit"
            label={topBtn.label}
            color={topBtn.color}
            disabled={topBtn.disabled}
          />

          {/* berekening fase 1 */}
          {(step === "PHASE1_CALC" ||
            step === "PHASE2_INPUT" ||
            step === "PHASE2_CALC" ||
            step === "DONE") && (
            <div className="mt-4 rounded overflow-hidden">
              <div
                className="flex px-2 py-2 cursor-pointer"
                style={{ backgroundColor: "#DE7862" }}
                onClick={() =>
                  col1ReadOnly && setPhase1Collapsed(!phase1Collapsed)
                }
              >
                <span className="flex-1 text-xs font-black text-white uppercase">
                  {t("calculation_phase1")}
                </span>
                <span className="text-xs font-black text-white uppercase">
                  {col1ReadOnly
                    ? phase1Collapsed
                      ? "▶"
                      : "▼"
                    : t("calculation")}
                </span>
              </div>

              {!phase1Collapsed && (
                <div style={{ backgroundColor: "#FBE2D5" }}>
                    {renderCalcRows(P1_CALC_ROWS, p1CalcReadOnly)}

                  <div
                    className="flex gap-0 text-xs font-black items-center"
                    style={{ backgroundColor: "#DE7862" }}
                  >
                    <div className="flex-1 px-2 py-2 pr-2">
                      <span className="text-white uppercase">
                        {t("total1")}
                      </span>
                    </div>
                    <div className="w-1/3 px-2 py-2 flex items-center gap-1">
                      <span className="text-white whitespace-nowrap">= ●</span>
                      <input
                        name="calc_p1_total1"
                        type="number"
                        onChange={(e) => {
                          clearField("calc_p1_total1");
                          formik.handleChange(e);
                        }}
                        value={formik.values.calc_p1_total1}
                        readOnly={p1CalcReadOnly}
                        style={totalErrorStyle(invalidFields.includes("calc_p1_total1"))}
                        className={`flex-1 min-w-0 text-right bg-transparent outline-none border-b ${p1CalcReadOnly ? "border-transparent opacity-70 pointer-events-none text-white" : "border-white text-white"}`}
                      />
                    </div>
                  </div>

                  {step === "PHASE1_CALC" && (
                    <div className="bg-white pt-2">
                      <PhaseButton
                        type="submit"
                        label={
                          isChecking
                            ? t("checking")
                            : hasError
                              ? t("error_calc")
                              : t("check_calc_phase1")
                        }
                        color={hasError ? "#991a00" : "#076b67"}
                        disabled={isChecking}
                      />
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          {/* berekening fase 2 */}
          {(step === "PHASE2_CALC" || step === "DONE") && (
            <div className="mt-4 rounded overflow-hidden">
              <div
                className="flex px-2 py-2"
                style={{ backgroundColor: "#11B0A5" }}
              >
                <span className="flex-1 text-xs font-black text-white uppercase">
                  {t("calculation_phase2")}
                </span>
                <span className="text-xs font-black text-white uppercase">
                  {t("calculation")}
                </span>
              </div>
              <div style={{ backgroundColor: "#D8F1EF" }}>
                {renderCalcRows(P2_CALC_ROWS_TOP, p2CalcReadOnly)}
                <div className="grid grid-cols-3 border-b border-white text-xs">
                  {P2_ACCOUNT_COLUMNS.map((col) => (
                    <div
                      key={col.name}
                      className="p-2 border-r border-white font-bold text-center flex flex-col items-center justify-center last:border-r-0"
                      style={{ backgroundColor: "#11B0A5", color: "white" }}
                    >
                      <p>{t(col.labelKey)}</p>
                      {col.subKey && (
                        <p className="font-normal text-[10px] text-center">
                          {t(col.subKey)}
                        </p>
                      )}
                    </div>
                  ))}
                </div>

                <div className="grid grid-cols-3 border-b border-white text-xs">
                  {P2_ACCOUNT_COLUMNS.map((col) => (
                    <div
                      key={col.name}
                      className="p-2 border-r border-white flex justify-center last:border-r-0"
                      style={{ backgroundColor: "#E8F7F5" }}
                    >
                      <CalcInput
                        name={col.name}
                        formik={formik}
                        readOnly={p2CalcReadOnly}
                        align="center"
                      />
                    </div>
                  ))}
                </div>

                {renderCalcRows(P2_CALC_ROWS_BOTTOM, p2CalcReadOnly)}

                <div
                  className="flex gap-0 text-xs font-black items-center"
                  style={{ backgroundColor: "#11B0A5" }}
                >
                  <div className="flex-1 px-2 py-2 pr-2">
                    <span className="text-white uppercase">{t("total5")}</span>
                  </div>
                  <div className="w-1/3 px-2 py-2 flex items-center gap-1">
                    <span className="text-white whitespace-nowrap">=</span>
                    <input
                      name="calc_p2_total5"
                      type="number"
                      onChange={(e) => {
                        clearField("calc_p2_total5");
                        formik.handleChange(e);
                      }}
                      value={formik.values.calc_p2_total5}
                      readOnly={p2CalcReadOnly}
                      style={totalErrorStyle(invalidFields.includes("calc_p2_total5"))}
                      className={`flex-1 min-w-0 text-right bg-transparent outline-none border-b ${p2CalcReadOnly ? "border-transparent opacity-70 pointer-events-none text-white" : "border-white text-white"}`}
                    />
                  </div>
                </div>

                <div className="bg-white pt-2">
                  <PhaseButton
                    type={step === "DONE" ? "button" : "submit"}
                    label={
                      step === "DONE"
                        ? t("done")
                        : isChecking
                          ? t("checking")
                          : hasError
                            ? t("error_calc")
                            : t("check_calc_phase2")
                    }
                    color={
                      step === "DONE"
                        ? "#11B0A5"
                        : hasError
                          ? "#991a00"
                          : "#076b67"
                    }
                    disabled={step === "DONE" || isChecking}
                  />
                </div>
              </div>
            </div>
          )}
        </form>
      </div>
      <PartnerWheelOverlay
        isOpen={showPartnerWheel}
        partners={character.livingSituation.allPartners}
        predeterminedPartnerIndex={chosenPartnerIndex}
        onPartnerAssigned={handlePartnerAssigned}
      />

      <ChildrenWheelOverlay
        isOpen={showChildrenWheel}
        predeterminedChildrenIndex={chosenChildrenIndex}
        onChildrenAssigned={handleChildrenAssigned}
      />
    </div>
    </InvalidFieldsContext.Provider>
  );
};

export default InputSheet;
