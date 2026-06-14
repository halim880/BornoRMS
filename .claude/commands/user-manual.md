---
description: Build/update the static HTML user-manual site in docs/manual/ from current app features
argument-hint: "[feature]  (omit = full sync, e.g. /user-manual pos)"
allowed-tools: Read, Glob, Grep, Write, Edit, Bash(git status:*)
---

# Generate / sync the BornoBit Restaurant user manual

You are building a **static HTML/CSS/JS user-manual website** in `docs/manual/` for the
**Admin** audience (the person running the whole staff console: users/roles, menu permissions,
numbering, inventory, store, accounts, reports, plus the operational pages admin oversees).

`$ARGUMENTS` = optional single feature slug.
- **Empty** → full rebuild + sync of every section.
- **A slug** (e.g. `pos`, `accounts`, `users-roles`) → only re-scan and update that one section page
  (and refresh the nav + changelog). Do NOT touch other section pages.

Plain static site — no framework, no build step. Output must open directly via `docs/manual/index.html`.

---

## Step 1 — Scan current features (ALWAYS do this first)

Read the live sources so the manual reflects reality, never stale assumptions:

- Staff/admin pages: `backend/src/BornoBit.Restaurant.Web/Components/Pages/**`
  (each folder/route = a feature group). Read the `.razor` route directive (`@page "..."`) and the
  page heading to learn the route + what the user does there.
- API endpoints (reference only): `backend/src/BornoBit.Restaurant.Api/Endpoints/**`.
- Roles: `backend/src/BornoBit.Restaurant.Domain/Identity/Roles.cs`.
- Context: `CLAUDE.md` and `README.md` (architecture, conventions, gotchas, roles, run steps).

Build an in-memory **feature list**: `{ slug, title, route, roles, purpose }`.

If `$ARGUMENTS` names a feature, filter the list to just that slug.

---

## Step 2 — Generate / update the site in `docs/manual/`

### Shared shell (create if missing, otherwise leave structure intact)
- `index.html` — landing page: intro, sidebar nav linking every section, a search box.
- `assets/styles.css` — clean responsive layout: fixed sidebar + scrollable content pane, readable
  typography, **light/dark toggle**. Reuse the `--bo-*` CSS-variable naming idea from the staff
  console (`backend/src/BornoBit.Restaurant.Web/wwwroot/app.css`) for visual consistency — copy the
  *naming convention*, not a hard dependency.
- `assets/app.js` — sidebar toggle (mobile), **client-side search/filter** over nav items, dark-mode
  persistence via `localStorage`, smooth-scroll + active-anchor highlight.
- `assets/img/` — screenshot folder. Reference `assets/img/<slug>.png` with descriptive `alt` text;
  if the file is absent, leave an inline `<!-- TODO: capture screenshot assets/img/<slug>.png -->`
  note and a styled placeholder box. Never invent binary images.

### Section pages (one per feature group)
File name = `<slug>.html`. Cover the Admin-relevant groups, e.g.:
`orders.html`, `pos.html`, `kitchen-display.html`, `dashboard.html`, `cash-counter.html`,
`inventory.html` (categories / products / tables / QR), `stock.html`, `store.html`,
`accounts.html`, `reports.html`, `users-roles.html`, `menu-permissions.html`,
`numbering.html`, `settings.html`.

Each section page contains, in order:
1. **Title** + route (e.g. `/pos`).
2. **Who can access** — the roles allowed (from the page's policy / `Roles.cs`).
3. **Purpose** — 1–2 sentences.
4. **Step-by-step how-to** — numbered, concrete actions.
5. **Screenshot** — `<img>` placeholder per the rule above.
6. **Tips / gotchas** — edge cases, warnings (pull real ones from CLAUDE.md where relevant).
7. A **"Last updated: YYYY-MM-DD"** stamp (use today's date).

Keep markup consistent across pages — same header, sidebar include, and footer on every page.

---

## Step 3 — Sync rule (keep manual aligned when features change)

This is the core reason the command exists. On every run:

1. **Diff** the scanned feature list against existing `docs/manual/*.html`.
2. **Add** a section page + nav entry for any new feature folder/route.
3. **Update** any section whose source page changed (route, heading, roles, or steps).
4. **Flag, do NOT delete**, a section whose source feature no longer exists: prepend a visible
   `⚠ TODO: source feature not found — verify if removed` banner to that page and mark it in the nav.
5. **Refresh the nav** in `index.html` and the **"Last updated"** stamp on every page you touched.
6. Append an entry to **`docs/manual/CHANGELOG.md`** (create if missing): date + bullet list of
   pages added / updated / flagged this run.

When `$ARGUMENTS` targets one feature, apply steps 3–6 to that page only (still refresh nav + changelog).

---

## Step 4 — Report

After writing, print a short summary: which files were created/updated/flagged, and remind the user to
open `docs/manual/index.html` in a browser. Note any screenshot TODOs left behind.
