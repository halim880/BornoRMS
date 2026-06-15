"use client";

import { useEffect, useState } from "react";
import type { OrderDetail, OrderStatus } from "@/lib/api";

// Fulfilment order so we can tell "accepted or later" from "still placed".
const RANK: Record<OrderStatus, number> = {
  Placed: 0,
  Confirmed: 1,
  Preparing: 2,
  Ready: 3,
  Served: 4,
  Completed: 5,
  Cancelled: -1,
};

const TERMINAL: OrderStatus[] = ["Served", "Completed", "Cancelled"];

function formatTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", timeZone: "Asia/Dhaka" });
}

export default function OrderStatusTracker({ initial }: { initial: OrderDetail }) {
  const [order, setOrder] = useState<OrderDetail>(initial);

  useEffect(() => {
    if (TERMINAL.includes(order.status)) return;
    let active = true;
    const tick = async () => {
      try {
        const res = await fetch(`/api/orders/${order.id}`, { cache: "no-store" });
        if (!res.ok) return;
        const fresh = (await res.json()) as OrderDetail;
        if (active) setOrder(fresh);
      } catch {
        /* transient — try again next tick */
      }
    };
    const timer = setInterval(tick, 10_000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [order.id, order.status]);

  const accepted = RANK[order.status] >= RANK.Confirmed;
  const eta = order.estimatedReadyAtUtc;
  const minsLeft = eta ? Math.round((new Date(eta).getTime() - Date.now()) / 60000) : null;

  let banner: { tone: string; text: string };
  if (order.status === "Cancelled") {
    banner = { tone: "rose", text: "This order was cancelled." };
  } else if (order.status === "Ready") {
    banner = { tone: "emerald", text: "Your order is ready! 🍽️" };
  } else if (TERMINAL.includes(order.status)) {
    banner = { tone: "emerald", text: order.status === "Served" ? "Served — enjoy your meal!" : "Order complete." };
  } else if (accepted) {
    const when = eta ? ` — ready by ~${formatTime(eta)}` : "";
    const left = minsLeft !== null && minsLeft > 0 ? ` (≈${minsLeft} min)` : "";
    banner = { tone: "emerald", text: `Accepted ✓ — kitchen is preparing your order${when}${left}` };
  } else {
    banner = { tone: "amber", text: "Awaiting confirmation from the restaurant…" };
  }

  const toneClass =
    banner.tone === "rose"
      ? "border-rose-200 bg-rose-50 text-rose-700"
      : banner.tone === "amber"
        ? "border-amber-200 bg-amber-50 text-amber-800"
        : "border-emerald-200 bg-emerald-50 text-emerald-800";

  return (
    <div className={`rounded-xl border p-4 ${toneClass}`}>
      <div className="text-sm font-medium">{banner.text}</div>
      <div className="mt-1 text-xs opacity-80">Status: {order.status}</div>
    </div>
  );
}
