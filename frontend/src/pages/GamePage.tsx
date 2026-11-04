import CharacterProfileCard from "../components/CharacterProfileCard";
import HousingContent from "../components/dossier/HousingContent";
import LivingCostsContent from "../components/dossier/LivingCostsContent";
import InsuranceContent from "../components/dossier/InsuranceContent";
import MobilityContent from "../components/dossier/MobilityContent";
import SavingsContent from "../components/dossier/SavingsContent";
import InputSheet from "../components/InputSheet";
import { useTranslation } from "react-i18next";
import { useState } from "react";
import { useResizablePanel } from "../hooks/useResizablePanel";
import { useCharacter } from "../hooks/useCharacter";
import { useEventCards } from "../hooks/useEventCards";
import { FaDice } from "react-icons/fa";
import StudentPhaseOverlay from "../components/gamePage/StudentPhaseOverlay";
import LivingSituationOverlay from "../components/gamePage/LivingSituationOverlay";
import EventCardsOverlay from "../components/gamePage/EventCardOverlay";
import ModalOverlay from "../components/gamePage/ModalOverlay";
import EndScreen from "../components/gamePage/EndScreen";
import type { BudgetFormValues } from "../types/inputsheet";
import { useEffect } from "react";

const GamePage = () => {
  const { character } = useCharacter();
  const { t } = useTranslation("input_sheet");
  const { rightWidth, startDragging } = useResizablePanel();
  const eventCards = useEventCards();

  const [activeDossier, setActiveDossier] = useState<string | null>(null);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [isStudentPhaseOpen, setIsStudentPhaseOpen] = useState(false);
  const [isLivingSituationOpen, setIsLivingSituationOpen] = useState(false);
  const [completedValues, setCompletedValues] =
    useState<BudgetFormValues | null>(null);

  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };

    window.addEventListener("beforeunload", handleBeforeUnload); // koppelt de functie aan het event dat afgaat bij refresh, tab sluiten of wegnavigeren
    return () => window.removeEventListener("beforeunload", handleBeforeUnload); // cleanup: haalt de listener weg als de component verdwijnt, zodat ze zich niet opstapelen
  }, []); // Geeft standaard popup bij refresh


  return (
    <div className="h-screen flex overflow-hidden">
      {/* Kolom 1: sidebar */}
      <div className="w-16 bg-white border-r border-slate-200 flex flex-col justify-between py-4 sticky top-0 h-screen">
        <div className="flex flex-col items-center gap-4">
          {activeDossier !== null && (
            <button
              onClick={() => setActiveDossier(null)}
              className="text-slate-400 hover:text-slate-600 transition-colors text-xs font-black"
              title="Sluit categorie"
            >
              ✕
            </button>
          )}
          <button
            onClick={() => setActiveDossier("huisvesting")}
            title="Housing"
            className={`hover:scale-130 transition-transform hover:opacity-80 ${activeDossier === "huisvesting" ? "opacity-100" : "opacity-40"}`}
          >
            <img src="/huisvestiging.svg" alt="" className="w-8 h-8" />
          </button>
          <button
            onClick={() => setActiveDossier("leefkosten")}
            title="Expenses"
            className={`hover:scale-130 transition-transform hover:opacity-80 ${activeDossier === "leefkosten" ? "opacity-100" : "opacity-40"}`}
          >
            <img src="/leefkosten.svg" alt="" className="w-8 h-8" />
          </button>
          <button
            onClick={() => setActiveDossier("mobiliteit")}
            title="Mobility"
            className={`hover:scale-130 transition-transform hover:opacity-80 ${activeDossier === "mobiliteit" ? "opacity-100" : "opacity-40"}`}
          >
            <img src="/mobiliteit.svg" alt="" className="w-8 h-8" />
          </button>
          <button
            onClick={() => setActiveDossier("verzekeringen")}
            title="Insurances"
            className={`hover:scale-130 transition-transform hover:opacity-80 ${activeDossier === "verzekeringen" ? "opacity-100" : "opacity-40"}`}
          >
            <img src="/verzekering.svg" alt="" className="w-8 h-8" />
          </button>
          <button
            onClick={() => setActiveDossier("sparenenbeleggen")}
            title="Savings & Investments"
            className={`hover:scale-130 transition-transform hover:opacity-80 ${activeDossier === "sparenenbeleggen" ? "opacity-100" : "opacity-40"}`}
          >
            <img src="/sparenenbeleggen.svg" alt="" className="w-8 h-8" />
          </button>
        </div>

        <div className="flex flex-col items-center gap-4 pb-2">
          {eventCards.eventCardsDone && (
            <button
              onClick={eventCards.openSummary}
              title="Event cards bekijken"
              className="hover:scale-130 transition-transform opacity-40 hover:opacity-80"
            >
              <FaDice size={22} style={{ color: "#056b67" }} />{" "}
            </button>
          )}
          <button
            onClick={() => setIsLivingSituationOpen(true)}
            title="Leefsituatie bekijken"
            className="hover:scale-130 transition-transform opacity-40 hover:opacity-80"
          >
            <img src="/livingSituation.png" alt="" className="w-8 h-8" />
          </button>
          <button
            onClick={() => setIsStudentPhaseOpen(true)}
            title="Studentenfase bekijken"
            className="hover:scale-130 transition-transform opacity-40 hover:opacity-80"
          >
            <img src="/student.svg" alt="" className="w-8 h-8" />
          </button>
          <div
            className="relative"
            onMouseEnter={(e) =>
              ((
                e.currentTarget.querySelector("button") as HTMLElement
              ).style.transform = "scale(3)")
            }
            onMouseLeave={(e) => {
              (
                e.currentTarget.querySelector("button") as HTMLElement
              ).style.transform = "scale(1)";
            }}
          >
            <button
              onClick={() => setIsProfileOpen(true)}
              className="w-10 h-10 rounded-full bg-teal-500 transition-transform overflow-hidden"
              style={{
                transformOrigin: "bottom left",
                transform: isProfileOpen ? "scale(3)" : "scale(1)",
              }}
              title="Bekijk personnagefiche"
            >
              <img
                src={character ? `/${character.profile.id}.png` : "/char1.png"}
                alt="avatar"
                className="w-full h-full object-cover"
              />
            </button>
          </div>
        </div>
      </div>

      {/* Kolom 2: dossiercontent */}
      <div className="flex-1 bg-slate-50 p-6 overflow-hidden">
        {activeDossier === null && (
          <div className="h-full flex flex-col relative overflow-hidden">
            <div className="flex items-center gap-2 pt-8 pl-2">
              <div
                className="animate-bounce text-2xl"
                style={{ color: "#056b67" }}
              >
                ←
              </div>
              <p
                className="text-sm font-black uppercase tracking-[0.3em]"
                style={{ color: "#056b67" }}
              >
                {t("select_category")
                  .split("")
                  .map((char, i) => (
                    <span
                      key={i}
                      className="wave-letter"
                      style={{ animationDelay: `${i * 0.05}s` }}
                    >
                      {char === " " ? "\u00A0" : char}
                    </span>
                  ))}
              </p>
            </div>
            <img
              src="/Logo_Vlek.svg"
              alt="Live Your Life"
              className="absolute self-center-safe w-150 opacity-50"
            />
          </div>
        )}
        {activeDossier === "huisvesting" && <HousingContent />}
        {activeDossier === "leefkosten" && <LivingCostsContent />}
        {activeDossier === "verzekeringen" && <InsuranceContent />}
        {activeDossier === "mobiliteit" && <MobilityContent />}
        {activeDossier === "sparenenbeleggen" && <SavingsContent />}
      </div>

      {/* Resizer */}
      <div
        className="w-1 bg-slate-200 hover:bg-teal-400 cursor-col-resize transition-colors"
        onMouseDown={startDragging}
      />

      {/* Kolom 3: invulblad */}
      <div
        className="bg-white border-l border-slate-200 p-4 overflow-y-auto relative"
        style={{ width: `${rightWidth}px`, minWidth: "200px" }}
      >
        {rightWidth < 380 && (
          <div className="absolute inset-y-0 left-0 w-6 bg-linear-to-r from-slate-200 to-transparent flex items-center justify-start pl-1 pointer-events-none">
            <span className="text:#056b67 text-10xl">◀</span>
          </div>
        )}
        <InputSheet
          onPhase2Complete={eventCards.fetchAndStartEventCards}
          onGameComplete={setCompletedValues}
        />
      </div>

      {isProfileOpen && (
        <ModalOverlay onClose={() => setIsProfileOpen(false)}>
          <CharacterProfileCard />
        </ModalOverlay>
      )}
      {isStudentPhaseOpen && (
        <StudentPhaseOverlay onClose={() => setIsStudentPhaseOpen(false)} />
      )}
      {isLivingSituationOpen && (
        <LivingSituationOverlay onClose={() => setIsLivingSituationOpen(false)} />
      )}

      {eventCards.showEventCards && (
        <EventCardsOverlay eventCards={eventCards} />
      )}
      {completedValues && <EndScreen values={completedValues} />}
    </div>
  );
};

export default GamePage;
