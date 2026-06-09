"use client";

export type CartItem = {
  menuItemId: string;
  name: string;
  price: number;
  currency: string;
  quantity: number;
};

const KEY = "bb_cart";

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
  const existing = cart.find((c) => c.menuItemId === item.menuItemId);
  if (existing) {
    existing.quantity += quantity;
  } else {
    cart.push({ ...item, quantity });
  }
  writeCart(cart);
}

export function setQuantity(menuItemId: string, quantity: number) {
  let cart = readCart();
  if (quantity <= 0) {
    cart = cart.filter((c) => c.menuItemId !== menuItemId);
  } else {
    const existing = cart.find((c) => c.menuItemId === menuItemId);
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
