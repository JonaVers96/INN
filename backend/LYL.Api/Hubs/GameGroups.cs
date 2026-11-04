namespace LYL.Api.Hubs;

public static class GameGroups
{
    // Alleen de supervisor zit in deze groep; spelers zitten in de groep "roomCode"
    public static string Supervisor(string roomCode) => $"{roomCode}-supervisor";
}