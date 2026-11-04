using LYL.Domain.Model;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Services.Mapping;

namespace LYL.Domain.Test;

public class TestPlayerAmountsMapper
{
    [Fact]
    public void AsAmounts_WithoutDossier_ReturnsOnlyNulls()
    {
        Dossier? dossier = null;

        var amounts = dossier.AsAmounts();

        Assert.Null(amounts.MonthlyBudgetPhase1);
        Assert.Null(amounts.BalanceAfterPhase1);
        Assert.Null(amounts.MonthlyBudgetPhase2);
        Assert.Null(amounts.BalanceAfterPhase2);
    }

    [Fact]
    public void AsAmounts_WithPhase1Only_LeavesPhase2Empty()
    {
        var dossier = new Dossier(new Character(), null!)
        {
            firstWorkphaseDossier = new DossierData { RemainingBudget = 905m, Total1 = 12000m }
        };

        var amounts = dossier.AsAmounts();

        Assert.Equal(905m, amounts.MonthlyBudgetPhase1);
        Assert.Equal(12000m, amounts.BalanceAfterPhase1);
        Assert.Null(amounts.MonthlyBudgetPhase2);
        Assert.Null(amounts.BalanceAfterPhase2);
    }

    [Fact]
    public void AsAmounts_WithBothPhases_MapsAllFourAmounts()
    {
        var dossier = new Dossier(new Character(), null!)
        {
            firstWorkphaseDossier = new DossierData { RemainingBudget = 905m, Total1 = 12000m },
            secondWorkphaseDossier = new DossierData { RemainingBudget = 1200m, Total5 = 85000m }
        };

        var amounts = dossier.AsAmounts();

        Assert.Equal(905m, amounts.MonthlyBudgetPhase1);
        Assert.Equal(12000m, amounts.BalanceAfterPhase1);
        Assert.Equal(1200m, amounts.MonthlyBudgetPhase2);
        Assert.Equal(85000m, amounts.BalanceAfterPhase2);
    }
}