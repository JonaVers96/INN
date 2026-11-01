import { useState } from "react";
import axios from "axios";
import eventList from "../assets/eventData.json";
import type { EventCard } from "../types/EventCardType";
import type { SaveEventCardsRequest } from "../types/EventCardContracts";
import { getSession } from "../api/dossierApi";

export type EventPhase = "welcome" | "choosing" | "summary";
export type ChosenCard = { card: EventCard; conditionId: string | null };

const createPacks = (): EventCard[][] => {
  const shuffled = [...eventList].sort(() => Math.random() - 0.5);
    return [
      shuffled.slice(0, 3) as EventCard[],
      shuffled.slice(3, 6) as EventCard[],
      shuffled.slice(6, 9) as EventCard[],
    ];
};

export const useEventCards = () => {
  const [showEventCards, setShowEventCards] = useState(false);
  const [eventPhase, setEventPhase] = useState<EventPhase>("welcome");
  const [currentSet, setCurrentSet] = useState(0);
  const [eventPacks, setEventPacks] = useState<EventCard[][]>([]);
  const [chosenCards, setChosenCards] = useState<ChosenCard[]>([]);
  const [selectedCard, setSelectedCard] = useState<EventCard | null>(null);
  const [eventCardsDone, setEventCardsDone] = useState(false);

  const fetchAndStartEventCards = async () => {
    const session = getSession();
    try {
      if (!session) throw new Error("Geen actieve sessie gevonden");
      const response = await axios.get(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/${session.roomCode}/player/${session.playerId}/getEventCards`,
      );

      const packs = Object.values(response.data) as EventCard[][];
      setEventPacks(packs);
    } catch (err) {
      console.error("Fallback naar lokale data:", err);
      setEventPacks(createPacks());
    }
    setShowEventCards(true);
    setEventPhase("welcome");
    setCurrentSet(0);
    setChosenCards([]);
  };

  const saveEventCardChoices = async () => {
    const session = getSession();
    if (!session) return;
    try {
      const payload: SaveEventCardsRequest = {
        choices: chosenCards.map(({ card, conditionId }) => ({
          cardId: card.id,
          conditionId: conditionId,
        })),
      };
      console.log("Payload:", JSON.stringify(payload, null, 2));

      await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/${session.roomCode}/player/${session.playerId}/saveEventCardsChoices`,
        payload,
      );
    } catch (err) {
      console.error("Fout bij opslaan event card keuzes:", err);
    }
  };

  const startChoosing = () => setEventPhase("choosing");

  const confirmCard = (conditionId: string | null) => {
    if (!selectedCard) return;
    setChosenCards((prev) => [...prev, { card: selectedCard, conditionId }]);
    setSelectedCard(null);
    if (currentSet < 2) {
      setCurrentSet((prev) => prev + 1);
    } else {
      setEventPhase("summary");
    }
  };

  const openSummary = () => {
    setEventPhase("summary");
    setShowEventCards(true);
  };

  const finish = async () => {
    await saveEventCardChoices();
    setShowEventCards(false);
    setEventCardsDone(true);
  };

  return {
    showEventCards,
    eventPhase,
    currentPack: eventPacks[currentSet],
    currentSet,
    chosenCards,
    selectedCard,
    eventCardsDone,
    selectCard: setSelectedCard,
    fetchAndStartEventCards,
    startChoosing,
    confirmCard,
    openSummary,
    finish,
  };
};
