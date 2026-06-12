"use client";

import { useEffect, useState } from "react";
import { clearTable, readTable, type TableInfo } from "@/lib/table";

export default function TableBadge() {
  const [table, setTable] = useState<TableInfo | null>(null);

  useEffect(() => {
    const update = () => setTable(readTable());
    update();
    window.addEventListener("bb_table_changed", update);
    return () => window.removeEventListener("bb_table_changed", update);
  }, []);

  if (!table) return null;

  return (
    <div className="bg-emerald-50 border-b border-emerald-200">
      <div className="mx-auto flex max-w-4xl items-center justify-between px-4 py-2 text-sm text-emerald-800">
        <span>
          Ordering for <strong>Table {table.tableNumber}</strong> (dine-in)
        </span>
        <button
          type="button"
          onClick={clearTable}
          className="rounded-md px-2 py-0.5 text-emerald-700 hover:bg-emerald-100"
          aria-label="Clear table"
        >
          ✕ clear
        </button>
      </div>
    </div>
  );
}
