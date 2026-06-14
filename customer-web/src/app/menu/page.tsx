import { apiFetch, resolveImageUrl, type MenuCategory, type TableDto } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import AddToCartButton from "./AddToCartButton";
import TableCapture from "@/components/TableCapture";
import CallWaiter from "@/components/CallWaiter";

export const dynamic = "force-dynamic";

export default async function MenuPage({
  searchParams,
}: {
  searchParams: Promise<{ table?: string }>;
}) {
  let categories: MenuCategory[] = [];
  let error: string | null = null;

  try {
    categories = await apiFetch<MenuCategory[]>("/menu", { authenticated: false });
  } catch {
    error = "Could not load the menu. Is the API running on port 5000?";
  }

  // QR deep-link: resolve ?table=<guid> to a live table; unknown/inactive ids are ignored.
  const { table: tableId } = await searchParams;
  let table: { id: string; tableNumber: string } | null = null;
  if (tableId) {
    try {
      const tables = await apiFetch<TableDto[]>("/tables", { authenticated: false });
      const match = tables.find((t) => t.id.toLowerCase() === tableId.toLowerCase());
      if (match) table = { id: match.id, tableNumber: match.tableNumber };
    } catch {}
  }

  return (
    <div className="space-y-8">
      <TableCapture table={table} />
      <div>
        <h1 className="text-2xl font-bold">Our Menu</h1>
        <p className="text-sm text-slate-500">Browse and add items to your cart.</p>
      </div>

      <CallWaiter />

      {error && (
        <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      )}

      {categories.map((cat) => (
        <section key={cat.id} className="space-y-3">
          <h2 className="text-lg font-semibold text-slate-800">{cat.name}</h2>
          {cat.description && <p className="text-sm text-slate-500">{cat.description}</p>}
          <div className="grid gap-3 sm:grid-cols-2">
            {cat.items.map((item) => {
              const img = resolveImageUrl(item.imageUrl);
              return (
              <div
                key={item.id}
                className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4"
              >
                {img && (
                  <img
                    src={img}
                    alt={item.name}
                    className="mr-3 h-16 w-16 flex-shrink-0 rounded-lg border border-slate-200 object-cover"
                  />
                )}
                <div className="pr-3">
                  <div className="font-medium">{item.name}</div>
                  {item.banglaName && <div className="text-sm text-slate-500">{item.banglaName}</div>}
                  {item.description && <div className="text-xs text-slate-500">{item.description}</div>}
                  <div className="mt-1 text-sm font-semibold text-emerald-700">
                    {item.variants.length > 0
                      ? `from ${formatMoney(Math.min(...item.variants.map((v) => v.price)), item.currency)}`
                      : formatMoney(item.price, item.currency)}
                  </div>
                </div>
                <AddToCartButton item={item} />
              </div>
              );
            })}
          </div>
        </section>
      ))}

      {!error && categories.length === 0 && (
        <p className="text-sm text-slate-500">No menu items available.</p>
      )}
    </div>
  );
}
