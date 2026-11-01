using LYL.Api.Contracts.EventCards;
using LYL.Domain.Model.JsonModel.Event;

namespace LYL.Domain.Services.Interfaces;

public interface IEventCardService
{
    Task<Dictionary<int, List<EventCard>>> GetEventCards();
    Task SaveEventCardsInput(string roomId, Guid playerId, EventCardSaveRequestContract request);
}
