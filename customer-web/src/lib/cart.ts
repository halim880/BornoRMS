"use client";

export type CartItem = {
  menuItemId: string;
  variantId: string | null;
  name: string;
  variantName: string | null;
  price: number;
  currency: string;
  quantity: number;
};

// v2: lines carry Product ids (+ optional variant) instead of legacy MenuItem ids,
// so carts saved under the old key are intentionally abandoned.
const KEY = "bb_cart_v2";

export function lineKey(item: Pick<CartItem, "menuItemId" | "variantId">): string {
  return `${item.menuItemId}:${item.variantId ?? ""}`;
}

export function readCart(): CartItem[] {
  if (typeof window === "undefined") return [];
  try {
    return JSON.parse(window.localStorage.getItem(KEY) || "[]") as CartItem[];
  } catch {
    return [];
  }
}

export function writeCart(items: CartItem[]) {
  window.localStorage.setItem(KEY, JSON.stringify(items));
  window.dispatchEvent(new Event("bb_cart_changed"));
}

export function addToCart(item: Omit<CartItem, "quantity">, quantity = 1) {
  const cart = readCart();
  const existing = cart.find((c) => lineKey(c) === lineKey(item));
  if (existing) {
    existing.quantity += quantity;
  } else {
    cart.push({ ...item, quantity });
  }
  writeCart(cart);
}

export function setQuantity(key: string, quantity: number) {
  let cart = readCart();
  if (quantity <= 0) {
    cart = cart.filter((c) => lineKey(c) !== key);
  } else {
    const existing = cart.find((c) => lineKey(c) === key);
    if (existing) existing.quantity = quantity;
  }
  writeCart(cart);
}

export function clearCart() {
  writeCart([]);
}

export function cartTotal(cart: CartItem[]): number {
  return cart.reduce((sum, c) => sum + c.price * c.quantity, 0);
}

export function cartCount(cart: CartItem[]): number {
  return cart.reduce((sum, c) => sum + c.quantity, 0);
}
