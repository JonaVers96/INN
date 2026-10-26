using LYL.Api.Contracts.Game;

namespace LYL.Domain.Services.Interfaces;

public interface IRoomService
{
    Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request);
    Task<JoinTableResponse> JoinTableAsync(JoinTableRequest request);
    Task<LeaveTableResponse> LeaveTableAsync(LeaveTableRequest request);
    Task SetRoomActiveAsync(string roomCode, bool isActive);
    Task<AddTableResponse> AddTableAsync(string roomCode);
    Task<RoomStateResponse?> GetRoomStateAsync(string roomCode);
    Task CloseRoomAsync(string roomCode);
}
