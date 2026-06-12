import { getCustomerToken } from "@/lib/auth";

const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:5000";

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

type FetchOptions = RequestInit & { authenticated?: boolean };

export async function apiFetch<T>(path: string, opts: FetchOptions = {}): Promise<T> {
  const headers = new Headers(opts.headers);
  headers.set("Accept", "application/json");
  if (opts.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (opts.authenticated !== false) {
    const token = await getCustomerToken();
    if (token) headers.set("Authorization", `Bearer ${token}`);
  }

  const res = await fetch(`${BACKEND_URL}${path}`, {
    ...opts,
    headers,
    cache: "no-store",
  });

  if (!res.ok) {
    let message = `Request failed (${res.status})`;
    try {
      const data = await res.json();
      if (data?.message) message = data.message;
    } catch {}
    throw new ApiError(res.status, message);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export function getBackendUrl(): string {
  return BACKEND_URL;
}

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type MenuVariant = {
  id: string;
  name: string;
  price: number;
  displayOrder: number;
};

export type MenuItem = {
  id: string;
  code: string;
  name: string;
  banglaName: string | null;
  description: string | null;
  price: number;
  currency: string;
  imageUrl: string | null;
  displayOrder: number;
  variants: MenuVariant[];
};

export type MenuCategory = {
  id: string;
  name: string;
  description: string | null;
  displayOrder: number;
  items: MenuItem[];
};

export type TableDto = {
  id: string;
  tableNumber: string;
  capacity: number;
};

export type OrderType = "DineIn" | "Takeaway" | "Delivery" | "Collection" | "Waiting";

export type OrderStatus =
  | "Placed"
  | "Confirmed"
  | "Preparing"
  | "Ready"
  | "Served"
  | "Completed"
  | "Cancelled";

export type PaymentMethod = "Cash" | "Card" | "Mobile";

export type OrderListItem = {
  id: string;
  orderNumber: string;
  customerId: string;
  customerPhone: string;
  customerName: string | null;
  tableNumber: string | null;
  orderType: OrderType;
  status: OrderStatus;
  orderedAtUtc: string;
  currency: string;
  itemCount: number;
  subtotal: number;
  discountAmount: number;
  total: number;
  isPaid: boolean;
};

export type OrderLine = {
  menuItemId: string;
  variantId: string | null;
  code: string;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
};

export type OrderDetail = {
  id: string;
  orderNumber: string;
  customerId: string;
  customerPhone: string;
  customerName: string | null;
  tableNumber: string | null;
  orderType: OrderType;
  status: OrderStatus;
  orderedAtUtc: string;
  currency: string;
  notes: string | null;
  subtotal: number;
  discountAmount: number;
  discountReason: string | null;
  grandTotal: number;
  total: number;
  isPaid: boolean;
  paymentMethod: PaymentMethod | null;
  amountTendered: number | null;
  changeGiven: number | null;
  lines: OrderLine[];
};
