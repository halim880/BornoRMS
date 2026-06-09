"use client";

import { useState } from "react";
import { addToCart } from "@/lib/cart";

type Props = {
  menuItemId: string;
  name: string;
  price: number;
  currency: string;
};

export default function AddToCartButton({ menuItemId, name, price, currency }: Props) {
  const [added, setAdded] = useState(false);

  return (
    <button
      type="button"
      onClick={() => {
        addToCart({ menuItemId, name, price, currency });
        setAdded(true);
        setTimeout(() => setAdded(false), 1200);
      }}
      className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
    >
      {added ? "Added ✓" : "Add"}
    </button>
  );
}
