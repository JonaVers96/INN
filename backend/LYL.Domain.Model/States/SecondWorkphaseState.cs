using System;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;
using static LYL.Domain.Model.States.Amounts;


namespace LYL.Domain.Model.States;

public class SecondWorkphaseState(Dossier dossier, IMemoryAccessService service) : DossierState(dossier, service)
{
    public override DossierCheckResult CheckBalanceMonth(DossierData data)
    {
        var invalid = new List<string>();
        void Check(bool ok, string field) { if (!ok) invalid.Add(field); }

        int adults = 1;
        if (Dossier.LivingSituation.Partner.Wage > 0) adults = 2;
        int children = Dossier.LivingSituation.Children;

        //Salary
        Check(Dossier.Character.ChosenJob?.AvgNet == data.NetSalary, "net_salary_phase2");

        //Salary partner
        Check(Dossier.LivingSituation.Partner.Wage == data.NetSalaryPartner.GetValueOrDefault(), "partner_salary_phase2");

        //Housing: price exists: int (monthly loan/monthly) => string (house_id), string (buy/rent)
        string? house_id = service.GetHouseId(data.HousingCost);
        if (house_id == "rent_social") 
        {
            decimal totalSalary = data.NetSalary + data.NetSalaryPartner.GetValueOrDefault();
            Check(CheckSocialHousingCondition(totalSalary, children), "monthly_rent_phase2");
        }
        //Housing buy: int (maxSpace), bool (needsCar), int (startBudget)
        //=> check living situation and budget
        //Housing rent: house_id => int (maxSpace), bool (needsCar)
        //=> check living situation

        bool needsCar = false; //kan merge conflict veroorzaken vanaf hier dit accepteren! Als er extra's aan toegevoegd zijn. Beide accepteren!!
        if (house_id?.StartsWith("buy") == true)
        {
            var result = service.GetHouseInfoBuy(house_id);
            Check(result.maxSpace >= adults + children, "monthly_rent_phase2");
            if (result.needsCar) Check(data.TransportMonthlyCost > 100, "monthly_cost_phase2");
            needsCar = result.needsCar;
            Check(result.startBudget <= Dossier.firstWorkphaseDossier.Total1 + Dossier.LivingSituation.Partner.Savings, "monthly_rent_phase2");
            //TODO: buy conditions (up to 40% of salary may be used to repay the loan)
        } else if (!string.IsNullOrEmpty(house_id))
        {
            var result = service.GetHouseInfoRent(house_id);
            Check(result.maxSpace >= adults + children, "monthly_rent_phase2");
            needsCar = result.needsCar;
            if (result.needsCar) Check(data.TransportMonthlyCost > 100, "monthly_cost_phase2");
            //TODO 100 is hardcoded for no reason, needs to come from database, but 100 is high enough inflation won't have impact anytime soon
        }
        else
        {
            invalid.Add("monthly_rent_phase2");
            return new DossierCheckResult(false, invalid);
        }
        

        //Other living expenses: price is correct: int price, string house_id, int children, bool hasPartner => bool
        Check(service.CheckLivingExpense(data.OtherLivingCost, house_id, 
                children, adults > 1), "living_costs_phase2");
        
        //Transport: monthly purchase amount: int price => int (check equal to monthly cost), int seats
        //=> check living situation
        (decimal monthlyCarCost, bool isSport, int seats) = service.GetCarInfo(data.TransportPurchaseCost, adults + children);
        Check(monthlyCarCost != default, "purchase_monthly_phase2");
        if (seats == 1) monthlyCarCost *= (adults + children);
        Check(monthlyCarCost == data.TransportMonthlyCost, "monthly_cost_phase2");
        if (needsCar) Check(seats >= adults + children, "car_insurance_phase2");
        

        //Alle volgende velden kijken indien ingevuld

        //Transport insurance: price exists: int => bool
        if (data.TransportInsuranceCost != null && data.TransportInsuranceCost != 0m)
        {
            Check(service.CheckTransportInsurance(data.TransportInsuranceCost, isSport), "car_insurance_phase2");
        }

        //Fire Insurance: price is correct: int, house_id => bool
        if (data.FireInsuranceCost != null && data.FireInsuranceCost != 0m)
        {
            Check(service.CheckFireInsuranceCost(data.FireInsuranceCost, house_id), "fire_insurance_phase2");
        }
        //Family insurance: price is correct: int => bool
        if (data.FamilyInsuranceCost != null && data.FamilyInsuranceCost != 0m)
        {
            Check(service.CheckFamilyInsurance(data.FamilyInsuranceCost), "family_insurance_phase2");
        }
        //Hosp insurance: getting price pp:  => int adultPrice, int CHildPrice
        //=> check living situation => calculate cost => compare
        if (data.HospitalisationInsuranceCost != null && data.HospitalisationInsuranceCost != 0m)
        {
            var insurance = service.GetHospitalisationInsurance();
            var result = insurance.adult * adults + insurance.child * children ; 
            Check(data.HospitalisationInsuranceCost == result, "hospital_insurance_phase2");
        }
        //Accident insurance: look at living sit. => calculate pp => price is correct: int => bool
        if (data.AccidentInsuranceCost != null && data.AccidentInsuranceCost != 0m)
        {
            var price = data.AccidentInsuranceCost / (adults + children);
            Check(service.CheckAccidentInsurance(data.AccidentInsuranceCost), "accident_insurance_phase2");
        }

        return new DossierCheckResult(invalid.Count == 0, invalid.Distinct().ToList());
    }

    public override DossierCheckResult CheckTotalMonthly(DossierData data, Player player)
    {
        var check = CheckBalanceMonth(data);
        var invalid = new List<string>(check.InvalidFields);

        //uitrekenen balance
        var balance = data.NetSalary 
                    + data.NetSalaryPartner.GetValueOrDefault()
                    - data.HousingCost
                    - data.OtherLivingCost
                    - data.TransportMonthlyCost
                    - data.TransportPurchaseCost
                    - data.TransportInsuranceCost.GetValueOrDefault()
                    - data.FireInsuranceCost.GetValueOrDefault()
                    - data.FamilyInsuranceCost.GetValueOrDefault()
                    - data.HospitalisationInsuranceCost.GetValueOrDefault()
                    - data.AccidentInsuranceCost.GetValueOrDefault();

        if (balance != data.RemainingBudget)
            invalid.Add("remaining_budget_phase2");

        if (invalid.Count > 0)
            return new DossierCheckResult(false, invalid);

        // Alles klopt: gegevens opslaan
        if (player.DossierState is SecondWorkphaseState)
        {
            Dossier.secondWorkphaseDossier = data; 
            isChecked = true;
            //TODO: opslaan insurances
        }
        else throw new Exception("Invalid state");

        return new DossierCheckResult(true, invalid);
    }

    private bool CheckSocialHousingCondition(decimal netSalary, int children)
    {
        //Net monthly salary - (number of children x {{childDeduction}}) < {{maxIncome}}
        (decimal childDeduction, decimal maxIncome) = service.GetSocialHousingCondition();
        return netSalary - (children * childDeduction) < maxIncome;
    }

    public override DossierCheckResult CheckCalculationPhase(DossierData data, Player player, IInvestmentCalculator calculator)
    {
        //Fouten uit de bovenste tabel worden mee teruggegeven zodat die ook gehighlight worden
        var invalid = new List<string>(CheckTotalMonthly(data, player).InvalidFields);
        void Check(bool ok, string field) { if (!ok) invalid.Add(field); }

        //Total career phase 1
        Check(data.Total1Carry == Dossier.firstWorkphaseDossier.Total1, "calc_p2_total1_carry");

        //Support (parents) partner
        Check(data.PartnerSavings.GetValueOrDefault() == Dossier.LivingSituation.Partner.Savings, "calc_p2_partner_savings");

        //Total 2: startbudget career phase 2 (^total above)
        Check(data.Total2 == data.Total1Carry + data.PartnerSavings.GetValueOrDefault(), "calc_p2_total2");

        //One-time purchase cost
        string? house_id = service.GetHouseId(data.HousingCost);
        if (house_id?.StartsWith("buy") == true)
        {
            decimal purchaseCost = service.GetHousePurchaseCost(house_id);
            Check(data.HousingPurchaseCost == purchaseCost, "calc_p2_housing");
        }
        else Check(data.HousingPurchaseCost.GetValueOrDefault() == 0, "calc_p2_housing");

        //Subtotal 3 / factor phase 2 = total 3
        Check(data.Total3 == (data.Total2
                              - data.HousingPurchaseCost.GetValueOrDefault())/2, "calc_p2_total3");
        //TODO: factor?

        //Remaining budget career phase 2: budget/maand x 12 maanden x 20 jaar
        Check(data.CalcRemainingBudgetWholePhase2 == data.RemainingBudget * 12 * 20, "calc_p2_remaining");

        isChecked &= invalid.Count == 0;
        if (player.DossierState is SecondWorkphaseState)
        {
            Dossier.secondWorkphaseDossier = data;
        }
        else throw new Exception("Invalid state");

        try
        {
            invalid.AddRange(CheckEventCardsInputAndTotal4(data, player).InvalidFields);
        }
        catch (Exception ex)
        {
            //Vroeger werd deze fout ingeslikt en werden de eventkaarten als correct beschouwd
            Console.WriteLine($"Fout bij controleren eventkaarten: {ex.Message}");
            invalid.AddRange(["calc_p2_event1", "calc_p2_event2", "calc_p2_event3", "calc_p2_total4"]);
        }
        
        //checks laatste velden onderste tabel
        invalid.AddRange(CheckSavingsAccountCalc(data));
        invalid.AddRange(CheckInvestmentCalc(data, calculator));
        Check(CheckTotal5Calc(data), "calc_p2_total5");
        
        return new DossierCheckResult(invalid.Count == 0, invalid.Distinct().ToList());
    }

    private bool CheckTotal5Calc(DossierData data)
    {
        //gecontroleerd op de ingevulde waarden: als een van deze hiervoor al fout was, is dat veld al gehighlight
        if (data.Total4 == null || data.ReturnSavings == null || data.ReturnInvestment == null ||
            data.InflationImpact == null || data.Total5 == null) return false;

        //inflatie mag positief of negatief ingevuld worden (zelfde als in CheckInvestmentCalc)
        var total5 = data.Total4 + data.ReturnSavings + data.ReturnInvestment - AsAmount(data.InflationImpact);
        return total5 == data.Total5;
    }

    private List<string> CheckSavingsAccountCalc(DossierData data)
    {
        double interestRate = 0.01; //todo flexibel binnenhalen
        double years = 20;
        double marge = 0.05;

        if (data.SavingsAccount == null) return ["calc_p2_savings", "calc_p2_return_savings"];
        if (data.ReturnSavings == null) return ["calc_p2_return_savings"];

        double principal = (double)data.SavingsAccount;
        double totalAmount = principal * Math.Pow(1 + interestRate, years);
        double profit = totalAmount - principal;

        var correct = Math.Abs(profit - (double)data.ReturnSavings) <= marge;
        return correct ? [] : ["calc_p2_return_savings"];
    }

    private List<string> CheckInvestmentCalc(DossierData data, IInvestmentCalculator calculator)
    {
        //TODO testen
        var invalid = new List<string>();
        int years = 20;
        decimal rate = 0.06m; //todo flexibel binnenhalen
        decimal inflation = 0.02m;
        decimal marge = 0.05m;

        if (data.Investment == null)
        {
            invalid.Add("calc_p2_investment");
            invalid.Add("calc_p2_return_investment");
        }
        else
        {
            var returnment = calculator.CalculateReturn((decimal)data.Investment, rate, years);
            if (data.ReturnInvestment == null || Math.Abs(returnment - data.ReturnInvestment.Value) > marge)
                invalid.Add("calc_p2_return_investment");
        }

        //Total3 null wordt al gehighlight via calc_p2_total3, inflatie kan dan niet berekend worden
        if (data.Total3 == null || data.InflationImpact == null)
        {
            invalid.Add("calc_p2_inflation");
        }
        else
        {
            var inflationImpact = calculator.CalculateInflationImpact((decimal)data.Total3, inflation, years);
            //check if user added - we have to correct this or it will always say false
            var inflImpactRobust = AsAmount(data.InflationImpact).Value;
            if (Math.Abs(inflationImpact * (-1) - inflImpactRobust) > marge)
                invalid.Add("calc_p2_inflation");
        }

        return invalid;
    }

    public override void NextPhase(Player player)
    {
        //TODO er is geen volgende fase: zou bij uitbreiding de stap naar de samenvatting kunnen zijn of einde game melding!
        throw new NotImplementedException();
    }
    
    
       public override DossierCheckResult CheckEventCardsInputAndTotal4(DossierData data, Player player)
    {
        var choices = player.Dossier.ChoosenEventCards.Choices;
        if (choices.Count == 0) throw new Exception("No eventcards choosen or saved");

        var checker = new EventCardChecker(service, service.GetAllEventCards());
        decimal?[] inputs = [data.Event1Impact, data.Event2Impact, data.Event3Impact];
        var invalid = new List<string>();
        decimal? totalImpactEventCards = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            //per kaart bijhouden of de ingevulde impact klopt, zodat het juiste event-veld gehighlight wordt
            var result = checker.Check(choices[i], i < inputs.Length ? inputs[i] : null, data, player.Dossier);
            if (!result.IsCorrect) invalid.Add($"calc_p2_event{i + 1}");
            totalImpactEventCards += result.Impact;
        }

        //TOTAL 3 + resterend budget tweede werkfase (calc_p2_remaining) - eventcards
        //gecontroleerd op de ingevulde waarde: een fout resterend budget wordt al gehighlight via calc_p2_remaining
        var total4 = data.Total3 + data.CalcRemainingBudgetWholePhase2 + totalImpactEventCards;
        if (total4 != data.Total4) invalid.Add("calc_p2_total4");

        return new DossierCheckResult(invalid.Count == 0, invalid);
    }

    public override void AcceptWithoutCheck(DossierData data)
    {
        Dossier.secondWorkphaseDossier = data;
        isChecked = true;
    }
}
