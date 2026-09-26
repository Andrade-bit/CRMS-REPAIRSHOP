# CRMS — Admin Side: Full Context

## Purpose of the Admin role

Admin is the **shop owner/oversight role** for one tenant (one repair shop). Where Staff and Manager are doing the day-to-day repair work, Admin's job is running the shop: managing who has accounts, seeing the shop's overall health (revenue, workload, payments), keeping a permanent record of finished work, and deciding what promotions actually go out. Admin does **not** do walk-in ticket intake or diagnosis — that's Staff/Manager's job.

Admin is scoped to their own tenant only — they see and manage only their own shop's data (own Companies row, own tenant database), never other tenants. That's Superadmin's job (platform-level, across all tenants).

## Admin's pages (sidebar), what's real vs placeholder

Admin has 6 sidebar pages. Three are fully real and wired to the database; two are still literal placeholders.

| Page | Status | What it does |
|---|---|---|
| **Dashboard** | ✅ Real | Shop-wide KPIs and charts — see below |
| **User Management** | ✅ Real | Create/list Staff, Manager, Admin accounts for this tenant |
| **Customer History** | ✅ Real | Every completed/cancelled/unrepairable ticket, pickup + payment tracking, Archive |
| **Promotions** | ✅ Real | Approve/reject promotions Managers have submitted |
| **Reports** | ⚠️ Placeholder | `AnalyticReportControl.cs` just shows "Reporting content is coming soon." — nothing behind it |
| **Settings** | ⚠️ Placeholder | Same — a static "coming soon" card, no real settings exist yet |

## Dashboard (most recently built)

Backed by a real `GET /api/dashboard` endpoint — every number is a live aggregate over this tenant's own `ServiceTickets`/`Promotions`, computed fresh on every page load. Nothing here is sample data.

- **KPI strip** (clickable — each opens a popup with the actual rows behind the number):
  - Revenue (MTD) — sum of `EstimatedCost` for tickets marked `Repaired` this calendar month
  - Ready, not picked up — `Repaired` tickets with `PickedUpAt` still null
  - Unpaid tickets — count where `PaymentStatus == "Unpaid"` (currently ≈ all tickets, since payment status has only just been wired up — see Known Gaps)
  - Promotions pending — count of `Status == "PendingApproval"`
- **Attention banner** — appears only when promotions are actually waiting, links straight to the Promotions page
- **Revenue trend** — a real 14-day line/area chart, day-by-day sum of completed-ticket value
- **Payment status split** — Paid/Partial/Unpaid as a proportion bar, real amounts
- **Ticket pipeline** — Diagnose / Awaiting Approval / Repairing counts, shop-wide (all staff combined — this is the thing that makes Admin's dashboard different from Manager's, which is personal-only)
- **Staff workload** — active ticket count per staff member, shop-wide

The visual design was explicitly modeled on a GDI+ mockup the user provided and hand-translated to pull real data instead of the sample numbers in that mockup.

## User Management

Lets Admin create new Staff/Manager (and Admin) accounts for their tenant, and see the list of existing accounts. Backed by the same `POST /api/Auth/register-temp` endpoint used throughout this project to bootstrap accounts — explicitly marked `TEMPORARY` in the API code, meant to be replaced with a real "Admin creates account" flow that requires the caller to actually be an authenticated Admin (not currently enforced server-side).

## Customer History

Shared control (`CustomerHistoryControl`) also used by Staff and Manager, but **only Admin's instance gets `allowArchive: true`** — Archive was deliberately moved off Staff's page and onto Admin-only earlier this session, per explicit user request ("so the admin only can archive").

Shows every ticket that reached a terminal status: `Repaired`, `Cancelled`, or `Unrepairable` (added later — originally only `Repaired` counted as "history"). Real columns:
- **Payment** — Unpaid/Partial/Paid dropdown, persists via `PUT /api/tickets/{id}/payment`
- **Picked Up** — Not Yet/Picked Up dropdown, persists via `PUT /api/tickets/{id}/pickup`
- **Archive** — Admin-only button, soft-hides the ticket (`ArchivedAt` set, never a real delete)
- **Download PDF** — per-ticket export via `PdfExportService`

## Promotions (review)

Admin's Promotions page (`PromotionsReviewControl`) lists every promotion submitted by any Manager in the tenant, with Approve/Reject buttons — only enabled while a promotion is still `PendingApproval`. Backed by `PUT /api/promotions/{id}/approve` and `/reject`.

**What happens after approval: nothing, yet.** Approving a promotion just changes its `Status` to `Approved` — there's still no automatic customer delivery (no email/SMS). The one real consequence of approval that *does* exist: Staff/Manager can now pick that promotion from the "Apply Promotion" dropdown when creating or editing a ticket (added most recently), which just records that the customer used it — the discount itself is still applied manually by Staff reading the promotion's text.

## Admin-specific backend surface

- `GET /api/dashboard` — the aggregates above
- `GET /api/tickets` (shared) — Admin's Customer History reads this with `status=history`
- `PUT /api/tickets/{id}/archive` — Admin-only action (`AdminTicketsController`)
- `PUT /api/tickets/{id}/pickup`, `PUT /api/tickets/{id}/payment` — shared with Staff/Manager
- `GET /api/promotions` (mine=false) — every promotion in the tenant
- `PUT /api/promotions/{id}/approve`, `/reject`
- `POST /api/Auth/register-temp`, `GET /api/Auth/users` — account management (shared temporary endpoints, not Admin-exclusive server-side)

**Important gap, called out before and still true:** none of these endpoints actually check the caller's *role* server-side (`[Authorize(Roles = "Admin")]` is not used anywhere) — anyone with a valid token for this tenant can currently call Admin-only actions like Archive or Approve/Reject directly via the API, even though the WinForms UI only shows those buttons to Admin. The UI hides it; the server doesn't enforce it.

## Known gaps specific to Admin

1. **Reports and Settings are empty placeholders** — no work has been done on either yet.
2. **No real "create a new tenant/company" flow** — that's Superadmin's page, and it's currently a fake in-memory stub, not real. Admin accounts for a brand-new shop have always been bootstrapped by hand.
3. **No server-side role enforcement** on Admin-only actions (see above).
4. **Payment status is very new** — every ticket defaults to Unpaid and almost none have been changed yet, so the Dashboard's payment chart and "Unpaid tickets" KPI will look uninteresting (near-100% unpaid) until Staff/Admin actually start using the new Payment dropdown in Customer History.
5. **Promotions have no real customer delivery** — approval is still the last automated step.

## What Admin is trying to achieve, in one line

Give the shop owner one place to see the whole shop's health at a glance (revenue, workload, payments, pending decisions), keep a clean permanent record of every finished repair, control who has system access, and gate what promotional offers actually go live — without doing any of the hands-on repair work themselves.
