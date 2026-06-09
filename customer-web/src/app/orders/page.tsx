import Link from "next/link";
import { apiFetch, type PagedResult, type OrderListItem } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";

export const dynamic = "force-dynamic";

export default async function OrdersPage() {
  const result = await apiFetch<PagedResult<OrderListItem>>("/orders/mine?page=1&pageSize=50");

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">My Orders</h1>

      {result.items.length === 0 ? (
        <p className="text-sm text-slate-500">No orders yet.</p>
      ) : (
        <div className="space-y-2">
          {result.items.map((o) => (
            <Link
              key={o.id}
              href={`/orders/${o.id}`}
              className="block rounded-xl border border-slate-200 bg-white p-4 hover:border-emerald-300"
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">{o.orderNumber}</span>
                <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                  {o.status}
                </span>
              </div>
              <div className="mt-1 flex items-center justify-between text-sm text-slate-500">
                <span>{o.itemCount} item(s) · {o.orderType}</span>
                <span className="font-semibold text-emerald-700">{formatMoney(o.total, o.currency)}</span>
              </div>
              <div className="mt-1 text-xs text-slate-400">{formatDateTime(o.orderedAtUtc)}</div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
