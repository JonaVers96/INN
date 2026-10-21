import { useState, useEffect, useCallback, type ReactNode } from "react";
import axios from "axios";
import type { CharacterResponse } from "../types/CharacterModel";
import { CharacterContext } from "./CharacterContext";

export const CharacterProvider = ({ children }: { children: ReactNode }) => {
  const [character, setCharacter] = useState<CharacterResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCharacter = useCallback(
    async (retryCount = 0) => {
      const loadCharacter = async (attempt: number): Promise<void> => {
        if (attempt === 0) setIsLoading(true);
        setError(null);

        const roomCode = localStorage.getItem("roomCode");
        const playerId = localStorage.getItem("playerId");

        if (!roomCode || !playerId) {
          setIsLoading(false);
          return;
        }

        try {
          const response = await axios.get<CharacterResponse>(
            `${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}/player/${playerId}/character`,
          );
          setCharacter(response.data);
          setIsLoading(false);
        } catch (err) {
          if (axios.isAxiosError(err) &&
          err.response?.status === 404 &&
          attempt < 10) {
            console.warn(
              `Database is nog aan het laden (poging ${attempt + 1}/10). 1 seconde wachten...`,
            );
            setTimeout(() => {
              void loadCharacter(attempt + 1);
            }, 5000);
            return;
          }

          console.error("Fout bij ophalen van character context:", err);
          setError("Kon je rol niet ophalen.");
          setIsLoading(false);
        }
      };

      await loadCharacter(retryCount);
    },
    [],
  );

  useEffect(() => {
    void fetchCharacter();
  }, [fetchCharacter]);

  return (
    <CharacterContext.Provider
      value={{ character, isLoading, error, fetchCharacter }}
    >
      {children}
    </CharacterContext.Provider>
  );
};
