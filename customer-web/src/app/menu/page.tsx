import { apiFetch, type MenuCategory } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import AddToCartButton from "./AddToCartButton";

export const dynamic = "force-dynamic";

export default async function MenuPage() {
  let categories: MenuCategory[] = [];
  let error: string | null = null;

  try {
    categories = await apiFetch<MenuCategory[]>("/menu", { authenticated: false });
  } catch {
    error = "Could not load the menu. Is the API running on port 5000?";
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold">Our Menu</h1>
        <p className="text-sm text-slate-500">Browse and add items to your cart.</p>
      </div>

      {error && (
        <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      )}

      {categories.map((cat) => (
        <section key={cat.id} className="space-y-3">
          <h2 className="text-lg font-semibold text-slate-800">{cat.name}</h2>
          {cat.description && <p className="text-sm text-slate-500">{cat.description}</p>}
          <div className="grid gap-3 sm:grid-cols-2">
            {cat.items.map((item) => (
              <div
                key={item.id}
                className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4"
              >
                <div className="pr-3">
                  <div className="font-medium">{item.name}</div>
                  {item.description && <div className="text-xs text-slate-500">{item.description}</div>}
                  <div className="mt-1 text-sm font-semibold text-emerald-700">
                    {formatMoney(item.price, item.currency)}
                  </div>
                </div>
                <AddToCartButton
                  menuItemId={item.id}
                  name={item.name}
                  price={item.price}
                  currency={item.currency}
                />
              </div>
            ))}
          </div>
        </section>
      ))}

      {!error && categories.length === 0 && (
        <p className="text-sm text-slate-500">No menu items available.</p>
      )}
    </div>
  );
}
