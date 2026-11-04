using LYL.Api.Contracts.Game;
using LYL.Domain.Model.Dossiers;

namespace LYL.Domain.Services.Mapping;

public static class PlayerAmountsMapper
{
    //welk dossierveld hoort bij welk dashboard getal, de ? zorgen ervoor dat voor de start of fase 2 null wordt gegeven ipv crash
    public static PlayerAmountsDto AsAmounts(this Dossier? dossier)
    {
        var phase1 = dossier?.firstWorkphaseDossier;
        var phase2 = dossier?.secondWorkphaseDossier;

        return new PlayerAmountsDto
        {
            MonthlyBudgetPhase1 = phase1?.RemainingBudget,
            BalanceAfterPhase1 = phase1?.Total1,
            MonthlyBudgetPhase2 = phase2?.RemainingBudget,
            BalanceAfterPhase2 = phase2?.Total5
        };
    }
}