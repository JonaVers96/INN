import type { CalcTableValues } from "../../types/inputsheet";

// Basisnamen van de bovenste tabel; "_phase1" / "_phase2" wordt er in de JSX achter geplakt
type InputField =
  | "net_salary"
  | "partner_salary"
  | "monthly_rent"
  | "living_costs"
  | "purchase_monthly"
  | "monthly_cost"
  | "car_insurance"
  | "fire_insurance"
  | "family_insurance"
  | "hospital_insurance"
  | "accident_insurance";

export type InputRowConfig = {
  labelKey: string;
  field: InputField;
  col1Locked?: boolean; // kolom 1 altijd readonly (partner heb je in fase 1 nog niet)
  placeholder?: string;
};

export type InputSectionConfig = {
  labelKey: string;
  categories: { icon: string; labelKey: string; rows: InputRowConfig[] }[];
};

export type CalcRowConfig = {
  labelKey: string;
  subKey?: string;
  name: keyof CalcTableValues;
  prefix?: string;
  alwaysReadOnly?: boolean;
};

export const INPUT_SECTIONS: InputSectionConfig[] = [
  {
    labelKey: "income_label",
    categories: [
      {
        icon: "/Loon.svg",
        labelKey: "salary",
        rows: [
          { labelKey: "net_salary", field: "net_salary" },
          { labelKey: "partner_salary", field: "partner_salary", col1Locked: true, placeholder: "XXX" },
        ],
      },
    ],
  },
  {
    labelKey: "expenses_label",
    categories: [
      {
        icon: "/huisvestiging.svg",
        labelKey: "housing",
        rows: [{ labelKey: "monthly_rent", field: "monthly_rent" }],
      },
      {
        icon: "/leefkosten.svg",
        labelKey: "living_costs",
        rows: [{ labelKey: "living_costs", field: "living_costs" }],
      },
      {
        icon: "/mobiliteit.svg",
        labelKey: "mobility",
        rows: [
          { labelKey: "purchase_monthly", field: "purchase_monthly" },
          { labelKey: "monthly_cost", field: "monthly_cost" },
          { labelKey: "car_insurance", field: "car_insurance" },
        ],
      },
      {
        icon: "/verzekering.svg",
        labelKey: "insurance",
        rows: [
          { labelKey: "fire_insurance", field: "fire_insurance" },
          { labelKey: "family_insurance", field: "family_insurance" },
          { labelKey: "hospital_insurance", field: "hospital_insurance" },
          { labelKey: "accident_insurance", field: "accident_insurance" },
        ],
      },
    ],
  },
];

export const P1_CALC_ROWS: CalcRowConfig[] = [
  { labelKey: "remaining_budget_phase1", subKey: "remaining_budget_phase1_sub", name: "calc_p1_remaining", prefix: "▲" },
  { labelKey: "student_job_income", subKey: "student_job_income_sub", name: "calc_p1_student", prefix: "+" },
  { labelKey: "factor", subKey: "factor_sub", name: "calc_p1_factor", prefix: "/2 =" },
  { labelKey: "parental_support", subKey: "parental_support_sub", name: "calc_p1_parental", prefix: "+" },
  { labelKey: "housing_purchase_cost", subKey: "housing_purchase_cost_sub", name: "calc_p1_housing", alwaysReadOnly: true },
];

// Fase 2 is in twee gesplitst: de rekening-grid met 3 kolommen staat ertussen
export const P2_CALC_ROWS_TOP: CalcRowConfig[] = [
  { labelKey: "total1_carry", name: "calc_p2_total1_carry", prefix: "●" },
  { labelKey: "partner_savings", name: "calc_p2_partner_savings", prefix: "+" },
  { labelKey: "total2", subKey: "total2_sub", name: "calc_p2_total2", prefix: "=" },
  { labelKey: "housing_purchase_cost", subKey: "housing_purchase_cost_phase2_sub", name: "calc_p2_housing", prefix: "-" },
  { labelKey: "total3", subKey: "total3_sub", name: "calc_p2_total3", prefix: "★ =" },
];

export const P2_CALC_ROWS_BOTTOM: CalcRowConfig[] = [
  { labelKey: "total3_age50", name: "calc_p2_total3_age50", prefix: "★" },
  { labelKey: "remaining_budget_phase2", subKey: "remaining_budget_phase2_sub", name: "calc_p2_remaining", prefix: "■" },
  { labelKey: "event_cards_impact", name: "calc_p2_event1", prefix: "±" },
  { labelKey: "event_cards_impact", name: "calc_p2_event2", prefix: "±" },
  { labelKey: "event_cards_impact", name: "calc_p2_event3", prefix: "±" },
  { labelKey: "total4", name: "calc_p2_total4", prefix: "=" },
  { labelKey: "return_savings", name: "calc_p2_return_savings", prefix: "+" },
  { labelKey: "return_investment", name: "calc_p2_return_investment", prefix: "+" },
  { labelKey: "impact_inflation", name: "calc_p2_inflation", prefix: "-" },
];

export type AccountColumnConfig = {
  labelKey: string;
  subKey?: string;
  name: keyof CalcTableValues;
};

// De drie rekeningen in fase 2: label bovenaan, invoerveld eronder
export const P2_ACCOUNT_COLUMNS: AccountColumnConfig[] = [
  { labelKey: "checking_account", name: "calc_p2_checking" },
  { labelKey: "savings_account", subKey: "savings_account_sub", name: "calc_p2_savings" },
  { labelKey: "investment", subKey: "investment_sub", name: "calc_p2_investment" },
];

