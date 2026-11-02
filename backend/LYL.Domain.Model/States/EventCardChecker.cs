using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.JsonModel.Event;
using static LYL.Domain.Model.States.Amounts;

namespace LYL.Domain.Model.States;

public record EventCheckContext(EventCard Card, EventCardChoices Choice, decimal? Input, DossierData Data, Dossier Dossier);

//Impact = wat bij het totaal van de eventkaarten opgeteld wordt (kost negatief, inkomen positief)
public record EventCheckResult(bool IsCorrect, decimal? Impact);

public enum CarInsuranceTier { None, Basic, Medium, Franchise }


public class EventCardChecker
{
    private readonly IMemoryAccessService _service;
    private readonly List<EventCard> _allEventcards;
    private readonly Dictionary<string, Func<EventCheckContext, EventCheckResult>> _eventChecks;

    public EventCardChecker(IMemoryAccessService service, List<EventCard> allEventcards)
    {
        _service = service;
        _allEventcards = allEventcards;
        _eventChecks = new()
        {
            ["second_car"] = CheckSecondCar,
            ["divorce"] = CheckDivorce,
            ["car_accident_parked"] = CheckCarAccidentParked,
            ["car_accident_hail"] = CheckCarAccidentHail,
            ["car_accident_traffic_jam"] = CheckCarAccidentTrafficJam,
            ["house_on_fire"] = ctx => CostUnlessInsured(ctx, ctx.Data.FireInsuranceCost),
            ["storm_damage_roof"] = ctx => CostUnlessInsured(ctx, ctx.Data.FireInsuranceCost),
            ["storm_damage_trampoline"] = ctx => CostUnlessInsured(ctx, ctx.Data.FamilyInsuranceCost),
            ["hospital_treatment"] = ctx => CostUnlessInsured(ctx, ctx.Data.HospitalisationInsuranceCost),
            ["inheritance"] = CheckConditionIncome,
            ["new_job"] = CheckConditionIncome,
            ["financial_support"] = CheckConditionIncome,
            ["illness_dice"] = CheckConditionCost,
            ["housing_costs_1"] = CheckConditionCost,
            ["housing_costs_2"] = CheckConditionCost,
            ["depression"] = CheckConditionCost,
            ["dismissal"] = CheckDismissal,
            ["promotion"] = CheckPromotion,
            ["holiday"] = CheckHoliday,
            ["leaking_roof"] = CheckLeakingRoof,
            //illness_work: TODO, staat momenteel niet in de db
        };
    }

    public EventCheckResult Check(EventCardChoices choice, decimal? input, DossierData data, Dossier dossier)
    {
        //onbekende kaarten (bv. illness_work) worden als correct beschouwd, zoals voorheen
        if (!_eventChecks.TryGetValue(choice.CardId, out var check)) return NotApplicable;

        var card = GetCard(choice.CardId);
        return check(new EventCheckContext(card, choice, input, data, dossier));
    }

        private EventCard GetCard(string id) =>
        _allEventcards.Find(x => x.Id == id) ?? throw new Exception($"Card not found: {id}");

    private static EventCardOption GetOption(EventCard card, string? conditionId) =>
        card.Options.Find(x => x.ConditionId == conditionId) ?? throw new Exception($"Option {conditionId} not found on card {card.Id}");

    private static CarInsuranceTier GetCarInsuranceTier(decimal? insuranceCost) => insuranceCost switch
    {
        //TODO getinsurances methods waardoor deze flexibel worden ingeladen
        25 or 50 => CarInsuranceTier.Basic,
        55 or 125 => CarInsuranceTier.Medium,
        70 or 350 => CarInsuranceTier.Franchise,
        _ => CarInsuranceTier.None
    };

    private static readonly EventCheckResult NotApplicable = new(true, 0);

    private static EventCheckResult Cost(decimal? expected, decimal? input) => new(expected == AsCost(input), -AsAmount(input));
    private static EventCheckResult Income(decimal? expected, decimal? input) => new(expected == input, input);
    private static EventCheckResult Covered(decimal? input) => new(input is 0 or null, 0);
    private static EventCheckResult CheckSecondCar(EventCheckContext ctx) =>
        Cost(ctx.Card.Options[0].Amount, ctx.Input);

    private static EventCheckResult CheckDivorce(EventCheckContext ctx) =>
        new(ctx.Data.NetSalaryPartner == AsAmount(ctx.Input), -AsAmount(ctx.Input));

    private static EventCheckResult CheckCarAccidentParked(EventCheckContext ctx) =>
        GetCarInsuranceTier(ctx.Data.TransportInsuranceCost) switch
        {
            CarInsuranceTier.Basic => Cost(ctx.Card.Options[0].Amount, ctx.Input),
            CarInsuranceTier.Medium => Cost(ctx.Card.Options[1].Amount, ctx.Input),
            CarInsuranceTier.Franchise => Covered(ctx.Input),
            _ => NotApplicable //geen auto, geen verzekering
        };

    private static EventCheckResult CheckCarAccidentHail(EventCheckContext ctx) =>
        GetCarInsuranceTier(ctx.Data.TransportInsuranceCost) switch
        {
            CarInsuranceTier.Basic => Cost(GetOption(ctx.Card, ctx.Choice.ConditionId).Amount, ctx.Input),
            CarInsuranceTier.Medium or CarInsuranceTier.Franchise => Covered(ctx.Input),
            _ => NotApplicable
        };

    private static EventCheckResult CheckCarAccidentTrafficJam(EventCheckContext ctx) =>
        GetCarInsuranceTier(ctx.Data.TransportInsuranceCost) switch
        {
            CarInsuranceTier.Basic => Cost(ctx.Card.Options[0].Amount, ctx.Input),
            CarInsuranceTier.Medium => Cost(ctx.Card.Options[1].Amount, ctx.Input),
            CarInsuranceTier.Franchise => Covered(ctx.Input),
            //TODO wat dekt de ongevallenverzekering precies?
            _ when ctx.Data.AccidentInsuranceCost == 7 => Covered(ctx.Input),
            _ => NotApplicable
        };

    //house_on_fire, storm_damage_roof, storm_damage_trampoline, hospital_treatment
    private static EventCheckResult CostUnlessInsured(EventCheckContext ctx, decimal? insuranceCost) =>
        insuranceCost > 0 ? Covered(ctx.Input) : Cost(GetOption(ctx.Card, "NO").Amount, ctx.Input);

    //inheritance, new_job, financial_support
    private static EventCheckResult CheckConditionIncome(EventCheckContext ctx) =>
        Income(GetOption(ctx.Card, ctx.Choice.ConditionId).Amount, ctx.Input);

    //illness_dice, housing_costs_1, housing_costs_2, depression
    private static EventCheckResult CheckConditionCost(EventCheckContext ctx) =>
        Cost(GetOption(ctx.Card, ctx.Choice.ConditionId).Amount, ctx.Input);

    private static StudentPhaseOption? GetEducation(Dossier dossier) =>
        dossier.Character.StudentPhase.Options.FirstOrDefault(x => x.Id == "education");

    private static EventCheckResult CheckDismissal(EventCheckContext ctx)
    {
        var education = GetEducation(ctx.Dossier);
        bool ok = education == null || ctx.Card.Options[education.FilledSlots].Amount == AsCost(ctx.Input);
        return new(ok, -AsAmount(ctx.Input));
    }

    private static EventCheckResult CheckPromotion(EventCheckContext ctx)
    {
        var education = GetEducation(ctx.Dossier);
        if (education == null) return NotApplicable;
        if (education.FilledSlots == 0) return Covered(ctx.Input);
        return Income(ctx.Card.Options[education.FilledSlots].Amount, ctx.Input);
    }

    private static EventCheckResult CheckHoliday(EventCheckContext ctx)
    {
        var option = GetOption(ctx.Card, ctx.Choice.ConditionId);
        int amountOfpp = 1;
        if (ctx.Data.NetSalaryPartner > 0) amountOfpp += 1; //TODO beter checken na update sarah partner
        amountOfpp += ctx.Dossier.LivingSituation.Children; //TODO nog aanpassen aan logica sarah
        return Cost(option.Amount * amountOfpp, ctx.Input);
    }

    private EventCheckResult CheckLeakingRoof(EventCheckContext ctx)
    {
        bool ownsHouse = _service.GetHouseId(ctx.Data.HousingCost)?.StartsWith("buy") == true;
        return ownsHouse ? Cost(GetOption(ctx.Card, "owner").Amount, ctx.Input) : Covered(ctx.Input);
    }
}
