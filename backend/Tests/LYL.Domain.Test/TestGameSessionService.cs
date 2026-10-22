using LYL.Api.Contracts.FirstWorkPhase;
using LYL.Domain.Model;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using LYL.Domain.Model.States;
using LYL.Domain.Services;
using LYL.Domain.Services.Interfaces;
using LYL.Persistence.Interfaces;
using Moq;

namespace LYL.Domain.Test;

public class TestGameSessionService
{
    private const string RoomCode = "450090";
    private readonly Guid _playerId = Guid.NewGuid();

    private static DossierCheckRequest CreateValidRequest() => new()
    {
        NetSalaryPhase1 = 2450m,
        MonthlyRentPhase1 = 450m,
        LivingCostsPhase1 = 750m,
        PurchaseMonthlyPhase1 = 150m,   // input voor GetMonthlyCarCost
        MonthlyCostPhase1 = 150m,       // moet gelijk zijn aan monthlyCarCost uit de mock
        CarInsurancePhase1 = 25m,
        FireInsurancePhase1 = 10m,
        HospitalInsurancePhase1 = 10m,  // moet gelijk zijn aan result.adult
        FamilyInsurancePhase1 = null,   // niet ingevuld => check wordt overgeslagen
        AccidentInsurancePhase1 = null,
        RemainingBudgetphase1monthly = 905m
    };

    private (GameSessionService Service, Mock<IMemoryAccessService> Memory) CreateSut()
{
    var roomRepoMock = new Mock<IGameRoomRepository>();
    var supervisorRepoMock = new Mock<ISupervisorRepository>();
    var jsonSerializerMock = new Mock<IJsonSerializerService>();
    var memoryMock = new Mock<IMemoryAccessService>();
    var investmentCalculatorMock = new Mock<IInvestmentCalculator>();
    var randomIntProviderMock = new Mock<IRandomIntProvider>();

    memoryMock.Setup(m => m.GetHouseId(450m)).Returns("house_1");
    memoryMock.Setup(m => m.CheckLivingExpense(750m, "house_1")).Returns(true);
    memoryMock.Setup(m => m.GetMonthlyCarCost(150m)).Returns((150m, false));
    memoryMock.Setup(m => m.CheckTransportInsurance(25m, false)).Returns(true);
    memoryMock.Setup(m => m.CheckFireInsuranceCost(10m, "house_1")).Returns(true);
    memoryMock.Setup(m => m.GetHospitalisationInsurance()).Returns((10m, 5m));
    randomIntProviderMock.Setup(r => r.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns(0);

    var partners = new List<Partner> { new Partner() }; // lijst mag niet leeg zijn
    var livingSituation = new LivingSituation(partners, randomIntProviderMock.Object);

    var character = new Character { ChosenJob = new Job { StartNet = 2450 } };
    var dossier = new Dossier(character, livingSituation); // pas aan

    var room = new GameRoom(new Supervisor(), RoomCode);
    var table = room.Tables[0];

    var player = new Player
    {
        PlayerId = _playerId,
        Nickname = "TestPlayer",
        Table = table,
        CurrentPhase = "first-work-phase",
        Dossier = dossier,
        DossierState = new FirstWorkphaseState(dossier, memoryMock.Object)
    };
    table.TryAddPlayer(player);

    roomRepoMock.Setup(r => r.GetByRoomCodeAsync(RoomCode)).ReturnsAsync(room);

    var service = new GameSessionService(
        roomRepoMock.Object,
        supervisorRepoMock.Object,
        jsonSerializerMock.Object,
        memoryMock.Object,
        investmentCalculatorMock.Object,
        randomIntProviderMock.Object);

    return (service, memoryMock);
}

    [Fact]
    public async Task CheckPhaseInputsMonthlyAsync_ValidInput_HasNoInvalidFields()
    {
        var (service, _) = CreateSut();

        var result = await service.CheckPhaseInputsMonthlyAsync(
            RoomCode, _playerId, CreateValidRequest());

        Assert.Empty(result.InvalidFields);
    }

    [Fact]
    public async Task CheckPhaseInputsMonthlyAsync_WrongRemainingBudget_FlagsRemainingBudget()
    {
        var (service, _) = CreateSut();
        var request = CreateValidRequest();
        request.RemainingBudgetphase1monthly = 900m;

        var result = await service.CheckPhaseInputsMonthlyAsync(RoomCode, _playerId, request);

        Assert.Contains("remaining_budget_phase1", result.InvalidFields);
    }

    [Fact]
    public async Task CheckPhaseInputsMonthlyAsync_WrongSalary_FlagsSalary()
    {
        var (service, _) = CreateSut();
        var request = CreateValidRequest();
        request.NetSalaryPhase1 = 2000m;

        var result = await service.CheckPhaseInputsMonthlyAsync(RoomCode, _playerId, request);

        Assert.Contains("net_salary_phase1", result.InvalidFields);
    }

    [Fact]
    public async Task CheckPhaseInputsMonthlyAsync_WrongLivingCost_FlagsLivingCosts()
    {
        var (service, memory) = CreateSut();
        memory.Setup(m => m.CheckLivingExpense(It.IsAny<decimal>(), It.IsAny<string>()))
              .Returns(false);

        var result = await service.CheckPhaseInputsMonthlyAsync(
            RoomCode, _playerId, CreateValidRequest());

        Assert.Contains("living_costs_phase1", result.InvalidFields);
    }

    [Fact]
    public async Task CheckPhaseInputsMonthlyAsync_UnknownRoom_Throws()
    {
        var (service, _) = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CheckPhaseInputsMonthlyAsync("000000", _playerId, CreateValidRequest()));
    }
}