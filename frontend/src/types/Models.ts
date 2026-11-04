import type { CharacterResponse } from "./CharacterModel";


export interface PlayerAmounts {
  monthlyBudgetPhase1?: number | null;
  balanceAfterPhase1?: number | null;
  monthlyBudgetPhase2?: number | null;
  balanceAfterPhase2?: number | null;
}

export interface Player {
  playerId: string;
  nickName: string;
  tableNumber: number;
  isOffline?: boolean;
  currentPhase?: string;
  amounts?: PlayerAmounts;
  assignedCharacter?: CharacterResponse;
}