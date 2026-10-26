using LYL.Domain.Model;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

internal static class RoomLookupExtensions
{
    public static async Task<GameRoom> GetRoomOrThrowAsync(this IGameRoomRepository repo, string roomCode)
    {
        var room = await repo.GetByRoomCodeAsync(roomCode);
        return room ?? throw new ArgumentException("Room not found");
    }

    public static async Task<(GameRoom room, Player player)> GetPlayerOrThrowAsync(
        this IGameRoomRepository repo, string roomCode, Guid playerId)
    {
        var room = await repo.GetRoomOrThrowAsync(roomCode);
        var player = room.FindPlayer(playerId) ?? throw new ArgumentException("Player not found");
        return (room, player);
    }
}
