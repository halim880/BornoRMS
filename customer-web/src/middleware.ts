import { NextResponse, type NextRequest } from "next/server";

const COOKIE_NAME = process.env.PORTAL_COOKIE_NAME || "bb_customer_token";

const PROTECTED_PREFIXES = ["/checkout", "/orders"];

export function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;
  if (!PROTECTED_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`))) {
    return NextResponse.next();
  }

  const cookie = req.cookies.get(COOKIE_NAME);
  if (cookie?.value) {
    try {
      const session = JSON.parse(cookie.value) as { expiresAtUtc?: string };
      if (session.expiresAtUtc && Date.parse(session.expiresAtUtc) > Date.now()) {
        return NextResponse.next();
      }
    } catch {}
  }

  const url = req.nextUrl.clone();
  url.pathname = "/login";
  url.searchParams.set("from", pathname);
  return NextResponse.redirect(url);
}

export const config = {
  matcher: ["/checkout/:path*", "/orders/:path*"],
};
