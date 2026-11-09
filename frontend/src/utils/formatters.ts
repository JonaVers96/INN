// Rondt af op centen; geeft enkel decimalen als die niet ",00" zijn.
// 1000000 -> "1 000 000", 1000000.5 -> "1 000 000,50"
export const formatNumber = (amount: number) => {
  const cents = Math.round(Math.abs(amount) * 100);
  const decimals = cents % 100 === 0 ? 0 : 2;

  return (cents / 100)
    .toLocaleString("nl-BE", {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    })
    .replace(/\./g, "\u00A0");
};

// Bedrag in euro: "€ 1 000 000", "- € 3 270,29" of "+ € 500"
export const formatCurrency = (
  amount: number,
  { showSign = false }: { showSign?: boolean } = {},
) => {
  const cents = Math.round(amount * 100);
  const sign = cents < 0 ? "- " : showSign && cents > 0 ? "+ " : "";
  return `${sign}€ ${formatNumber(amount)}`;
};
