import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
  postDossierCheck,
  completeFirstWorkPhase,
  type DossierCheckEndpoint,
} from "../api/dossierApi";
import type { FlowStep } from "../types/inputsheet";

// Welk endpoint bij welke stap hoort; in DONE valt er niets meer te checken
const CHECK_ENDPOINT: Record<FlowStep, DossierCheckEndpoint | null> = {
  PHASE1_INPUT: "check-dossier-monthly",
  PHASE1_CALC: "check-dossier-total",
  PHASE2_INPUT: "check-dossier-monthly",
  PHASE2_CALC: "check-dossier-total",
  DONE: null,
};

export const useDossierSubmit = ({
  step,
  setStep,
  onPhase1Valid,
  onPhase2Complete,
}: {
  step: FlowStep;
  setStep: (step: FlowStep) => void;
  onPhase1Valid: () => void;
  onPhase2Complete?: () => void;
}) => {
  const { t } = useTranslation("input_sheet");

  const [hasError, setHasError] = useState(false);
  const [isChecking, setIsChecking] = useState(false);
  const [invalidFields, setInvalidFields] = useState<string[]>([]);

  const clearField = (name: string) =>
    setInvalidFields((prev) => prev.filter((f) => f !== name));

  const onSubmit = async (values: object) => {
    const endpoint = CHECK_ENDPOINT[step];
    if (!endpoint) return;

    setHasError(false);
    setInvalidFields([]);
    setIsChecking(true);

    try {
      const result = await postDossierCheck(endpoint, values);
      if (!result.isValid) {
        setInvalidFields(result.invalidFields);
        setHasError(true);
        return;
      }

      // Wat er na een geldige check gebeurt, verschilt per stap
      switch (step) {
        case "PHASE1_INPUT":
          setStep("PHASE1_CALC");
          break;
        case "PHASE1_CALC":
          await completeFirstWorkPhase();
          onPhase1Valid();
          break;
        case "PHASE2_INPUT":
          setStep("PHASE2_CALC");
          onPhase2Complete?.();
          break;
        case "PHASE2_CALC":
          setStep("DONE");
          break;
      }
    } catch (error) {
      console.error("Fout tijdens validatie:", error);
      setHasError(true);
    } finally {
      setIsChecking(false);
    }
  };

  const getTopButtonProps = () => {
    if (step === "PHASE1_INPUT" || step === "PHASE2_INPUT") {
      return {
        label: isChecking
          ? t("checking")
          : hasError
            ? t("error_retry")
            : t(step === "PHASE1_INPUT" ? "check_phase1_input" : "check_phase2_input"),
        color: hasError ? "#991a00" : "#076b67",
        disabled: isChecking,
      };
    }
    if (step === "PHASE1_CALC") {
      return { label: t("phase1_saved"), color: "#DE7862", disabled: true };
    }
    return { label: t("phase2_saved"), color: "#11B0A5", disabled: true };
  };

  return {
    onSubmit,
    topBtn: getTopButtonProps(),
    hasError,
    isChecking,
    invalidFields,
    clearField,
  };
};
