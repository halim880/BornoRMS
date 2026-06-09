import type { Metadata } from "next";
import "./globals.css";
import NavBar from "@/components/NavBar";
import { getCustomerSession } from "@/lib/auth";

export const metadata: Metadata = {
  title: "BornoBit Restaurant",
  description: "Order food from BornoBit",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const session = await getCustomerSession();
  return (
    <html lang="en">
      <body className="min-h-screen">
        <NavBar phone={session?.customer.phone ?? null} />
        <main className="mx-auto max-w-4xl px-4 py-6">{children}</main>
      </body>
    </html>
  );
}
