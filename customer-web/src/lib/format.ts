// Locale and timezone are pinned so a string formatted during server-side rendering
// matches the one the browser produces on hydration (an unpinned locale/timezone differs
// between the Node server and the client and triggers React hydration mismatches).
export function formatMoney(amount: number, currency = "Tk"): string {
  return `${currency} ${amount.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
}

export function formatDateTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString("en-GB", { timeZone: "Asia/Dhaka" });
}
