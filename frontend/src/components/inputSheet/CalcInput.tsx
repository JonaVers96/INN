import { useContext } from "react";
import type { FormikProps } from "formik";
import type { BudgetFormValues } from "../../types/inputsheet";
import { InvalidFieldsContext, errorStyle } from "./invalidFields";

const CalcInput = ({
  name,
  formik,
  readOnly,
  prefix,
  align = "right",
}: {
  name: keyof BudgetFormValues;
  formik: FormikProps<BudgetFormValues>;
  readOnly: boolean;
  prefix?: string;
  align?: "right" | "center";
}) => {
  const { invalidFields, clearField } = useContext(InvalidFieldsContext);

  return (
    <div className="flex items-center w-full gap-1">
      {prefix && <span className="whitespace-nowrap">{prefix}</span>}
      <input
        name={name}
        type="number"
        onChange={(e) => {
          clearField(name);
          formik.handleChange(e);
        }}
        value={formik.values[name]}
        readOnly={readOnly}
        className={`${
          align === "center" ? "w-full text-center" : "flex-1 min-w-0 text-right"
        } bg-transparent outline-none border-b ${
          readOnly
            ? "border-transparent opacity-70 pointer-events-none"
            : "border-slate-400"
        }`}
        style={errorStyle(invalidFields.includes(name))}
      />
    </div>
  );
};

export default CalcInput;
