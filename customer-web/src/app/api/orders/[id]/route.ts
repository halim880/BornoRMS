import { NextResponse } from "next/server";
import { apiFetch, ApiError, type OrderDetail } from "@/lib/api";

// Lets the order-tracking client component poll status + ETA while keeping the JWT server-side.
export async function GET(_req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  try {
    const order = await apiFetch<OrderDetail>(`/orders/${id}`);
    return NextResponse.json(order);
  } catch (e) {
    const err = e as ApiError;
    return NextResponse.json({ message: err.message }, { status: err.status || 500 });
  }
}
