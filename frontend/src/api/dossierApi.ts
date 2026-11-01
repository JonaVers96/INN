import axios from "axios";

export type CheckResult = { isValid: boolean; invalidFields: string[] };

export type Session = { roomCode: string; playerId: string };

export type DossierCheckEndpoint =
  | "check-dossier-monthly"
  | "check-dossier-total";

export const getSession = (): Session | null => {
  const roomCode = localStorage.getItem("roomCode");
  const playerId = localStorage.getItem("playerId");
  return roomCode && playerId ? { roomCode, playerId } : null;
};

const BASE_URL = import.meta.env.VITE_API_BASE_URL;

const playerUrl = ({ roomCode, playerId }: Session, path: string) =>
  `${BASE_URL}/api/game/${roomCode}/player/${playerId}/${path}`;

// Controleert de invoer (monthly) of de berekeningen (total) van het dossier
export const postDossierCheck = async (
  endpoint: DossierCheckEndpoint,
  data: object,
): Promise<CheckResult> => {
  const session = getSession();
  if (!session) return { isValid: false, invalidFields: [] };

  // Als een veld een lege string is, verander naar null, anders backend helemaal flippen
  const payload = Object.fromEntries(
    Object.entries(data).map(([key, value]) => [
      key,
      value === "" ? null : value,
    ]),
  );

  try {
    const response = await axios.post(playerUrl(session, endpoint), payload);
    // oude backend: gewoon true/false
    if (typeof response.data === "boolean")
      return { isValid: response.data, invalidFields: [] };
    return {
      isValid: !!response.data.isValid,
      invalidFields: response.data.invalidFields ?? [],
    };
  } catch (error) {
    console.error(`Fout bij ${endpoint}:`, error);
    return { isValid: false, invalidFields: [] };
  }
};

export const completeFirstWorkPhase = async (): Promise<void> => {
  const session = getSession();
  if (!session) return;
  try {
    await axios.post(playerUrl(session, "first-work-phase"));
  } catch (err) {
    console.error("Fout bij melden eerste werkfase voltooid:", err);
  }
};
