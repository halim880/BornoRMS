import { NextResponse } from "next/server";
import { apiFetch, ApiError } from "@/lib/api";

// Proxies a table service request (call waiter / bill / water / tissue) to the API, keeping the
// customer JWT server-side. Surfaces in real time on the staff Operations Dashboard.
export async function POST(req: Request) {
  const body = await req.json();
  try {
    const result = await apiFetch<{ id: string }>("/customer/requests", {
      method: "POST",
      body: JSON.stringify({
        tableId: body.tableId,
        type: body.type,
        note: body.note ?? null,
      }),
    });
    return NextResponse.json(result, { status: 201 });
  } catch (e) {
    const err = e as ApiError;
    return NextResponse.json({ message: err.message }, { status: err.status || 500 });
  }
}
