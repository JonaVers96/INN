
using LYL.Api.Contracts.FirstWorkPhase;
using LYL.Api.Contracts.StudentPhase;
using LYL.Domain.Model;
using LYL.Api.Contracts.Game;


namespace LYL.Domain.Services.Interfaces;

public interface IPhaseService
{

    //TODO Don't return model here! This was quick and dirty testing!
    Task<Character?> GetPlayerCharacterAsync(string roomCode, string playerId);
    Task<string> CompleteStudentPhaseAsync(string roomCode, Guid playerId, CompleteStudentPhaseRequest request);
    Task<string> CompleteFirstWorkPhaseAsync(string roomCode, Guid playerId);
    Task<DossierCheckResult> CheckPhaseInputsMonthlyAsync(string roomCode, Guid playerId, DossierCheckRequest request); //TODO aanpassen naa request
    Task<DossierCheckResult> CheckPhaseInputsCalcsAsync(string roomCode, Guid playerId, DossierCheckRequest request);
    Task<PlayerAmountsDto> GetPlayerAmountsAsync(string roomCode, Guid playerId);
}