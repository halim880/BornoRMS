import { cookies } from "next/headers";

const COOKIE_NAME = process.env.PORTAL_COOKIE_NAME || "bb_customer_token";

export type CustomerSession = {
  accessToken: string;
  expiresAtUtc: string;
  customer: {
    customerId: string;
    phone: string;
    fullName: string | null;
  };
};

export async function getCustomerToken(): Promise<string | null> {
  const session = await getCustomerSession();
  return session?.accessToken ?? null;
}

export async function getCustomerSession(): Promise<CustomerSession | null> {
  const store = await cookies();
  const c = store.get(COOKIE_NAME);
  if (!c?.value) return null;
  try {
    const session = JSON.parse(c.value) as CustomerSession;
    if (Date.parse(session.expiresAtUtc) <= Date.now()) return null;
    return session;
  } catch {
    return null;
  }
}

export async function setCustomerSession(session: CustomerSession) {
  const store = await cookies();
  const expires = new Date(session.expiresAtUtc);
  store.set(COOKIE_NAME, JSON.stringify(session), {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires,
  });
}

export async function clearCustomerSession() {
  const store = await cookies();
  store.delete(COOKIE_NAME);
}
