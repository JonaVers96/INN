// Zet de fase die de backend teruggeeft om naar een route van de frontend.
export const phaseToRoute = (phase: string | null | undefined, roomCode: string): string => {
  switch (phase) {
    case "character-discovery":
      return "/character-discovery";
    case "student-phase":
      return "/student-phase";
    case "first-work-phase":
    case "second-work-phase":
      return "/game";
    case "waiting":
      return `/waitingroom/${roomCode}`;
    default:
      // Onbekende fase: veilig terug naar het begin van het spel
      return "/character-discovery";
  }
};
