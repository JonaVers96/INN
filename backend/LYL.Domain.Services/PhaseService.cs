using LYL.Api.Contracts.FirstWorkPhase;
using LYL.Api.Contracts.StudentPhase;
using LYL.Domain.Model;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.States;
using LYL.Domain.Services.Interfaces;
using LYL.Domain.Services.Mapping;
using LYL.Persistence.Interfaces;

namespace LYL.Domain.Services;

public class PhaseService : IPhaseService
{
    private readonly IGameRoomRepository roomRepo;
    private readonly IMemoryAccessService memoryAccessService;
    private readonly IInvestmentCalculator investmentCalculator;
     private readonly IRandomIntProvider randomIntProvider;

    public PhaseService(IGameRoomRepository roomRepo, IMemoryAccessService memoryService, IInvestmentCalculator investmentCalculator, IRandomIntProvider random)
    {
        this.roomRepo = roomRepo;
        this.memoryAccessService = memoryService;
        this.investmentCalculator =  investmentCalculator;
        this.randomIntProvider = random;  
    }

    private DossierData PrepareDossierData(Player player, DossierCheckRequest request)
    {
        if (player.Dossier == null) throw new InvalidOperationException("Het spel is nog niet gestart.");

        // State eerst zetten, anders blijft dossierData leeg bij de eerste aanroep
        player.DossierState ??= new FirstWorkphaseState(player.Dossier, memoryAccessService);

        return player.DossierState switch
        {
            FirstWorkphaseState  => request.AsModelPhase1(),
            SecondWorkphaseState => request.AsModelPhase2(),
            _                    => new DossierData()
        };
    }

    private static void ValidateStudentSelections(Character character, CompleteStudentPhaseRequest request)
    {
        int newlyAddedPoints = 0;
        foreach (var selection in request.Selections)
        {
            var existingOption = character.StudentPhase.Options.FirstOrDefault(o => o.Id == selection.Key);
            if (existingOption != null)
            {
                if (selection.Value < existingOption.FilledSlots || selection.Value > existingOption.MaxSlots)
                {
                    throw new ArgumentException($"Cheat gedetecteerd: Ongeldig aantal slots voor {existingOption.TitleKey}");
                }

                newlyAddedPoints += (selection.Value - existingOption.FilledSlots);
            }
        }

        if (newlyAddedPoints > character.AllocatablePoints)
        {
            throw new ArgumentException("Je hebt te veel punten proberen te verdelen!");
        }
    }

    private static void ApplyStudentSelections(Character character, CompleteStudentPhaseRequest request)
    {
        foreach (var selection in request.Selections)
        {
            var optionToUpdate = character.StudentPhase.Options.FirstOrDefault(o => o.Id == selection.Key);

            if (optionToUpdate != null)
            {
                optionToUpdate.FilledSlots = selection.Value;
            }
        }
    }



    public async Task<Character?> GetPlayerCharacterAsync(string roomCode, string playerId)
    {
        if (!Guid.TryParse(playerId, out var playerGuid)) return null;

        var room = await roomRepo.GetByRoomCodeAsync(roomCode);
        if (room == null) return null;

        var player = room.FindPlayer(playerGuid);


        if (player?.Character == null) return null;

        if (player.Character.ChosenJob == null)
        {
            player.Character.AssignRandomJob(randomIntProvider);

            await roomRepo.UpdateAsync(room);
        }

        return player.Character;
    }

    public async Task<string> CompleteStudentPhaseAsync(string roomCode, Guid playerId,
        CompleteStudentPhaseRequest request)
    {
        var (room, player) = await roomRepo.GetPlayerOrThrowAsync(roomCode, playerId);

        if (player.Character?.StudentPhase == null)
            throw new ArgumentException("Player or Character Data not found");

        // De studentenfase mag maar één keer afgerond worden
        if (player.CurrentPhase is not ("character-discovery" or "student-phase"))
        {
            throw new InvalidOperationException("De studentenfase is al afgerond.");
        }

        // Eerst alles valideren, pas daarna wegschrijven (anders blijft een half aangepaste staat achter)
        ValidateStudentSelections(player.Character, request);
        ApplyStudentSelections(player.Character, request);

        player.CurrentPhase = "first-work-phase";
        await roomRepo.UpdateAsync(room);

        return player.CurrentPhase;
    }

    public async Task<string> CompleteFirstWorkPhaseAsync(string roomCode, Guid playerId)
    {
        var (room, player) = await roomRepo.GetPlayerOrThrowAsync(roomCode, playerId);

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
    

    public async Task<DossierCheckResult> CheckPhaseInputsMonthlyAsync(string roomCode, Guid playerId, DossierCheckRequest request) 
    {
        var (room, player) = await roomRepo.GetPlayerOrThrowAsync(roomCode, playerId);
        var dossierData = PrepareDossierData(player, request);

        if (room.Host.SkipChecks)
        {
            player.DossierState.AcceptWithoutCheck(dossierData);
            return new DossierCheckResult(true, []);
        }
        
        return player.CheckMontlhy(dossierData);
        
    }
    
    public async Task<DossierCheckResult> CheckPhaseInputsCalcsAsync(string roomCode, Guid playerId, DossierCheckRequest request) 
    {
        var (room, player) = await roomRepo.GetPlayerOrThrowAsync(roomCode, playerId);
        var dossierData = PrepareDossierData(player, request);

        //TODO: just a quick way to go to nextphase rn
        if (room.Host.SkipChecks)
        {
            player.DossierState.AcceptWithoutCheck(dossierData);
            if (player.DossierState is FirstWorkphaseState) player.DossierState.NextPhase(player);
            return new DossierCheckResult(true, []);
        }

        var result = player.CheckCalculations(dossierData, investmentCalculator);
        if (result.IsValid && player.DossierState is FirstWorkphaseState) player.DossierState.NextPhase(player);
        return result;
        
    }
}