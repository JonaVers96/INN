namespace LYL.Api.Contracts.Game;

public class PlayerAmountsDto
{
        //decimal? omdat nog niet ingevuld onderscheiden wordt van ingevuld met '0'

    public decimal? MonthlyBudgetPhase1 { get; set; }
    public decimal? BalanceAfterPhase1 { get; set; }
    public decimal? MonthlyBudgetPhase2 { get; set; }
    public decimal? BalanceAfterPhase2 { get; set; }

}