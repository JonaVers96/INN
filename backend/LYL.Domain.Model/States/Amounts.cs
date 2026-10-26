namespace LYL.Domain.Model.States;

public static class Amounts
{
    //Kosten worden als negatief bedrag verwacht, ongeacht of de speler een - typt of niet
    public static decimal? AsCost(decimal? value) => value > 0 ? -value : value;

    //Absolute waarde van de ingevulde impact
    public static decimal? AsAmount(decimal? value) => value < 0 ? -value : value;
}