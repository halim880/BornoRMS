import { NextResponse } from "next/server";
import { clearCustomerSession } from "@/lib/auth";

export async function POST() {
  await clearCustomerSession();
  return NextResponse.redirect(new URL("/login", "http://localhost:3000"), { status: 303 });
}
