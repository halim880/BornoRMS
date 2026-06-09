import { NextResponse } from "next/server";
import { setCustomerSession, type CustomerSession } from "@/lib/auth";

const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:5000";

export async function POST(req: Request) {
  const body = await req.json();
  const res = await fetch(`${BACKEND_URL}/auth/verify-otp`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ phone: body.phone, code: body.code }),
  });

  if (!res.ok) {
    let message = "Invalid or expired code.";
    try {
      const data = await res.json();
      if (data?.message) message = data.message;
    } catch {}
    return NextResponse.json({ message }, { status: res.status });
  }

  const session = (await res.json()) as CustomerSession;
  await setCustomerSession(session);

  return NextResponse.json({ ok: true });
}
