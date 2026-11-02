using LYL.Domain.Model;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Services;
using LYL.Domain.Services.Interfaces;
using LYL.Persistence.Interfaces;
using Moq;

namespace LYL.Domain.Test;

public class TestAddTable
{
    private const string RoomCode = "123456";

    private readonly Mock<IGameRoomRepository> roomRepoMock = new();
    private readonly Mock<IJsonSerializerService> jsonSerializerMock = new();

    private RoomService CreateService()
    {
        jsonSerializerMock
            .Setup(j => j.getCharacters())
            .Returns(() => new List<Character> { new Character(), new Character() });

        return new RoomService(
            roomRepoMock.Object,
            new Mock<ISupervisorRepository>().Object,
            jsonSerializerMock.Object,
            new Mock<IRandomIntProvider>().Object);
    }

    private GameRoom RoomInRepo(bool isActive)
    {
        var room = new GameRoom(new Supervisor(), RoomCode) { IsActive = isActive };
        roomRepoMock.Setup(r => r.GetByRoomCodeAsync(RoomCode)).ReturnsAsync(room);
        return room;
    }

    [Fact]
    public async Task AddTableAsync_GivesNextTableNumber()
    {
        RoomInRepo(isActive: false);

        var response = await CreateService().AddTableAsync(RoomCode);

        Assert.Equal(GameRoom.DefaultTableCount + 1, response.TableNumber);
        Assert.Equal(GameRoom.DefaultTableCount + 1, response.TableCount);
    }

    [Fact]
    public async Task AddTableAsync_WhenGameIsActive_GivesNewTableCharacters()
    {
        var room = RoomInRepo(isActive: true);

        await CreateService().AddTableAsync(RoomCode);

        Assert.NotEmpty(room.Tables.Last().Characters);
    }

    [Fact]
    public async Task AddTableAsync_WhenGameNotStarted_LeavesCharactersForGameStart()
    {
        var room = RoomInRepo(isActive: false);

        await CreateService().AddTableAsync(RoomCode);

        Assert.Empty(room.Tables.Last().Characters);
    }

    [Fact]
    public async Task AddTableAsync_UnknownRoom_Throws()
    {
        roomRepoMock.Setup(r => r.GetByRoomCodeAsync("000000")).ReturnsAsync((GameRoom?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().AddTableAsync("000000"));
    }

    [Fact]
    public void AddTable_Concurrently_NeverReusesATableNumber()
    {
        var room = new GameRoom(new Supervisor(), RoomCode);

        Parallel.For(0, 20, _ => room.AddTable(new List<Character>()));

        Assert.Equal(GameRoom.DefaultTableCount + 20, room.Tables.Count);
        Assert.Equal(room.Tables.Count, room.Tables.Select(t => t.TableNumber).Distinct().Count());
    }
}