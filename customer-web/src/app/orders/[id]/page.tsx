import Link from "next/link";
import { apiFetch, ApiError, type OrderDetail } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";

export const dynamic = "force-dynamic";

export default async function OrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let order: OrderDetail | null = null;
  let error: string | null = null;
  try {
    order = await apiFetch<OrderDetail>(`/orders/${id}`);
  } catch (e) {
    error = (e as ApiError).message || "Could not load order.";
  }

  if (error || !order) {
    return (
      <div className="space-y-4">
        <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
        <Link href="/menu" className="text-emerald-700 underline">Back to menu</Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
        <div className="flex items-center justify-between">
          <div className="text-sm text-emerald-700">Order placed 🎉</div>
          {order.isPaid && (
            <span className="rounded-full bg-emerald-600 px-3 py-1 text-xs font-semibold text-white">
              Paid{order.paymentMethod ? ` · ${order.paymentMethod}` : ""}
            </span>
          )}
        </div>
        <div className="text-xl font-bold">{order.orderNumber}</div>
        <div className="mt-1 text-sm text-slate-600">
          {order.status} · {order.orderType} · {formatDateTime(order.orderedAtUtc)}
        </div>
      </div>

      <div className="space-y-2 rounded-xl border border-slate-200 bg-white p-4">
        {order.lines.map((l) => (
          <div key={l.menuItemId} className="flex justify-between text-sm">
            <span>{l.name} × {l.quantity}</span>
            <span>{formatMoney(l.lineTotal, order!.currency)}</span>
          </div>
        ))}
        <div className="mt-2 flex justify-between border-t border-slate-100 pt-2 text-sm text-slate-600">
          <span>Subtotal</span>
          <span>{formatMoney(order.subtotal, order.currency)}</span>
        </div>
        {order.discountAmount > 0 && (
          <div className="flex justify-between text-sm text-slate-600">
            <span>Discount{order.discountReason ? ` (${order.discountReason})` : ""}</span>
            <span>− {formatMoney(order.discountAmount, order.currency)}</span>
          </div>
        )}
        <div className="flex justify-between border-t border-slate-100 pt-2 font-semibold">
          <span>Total</span>
          <span className="text-emerald-700">{formatMoney(order.total, order.currency)}</span>
        </div>
      </div>

      {order.notes && (
        <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm">
          <span className="font-medium">Notes: </span>{order.notes}
        </div>
      )}

      <div className="flex gap-3">
        <Link href="/menu" className="rounded-lg border border-slate-300 px-4 py-2 text-sm hover:bg-slate-50">
          Order more
        </Link>
        <Link href="/orders" className="rounded-lg border border-slate-300 px-4 py-2 text-sm hover:bg-slate-50">
          All my orders
        </Link>
      </div>
    </div>
  );
}
