using System.Diagnostics.CodeAnalysis;
using LYL.Api.Contracts.EventCards;
using LYL.Api.Contracts.FirstWorkPhase;
using LYL.Api.Contracts.Game;
using LYL.Api.Contracts.StudentPhase;
using LYL.Domain.Model;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.JsonModel.Event;
using LYL.Domain.Model.States;
using LYL.Domain.Services.Interfaces;
using LYL.Domain.Services.Mapping;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

public class GameSessionService : IGameSessionService
{
    private readonly IGameRoomRepository roomRepo;
    private readonly ISupervisorRepository supervisorRepo;
    private readonly IJsonSerializerService jsonSerializer;
    private readonly IMemoryAccessService memoryAccessService;
    private readonly IInvestmentCalculator investmentCalculator;

     private readonly IRandomIntProvider _randomIntProvider;

    public GameSessionService(IGameRoomRepository roomRepo, ISupervisorRepository supervisorRepo,
        IJsonSerializerService jsonSerializer, IMemoryAccessService memoryService, IInvestmentCalculator investmentCalculator, IRandomIntProvider random)
    {
        this.roomRepo = roomRepo;
        this.supervisorRepo = supervisorRepo;
        this.jsonSerializer = jsonSerializer;
        this.memoryAccessService = memoryService;
        this.investmentCalculator =  investmentCalculator;
        this._randomIntProvider = random;  
    }

    public async Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var supervisor = await supervisorRepo.GetByIdAsync(request.SupervisorId);
        //TODO: Handle this cleaner
        if (supervisor == null)
        {
            throw new ArgumentException("Docent niet gevonden.");
        }

        // Spelgegevens eerst inlezen: zonder data kan het spel later niet starten
        if (!jsonSerializer.SerializeAllJsonFiles())
        {
            throw new InvalidOperationException("Spelgegevens konden niet ingelezen worden.");
        }

        var room = new GameRoom(supervisor, await GenerateUniqueRoomCodeAsync());

        await roomRepo.AddAsync(room);

        return new CreateRoomResponse
        {
            RoomCode = room.RoomCode
        };
    }

    public async Task<JoinTableResponse> JoinTableAsync(JoinTableRequest request)
    {
        var room = await roomRepo.GetByRoomCodeAsync(request.RoomCode);
        if (room == null) throw new ArgumentException("Room does not exist.");

        var table = room.Tables.FirstOrDefault(t => t.TableNumber == request.TableNumber);
        if (table == null) throw new ArgumentException("Table does not exist.");

        // Opnieuw verbinden kan enkel met de eigen PlayerId, niet louter op nickname
        var existingPlayer = request.PlayerId is Guid playerId
            ? table.Players.FirstOrDefault(p => p.PlayerId == playerId)
            : null;

        Player activePlayer;

        if (existingPlayer != null)
        {
            activePlayer = existingPlayer;
        }
        else
        {
            if (table.Players.Any(p => p.Nickname == request.NickName))
            {
                throw new InvalidOperationException("This nickname is already taken at this table.");
            }

            activePlayer = new Player
            {
                PlayerId = Guid.NewGuid(),
                Nickname = request.NickName,
                CurrentPhase = room.IsActive ? "character-discovery" : "waiting"
            };

            if (!table.TryAddPlayer(activePlayer))
            {
                throw new InvalidOperationException("This table is full.");
            }

            // Laatkomer: spel is al gestart, dus meteen een vrij character toewijzen
            if (room.IsActive && !table.TryAssignCharacterToPlayer(activePlayer, jsonSerializer.GetPartners(), _randomIntProvider))
            {
                table.RemovePlayer(activePlayer);
                throw new InvalidOperationException("No characters left at this table.");
            }
        }

        var response = new JoinTableResponse
        {
            NickName = activePlayer.Nickname,
            TableNumber = request.TableNumber,
            PlayerId = activePlayer.PlayerId,
            IsGameAlreadyStarted = room.IsActive,
            CurrentPhase = activePlayer.CurrentPhase
        };

        await roomRepo.UpdateAsync(room);

        return response;
    }

    public async Task<LeaveTableResponse> LeaveTableAsync(LeaveTableRequest request)
    {
        var room = await roomRepo.GetByRoomCodeAsync(request.RoomCode);
        if (room == null) throw new ArgumentException("Room does not exist.");

        var table = room.Tables.FirstOrDefault(t => t.Players.Any(p => p.PlayerId == request.PlayerId));
        if (table == null) throw new ArgumentException("Table does not exist.");

        //If players are not inside the DB we can use linq to find them?
        var playerToRemove = table.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (playerToRemove == null)
        {
            throw new ArgumentException("Player zit niet aan deze tafel.");
        }

        table.RemovePlayer(playerToRemove);

        await roomRepo.UpdateAsync(room);

        var response = new LeaveTableResponse
        {
            TableNumber = table.TableNumber,
            PlayerId = playerToRemove.PlayerId
        };
        return response;
    }

    public async Task SetRoomActiveAsync(string roomCode, bool isActive)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");

        // Al gestart: niet opnieuw characters uitdelen (zou alle dossiers wissen)
        if (isActive && room.IsActive) return;

        if (isActive)
        {
            // IsActive pas na het toewijzen zetten, zodat een fout hier de room niet half gestart achterlaat
            List<Partner> partners = jsonSerializer.GetPartners();
            foreach (var table in room.Tables)
            {
                // Per tafel opvragen: getCharacters() geeft telkens nieuwe kopieën, zodat tafels geen Character-objecten delen
                table.Characters = jsonSerializer.getCharacters();
                table.AssignCharactersToPlayers(partners, _randomIntProvider);
                foreach (var player in table.Players)
                {
                    player.CurrentPhase = "character-discovery";
                }
            }
        }

        room.IsActive = isActive;
        await roomRepo.UpdateAsync(room);
    }

    //TODO Use mapping? 
    public async Task<RoomStateResponse?> GetRoomStateAsync(string roomCode)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);

        if (room == null) return null;

        var response = new RoomStateResponse
        {
            RoomCode = room.RoomCode,
            IsActive = room.IsActive,
            Players = new List<PlayerStateDto>()
        };

        foreach (var table in room.Tables)
        {
            foreach (var player in table.Players)
            {
                response.Players.Add(new PlayerStateDto
                {
                    PlayerId = player.PlayerId.ToString(),
                    NickName = player.Nickname,
                    TableNumber = table.TableNumber,
                    IsOffline = false,
                    CurrentPhase = player.CurrentPhase
                });
            }
        }

        return response;
    }

    public async Task CloseRoomAsync(string roomCode)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");
        await roomRepo.RemoveAsync(roomCode);
    }

    public Task<Dictionary<int, List<EventCard>>> GetEventCards()
    {
        // Haal alle kaarten op (zorg dat de Id property in deze lijst goed gevuld is vanuit de JSON keys!)
        List<EventCard> eventCards = memoryAccessService.GetAllEventCards();

        Dictionary<int, List<string>> packages = new()
        {
            { 1, ["divorce", "promotion", "car_accident_parked"] },
            //{ 2, ["inheritance", "house_on_fire", "dismissal"] },
            //{ 3, ["financial_support", "car_accident_hail", "storm_damage_trampoline"] }, //TODO hail vervangen met illness work
            { 4, ["new_job", "depression", "leaking_roof"] },
           // { 5, ["holiday", "hospital_treatment", "car_accident_hail"] },
           // { 6, ["second_car", "housing_costs_1", "storm_damage_roof"] },
            { 7, ["car_accident_traffic_jam", "illness_dice", "housing_costs_2"] }
        };

        Dictionary<int, List<EventCard>> packagesInGroupsToReturn = new();
        var randomKeys = packages.Keys.OrderBy(x => Guid.NewGuid()).Take(3).ToList();

        foreach (var key in randomKeys)
        {
            var selectedPackageStrings = packages[key];
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

    public async Task SaveEventCardsInput(string roomId, string playerGuid, EventCardSaveRequestContract request)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomId);
        if (room == null) throw new Exception("Room not found");

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId.ToString() == playerGuid);
        if (player == null) throw new Exception("Player not found");
        if (player.Dossier == null) throw new InvalidOperationException("Het spel is nog niet gestart.");

        try
        {
            var choosenEventsAsModel = request.AsModel();
            player.Dossier.ChoosenEventCards = choosenEventsAsModel; 
            await roomRepo.UpdateAsync(room);
           
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    //Helper, we can extract this into a utils folder and inject via interface
    private async Task<string> GenerateUniqueRoomCodeAsync()
    {
        // Opnieuw proberen tot de code niet in gebruik is, anders weigert AddAsync de room stilletjes
        string code;
        do
        {
            code = Random.Shared.Next(100000, 1000000).ToString();
        } while (await roomRepo.GetByRoomCodeAsync(code) != null);

        return code;
    }

    //TODO Look at interface!!
    public async Task<Character?> GetPlayerCharacterAsync(string roomCode, string playerId)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) return null;

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId.ToString() == playerId);

        if (player?.Character == null) return null;

        if (player.Character.ChosenJob == null)
        {
            player.Character.AssignRandomJob(_randomIntProvider);

            await roomRepo.UpdateAsync(room);
        }

        return player.Character;
    }

    public async Task<string> CompleteStudentPhaseAsync(string roomCode, Guid playerId,
        CompleteStudentPhaseRequest request)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId == playerId);

        if (player?.Character?.StudentPhase == null)
            throw new ArgumentException("Player or Character Data not found");

        // De studentenfase mag maar één keer afgerond worden
        if (player.CurrentPhase is not ("character-discovery" or "student-phase"))
        {
            throw new InvalidOperationException("De studentenfase is al afgerond.");
        }

        // Eerst alles valideren, pas daarna wegschrijven (anders blijft een half aangepaste staat achter)
        int newlyAddedPoints = 0;
        foreach (var selection in request.Selections)
        {
            var existingOption = player.Character.StudentPhase.Options.FirstOrDefault(o => o.Id == selection.Key);
            if (existingOption != null)
            {
                if (selection.Value < existingOption.FilledSlots || selection.Value > existingOption.MaxSlots)
                {
                    throw new ArgumentException($"Cheat gedetecteerd: Ongeldig aantal slots voor {existingOption.TitleKey}");
                }

                newlyAddedPoints += (selection.Value - existingOption.FilledSlots);
            }
        }

        if (newlyAddedPoints > player.Character.AllocatablePoints)
        {
            throw new ArgumentException("Je hebt te veel punten proberen te verdelen!");
        }

        foreach (var selection in request.Selections)
        {
            var optionToUpdate = player.Character.StudentPhase.Options.FirstOrDefault(o => o.Id == selection.Key);

            if (optionToUpdate != null)
            {
                optionToUpdate.FilledSlots = selection.Value;
            }
        }

        player.CurrentPhase = "first-work-phase";
        await roomRepo.UpdateAsync(room);

        return player.CurrentPhase;
    }

    public async Task<string> CompleteFirstWorkPhaseAsync(string roomCode, Guid playerId)
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId == playerId);

        if (player == null) throw new ArgumentException("Player not found");

        // Dubbele aanroep: al afgerond
        if (player.CurrentPhase == "second-work-phase") return player.CurrentPhase;

        // Enkel afronden als de berekeningen goedgekeurd zijn (NextPhase zet dan SecondWorkphaseState)
        if (player.CurrentPhase != "first-work-phase" || player.DossierState is not SecondWorkphaseState)
        {
            throw new InvalidOperationException("De eerste werkfase is nog niet correct afgerond.");
        }

        player.CurrentPhase = "second-work-phase";
        await roomRepo.UpdateAsync(room);

        return player.CurrentPhase;
    }
    

    public async Task<bool> CheckPhaseInputsMonthlyAsync(string roomCode, Guid playerId, DossierCheckRequest request) 
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) throw new ArgumentException("Player not found");
        if (player.Dossier == null) throw new InvalidOperationException("Het spel is nog niet gestart.");

        player.DossierState ??= new FirstWorkphaseState(player.Dossier, memoryAccessService);
        var dossierData = new DossierData();
        if (player.DossierState is FirstWorkphaseState)  dossierData = request.AsModelPhase1();
        if (player.DossierState is SecondWorkphaseState) dossierData = request.AsModelPhase2();
        
        return player.CheckMontlhy(dossierData);
        
    }
    
    public async Task<bool> CheckPhaseInputsCalcsAsync(string roomCode, Guid playerId, DossierCheckRequest request) 
    {
        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) throw new ArgumentException("Room not found");

        var player = room.Tables
            .SelectMany(t => t.Players)
            .FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) throw new ArgumentException("Player not found");
        if (player.Dossier == null) throw new InvalidOperationException("Het spel is nog niet gestart.");

        // State eerst zetten, anders blijft dossierData leeg bij de eerste aanroep
        player.DossierState ??= new FirstWorkphaseState(player.Dossier, memoryAccessService);
        var dossierData = new DossierData();
        if (player.DossierState is FirstWorkphaseState)  dossierData = request.AsModelPhase1();
        if (player.DossierState is SecondWorkphaseState) dossierData = request.AsModelPhase2();

        //TODO: just a quick way to go to nextphase rn
        var result = player.CheckCalculations(dossierData, investmentCalculator);
        if (result && player.DossierState is FirstWorkphaseState) player.DossierState.NextPhase(player);
        return result;
        
    }
}