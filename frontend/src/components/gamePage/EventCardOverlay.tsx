import { useTranslation } from "react-i18next";
import EventCardDisplay from "../EventCardDisplay";
import type { useEventCards } from "../../hooks/useEventCards";

type Props = { eventCards: ReturnType<typeof useEventCards> };

const EventCardsOverlay = ({ eventCards }: Props) => {
  const { t } = useTranslation("eventData");
  const {
    eventPhase,
    currentPack,
    currentSet,
    chosenCards,
    selectedCard,
    selectCard,
    startChoosing,
    confirmCard,
    finish,
  } = eventCards;

  return (
    <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center">
      <div
        className="relative w-[60%] max-h-[85vh] opacity-100 rounded-3xl overflow-y-auto flex flex-col items-center justify-center p-8"
        style={{ backgroundColor: "#1a2f3a" }}
      >
        {/* WELKOMSTSCHERM */}
        {eventPhase === "welcome" && (
          <div className="flex flex-col items-center gap-8 text-center px-8">
            <div className="text-6xl">🎲</div>
            <h1 className="text-4xl font-black text-white uppercase tracking-widest">
              Life happens...
            </h1>
            <p className="text-slate-300 text-lg max-w-md">
              {t("event_cards.welcome_text")}
            </p>
            <button
              onClick={startChoosing}
              className="mt-4 bg-amber-500 hover:bg-amber-400 text-white font-black py-4 px-12 rounded-full text-lg uppercase tracking-widest shadow-lg transition-transform hover:scale-105"
            >
              {t("event_cards.start")}
            </button>
          </div>
        )}
        {/* KIESSCHERM */}
        {eventPhase === "choosing" && currentPack && (
          <div className="flex flex-col items-center gap-6 w-full px-4">
            <div className="text-center">
              <p className="text-slate-400 text-sm uppercase tracking-widest mb-1">
                {currentSet + 1} / 3
              </p>
              <h2 className="text-2xl font-black text-white uppercase tracking-widest">
                {t("event_cards.choose_one")}
              </h2>
            </div>

            <div className="relative grid grid-cols-3 gap-6 w-full">
              {currentPack.map((card) => (
                <div
                  key={card.id}
                  onClick={() => !selectedCard && selectCard(card)}
                  className={`cursor-pointer transition-all duration-300 rounded-3xl ${
                    selectedCard?.id === card.id
                      ? "invisible"
                      : selectedCard
                        ? "blur-sm opacity-40 pointer-events-none"
                        : "hover:scale-[1.01]"
                  }`}
                >
                  <EventCardDisplay
                    card={card}
                    mode="choose"
                    onConfirm={() => {}}
                  />
                </div>
              ))}

              {selectedCard && (
                <div className="absolute inset-0 flex items-center justify-center z-10">
                  <div className="w-95">
                    <EventCardDisplay
                      card={selectedCard}
                      mode="confirm"
                      onConfirm={(_cardId, conditionId) =>
                        confirmCard(conditionId)
                      }
                    />
                    <button
                      onClick={() => selectCard(null)}
                      className="mt-2 w-full text-slate-300 hover:text-white text-xs text-center"
                    >
                      {t("event_cards.back")}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
        {/* SAMENVATTING */}
        {eventPhase === "summary" && (
          <div className="flex flex-col items-center gap-6 w-full px-4">
            <h2 className="text-2xl font-black text-white uppercase tracking-widest">
              {t("event_cards.your_cards")}
            </h2>
            <div className="grid grid-cols-3 gap-6 w-full">
              {chosenCards.map(({ card, conditionId }) => (
                <EventCardDisplay
                  key={card.id}
                  card={card}
                  mode="readonly"
                  readOnlyConditionId={conditionId}
                />
              ))}
            </div>
            <button
              onClick={finish}
              className="bg-teal-500 hover:bg-teal-600 text-white font-black py-3 px-10 rounded-full uppercase tracking-widest transition-transform hover:scale-105"
            >
              {t("event_cards.back_to_sheet")}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default EventCardsOverlay;
