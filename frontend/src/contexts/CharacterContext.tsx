import { createContext } from "react";
import type { CharacterResponse } from "../types/CharacterModel";

export interface CharacterContextType {
  character: CharacterResponse | null;
  isLoading: boolean;
  error: string | null;
  fetchCharacter: () => Promise<void>;
}

export interface CharacterContextType {
  character: CharacterResponse | null;
  isLoading: boolean;
  error: string | null;
  fetchCharacter: () => Promise<void>;
  revealLivingSituation: () => void;
}

export const CharacterContext = createContext<CharacterContextType | undefined>(
  undefined,
);
