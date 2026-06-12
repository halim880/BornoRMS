"use client";

export type TableInfo = {
  id: string;
  tableNumber: string;
};

const KEY = "bb_table";

export function readTable(): TableInfo | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.sessionStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as TableInfo) : null;
  } catch {
    return null;
  }
}

export function writeTable(table: TableInfo) {
  window.sessionStorage.setItem(KEY, JSON.stringify(table));
  window.dispatchEvent(new Event("bb_table_changed"));
}

export function clearTable() {
  window.sessionStorage.removeItem(KEY);
  window.dispatchEvent(new Event("bb_table_changed"));
}
