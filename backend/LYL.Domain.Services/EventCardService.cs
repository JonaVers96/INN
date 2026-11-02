using LYL.Api.Contracts.EventCards;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.JsonModel.Event;
using LYL.Domain.Services.Interfaces;
using LYL.Domain.Services.Mapping;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

public class EventCardService(IGameRoomRepository roomRepo, IMemoryAccessService memoryAccessService) : IEventCardService
{
    public Task<Dictionary<int, List<EventCard>>> GetEventCards()
{
    List<EventCard> eventCards = memoryAccessService.GetAllEventCards();

    if (eventCards.Count < 9)
        throw new InvalidOperationException("Er zijn minstens 9 event cards nodig.");

    // Eén keer schudden en 9 unieke kaarten nemen
    var drawnCards = eventCards
        .OrderBy(_ => Random.Shared.Next())
        .Take(9)
        .ToList();

    // Opdelen in 3 groepen van 3 (keys 1, 2, 3)
    Dictionary<int, List<EventCard>> packagesInGroupsToReturn = drawnCards
        .Chunk(3)
        .Select((cards, index) => new { Key = index + 1, Cards = cards.ToList() })
        .ToDictionary(x => x.Key, x => x.Cards);

    return Task.FromResult(packagesInGroupsToReturn);
}

    public async Task SaveEventCardsInput(string roomId, Guid playerId, EventCardSaveRequestContract request)
    {
        var (room, player) = await roomRepo.GetPlayerOrThrowAsync(roomId, playerId);
        if (player.Dossier == null) throw new InvalidOperationException("Het spel is nog niet gestart.");

        player.Dossier.ChoosenEventCards = request.AsModel();
        await roomRepo.UpdateAsync(room);
    }
}
