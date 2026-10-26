using LYL.Api.Contracts.EventCards;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.JsonModel.Event;
using LYL.Domain.Services.Interfaces;
using LYL.Domain.Services.Mapping;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

public class EventCardService(IGameRoomRepository roomRepo, IMemoryAccessService memoryAccessService) : IEventCardService
{
    private static readonly Dictionary<int, List<string>> Packages = new()
    {
        { 1, ["divorce", "promotion", "car_accident_parked"] },
        //{ 2, ["inheritance", "house_on_fire", "dismissal"] },
        //{ 3, ["financial_support", "car_accident_hail", "storm_damage_trampoline"] }, //TODO hail vervangen met illness work
        { 4, ["new_job", "depression", "leaking_roof"] },
        // { 5, ["holiday", "hospital_treatment", "car_accident_hail"] },
        // { 6, ["second_car", "housing_costs_1", "storm_damage_roof"] },
        { 7, ["car_accident_traffic_jam", "illness_dice", "housing_costs_2"] }
    };

          public Task<Dictionary<int, List<EventCard>>> GetEventCards()
    {
        // Haal alle kaarten op (zorg dat de Id property in deze lijst goed gevuld is vanuit de JSON keys!)
        List<EventCard> eventCards = memoryAccessService.GetAllEventCards();

        Dictionary<int, List<EventCard>> packagesInGroupsToReturn = new();
        var randomKeys = Packages.Keys.OrderBy(x => Guid.NewGuid()).Take(3).ToList();

        foreach (var key in randomKeys)
        {
            var selectedPackageStrings = Packages[key];
            var cardsForPackage = new List<EventCard>();

            foreach (var stringId in selectedPackageStrings)
            {
                var card = eventCards.FirstOrDefault(x => x.Id == stringId);

                if (card != null)
                {
                    cardsForPackage.Add(card);
                }
            }

            packagesInGroupsToReturn.Add(key, cardsForPackage);
        }

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
