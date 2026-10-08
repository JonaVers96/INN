namespace LYL.Api.Contracts.Game;

public class JoinTableRequest
{
    public string RoomCode { get; set; }
    public int TableNumber { get; set; }
    public string NickName { get; set; }
    // Only set when rejoining, so a player can only reclaim their own seat
    public Guid? PlayerId { get; set; }
}