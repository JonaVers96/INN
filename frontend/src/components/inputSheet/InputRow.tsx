import { useContext } from "react";
import type { FormikProps } from "formik";
import type { BudgetFormValues, TopTableValues } from "../../types/inputsheet";
import { InvalidFieldsContext, errorStyle, col2Class } from "./invalidFields";


const InputRow = ({
  label,
  namePhase1,
  namePhase2,
  formik,
  col1ReadOnly,
  col2ReadOnly,
  col2Blurred,
  placeholder,
}: {
  label: string;
  namePhase1: keyof TopTableValues;
  namePhase2: keyof TopTableValues;
  formik: FormikProps<BudgetFormValues>;
  col1ReadOnly: boolean;
  col2ReadOnly: boolean;
  col2Blurred: boolean;
  placeholder?: string;
}) => {
  const { invalidFields, clearField } = useContext(InvalidFieldsContext);

  return (
    <div
      className="flex gap-0 mb-1 text-xs"
      style={{ backgroundColor: "#FEF9E7" }}
    >
      <span className="flex-1 text-slate-600 py-1 pl-2">{label}</span>

      <input
        name={namePhase1}
        type="number"
        placeholder={placeholder}
        onChange={(e) => {
          clearField(namePhase1);
          formik.handleChange(e);
        }}
        value={formik.values[namePhase1]}
        readOnly={col1ReadOnly}
        className={`w-32 text-center py-1 outline-none ${
          col1ReadOnly ? "opacity-70 pointer-events-none" : ""
        }`}
        style={{
          backgroundColor: "#FDE8DF",
          ...errorStyle(invalidFields.includes(namePhase1)),
        }}
      />

      <input
        name={namePhase2}
        type="number"
        onChange={(e) => {
          clearField(namePhase2);
          formik.handleChange(e);
        }}
        value={formik.values[namePhase2]}
        readOnly={col2ReadOnly}
        className={`w-32 text-center py-1 outline-none ${col2Class(col2Blurred, col2ReadOnly)}`}
        style={{
          backgroundColor: "#E8F7F5",
          ...errorStyle(invalidFields.includes(namePhase2)),
        }}
      />
    </div>
  );
};

export default InputRow;