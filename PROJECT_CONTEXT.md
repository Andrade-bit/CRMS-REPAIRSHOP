# CRMS — Full Project Context

This is a context dump of the CRMS project, meant to be pasted to another AI assistant so it has full background without needing to re-derive everything.

## What CRMS is

CRMS = Computer Repair Shop Management System — a multi-tenant SaaS CRM for computer repair shops, built as a school/personal project. Multiple repair shops ("tenants") each get their own isolated database; a central "Master" database handles login/accounts and tenant routing.

**Stack:**
- Backend: ASP.NET Core Web API (.NET 10), EF Core (Code-First migrations), ASP.NET Core Identity (JWT bearer auth)
- Frontend: WinForms desktop app (.NET 10, `net10.0-windows`)
- Database: SQL Server — hosted on MonsterASP.net (free/shared ASP.NET+SQL hosting) for production, LocalDB for local dev
- PDF export: QuestPDF

**Solution structure (project references):**
- `CRMS.domain` — entities only (POCOs), no dependencies
- `CRMS.infrastructure` — EF Core DbContexts, migrations, services (TenantConnectionService, PdfExportService, NotificationService)
- `CRMS.api` — ASP.NET Core Web API, controllers, Program.cs, appsettings
- `CRMS` (namespace `CRMS.winforms` / `CRMS.Winforms`) — WinForms desktop client; **oddly references `CRMS.api` directly as a project reference** (noted as unusual, never resolved/investigated)

## Multi-tenancy architecture

- **Master DB** (`db66485` on MonsterASP, cloud): holds `AspNetUsers`/`AspNetRoles` (ASP.NET Identity), `Admins`, `Superadmins`, `Companies`, `CompanyDatabases`, `TermsAgreements`, `Devices`.
- **Tenant DBs** (`db67075`=TenantA, `db67077`=TenantB, `db67078`=TenantC, all on MonsterASP): each holds that shop's own `ServiceTickets`, `Customers`, `Staff`, `RepairParts`, `Payments`, `Feedbacks`, `Notifications`, `Promotions`.
- **Tenant resolution flow**: JWT carries a `CompanyId` claim (added at login only if the account has one — Superadmin accounts don't). `TenantConnectionService.GetTenantConnectionStringAsync(companyId)` looks up `Companies.CompanyCode` for that ID in the Master DB, then reads `TenantCredentials:<CompanyCode>` from `appsettings.json` to build the tenant's SQL connection string. This is wired into DI in `CRMS.api/Program.cs` via a scoped `TenantCRMSDbContext` factory lambda that reads the `CompanyId` claim off `IHttpContextAccessor.HttpContext.User` on every request.
- **Tenant credentials are hardcoded in `appsettings.json`** (`TenantCredentials:TenantA/B/C`), not stored in a database table — a real Company row's `CompanyCode` must exactly match a `TenantCredentials` key.
- Result: each tenant is a **separate physical SQL Server database**, not row-level multi-tenancy. Full data isolation between shops.

## Roles

Four roles, each with its own WinForms shell (`*MainForm`) and its own accent color from one shared blue palette:
- **Staff** — handles walk-in tickets: Pending Customers (add/diagnose/repair), Customer History (view/pickup tracking). No archive.
- **Admin** — everything Staff does, plus Archive on Customer History, User Management (create Staff/Admin accounts), Promotions **review/approve/reject**, Dashboard, Analytics, Terms & Conditions.
- **Manager** — same day-to-day ticket work as Staff (reuses `PendingCustomersControl` via a shared `ITicketWorkController` interface so Staff and Manager use the identical UI/logic without misleading class names), plus Archive (like Admin), plus Promotions **drafting/submission**.
- **Superadmin** — platform-level: onboarding tenants, subscription status, Terms & Conditions versions, platform-wide analytics. **Note: Superadmin's tenant-creation (`SuperAdminController.CreateTenant`) is currently a fake in-memory stub (`AppDataStore.AddTenant`), not wired to the real database** — there is no real "create a new company" UI flow yet. New companies/first admin accounts have always been bootstrapped by hand (manual SQL, or the temporary `POST /api/Auth/register-temp` endpoint).

## Business flow (the actual real-world process, as implemented)

1. **Customer walks in with a device.** Staff (or Manager) uses "Add Customer" — creates a `Customer` row and a `ServiceTicket` in one step (device name, concern, estimated cost, optional repair parts/tools picked from a chip list). Ticket starts at status **`Diagnose`**.
2. **Ticket status moves forward**: `Diagnose` → `Repairing` → `Repaired` (these are the three real status values in code — `TicketStatus.Diagnose/Repairing/Repaired` in `AppDataStore.cs` — the domain entity's code comment lists a longer 5-stage lifecycle, `Received/Diagnosing/Waiting for Parts/In Repair/Ready for Pickup`, but that's aspirational/stale; only the 3-value version is actually wired up anywhere).
3. **"Pending Customers"** (Staff/Manager's main screen) shows every ticket that isn't `Repaired` yet. Staff can Edit (device/concern/cost/parts) at this stage.
4. Once a ticket reaches `Repaired`, it disappears from Pending Customers and appears in **"Customer History"** instead (the API's `GetTickets` endpoint filters on this: `status=pending` excludes `Repaired`, `status=history` includes only `Repaired`).
5. **Pickup**: in Customer History, a real "Picked Up / Not Yet" toggle sets `PickedUpAt`. This is tracked but doesn't gate anything else.
6. **Archive**: Admin (and Manager) can Archive a ticket from Customer History — sets `ArchivedAt`, a soft-hide only, never a real delete. Archived tickets are excluded from every normal view.
7. **Promotions** (separate from ticket flow): a Manager drafts a promotion (Title/Type/Message) and either saves it as Draft or submits it for approval (`Status = PendingApproval`). Admin reviews all submitted promotions and Approves or Rejects (`Status = Approved/Rejected`, `ReviewedAt` set). Nothing currently sends an approved promotion anywhere (no customer-facing delivery exists yet — approval is the last real step).
8. **Accounts**: Admin can create Staff/Manager accounts (via `UserManagementControl` → real API). Creating a new *Company* (onboarding an entirely new shop/tenant) has no real working flow yet — Superadmin's "Create Tenant" screen is a UI-only stub not connected to the database (see below); every real tenant so far has been set up by hand.

**Not yet real, despite having schema for it** (present as unused DB columns/entities, but no controller or UI code touches them at all — verified by search, zero references):
- `ServiceTicket.PaymentStatus` (defaults to `"Unpaid"`, never changed anywhere)
- `Payment`, `Feedback`, `Notification` entities — exist in the schema, completely unused

## Key domain entities

- **ServiceTicket**: `TicketId`, `CustomerId`→Customer, `StaffId`→Staff (nullable), `DeviceName`, `Concern`, `Status` ("Received"/"Diagnosing"/"Waiting for Parts"/"In Repair"/"Ready for Pickup"... in practice UI mostly uses Diagnose/Repairing/Repaired), `EstimatedCost`, `PaymentStatus` ("Unpaid"/etc, a Payment entity also exists), `CreatedAt`, `CompletedAt`, `PickedUpAt`, `ArchivedAt`, `TermsAccepted`, collections of `RepairPart`/`Payment`/`Feedback`/`Notification`.
- **Customer**: `CustomerId`, `AdminId` (FK to Admin, Master DB, plain column no nav), `FullName`, `PhoneNumber`, `Email`. **No `CreatedAt` field** (noted gap — "new customers this month" isn't derivable without adding one).
- **Company** (Master DB): `CompanyId`, `CompanyCode`, `CompanyName`, `IsActive`, `CreateAt` (note: typo, not "CreatedAt").
- **Promotion** (Tenant DB): `PromotionId`, `StaffId`→Staff (real FK — a Manager drafts it), `ReviewedByAdminId` (nullable, cross-DB plain column, **never actually populated** — known simplification), `Title`, `Type` (General/ThresholdDiscount/Referral/Seasonal), `Message`, `Status` (Draft/PendingApproval/Approved/Rejected), `CreatedAt`, `ReviewedAt`.

## Currency & locale

All money is displayed in **₱ (Philippine peso)**, formatted with the explicit custom format string `"₱#,##0.00"` everywhere (deliberately not `.ToString("C")`, which is culture-dependent and was unreliable across machines).

## Folder structure (as of now)

```
CRMS.api/Controllers/Auth/AuthController.cs
CRMS.api/Controllers/Tickets/{TicketsController,StaffTicketsController,AdminTicketsController,TicketDtos}.cs
CRMS.api/Controllers/Promotions/{PromotionsController,ManagerPromotionsController,AdminPromotionsController,PromotionDtos}.cs

CRMS/NavControl/AppDataStore.cs               (legacy in-memory store, partially dead — still used by Superadmin's fake tenant stub)
CRMS/NavControl/ITicketWorkController.cs       (shared interface: Staff + Manager controllers both implement it)
CRMS/NavControl/NavConStaff/StaffController.cs
CRMS/NavControl/NavConAdmin/AdminController.cs
CRMS/NavControl/NavConSuperAdmin/SuperAdminController.cs
CRMS/NavControl/NavConManager/ManagerController.cs
CRMS/Services/{NotificationService,PdfExportService}.cs
CRMS/View/Admin/*.cs        (+PromotionsReviewControl.cs, DashboardControl.cs is still a placeholder)
CRMS/View/StaffView/*.cs    (+EditTicketDialog.cs)
CRMS/View/ManagerView/{ManagerMainForm,PromotionsComposerControl}.cs
CRMS/View/SuperAdmin/*.cs
CRMS/View/Shared/*.cs
CRMS/View/Login.cs
CRMS/Program.cs
```

Naming conventions were deliberately chosen over several rounds: WinForms "controllers" live under `NavControl/` (not `Controllers/`, to avoid confusion with the real MVC-style API controllers) split into `NavConStaff/NavConAdmin/NavConManager/NavConSuperAdmin`; all view/design code lives under `View/`; API controllers are split into per-resource subfolders.

**Controller-splitting pattern used for both Tickets and Promotions**: split by *resource*, not by *role* — three controller classes sharing one `[Route]` prefix (`api/tickets` / `api/promotions`): one Staff/Manager-only, one Admin-only, one shared. Verified safe (no ASP.NET Core route ambiguity) via `dotnet run --no-build` smoke tests after each split.

## The lab / academic context

This whole project (including a **separate** ERP demo project by the professor, using near-identical architecture — `ERP.domain`/`ERP.infrastructure`/`ERP.api`, `Company`/`CompanyDatabase`/`MasterErpDbContext`/`TenantErpDbContext`, MonsterASP hosting) is the reference pattern this course teaches: **Master DB + per-tenant DB + a resolver service that maps a company ID to a tenant connection string**, built up incrementally through NuGet installs, `Add-Migration`/`Update-Database` in Package Manager Console, and manual testing via `.http` files or SSMS. CRMS already independently implements this same pattern (just with its own naming and a more built-out WinForms client instead of `.http` file testing).

## What's been built this session (chronological highlights)

1. Fixed missing Master DB tables (Admins/Superadmins/TermsAgreements) and backfilled a pre-existing Staff account across tenants — root cause was "migration history says applied but schema never actually ran," fixed by hand-extracting DDL from the migration's `Up()` and manually inserting into `__EFMigrationsHistory`.
2. Redesigned Login screen and Staff dashboard (Pending Customers / Customer History) to a professional look using a defined blue palette (`#0EA5E9`/`#0A74A3`/`#08638C`), always previewed as an Artifact first and approved before implementation — this was a hard requirement stated repeatedly by the user.
3. Added Update/Archive to tickets (Archive later moved from Staff to Admin-only, and Manager also got Archive), switched all currency to ₱, added real `PickedUpAt` pickup tracking.
4. Multiple rounds of folder reorganization (all via `git mv` to preserve history), each explicitly confirmed via a naming proposal before executing.
5. Split `TicketsController` into Staff/Admin/shared pieces; built a full **Manager role** (UI shell + ticket work + a **Promotions draft→approve workflow**: Manager drafts a promotion, Admin approves/rejects). Always previewed as an Artifact before being built for real.
6. Designed (Artifact preview only, never built into the real app) a professional Admin Dashboard — first a KPI-tile-heavy version, then reworked to be **distribution/chart-first** (revenue trend line chart, ticket pipeline stage distribution, staff workload bars, payment status split) after the user pointed out real CRM dashboards emphasize data distribution over raw numbers.
7. **Performance fix**: found that every tenant-scoped API request paid for an extra Master DB round-trip (`TenantConnectionService` re-resolving the CompanyId→connection-string mapping on every call) — fixed by caching it in `IMemoryCache` for 30 minutes.
8. **Resilience fix**: MonsterASP's free-tier SQL hosting has shown real, repeated *transient* connectivity flakiness this session (both DbContexts would throw and even crash the whole app on startup) — fixed by adding EF Core's `EnableRetryOnFailure` to both `MasterCRMSDbContext` and `TenantCRMSDbContext`.
9. Diagnosed a MonsterASP outage live: confirmed via SSMS + a raw `Test-NetConnection` TCP test that the databases were genuinely unreachable (not a CRMS bug); later found MonsterASP's dashboard has two separate tabs per database — **"Local access for websites"** (only reachable from apps hosted *on* MonsterASP itself) vs **"Remote access for SSMS"** (the one needed for anything external, including your own PC) — the user had to enable/use the latter. The database has continued to be genuinely flaky (up one minute, down the next) even after that.
10. Added a **Logout → back to Login** flow (previously logging out just closed the whole app) — each shell (`StaffMainForm`/`AdminMainForm`/`ManagerMainForm`/`SuperAdminMainForm`) now has a `LoggedOut` flag set only on confirmed Logout (never on the window's X button), and `Program.cs`'s `Main()` loops: Login → shell → if `LoggedOut`, loop back to Login; otherwise exit.
11. **Local-dev database setup** (just completed, in response to the MonsterASP flakiness): rather than building full offline-first sync (discussed and explicitly deferred — see below), set up CRMS to be able to run **entirely against local SQL Server (LocalDB)** during development, switching automatically via `appsettings.Development.json`:
    - `TenantConnectionService` now supports **Windows-integrated auth** (`Trusted_Connection=True`) for local databases when a tenant's `UserId` config is blank, alongside the existing cloud SQL-login style (untouched).
    - Reused an existing (but outdated-looking, actually-already-current) local database `CRMS_MasterCRMS` on `(localdb)\MSSQLLocalDB`, and created a new `CRMS_TenantA_Local` database via `dotnet ef database update`.
    - Seeded one test `Companies` row (`CompanyCode = "TenantA"`) and one real Admin login (username `localadmin`, password `Local@1234`) via the temporary `POST /api/Auth/register-temp` endpoint plus a hand-inserted `Admins` row.
    - Verified end-to-end locally: login → JWT → tenant data resolution, all working purely against LocalDB, zero cloud dependency.
    - **Gotcha hit and fixed**: ASP.NET Core config layering means an *omitted* key in `appsettings.Development.json` does NOT clear a value set in the base `appsettings.json` — it falls through. Initially only overrode `ServerName`/`DatabaseName` for the local tenant and left `UserId`/`Password` unset, which meant it silently tried to log into LocalDB using the **cloud** SQL login (`db67075`) and failed. Fixed by explicitly setting `"UserId": ""` and `"Password": ""` in the Development override too.
    - Local DB files physically live at `C:\Users\Ady\CRMS_MasterCRMS.mdf` and `C:\Users\Ady\CRMS_TenantA_Local.mdf` (+ `.ldf` logs).

## Explicitly deferred / not built

- **Full offline-first architecture** (SQLite/local DB + sync engine + offline login + auto-sync on reconnect) — discussed at length, user said "maybe later." Key open design point if revisited: ticket/customer data can sync with a simple last-write-wins rule, but **account creation cannot** — usernames/emails are globally unique in the Master DB (ASP.NET Identity), so offline account creation would need to be a "queued, not-yet-confirmed" request finalized only once back online, not an instantly-working offline account.
- `[Authorize(Roles = "...")]` role-locking on API endpoints — currently just `[Authorize]` (any authenticated user), not role-restricted. Flagged as a real gap, not yet done.
- `Promotion.ReviewedByAdminId` — schema exists, never populated (no UI needs it yet).
- Turning the WinForms app into a distributable installer (`dotnet publish` / ClickOnce) — discussed, one test self-contained publish was done pointed at `localhost:5029`, not yet pointed at a real public API address for actual distribution to other people/PCs.

## Known issues / things to watch

- **`appsettings.json` has real plaintext database passwords and the JWT signing key committed** — flagged once as a security concern, not yet addressed. Worth checking it's excluded from git, or moving secrets to user-secrets/environment variables.
- MonsterASP's free-tier database has shown **repeated, genuine transient outages** this session, confirmed via multiple independent methods (SSMS, raw TCP tests, MonsterASP's own dashboard). `EnableRetryOnFailure` mitigates but doesn't eliminate this for the live app; local dev now sidesteps it entirely.
- `CRMS.winforms` project references `CRMS.api` directly — unusual, never investigated/explained.
- Superadmin's "Create Tenant" flow is fake (in-memory only) — there is currently no real way to onboard a brand-new company through the app's own UI; it's always been done by hand.

## Working style / preferences established this session

- **Always show a design/UX change as an Artifact HTML preview and get explicit approval before writing real code** — stated and enforced repeatedly.
- **Ask a clarifying question when a decision is real and hard to reverse** (e.g. Update vs Delete semantics, Archive-vs-real-delete, local-DB technology choice) rather than assuming.
- **Strict scope discipline** — changes stay confined to exactly what's asked; shared WinForms controls get *opt-in* overrides (extra properties with sensible defaults) rather than changing shared behavior for every caller.
- Prefers direct, concrete explanations grounded in the actual code (file:line references) over generic advice.
- Currency, folder names, and other naming decisions are always run past the user first when there's more than one reasonable option.

## What the user is trying to achieve right now

Making local development fully self-sufficient and fast, independent of MonsterASP's flaky free-tier hosting, while keeping the real cloud setup for production untouched. This local-dev setup (Section 11 above) is the immediate deliverable. Next likely steps, not yet started: actually using the app day-to-day against the local database to build out more realistic test data, and revisiting the deferred items above (role-based endpoint locking, offline-first, real tenant-onboarding flow) as time allows.
