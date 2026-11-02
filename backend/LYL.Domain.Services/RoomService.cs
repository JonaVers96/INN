using LYL.Api.Contracts.Game;
using LYL.Domain.Model;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Services.Interfaces;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

public class RoomService(
    IGameRoomRepository roomRepo,
    ISupervisorRepository supervisorRepo,
    IJsonSerializerService jsonSerializer,
    IRandomIntProvider randomIntProvider) : IRoomService
{
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
            RoomCode = room.RoomCode,
            TableCount = room.Tables.Count
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
            if (room.IsActive && !table.TryAssignCharacterToPlayer(activePlayer, jsonSerializer.GetPartners(), randomIntProvider))
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
        var room = await roomRepo.GetRoomOrThrowAsync(roomCode);

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
                table.AssignCharactersToPlayers(partners, randomIntProvider);
                foreach (var player in table.Players)
                {
                    player.CurrentPhase = "character-discovery";
                }
            }
        }

        room.IsActive = isActive;
        await roomRepo.UpdateAsync(room);
    }

    public async Task<AddTableResponse> AddTableAsync(string roomCode)
    {
        var room = await roomRepo.GetRoomOrThrowAsync(roomCode);


        // Characters worden normaal pas bij de spelstart uitgedeeld (SetRoomActiveAsync).
        // Is het spel al bezig, dan moet de nieuwe tafel ze nu meteen krijgen: anders vindt een
        // laatkomer geen vrij character ("No characters left at this table").
        // Per tafel een eigen kopie opvragen, zodat tafels geen Character-objecten delen.
        var characters = room.IsActive ? jsonSerializer.getCharacters() : new List<Character>();

        var table = room.AddTable(characters);

        await roomRepo.UpdateAsync(room);

        return new AddTableResponse
        {
            TableNumber = table.TableNumber,
            TableCount = room.Tables.Count
        };
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
            Language = room.Host.Language,
            TableCount = room.Tables.Count,
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
        await roomRepo.GetRoomOrThrowAsync(roomCode);
        await roomRepo.RemoveAsync(roomCode);
    }

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

}
