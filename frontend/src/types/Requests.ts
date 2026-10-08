export interface JoinTableRequest {
    roomCode: string;
    tableNumber: number;
    nickName: string;
    playerId?: string;
}
 
export interface LeaveTableRequest {
    roomCode: string;
    tableNumber: number;
    playerId: string; 
}

// Het contract backend studentenfase
export interface StudentPhaseSubmitRequest {
  selections: { [categoryId: string]: number };
}
