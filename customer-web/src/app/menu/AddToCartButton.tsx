"use client";

import { useState } from "react";
import { addToCart } from "@/lib/cart";
import { formatMoney } from "@/lib/format";
import type { MenuItem } from "@/lib/api";

type Props = {
  item: MenuItem;
};

export default function AddToCartButton({ item }: Props) {
  const [addedKey, setAddedKey] = useState<string | null>(null);

  function add(variantId: string | null, variantName: string | null, price: number) {
    addToCart({
      menuItemId: item.id,
      variantId,
      name: item.name,
      variantName,
      price,
      currency: item.currency,
    });
    const key = variantId ?? "base";
    setAddedKey(key);
    setTimeout(() => setAddedKey((k) => (k === key ? null : k)), 1200);
  }

  if (item.variants.length > 0) {
    return (
      <div className="flex flex-col items-end gap-1.5">
        {item.variants.map((v) => (
          <button
            key={v.id}
            type="button"
            onClick={() => add(v.id, v.name, v.price)}
            className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
          >
            {addedKey === v.id ? "Added ✓" : `${v.name} · ${formatMoney(v.price, item.currency)}`}
          </button>
        ))}
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={() => add(null, null, item.price)}
      className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
    >
      {addedKey === "base" ? "Added ✓" : "Add"}
    </button>
  );
}
