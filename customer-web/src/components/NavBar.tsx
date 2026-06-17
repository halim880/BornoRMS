"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { cartCount, readCart } from "@/lib/cart";

export default function NavBar({ phone }: { phone: string | null }) {
  const [count, setCount] = useState(0);

  useEffect(() => {
    const update = () => setCount(cartCount(readCart()));
    update();
    window.addEventListener("bb_cart_changed", update);
    window.addEventListener("storage", update);
    return () => {
      window.removeEventListener("bb_cart_changed", update);
      window.removeEventListener("storage", update);
    };
  }, []);

  return (
    <header className="border-b border-slate-200 bg-white">
      <nav className="mx-auto flex max-w-4xl items-center justify-between px-4 py-3">
        <Link href="/menu" className="flex items-center gap-2 text-lg font-bold text-emerald-700">
          <img src="/logo.svg" width={24} height={24} alt="" className="rounded-md" />
          BornoBit
        </Link>
        <div className="flex items-center gap-4 text-sm">
          <Link href="/menu" className="hover:text-emerald-700">Menu</Link>
          <Link href="/cart" className="hover:text-emerald-700">
            Cart{count > 0 ? ` (${count})` : ""}
          </Link>
          {phone ? (
            <>
              <Link href="/orders" className="hover:text-emerald-700">My Orders</Link>
              <span className="text-slate-400">{phone}</span>
              <form action="/api/auth/logout" method="post">
                <button className="rounded-md border border-slate-300 px-2 py-1 hover:bg-slate-50" type="submit">
                  Logout
                </button>
              </form>
            </>
          ) : (
            <Link href="/login" className="rounded-md bg-emerald-600 px-3 py-1 text-white hover:bg-emerald-700">
              Login
            </Link>
          )}
        </div>
      </nav>
    </header>
  );
}
