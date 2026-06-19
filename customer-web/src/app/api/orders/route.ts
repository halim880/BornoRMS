import { NextResponse } from "next/server";
import { apiFetch, ApiError } from "@/lib/api";

export async function POST(req: Request) {
  const body = await req.json();
  try {
    const result = await apiFetch<{ orderId: string; orderNumber: string; total: number; currency: string }>(
      "/orders",
      {
        method: "POST",
        body: JSON.stringify({
          tableId: body.tableId ?? null,
          type: body.type,
          notes: body.notes ?? null,
          deliveryAddress: body.deliveryAddress ?? null,
          contactPhone: body.contactPhone ?? null,
          lines: body.lines,
        }),
      }
    );
    return NextResponse.json(result, { status: 201 });
  } catch (e) {
    const err = e as ApiError;
    return NextResponse.json({ message: err.message }, { status: err.status || 500 });
  }
}
