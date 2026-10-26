using LYL.Domain.Model;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.States;
using Moq;
using LYL.Domain.Model.JsonModel.Event;

namespace LYL.Domain.Test;

public class TestGameStateLogic
{
    private const string HouseId = "rent_1";

    private readonly Mock<IMemoryAccessService> _memory = new();

    // ---------- helpers ----------

    private static Dossier CreateDossier(int startNet = 1500, int avgNet = 2000, decimal partnerWage = 0m,
        decimal partnerSavings = 0m, int children = 0)
    {
        var random = new Mock<IRandomIntProvider>();
        random.Setup(r => r.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(0);

        var partner = new Partner { Id = "p1", Wage = partnerWage, Savings = partnerSavings };
        var livingSituation = new LivingSituation([partner], random.Object)
        {
            Partner = partner,
            Children = children
        };

        var character = new Character
        {
            ChosenJob = new Job { StartNet = startNet, AvgNet = avgNet },
            Profile = new CharacterProfile { ExperienceInMonths = 6, ParentalSupportKey = "/" },
            StudentPhase = new CharacterStudentPhase
            {
                Options =
                [
                    new StudentPhaseOption { Id = "job_save", FilledSlots = 2, SlotValues = [100m, 200m, 300m] }
                ]
            }
        };

        return new Dossier(character, livingSituation);
    }

    /// <summary>Mock setup waarbij alle fase 1 waarden van <see cref="ValidPhase1Data"/> kloppen.</summary>
    private void SetupValidPhase1(string? houseId = HouseId)
    {
        _memory.Setup(m => m.GetHouseId(450m)).Returns(houseId!);
        _memory.Setup(m => m.CheckLivingExpense(300m, It.IsAny<string>())).Returns(true);
        _memory.Setup(m => m.GetMonthlyCarCost(150m)).Returns((50m, false));
    }

    private static DossierData ValidPhase1Data() => new()
    {
        NetSalary = 1500m,
        HousingCost = 450m,
        OtherLivingCost = 300m,
        TransportPurchaseCost = 150m,
        TransportMonthlyCost = 50m,
        RemainingBudget = 550m // 1500 - 450 - 300 - 50 - 150
    };

    private void SetupValidPhase2()
    {
        _memory.Setup(m => m.GetHouseId(700m)).Returns(HouseId);
        _memory.Setup(m => m.GetHouseInfoRent(HouseId)).Returns((4, false));
        _memory.Setup(m => m.CheckLivingExpense(400m, HouseId, 1, true)).Returns(true);
        _memory.Setup(m => m.GetCarInfo(100m, 3)).Returns((200m, false, 5));
    }

    // 1 kind + partner => 3 personen
    private static DossierData ValidPhase2Data() => new()
    {
        NetSalary = 2000m,
        NetSalaryPartner = 1000m,
        HousingCost = 700m,
        OtherLivingCost = 400m,
        TransportPurchaseCost = 100m,
        TransportMonthlyCost = 200m,
        RemainingBudget = 1600m // 3000 - 700 - 400 - 200 - 100
    };

    private FirstWorkphaseState CreateFirstState(Dossier? dossier = null)
        => new(dossier ?? CreateDossier(), _memory.Object);

    private SecondWorkphaseState CreateSecondState(Dossier? dossier = null)
        => new(dossier ?? CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1), _memory.Object);

    private static Player CreatePlayer(DossierState state, Dossier dossier)
        => new() { Dossier = dossier, DossierState = state };

    // =====================================================================
    // CheckBalanceMonth - fase 1
    // =====================================================================

    [Fact]
    public void GivenValidPhase1Data_WhenCheckingBalanceMonth_ThenResultIsValid()
    {
        SetupValidPhase1();
        var state = CreateFirstState();

        var result = state.CheckBalanceMonth(ValidPhase1Data());

        Assert.True(result.IsValid);
        Assert.Empty(result.InvalidFields);
    }

    [Fact]
    public void GivenWrongNetSalary_WhenCheckingBalanceMonthPhase1_ThenNetSalaryIsInvalid()
    {
        SetupValidPhase1();
        var data = ValidPhase1Data();
        data.NetSalary = 1499m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.False(result.IsValid);
        Assert.Equal(new[] { "net_salary_phase1" }, result.InvalidFields);
    }

    [Fact]
    public void GivenNoChosenJob_WhenCheckingBalanceMonthPhase1_ThenNetSalaryIsInvalid()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        dossier.Character.ChosenJob = null;

        var result = CreateFirstState(dossier).CheckBalanceMonth(ValidPhase1Data());

        Assert.Contains("net_salary_phase1", result.InvalidFields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenUnknownHousingCost_WhenCheckingBalanceMonthPhase1_ThenMonthlyRentIsInvalid(string? houseId)
    {
        SetupValidPhase1(houseId);

        var result = CreateFirstState().CheckBalanceMonth(ValidPhase1Data());

        Assert.False(result.IsValid);
        Assert.Contains("monthly_rent_phase1", result.InvalidFields);
    }

    [Fact]
    public void GivenSocialHousingAndIncomeBelowMax_WhenCheckingBalanceMonthPhase1_ThenResultIsValid()
    {
        SetupValidPhase1("rent_social");
        _memory.Setup(m => m.GetSocialHousingCondition()).Returns((100m, 1600m)); // StartNet 1500 < 1600

        var result = CreateFirstState().CheckBalanceMonth(ValidPhase1Data());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenSocialHousingAndIncomeAboveMax_WhenCheckingBalanceMonthPhase1_ThenNetSalaryIsInvalid()
    {
        SetupValidPhase1("rent_social");
        _memory.Setup(m => m.GetSocialHousingCondition()).Returns((100m, 1500m)); // StartNet 1500 is niet < 1500

        var result = CreateFirstState().CheckBalanceMonth(ValidPhase1Data());

        Assert.False(result.IsValid);
        Assert.Contains("net_salary_phase1", result.InvalidFields);
    }

    [Fact]
    public void GivenNonSocialHousing_WhenCheckingBalanceMonthPhase1_ThenSocialConditionIsNotChecked()
    {
        SetupValidPhase1();

        CreateFirstState().CheckBalanceMonth(ValidPhase1Data());

        _memory.Verify(m => m.GetSocialHousingCondition(), Times.Never);
    }

    [Fact]
    public void GivenWrongLivingCost_WhenCheckingBalanceMonthPhase1_ThenLivingCostsIsInvalid()
    {
        SetupValidPhase1();
        var data = ValidPhase1Data();
        data.OtherLivingCost = 999m; // mock kent enkel 300

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.Equal(new[] { "living_costs_phase1" }, result.InvalidFields);
    }

    [Fact]
    public void GivenWrongMonthlyTransportCost_WhenCheckingBalanceMonthPhase1_ThenPurchaseMonthlyIsInvalid()
    {
        SetupValidPhase1();
        var data = ValidPhase1Data();
        data.TransportMonthlyCost = 51m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.Equal(new[] { "purchase_monthly_phase1" }, result.InvalidFields);
    }

    [Fact]
    public void GivenValidOptionalInsurances_WhenCheckingBalanceMonthPhase1_ThenResultIsValid()
    {
        SetupValidPhase1();
        _memory.Setup(m => m.CheckTransportInsurance(25m, false)).Returns(true);
        _memory.Setup(m => m.CheckFireInsuranceCost(10m, HouseId)).Returns(true);
        _memory.Setup(m => m.CheckFamilyInsurance(15m)).Returns(true);
        _memory.Setup(m => m.GetHospitalisationInsurance()).Returns((20m, 10m));
        _memory.Setup(m => m.CheckAccidentInsurance(5m)).Returns(true);

        var data = ValidPhase1Data();
        data.TransportInsuranceCost = 25m;
        data.FireInsuranceCost = 10m;
        data.FamilyInsuranceCost = 15m;
        data.HospitalisationInsuranceCost = 20m; // fase 1: enkel 1 volwassene
        data.AccidentInsuranceCost = 5m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenInvalidOptionalInsurances_WhenCheckingBalanceMonthPhase1_ThenEachInsuranceIsReported()
    {
        SetupValidPhase1();
        _memory.Setup(m => m.GetHospitalisationInsurance()).Returns((20m, 10m));
        // alle overige insurance checks geven standaard false terug

        var data = ValidPhase1Data();
        data.TransportInsuranceCost = 25m;
        data.FireInsuranceCost = 10m;
        data.FamilyInsuranceCost = 15m;
        data.HospitalisationInsuranceCost = 99m;
        data.AccidentInsuranceCost = 5m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.False(result.IsValid);
        Assert.Equivalent(new[]
        {
            "car_insurance_phase1", "fire_insurance_phase1", "family_insurance_phase1",
            "hospital_insurance_phase1", "accident_insurance_phase1"
        }, result.InvalidFields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void GivenEmptyOptionalInsurances_WhenCheckingBalanceMonthPhase1_ThenInsurancesAreNotChecked(int? cost)
    {
        SetupValidPhase1();
        var data = ValidPhase1Data();
        data.TransportInsuranceCost = cost;
        data.FireInsuranceCost = cost;
        data.FamilyInsuranceCost = cost;
        data.HospitalisationInsuranceCost = cost;
        data.AccidentInsuranceCost = cost;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.True(result.IsValid);
        _memory.Verify(m => m.CheckTransportInsurance(It.IsAny<decimal?>(), It.IsAny<bool>()), Times.Never);
        _memory.Verify(m => m.CheckFireInsuranceCost(It.IsAny<decimal?>(), It.IsAny<string>()), Times.Never);
        _memory.Verify(m => m.CheckFamilyInsurance(It.IsAny<decimal?>()), Times.Never);
        _memory.Verify(m => m.GetHospitalisationInsurance(), Times.Never);
        _memory.Verify(m => m.CheckAccidentInsurance(It.IsAny<decimal?>()), Times.Never);
    }

    [Fact]
    public void GivenSportCar_WhenCheckingTransportInsurancePhase1_ThenIsSportIsPassedToService()
    {
        SetupValidPhase1();
        _memory.Setup(m => m.GetMonthlyCarCost(150m)).Returns((50m, true));
        _memory.Setup(m => m.CheckTransportInsurance(25m, true)).Returns(true);
        var data = ValidPhase1Data();
        data.TransportInsuranceCost = 25m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenMultipleWrongFields_WhenCheckingBalanceMonthPhase1_ThenAllAreReported()
    {
        SetupValidPhase1();
        var data = ValidPhase1Data();
        data.NetSalary = 1m;
        data.OtherLivingCost = 1m;
        data.TransportMonthlyCost = 1m;

        var result = CreateFirstState().CheckBalanceMonth(data);

        Assert.Equivalent(new[] { "net_salary_phase1", "living_costs_phase1", "purchase_monthly_phase1" },
            result.InvalidFields);
    }

    [Fact]
    public void GivenPlayerWithState_WhenCheckingBalanceMonth_ThenPlayerDelegatesToState()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);

        var result = player.CheckBalanceMonth(ValidPhase1Data());

        Assert.True(result.IsValid);
    }

    // =====================================================================
    // CheckBalanceMonth - fase 2
    // =====================================================================

    [Fact]
    public void GivenValidPhase2Data_WhenCheckingBalanceMonth_ThenResultIsValid()
    {
        SetupValidPhase2();

        var result = CreateSecondState().CheckBalanceMonth(ValidPhase2Data());

        Assert.True(result.IsValid);
        Assert.Empty(result.InvalidFields);
    }

    [Fact]
    public void GivenWrongSalaries_WhenCheckingBalanceMonthPhase2_ThenSalaryFieldsAreInvalid()
    {
        SetupValidPhase2();
        var data = ValidPhase2Data();
        data.NetSalary = 1m;
        data.NetSalaryPartner = 1m;

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.Equivalent(new[] { "net_salary_phase2", "partner_salary_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenUnknownHouse_WhenCheckingBalanceMonthPhase2_ThenReturnsEarlyWithMonthlyRentInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseId(700m)).Returns((string)null!);

        var result = CreateSecondState().CheckBalanceMonth(ValidPhase2Data());

        Assert.False(result.IsValid);
        Assert.Equal(new[] { "monthly_rent_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenRentalTooSmallForHousehold_WhenCheckingBalanceMonthPhase2_ThenMonthlyRentIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseInfoRent(HouseId)).Returns((2, false)); // 3 personen

        var result = CreateSecondState().CheckBalanceMonth(ValidPhase2Data());

        Assert.Equal(new[] { "monthly_rent_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenRentalNeedingCarButCheapCar_WhenCheckingBalanceMonthPhase2_ThenMonthlyCostIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseInfoRent(HouseId)).Returns((4, true));
        _memory.Setup(m => m.GetCarInfo(100m, 3)).Returns((100m, false, 5)); // niet > 100
        var data = ValidPhase2Data();
        data.TransportMonthlyCost = 100m;

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.Equal(new[] { "monthly_cost_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenRentalNeedingCarButTooFewSeats_WhenCheckingBalanceMonthPhase2_ThenCarInsuranceIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseInfoRent(HouseId)).Returns((4, true));
        _memory.Setup(m => m.GetCarInfo(100m, 3)).Returns((200m, false, 2)); // 2 zitplaatsen voor 3 personen

        var result = CreateSecondState().CheckBalanceMonth(ValidPhase2Data());

        Assert.Equal(new[] { "car_insurance_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenSingleSeaterCar_WhenCheckingBalanceMonthPhase2_ThenMonthlyCostIsMultipliedByHouseholdSize()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetCarInfo(100m, 3)).Returns((70m, false, 1));
        var data = ValidPhase2Data();
        data.TransportMonthlyCost = 210m; // 70 * 3 personen
        data.RemainingBudget = 1590m;

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenUnknownCar_WhenCheckingBalanceMonthPhase2_ThenPurchaseMonthlyIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetCarInfo(100m, 3)).Returns((0m, false, 0));
        var data = ValidPhase2Data();
        data.TransportMonthlyCost = 0m;

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.Contains("purchase_monthly_phase2", result.InvalidFields);
    }

    [Fact]
    public void GivenHospitalInsurance_WhenCheckingBalanceMonthPhase2_ThenCostIsCalculatedPerAdultAndChild()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHospitalisationInsurance()).Returns((50m, 20m));
        var data = ValidPhase2Data();
        data.HospitalisationInsuranceCost = 120m; // 2 volwassenen * 50 + 1 kind * 20f
        data.RemainingBudget = 1480m;

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenWrongHospitalInsurance_WhenCheckingBalanceMonthPhase2_ThenHospitalInsuranceIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHospitalisationInsurance()).Returns((50m, 20m));
        var data = ValidPhase2Data();
        data.HospitalisationInsuranceCost = 50m; // enkel 1 volwassene gerekend

        var result = CreateSecondState().CheckBalanceMonth(data);

        Assert.Equal(new[] { "hospital_insurance_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenSocialHousing_WhenCheckingBalanceMonthPhase2_ThenChildDeductionIsAppliedToCombinedSalary()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseId(700m)).Returns("rent_social");
        _memory.Setup(m => m.GetHouseInfoRent("rent_social")).Returns((4, false));
        _memory.Setup(m => m.CheckLivingExpense(400m, "rent_social", 1, true)).Returns(true);

        // (2000 + 1000) - 1 * 500 = 2500
        _memory.Setup(m => m.GetSocialHousingCondition()).Returns((500m, 2501m));
        Assert.True(CreateSecondState().CheckBalanceMonth(ValidPhase2Data()).IsValid);

        _memory.Setup(m => m.GetSocialHousingCondition()).Returns((500m, 2500m));
        var result = CreateSecondState().CheckBalanceMonth(ValidPhase2Data());
        Assert.Equal(new[] { "monthly_rent_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenBuyHouseWithEnoughStartBudget_WhenCheckingBalanceMonthPhase2_ThenResultIsValid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseId(700m)).Returns("buy_1");
        _memory.Setup(m => m.GetHouseInfoBuy("buy_1")).Returns((4, false, 20000m));
        var dossier = CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1);
        dossier.firstWorkphaseDossier.Total1 = 15000m; // 15000 + 5000 partner >= 20000
        _memory.Setup(m => m.CheckLivingExpense(400m, "buy_1", 1, true)).Returns(true);

        var result = CreateSecondState(dossier).CheckBalanceMonth(ValidPhase2Data());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenBuyHouseWithInsufficientStartBudget_WhenCheckingBalanceMonthPhase2_ThenMonthlyRentIsInvalid()
    {
        SetupValidPhase2();
        _memory.Setup(m => m.GetHouseId(700m)).Returns("buy_1");
        _memory.Setup(m => m.GetHouseInfoBuy("buy_1")).Returns((4, false, 20001m));
        var dossier = CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1);
        dossier.firstWorkphaseDossier.Total1 = 15000m;
        _memory.Setup(m => m.CheckLivingExpense(400m, "buy_1", 1, true)).Returns(true);

        var result = CreateSecondState(dossier).CheckBalanceMonth(ValidPhase2Data());

        Assert.Equal(new[] { "monthly_rent_phase2" }, result.InvalidFields);
    }

    [Fact]
    public void GivenPartnerWithoutWage_WhenCheckingBalanceMonthPhase2_ThenHouseholdCountsOneAdult()
    {
        // kind + 1 volwassene => 2 personen
        _memory.Setup(m => m.GetHouseId(700m)).Returns(HouseId);
        _memory.Setup(m => m.GetHouseInfoRent(HouseId)).Returns((2, false));
        _memory.Setup(m => m.CheckLivingExpense(400m, HouseId, 1, false)).Returns(true);
        _memory.Setup(m => m.GetCarInfo(100m, 2)).Returns((200m, false, 5));
        var data = ValidPhase2Data();
        data.NetSalaryPartner = null;
        var dossier = CreateDossier(partnerWage: 0m, children: 1);

        var result = CreateSecondState(dossier).CheckBalanceMonth(data);

        Assert.True(result.IsValid);
    }

    // =====================================================================
    // NextPhase
    // =====================================================================

    [Fact]
    public void GivenNothingChecked_WhenGoingToNextPhaseFromFirstWorkphase_ThenStateDoesNotChange()
    {
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);

        state.NextPhase(player);

        Assert.Same(state, player.DossierState);
    }

    [Fact]
    public void GivenValidMonthlyCheck_WhenGoingToNextPhaseFromFirstWorkphase_ThenPlayerMovesToSecondWorkphase()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);

        var check = state.CheckTotalMonthly(ValidPhase1Data(), player);
        state.NextPhase(player);

        Assert.True(check.IsValid);
        Assert.IsType<SecondWorkphaseState>(player.DossierState);
    }

    [Fact]
    public void GivenInvalidMonthlyCheck_WhenGoingToNextPhaseFromFirstWorkphase_ThenStateDoesNotChange()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);
        var data = ValidPhase1Data();
        data.RemainingBudget = 1m; // klopt niet

        var check = state.CheckTotalMonthly(data, player);
        state.NextPhase(player);

        Assert.False(check.IsValid);
        Assert.Contains("remaining_budget_phase1", check.InvalidFields);
        Assert.Same(state, player.DossierState);
    }

    [Fact]
    public void GivenValidMonthlyCheck_WhenGoingToNextPhase_ThenValidatedDataIsStoredInDossier()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);
        var data = ValidPhase1Data();

        state.CheckTotalMonthly(data, player);

        Assert.Same(data, dossier.firstWorkphaseDossier);
    }

    [Fact]
    public void GivenValidMonthlyButInvalidCalculation_WhenGoingToNextPhaseFromFirstWorkphase_ThenStateDoesNotChange()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);
        var data = ValidPhase1Data();
        data.CalcRemainingBudgetWholePhase1 = 1m; // moet 550 * 6 = 3300 zijn

        var check = state.CheckCalculationPhase(data, player, Mock.Of<IInvestmentCalculator>());
        state.NextPhase(player);

        Assert.False(check.IsValid);
        Assert.Contains("calc_p1_remaining", check.InvalidFields);
        Assert.Same(state, player.DossierState);
    }

    [Fact]
    public void GivenValidCalculation_WhenGoingToNextPhaseFromFirstWorkphase_ThenPlayerMovesToSecondWorkphase()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(state, dossier);

        var data = ValidPhase1Data();
        data.CalcRemainingBudgetWholePhase1 = 3300m; // 550 * 6 maanden
        data.StudentJobIncome = 300m;                // slots 100 + 200
        data.ParentalSupport = 0m;                   // key "/"
        data.Factor = 1800m;                         // (3300 + 300) / 2
        data.Total1 = 1800m;                         // factor + 0

        var check = state.CheckCalculationPhase(data, player, Mock.Of<IInvestmentCalculator>());
        state.NextPhase(player);

        Assert.True(check.IsValid);
        Assert.IsType<SecondWorkphaseState>(player.DossierState);
    }

    [Fact]
    public void GivenPlayerNotInFirstWorkphase_WhenCheckingTotalMonthly_ThenThrows()
    {
        SetupValidPhase1();
        var dossier = CreateDossier();
        var state = CreateFirstState(dossier);
        var player = CreatePlayer(CreateSecondState(dossier), dossier); // speler zit niet in fase 1

        Assert.Throws<Exception>(() => state.CheckTotalMonthly(ValidPhase1Data(), player));
    }

    [Fact]
    public void GivenSecondWorkphase_WhenGoingToNextPhase_ThenThrowsNotImplemented()
    {
        // Er is nog geen fase na fase 2. Deze test documenteert het huidige gedrag;
        // pas aan zodra er een samenvatting/einde-spel bestaat.
        var dossier = CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1);
        var state = CreateSecondState(dossier);
        var player = CreatePlayer(state, dossier);

        Assert.Throws<NotImplementedException>(() => state.NextPhase(player));
    }

    // =====================================================================
    // CheckEventCardsInputAndTotal4 
    // =====================================================================
    private static EventCard Card(string id, params (string conditionId, decimal amount)[] options) => new()
    {
        Id = id,
        Options = options.Select(c => new EventCardOption { ConditionId = c.conditionId, Amount = c.amount }).ToList()
    };
        
    // kosten negatief, inkomsten positief (zoals in de JSON)
    private static List<EventCard> TestEventCards() =>
    [
        Card("second_car", ("", -5000m)),
        Card("divorce"),
        Card("car_accident_parked", ("normal", -500m), ("sport", -1000m)),
        Card("car_accident_hail", ("small", -300m), ("big", -800m)),
        Card("car_accident_traffic_jam", ("normal", -400m), ("sport", -900m)),
        Card("inheritance", ("small", 2000m), ("big", 10000m)),
        Card("house_on_fire", ("NO", -20000m)),
        Card("storm_damage_trampoline", ("NO", -150m)),
        Card("storm_damage_roof", ("NO", -2500m)),
        Card("dismissal", ("0", -1000m), ("1", -2000m), ("2", -3000m), ("3", -4000m)),
        Card("promotion", ("0", 0m), ("1", 1000m), ("2", 2000m), ("3", 3000m)),
        Card("new_job", ("a", 500m)),
        Card("financial_support", ("a", 750m)),
        Card("holiday", ("a", -600m)),
        Card("leaking_roof", ("owner", -1200m)),
        Card("illness_dice", ("1", -100m), ("2", -200m)),
        Card("hospital_treatment", ("NO", -3000m)),
        Card("housing_costs_1", ("a", -250m)),
        Card("housing_costs_2", ("a", -350m)),
        Card("depression", ("a", -450m)),
    ];

    // Total3 en resterend budget op 0, zodat Total4 gelijk is aan de som van de event-impacten
    private static DossierData EventData(decimal? event1 = null, decimal? event2 = null, decimal? event3 = null) => new()
    {
        NetSalaryPartner = 1000m,
        HousingCost = 700m,
        Total3 = 0m,
        CalcRemainingBudgetWholePhase2 = 0m,
        Event1Impact = event1,
        Event2Impact = event2,
        Event3Impact = event3
    };
    private (SecondWorkphaseState state, Player player) CreateEventCardSetup(
        DossierData data, Dossier? dossier = null, params EventCardChoices[] choices)
    {
        dossier ??= CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1);
        dossier.secondWorkphaseDossier = data;
        dossier.ChoosenEventCards = new SavedEventCards { Choices = choices.ToList() };
        _memory.Setup(m => m.GetAllEventCards()).Returns(TestEventCards());

        var state = CreateSecondState(dossier);
        return (state, CreatePlayer(state, dossier));
    }

    private static EventCardChoices Choice(string cardId, string? conditionId = null)
        => new() { CardId = cardId, ConditionId = conditionId };
    
    [Theory]
    [InlineData(5000)]
    [InlineData(-5000)]
    public void GivenCorrectSecondCarImpact_WhenCheckingEventCards_ThenEventIsValid(int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenWrongSecondCarImpact_WhenCheckingEventCards_ThenEvent1IsInvalid()
    {
        var data = EventData(1234m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("illness_dice", "2", 200)]
    [InlineData("housing_costs_1", "a", 250)]
    [InlineData("housing_costs_2", "a", 350)]
    [InlineData("depression", "a", 450)]
    public void GivenCorrectConditionCost_WhenCheckingEventCards_ThenEventIsValid(string cardId, string condition, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId, condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("illness_dice", "1", 200)]  // bedrag van optie 2 bij conditie 1
    [InlineData("housing_costs_1", "a", 251)]
    [InlineData("housing_costs_2", "a", 351)]
    [InlineData("depression", "a", 451)]
    public void GivenWrongConditionCost_WhenCheckingEventCards_ThenEvent1IsInvalid(string cardId, string condition, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId, condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("inheritance", "small", 2000)]
    [InlineData("inheritance", "big", 10000)]
    [InlineData("new_job", "a", 500)]
    [InlineData("financial_support", "a", 750)]
    public void GivenCorrectIncome_WhenCheckingEventCards_ThenEventIsValid(string cardId, string condition, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId, condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("inheritance", "small", 10000)]
    [InlineData("new_job", "a", 501)]
    [InlineData("financial_support", "a", 749)]
    public void GivenWrongIncome_WhenCheckingEventCards_ThenEvent1IsInvalid(string cardId, string condition, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId, condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(25, 500)]   // gewone verzekering -> optie 0
    [InlineData(55, 1000)]  // sportwagen -> optie 1
    [InlineData(70, 0)]     // franchise -> geen kost
    public void GivenCarAccidentParkedWithInsurance_WhenCheckingEventCards_ThenMatchingOptionIsUsed(int insurance, int impact)
    {
        var data = EventData(impact);
        data.TransportInsuranceCost = insurance;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_parked"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(25, 1000)]  // bedrag van de sportwagen bij gewone verzekering
    [InlineData(70, 500)]   // franchise maar toch een kost ingevuld
    public void GivenCarAccidentParkedWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid(int insurance, int impact)
    {
        var data = EventData(impact);
        data.TransportInsuranceCost = insurance;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_parked"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(25, "small", 300)]
    [InlineData(25, "big", 800)]
    [InlineData(55, "big", 0)]
    [InlineData(70, "small", 0)]
    public void GivenCarAccidentHailWithInsurance_WhenCheckingEventCards_ThenEventIsValid(int insurance, string condition, int impact)
    {
        var data = EventData(impact);
        data.TransportInsuranceCost = insurance;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_hail", condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(25, "small", 800)]  // bedrag van de andere conditie
    [InlineData(55, "big", 800)]    // franchise maar toch een kost ingevuld
    public void GivenCarAccidentHailWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid(int insurance, string condition, int impact)
    {
        var data = EventData(impact);
        data.TransportInsuranceCost = insurance;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_hail", condition));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(25, 400)]
    [InlineData(55, 900)]
    [InlineData(70, 0)]
    public void GivenTrafficJamWithInsurance_WhenCheckingEventCards_ThenMatchingOptionIsUsed(int insurance, int impact)
    {
        var data = EventData(impact);
        data.TransportInsuranceCost = insurance;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_traffic_jam"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenTrafficJamWithOnlyAccidentInsurance_WhenCheckingEventCards_ThenNoCostIsExpected()
    {
        var data = EventData(0m);
        data.TransportInsuranceCost = 0m;
        data.AccidentInsuranceCost = 7m;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_traffic_jam"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenTrafficJamWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid()
    {
        var data = EventData(900m); // sportwagenbedrag bij gewone verzekering
        data.TransportInsuranceCost = 25m;
        var (state, player) = CreateEventCardSetup(data, null, Choice("car_accident_traffic_jam"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    // zonder verzekering -> bedrag van optie "NO"
    [InlineData("house_on_fire", 0, 0, 0, 20000)]
    [InlineData("storm_damage_roof", 0, 0, 0, 2500)]
    [InlineData("storm_damage_trampoline", 0, 0, 0, 150)]
    [InlineData("hospital_treatment", 0, 0, 0, 3000)]
    // met de juiste verzekering -> geen kost
    [InlineData("house_on_fire", 20, 0, 0, 0)]
    [InlineData("storm_damage_roof", 20, 0, 0, 0)]
    [InlineData("storm_damage_trampoline", 0, 10, 0, 0)]
    [InlineData("hospital_treatment", 0, 0, 15, 0)]
    public void GivenInsuranceCard_WhenCheckingEventCards_ThenEventIsValid(
        string cardId, int fire, int family, int hospital, int impact)
    {
        var data = EventData(impact);
        data.FireInsuranceCost = fire;
        data.FamilyInsuranceCost = family;
        data.HospitalisationInsuranceCost = hospital;
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("house_on_fire", 0, 0, 0, 19999)]          // fout bedrag zonder verzekering
    [InlineData("hospital_treatment", 0, 0, 0, 2999)]      // fout bedrag zonder verzekering
    [InlineData("house_on_fire", 20, 0, 0, 20000)]         // verzekerd maar toch kost ingevuld
    [InlineData("storm_damage_trampoline", 20, 0, 0, 0)]   // brandverzekering dekt trampoline niet
    
    public void GivenInsuranceCardWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid(
        string cardId, int fire, int family, int hospital, int impact)
    {
        var data = EventData(impact);
        data.FireInsuranceCost = fire;
        data.FamilyInsuranceCost = family;
        data.HospitalisationInsuranceCost = hospital;
        var (state, player) = CreateEventCardSetup(data, null, Choice(cardId));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    private static Dossier DossierWithEducation(int filledSlots)
    {
        var dossier = CreateDossier(partnerWage: 1000m, partnerSavings: 5000m, children: 1);
        dossier.Character.StudentPhase.Options.Add(new StudentPhaseOption { Id = "education", FilledSlots = filledSlots });
        return dossier;
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(-1000)]
    public void GivenDivorceWithPartnerSalary_WhenCheckingEventCards_ThenEventIsValid(int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice("divorce"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenDivorceWithWrongAmount_WhenCheckingEventCards_ThenEvent1IsInvalid()
    {
        var data = EventData(999m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("divorce"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(1000, 1800)] // speler + partner + 1 kind = 3 personen
    [InlineData(0, 1200)]    // partner zonder loon telt niet mee = 2 personen
    public void GivenHoliday_WhenCheckingEventCards_ThenCostIsMultipliedByPersons(int partnerSalary, int impact)
    {
        var data = EventData(impact);
        data.NetSalaryPartner = partnerSalary;
        var (state, player) = CreateEventCardSetup(data, null, Choice("holiday", "a"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenHolidayForOnePerson_WhenCheckingEventCards_ThenEvent1IsInvalid()
    {
        var data = EventData(600m); // vergeten te vermenigvuldigen
        var (state, player) = CreateEventCardSetup(data, null, Choice("holiday", "a"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("buy_1", 1200)]
    [InlineData("rent_1", 0)]
    public void GivenLeakingRoof_WhenCheckingEventCards_ThenOnlyOwnerPays(string houseId, int impact)
    {
        _memory.Setup(m => m.GetHouseId(700m)).Returns(houseId);
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice("leaking_roof"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData("buy_1", 0)]     // eigenaar maar niets ingevuld
    [InlineData("rent_1", 1200)] // huurder maar toch kost ingevuld
    public void GivenLeakingRoofWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid(string houseId, int impact)
    {
        _memory.Setup(m => m.GetHouseId(700m)).Returns(houseId);
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, null, Choice("leaking_roof"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 2000)]
    [InlineData(2, 3000)]
    [InlineData(3, 4000)]
    public void GivenDismissal_WhenCheckingEventCards_ThenOptionMatchesEducation(int slots, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, DossierWithEducation(slots), Choice("dismissal"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenDismissalWithWrongEducationAmount_WhenCheckingEventCards_ThenEvent1IsInvalid()
    {
        var data = EventData(2000m); // bedrag voor 1 slot, maar speler heeft er 2
        var (state, player) = CreateEventCardSetup(data, DossierWithEducation(2), Choice("dismissal"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 3000)]
    public void GivenPromotion_WhenCheckingEventCards_ThenOptionMatchesEducation(int slots, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, DossierWithEducation(slots), Choice("promotion"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Theory]
    [InlineData(0, 500)]  // geen studie maar toch promotie ingevuld
    [InlineData(2, 1000)] // bedrag van 1 slot bij 2 slots
    public void GivenPromotionWithWrongImpact_WhenCheckingEventCards_ThenEvent1IsInvalid(int slots, int impact)
    {
        var data = EventData(impact);
        var (state, player) = CreateEventCardSetup(data, DossierWithEducation(slots), Choice("promotion"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_event1", result.InvalidFields);
    }

    // TODO: illness_work heeft nog geen logica, elk bedrag wordt aanvaard
    [Fact]
    public void GivenIllnessWork_WhenCheckingEventCards_ThenAnyImpactIsCurrentlyAccepted()
    {
        var data = EventData(12345m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("illness_work"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    // TODO: zonder "education"-optie wordt het ontslagbedrag niet gecontroleerd
    [Fact]
    public void GivenDismissalWithoutEducationOption_WhenCheckingEventCards_ThenAnyImpactIsCurrentlyAccepted()
    {
        var data = EventData(12345m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("dismissal"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

        [Fact]
    public void GivenThreeCards_WhenCheckingEventCards_ThenTotal4IsSumOfImpacts()
    {
        // -5000 (kost) + 2000 (erfenis) - 250 (kost) = -3250
        var data = EventData(5000m, 2000m, 250m);
        data.Total4 = -3250m;
        var (state, player) = CreateEventCardSetup(data, null,
            Choice("second_car"), Choice("inheritance", "small"), Choice("housing_costs_1", "a"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenTotal3AndRemainingBudget_WhenCheckingEventCards_ThenTheyAreAddedToTotal4()
    {
        // 100 + 1000 - 5000 = -3900
        var data = EventData(5000m);
        data.Total3 = 100m;
        data.CalcRemainingBudgetWholePhase2 = 1000m;
        data.Total4 = -3900m;
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenCostEnteredAsNegative_WhenCheckingEventCards_ThenTotal4StillSubtractsIt()
    {
        var data = EventData(-5000m);
        data.Total4 = -5000m;
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GivenWrongTotal4_WhenCheckingEventCards_ThenTotal4IsInvalid()
    {
        var data = EventData(5000m);
        data.Total4 = 0m;
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.Contains("calc_p2_total4", result.InvalidFields);
        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
    }

    [Fact]
    public void GivenWrongSecondCard_WhenCheckingEventCards_ThenOnlyEvent2IsInvalid()
    {
        var data = EventData(5000m, 999m);
        var (state, player) = CreateEventCardSetup(data, null,
            Choice("second_car"), Choice("housing_costs_1", "a"));

        var result = state.CheckEventCardsInputAndTotal4(data, player);

        Assert.DoesNotContain("calc_p2_event1", result.InvalidFields);
        Assert.Contains("calc_p2_event2", result.InvalidFields);
    }

    [Fact]
    public void GivenNoChosenEventCards_WhenCheckingEventCards_ThenThrows()
    {
        var data = EventData();
        var (state, player) = CreateEventCardSetup(data);

        Assert.Throws<Exception>(() => state.CheckEventCardsInputAndTotal4(data, player));
    }

    [Fact]
    public void GivenCardMissingFromData_WhenCheckingEventCards_ThenThrows()
    {
        var data = EventData(5000m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("second_car"));
        _memory.Setup(m => m.GetAllEventCards()).Returns([]);

        Assert.Throws<Exception>(() => state.CheckEventCardsInputAndTotal4(data, player));
    }

    // TODO: new_job controleert niet of de kaart bestaat, daardoor NullReferenceException i.p.v. "Card not found"
    [Fact]
    public void GivenNewJobMissingFromData_WhenCheckingEventCards_ThenCurrentlyThrowsNullReference()
    {
        var data = EventData(500m);
        var (state, player) = CreateEventCardSetup(data, null, Choice("new_job", "a"));
        _memory.Setup(m => m.GetAllEventCards()).Returns([]);

        Assert.Throws<NullReferenceException>(() => state.CheckEventCardsInputAndTotal4(data, player));
    }


}