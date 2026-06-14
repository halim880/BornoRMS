"use client";

import { useEffect, useState } from "react";
import { readTable, type TableInfo } from "@/lib/table";

type RequestType = "CallWaiter" | "RequestBill" | "NeedWater" | "NeedTissue";

const ACTIONS: { type: RequestType; label: string; icon: string }[] = [
  { type: "CallWaiter", label: "Call Waiter", icon: "🔔" },
  { type: "RequestBill", label: "Request Bill", icon: "🧾" },
  { type: "NeedWater", label: "Need Water", icon: "💧" },
  { type: "NeedTissue", label: "Need Tissue", icon: "🧻" },
];

export default function CallWaiter() {
  const [table, setTable] = useState<TableInfo | null>(null);
  const [busy, setBusy] = useState<RequestType | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    setTable(readTable());
    const onChange = () => setTable(readTable());
    window.addEventListener("bb_table_changed", onChange);
    return () => window.removeEventListener("bb_table_changed", onChange);
  }, []);

  // Service requests only make sense when seated at a table (QR deep-link).
  if (!table) return null;

  async function send(type: RequestType) {
    setBusy(type);
    setMessage(null);
    try {
      const res = await fetch("/api/requests", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ tableId: table!.id, type }),
      });
      if (res.status === 401) {
        setMessage("Please log in to send a request.");
      } else if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setMessage(data?.message || "Could not send your request.");
      } else {
        setMessage("Sent — a staff member will be with you shortly.");
      }
    } catch {
      setMessage("Could not send your request.");
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Need something?</h2>
        <span className="text-xs text-slate-500">Table {table.tableNumber}</span>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        {ACTIONS.map((a) => (
          <button
            key={a.type}
            onClick={() => send(a.type)}
            disabled={busy !== null}
            className="flex flex-col items-center gap-1 rounded-lg border border-slate-200 bg-slate-50 px-3 py-3 text-sm font-medium text-slate-700 transition hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-50"
          >
            <span className="text-lg">{a.icon}</span>
            {busy === a.type ? "Sending…" : a.label}
          </button>
        ))}
      </div>
      {message && <p className="mt-3 text-sm text-emerald-700">{message}</p>}
    </div>
  );
}
