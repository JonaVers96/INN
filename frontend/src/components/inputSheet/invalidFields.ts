import { createContext, type CSSProperties } from "react";

export const InvalidFieldsContext = createContext<{
  invalidFields: string[];
  clearField: (name: string) => void;
}>({ invalidFields: [], clearField: () => {} });

export const errorStyle = (invalid: boolean): CSSProperties =>
  invalid
    ? { boxShadow: "inset 0 0 0 2px #dc2626", backgroundColor: "#fee2e2" }
    : {};

// Totaalvelden hebben witte tekst: bij een fout donkere tekst zodat het getal leesbaar blijft op de rode achtergrond
export const totalErrorStyle = (invalid: boolean): CSSProperties =>
  invalid ? { ...errorStyle(true), color: "#991b1b" } : {};

// Klassen voor een veld in kolom 2 (fase 2): wazig zolang fase 1 bezig is, anders grijs als het readonly is
export const col2Class = (blurred: boolean, readOnly: boolean) =>
  blurred
    ? "blur-[1px] pointer-events-none"
    : readOnly
      ? "opacity-70 pointer-events-none"
      : "";
