using System;
using LYL.Domain.Model.Dossiers;
using LYL.Domain.Model.Interfaces;


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
        //TODO testen!!!
        
        var invalid = new List<string>();
        decimal? totalImpactEventCards = 0;
        
        var eventcardsChoices = player.Dossier.ChoosenEventCards.Choices;
        if (eventcardsChoices.Count == 0) throw new Exception("No eventcards choosen or saved");
        
        var allEventcards = service.GetAllEventCards();
        //TODO elke apart eventcard overlopen

        for (int i = 0; i < eventcardsChoices.Count; i++)
        {
            //per kaart bijhouden of de ingevulde impact klopt, zodat het juiste event-veld gehighlight wordt
            bool result = true;
            var id = eventcardsChoices[i].CardId;
            decimal? dataDecimal = i switch
            {
                0 => data.Event1Impact,
                1 => data.Event2Impact,
                2 => data.Event3Impact,
                _ => null 
            };

            if (id == "second_car")
            {
                var card = allEventcards.Find(x => x.Id == "second_car");
                if (card == null) throw new Exception("Card not found");
                result &= card.Options[0].Amount == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "divorce")
            {
                var card = allEventcards.Find(x => x.Id == "divorce");
                if (card == null) throw new Exception("Card not found");

                result &= player.Dossier.secondWorkphaseDossier.NetSalaryPartner ==
                          (AsAmount(dataDecimal));
               totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "car_accident_parked")
            {
                var card = allEventcards.Find(x => x.Id == "car_accident_parked");
                if (card == null) throw new Exception("Card not found");
                
                var choosenInsuranceDecimal = player.Dossier.secondWorkphaseDossier.TransportInsuranceCost;

                //TODO getinsurances methods waardoor deze flexibel worden ingeladen
                //check which insurance player has (normal || sportscar)
                if (choosenInsuranceDecimal is 25 or 50 )
                {
                    //special check in case user writes negative or positive in frontend
                     result &= card.Options[0].Amount == AsCost(dataDecimal);
                     totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else if (choosenInsuranceDecimal is 55 or 125)
                {
                    result &= card.Options[1].Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else if (choosenInsuranceDecimal is 70 or 350)
                {
                    //has franchise, no costs
                    result &= dataDecimal is 0 or null;
                }
                //No car, no insurance (previously checked if they hava car that they need an insurance)
                //TODO is it okay to keep the logic like this?

            }

            if (id == "car_accident_hail")
            {
                var card = allEventcards.Find(x => x.Id == "car_accident_hail");
                if (card == null) throw new Exception("Card not found");

                var choosenCondition = eventcardsChoices[i].ConditionId;
                var option = card.Options.Find(x => x.ConditionId == choosenCondition);

                var choosenInsuranceDecimal = player.Dossier.secondWorkphaseDossier.TransportInsuranceCost;
                
                //TODO getinsurances methods waardoor deze flexibel worden ingeladen
                //check which insurance player has (normal || sportscar)
                if (choosenInsuranceDecimal is 25 or 50 )
                {
                    result &= option.Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else if (choosenInsuranceDecimal is 55 or 125)
                {
                    //has franchise, no costs
                    result &= dataDecimal is 0 or null;
                }
                else if (choosenInsuranceDecimal is 70 or 350)
                {
                    //has franchise, no costs
                    result &= dataDecimal is 0 or null;
                }
            }

            if (id == "car_accident_traffic_jam")
            {
                var card = allEventcards.Find(x => x.Id == "car_accident_traffic_jam");
                if (card == null) throw new Exception("Card not found");
                
                var choosenInsuranceDecimal = player.Dossier.secondWorkphaseDossier.TransportInsuranceCost;
                var options = card.Options;
                
                //TODO getinsurances methods waardoor deze flexibel worden ingeladen
                //check which insurance player has (normal || sportscar)
                if (choosenInsuranceDecimal is 25 or 50 )
                {
                    result &= options[0].Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else if (choosenInsuranceDecimal is 55 or 125)
                {
                    result &= options[1].Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else if (choosenInsuranceDecimal is 70 or 350)
                {
                    //has franchise, no costs
                    result &= dataDecimal is 0 or null;
                } 
                else if (player.Dossier.secondWorkphaseDossier.AccidentInsuranceCost == 7)
                {
                    //TODO does this make sense, what do they mean when they have an accidentinsurance? What costs are covered?
                    //does not make sense tha accident insurance covers everything but we keep it like this for now
                    result &=dataDecimal is 0 or null;
                }
                //if 0 there is no car and also no car insurance, so no car accident is possible
                //TODO is this logic okay or do they want it different?
                
            }

            if (id == "inheritance")
            {
                var card = allEventcards.Find(x => x.Id == "inheritance");
                if (card == null) throw new Exception("Card not found");

                var choosenCondition = eventcardsChoices[i].ConditionId;
                var option = card.Options.Find(x => x.ConditionId == choosenCondition);
                result &= option.Amount == dataDecimal;
                totalImpactEventCards += dataDecimal;
            }

            if (id == "house_on_fire")
            {
                var card = allEventcards.Find(x => x.Id == "house_on_fire");
                if (card == null) throw new Exception("Card not found");

                //no specific check needed just if they have one.
                //This method gets called after the check so we're sure that everything is filled in correctly
                if (player.Dossier.secondWorkphaseDossier.FireInsuranceCost > 0)
                {
                    result &= dataDecimal is 0 or null;
                }
                else
                {
                    var option = card.Options.Find(x => x.ConditionId == "NO");
                    result &= option.Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
            }

            if (id == "storm_damage_trampoline")
            {
                var card = allEventcards.Find(x => x.Id == "storm_damage_trampoline");
                if (card == null) throw new Exception("Card not found");
                
                if (player.Dossier.secondWorkphaseDossier.FamilyInsuranceCost > 0)
                {
                    result &= dataDecimal is 0 or null;
                }
                else
                {
                    var option = card.Options.Find(x => x.ConditionId == "NO");
                    result &= option.Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                
            }

            if (id == "storm_damage_roof")
            {
                var card = allEventcards.Find(x => x.Id == "storm_damage_roof");
                if (card == null) throw new Exception("Card not found");
                
                if (player.Dossier.secondWorkphaseDossier.FireInsuranceCost > 0)
                {
                    result &= dataDecimal is 0 or null;
                }
                else
                {
                    var option = card.Options.Find(x => x.ConditionId == "NO");
                    result &= option.Amount == AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
            }

            if (id == "dismissal")
            {
                var card = allEventcards.Find(x => x.Id == "dismissal");
                if (card == null) throw new Exception("Card not found");
                var test = card.Options[1].Amount == AsCost(dataDecimal);
                
                var option = Dossier.Character.StudentPhase.Options.FirstOrDefault(x => x.Id == "education");
                if (option != default)
                {
                    var filled = option.FilledSlots;
                    if (filled == 0) result &= card.Options[0].Amount == AsCost(dataDecimal);
                    if (filled == 1) result &= card.Options[1].Amount == AsCost(dataDecimal);
                    if (filled == 2) result &= card.Options[2].Amount == AsCost(dataDecimal);
                    if (filled == 3) result &= card.Options[3].Amount == AsCost(dataDecimal);
                }
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "promotion")
            {
                var card = allEventcards.Find(x => x.Id == "promotion");
                if (card == null) throw new Exception("Card not found");
                
                var option = Dossier.Character.StudentPhase.Options.FirstOrDefault(x => x.Id == "education");
                if (option != default)
                {
                    var filled = option.FilledSlots;
                    if (filled == 0) result &= dataDecimal is 0 or null;
                    if (filled == 1) 
                    {
                        result &= card.Options[1].Amount == dataDecimal; 
                        totalImpactEventCards += dataDecimal;
                        
                    }

                    if (filled == 2)
                    {
                        result &= card.Options[2].Amount ==  dataDecimal; 
                        totalImpactEventCards += dataDecimal;
                    }

                    if (filled == 3)
                    {
                        result &= card.Options[3].Amount == dataDecimal; 
                        totalImpactEventCards += dataDecimal;
                    }
                }
            }

            if (id == "new_job")
            {
                var card = allEventcards.Find(x => x.Id == "new_job");
                
                var choosenCondition = eventcardsChoices[i].ConditionId;
                var option = card.Options.Find(x => x.ConditionId == choosenCondition);
                result &= option.Amount == dataDecimal;
                totalImpactEventCards += dataDecimal;
                
            }

            if (id == "illness_work")
            {
                //momenteel is deze uit de mogelijkheden + uit de db
                
                //TODO toevoegen logica in json files
                //TODO aanpassen model meet boolean
                //TODO toevoegen aan dbfile
                //TODO optie terug in gamesessionservice steken
                //TODO logica schrijven hier
                
            }

            if (id == "financial_support")
            {
                var card = allEventcards.Find(x => x.Id == "financial_support");
                if (card == null) throw new Exception("Card not found");

                string conditionId = eventcardsChoices[i].ConditionId;
                var option = card.Options.Find(x => x.ConditionId == conditionId);
                result &= option.Amount == dataDecimal;
                totalImpactEventCards += dataDecimal;

            }

            if (id == "holiday")
            {
                var card = allEventcards.Find(x => x.Id == "holiday");
                if (card == null) throw new Exception("Card not found");

                string conditionId = eventcardsChoices[i].ConditionId;
                var option = card.Options.Find(x => x.ConditionId == conditionId);
                int amountOfpp = 1;
                if (player.Dossier.secondWorkphaseDossier.NetSalaryPartner > 0) amountOfpp += 1; //TODO beter checken na update sarah partner
                if (player.Dossier.LivingSituation.Children > 0) amountOfpp += player.Dossier.LivingSituation.Children; 
                //TODO nog aanpassen aan logica sarah
                result &= (option.Amount * amountOfpp)  == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "leaking_roof")
            {
                var card = allEventcards.Find(x => x.Id == "leaking_roof");
                if (card == null) throw new Exception("Card not found");

                var rentsHouse = true;
                string? house_id = service.GetHouseId(data.HousingCost);
                
                if (house_id?.StartsWith("buy") == true) rentsHouse = false;
                
                if (!rentsHouse) 
                {
                    result &= (card.Options.Find(x => x.ConditionId == "owner")).Amount ==  AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
                else
                {
                    result &= dataDecimal is 0 or null;
                }
            }

            if (id == "illness_dice")
            {
                var card = allEventcards.Find(x => x.Id == "illness_dice");
                if (card == null) throw new Exception("Card not found");
                
                var option = card.Options.Find(x => x.ConditionId == eventcardsChoices[i].ConditionId);
                result &= option.Amount == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "hospital_treatment")
            {
                var card = allEventcards.Find(x => x.Id == "hospital_treatment");
                if (card == null) throw new Exception("Card not found");

                if (player.Dossier.secondWorkphaseDossier.HospitalisationInsuranceCost > 0)
                {
                    result &=  dataDecimal is 0 or null;
                }
                else //no insurance
                {
                    result &= (card.Options.Find(x => x.ConditionId == "NO")).Amount ==  AsCost(dataDecimal);
                    totalImpactEventCards -= AsAmount(dataDecimal);
                }
            }

            if (id == "housing_costs_1")
            {
                var card = allEventcards.Find(x => x.Id == "housing_costs_1");
                if (card == null) throw new Exception("Card not found");
                
                var option = card.Options.Find(x => x.ConditionId == eventcardsChoices[i].ConditionId);
                result &= option.Amount == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "housing_costs_2")
            {
                var card = allEventcards.Find(x => x.Id == "housing_costs_2");
                if (card == null) throw new Exception("Card not found");
                
                var option = card.Options.Find(x => x.ConditionId == eventcardsChoices[i].ConditionId);
                result &= option.Amount == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (id == "depression")
            {
                var card = allEventcards.Find(x => x.Id == "depression");
                if (card == null) throw new Exception("Card not found");
                
                var option = card.Options.Find(x => x.ConditionId == eventcardsChoices[i].ConditionId);
                result &= option.Amount == AsCost(dataDecimal);
                totalImpactEventCards -= AsAmount(dataDecimal);
            }

            if (!result) invalid.Add($"calc_p2_event{i + 1}");
        }
        
        //TOTAL 3 + resterend budget tweede werkfase (calc_p2_remaining) - eventcards
        //gecontroleerd op de ingevulde waarde: een fout resterend budget wordt al gehighlight via calc_p2_remaining
        var total4 = data.Total3 + data.CalcRemainingBudgetWholePhase2 + totalImpactEventCards;
        if (total4 != data.Total4) invalid.Add("calc_p2_total4");
        
        return new DossierCheckResult(invalid.Count == 0, invalid);
    }

    //Kosten worden als negatief bedrag verwacht, ongeacht of de speler een - typt of niet
    private static decimal? AsCost(decimal? value) => value > 0 ? -value : value;

    //Absolute waarde van de ingevulde impact
    private static decimal? AsAmount(decimal? value) => value < 0 ? -value : value;

    public override void AcceptWithoutCheck(DossierData data)
    {
        Dossier.secondWorkphaseDossier = data;
        isChecked = true;
    }
}
