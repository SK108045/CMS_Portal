# Mission School · Stewardship

A working local school financial accountability application: React + TypeScript + Vite, ASP.NET Core 8 Web API, EF Core and **one PostgreSQL database**. No Docker, cloud service or internet connection is required at runtime. The frontend can run separately during development or be served by ASP.NET in production. Currency is KES throughout.

## Repository

```text
school-management/
  frontend/
    src/{components,pages}/   reusable tables, dialogs, forms and portal pages
    src/{api,schema,App}.tsx  API client, form definitions, portal navigation
    public/fonts/            locally served fonts and licence
    tests/browser/           Playwright browser checks
  backend/
    Controllers/             authentication, records, workflows, evidence, insights
    Services/                access control, ledger and financial rules
    Models/                  relational entities and safe input binding
    Data/Migrations/         versioned EF Core PostgreSQL migrations
    Data/Seed.cs             Kenyan mission-school demo records
  database/setup.sql
  storage/{receipts,invoices,quotations,delivery-notes,payment-proofs,donations,other}/
  scripts/                   cross-platform build, optional Ubuntu PostgreSQL helper
  tests/                     xUnit financial tests and live acceptance suite
  MissionSchool.sln
```

## This Ubuntu workspace

A clean demo is already configured in the ignored `backend/appsettings.Development.json`, using the isolated PostgreSQL cluster at `$HOME/.local/share/mission-school-postgres` (TCP port 55432, database `mission_school_demo`). Run `./scripts/start-ubuntu-demo.sh` to start the configured cluster and backend, then `npm run dev` in frontend. The system PostgreSQL cluster on 5432 was not changed. Fresh clones should follow the setup instructions below.

## Requirements

Install **.NET 8 SDK**, **Node.js 20.19+ or 22 LTS**, and **PostgreSQL 16+**. Use the same major PostgreSQL version when restoring backups. Windows needs the PostgreSQL command-line tools on PATH; use PowerShell. Ubuntu normally installs PostgreSQL using `sudo apt install postgresql postgresql-contrib`. Install the .NET SDK using Microsoft's instructions for your distribution. Normal operation uses no Docker.

## Database setup (Ubuntu)

```bash
sudo -u postgres psql
```

Inside psql:

```sql
CREATE ROLE mission_app LOGIN;
\password mission_app
CREATE DATABASE mission_school OWNER mission_app;
\q
```

Choose a password; it is never stored in this repository. Alternatively use `database/setup.sql` through an administrator psql session. If you cannot administer the system cluster, `scripts/dev-postgres.sh` creates an optional isolated development cluster on port 55432 under your home directory, with password authentication. It asks you to choose its password. Never expose a development cluster to the LAN.

## Local day-to-day development

After the initial database/setup is complete, normal development is intentionally one server command. ASP.NET Debug builds automatically rebuild the React/Vite frontend and copy it into `backend/wwwroot` before starting.

```bash
# after pulling changes
cd school-management/backend
dotnet run
```

Open **http://127.0.0.1:5000**. You do not need to manually run `npm run build`, copy `dist`, or maintain a second Vite terminal just to see pulled frontend changes. If `frontend/node_modules` does not exist, the first run automatically performs `npm ci`.

## Ubuntu development

From `school-management/`:

```bash
export ConnectionStrings__School='Host=localhost;Port=5432;Database=mission_school;Username=mission_app;Password=YOUR_PASSWORD'
dotnet tool restore
dotnet restore
cd backend
dotnet ef database update
dotnet run -- --seed
ASPNETCORE_URLS=http://127.0.0.1:5000 dotnet run
```

In a second terminal:

```bash
cd school-management/frontend
npm ci
npm run dev
```

Open **http://127.0.0.1:5173**. Vite proxies `/api` to port 5000, so cookies and anti-forgery tokens are same-origin. The seed command also applies migrations and exits. It is idempotent: an installation with users is not seeded again. For subsequent runs, start the API and frontend; do not reseed.

You can use .NET user secrets instead of environment variables:

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set 'ConnectionStrings:School' 'Host=localhost;Database=mission_school;Username=mission_app;Password=YOUR_PASSWORD'
```

`backend/appsettings.Example.json` documents the settings. `appsettings.Development.json` and `appsettings.Production.json` are ignored by Git. Do not place real credentials in tracked `appsettings.json`.

## Windows development

Install the prerequisites, start the PostgreSQL Windows service, and open SQL Shell (psql) as the PostgreSQL administrator. Run the same role/database SQL shown above. From PowerShell in `school-management`:

```powershell
$env:ConnectionStrings__School='Host=localhost;Port=5432;Database=mission_school;Username=mission_app;Password=YOUR_PASSWORD'
dotnet tool restore
dotnet restore
Set-Location backend
dotnet ef database update
dotnet run -- --seed
$env:ASPNETCORE_URLS='http://127.0.0.1:5000'
dotnet run
```

Second PowerShell terminal:

```powershell
Set-Location school-management/frontend
npm ci
npm run dev
```

Use http://127.0.0.1:5173. File metadata uses portable relative paths with generated filenames; no Linux absolute paths are saved in PostgreSQL.

## Demo credentials — development only

| Role | Email | Password |
|---|---|---|
| Admin | admin@missionschool.local | `Admin123!` |
| Kitchen | kitchen@missionschool.local | `SchoolDemo123!` |
| Maintenance | maintenance@missionschool.local | `SchoolDemo123!` |
| ICT | ict@missionschool.local | `SchoolDemo123!` |
| Stores | stores@missionschool.local | `SchoolDemo123!` |

The Admin demo password is explicitly requested by the project specification. New/reset passwords require at least 10 characters, an uppercase letter, a digit and a symbol. Only Admin creates users; there is no public registration. Edit a user and enter a temporary password to reset access. Leaving this field blank preserves the existing password. Changing a department, role, status or password invalidates existing sessions. Five failed logins lock an account for 15 minutes.

For a **clean real installation**, use a new empty database and `dotnet run -- --create-admin` instead of `--seed`, with `Admin__Email` and `Admin__Password` environment variables set to your own credentials. Bootstrap refuses to run if users already exist. Then create the school's real departments, categories, budgets and users in the portal. Demo records must never be mixed into real accounts.

## Main workflows

- **Requests & quick approvals:** New request → add items → save draft → **Submit for approval** directly in the request table. Admin Home shows **Awaiting your approval** above all charts and financial summaries, with department, requester, amount, supplier and budget availability. Click the green **Approve** button to commit an ordinary submitted request immediately. Missing suppliers, self-approval or budget overrides open a short review form; overrides require a reason. **Review / adjust**, **Return** and **Reject** are available on every pending card. The dedicated **Approvals** inbox supports search and pagination. Drafts are clearly identified as not yet submitted. Approval updates commitments, never spending; existing payment and audit workflows remain authoritative.
- **Delivery/payment:** Open approved request → Ordered → Partially Received / Received. Record inventory receipts under Inventory → Stock Movements and link the request. Under Payments → Invoices, record the supplier's invoice and attach evidence. Under Payments, record each actual payment with the matching invoice and a unique reference. Partial payments reduce commitment only by the amount paid. Choose **Final payment** when all goods are received and every recorded invoice is settled; this releases any unused approval. Open the request to complete it. Its history, invoices and payments remain visible.
- **Expenses:** Admin or Finance records direct authorised payments against a department allocation, with supplier/person, payment reference and responsible person. Attach receipts from Open → Supporting evidence. The budget is charged immediately because this records an actual payment.
- **Petty cash:** Staff use Request funds / decisions to submit a funding request; Admin or Finance approves with comments/reference, posting the replenishment atomically, or rejects with a reason. Admin or Finance creates a departmental account, then an Opening movement. Only Admin or Finance may post Expense, Withdrawal, Return, Opening or Replenishment movements; departmental users request funding and attach permitted evidence. Expense requires a department budget and affects spending. Replenishment restores the cash balance without charging the budget again. Withdrawal records accountable cash issued, not an expense. To settle it, create an Expense and select the original withdrawal; this charges the budget without debiting petty cash twice. Return unused cash using a Return movement linked to that withdrawal. Settlements cannot exceed the outstanding advance. Account details show unretired advances separately. Attach receipts to spending records. No cash account can be overdrawn.
- **Donations:** Record Donation, Grant or Other Income, with source, donor and restricted purpose. For a non-cash donation first create the inventory item, choose Non-cash Donation and its quantity. A stock movement is posted in the same database transaction. Restricted purposes are highlighted in record detail.
- **Inventory/assets:** Record receipts, issues, returns, damage, loss, adjustments or transfers. Quantities come only from movements. Positive Adjusted movements add units; use Damaged/Lost for reductions. Transfers create a destination stock item and corresponding receipt. Open a Permanent Item and assign one unit to the asset register; this removes it from stock and records a linked asset. Each unit can have its own serial number after editing. Maintenance expenses linked to the asset appear in its lifetime cost.
- **Evidence:** Open a request, invoice, payment, expense, petty cash movement, donation, inventory item or asset; attach a PDF, PNG or JPEG, up to 10 MB. Extensions and signatures are validated. Evidence is stored outside the web root. Downloads require login and correct department access. Missing evidence and missing physical files are flagged.
- **Reports:** Choose a report and date/department/category/supplier/status/search filters. CSV uses the same server-side filters. Print / Save PDF uses the browser print dialog. Tables support their own search, department/status/supplier filters, sorting and pagination.

## Financial and security model

Money uses PostgreSQL `numeric(18,2)` and C# `decimal`. The server calculates item totals, commitments, spending, cash balances and stock quantities. Available budget = original allocation + adjustments − unpaid approved commitments − payments − direct expenses − petty cash expenses. Rejections/cancellations release commitments. Completed orders retain their payment history. Budget adjustments and posted financial entries are immutable through the API; corrections require explicit adjustment records. Admin can exceed availability with a recorded reason. Actual payments may differ from approval, with an explanation for excess payments. Duplicate method/reference pairs and duplicate supplier/invoice numbers are rejected.

Financial mutations use PostgreSQL serializable transactions. Concurrent conflicts return an actionable 409 response. Foreign keys prevent orphaned records; ledger entries are not deleted through the UI. Audit records are append-only through the application. The database administrator remains a trusted operator with direct database access.

Passwords use ASP.NET Core's password hasher. Authentication uses encrypted HTTP-only SameSite=Strict cookies and anti-forgery tokens on every mutation, including login. The API checks department/ADMIN access independently of the frontend. Navigation objects from input are discarded before EF binding. Six centrally mapped roles now separate finance, stores, departmental work and read-only oversight. Final financial approval and budget overrides remain Owner/Admin-only; financial posting belongs to Finance and physical stock management to Storekeeper. Staff submit their own requests and funding requests, while Department Heads collaborate across their department. Users see department-scoped activity; central inventory is shared only when no department owns the item. No external email, payment provider or cloud dependency exists.

## Build and test

```bash
# From school-management
 dotnet build
 dotnet test
 cd frontend
 npm ci
 npm run build
 npx playwright install chromium
 npx playwright test
```

For browser tests, run the API on 5000 and Vite on 5173 with seeded data. Playwright uses its downloaded Chromium by default. To use an installed browser, set `SCHOOL_CHROME` to its executable path (for example `/usr/bin/google-chrome` on this Ubuntu machine).

For the live acceptance suite, start the API against a **disposable seeded test database**, then run:

```bash
python3 tests/acceptance.py
# Windows: py tests/acceptance.py
```

It creates uniquely named data and exercises all twelve requested workflows, exact money/quantity assertions, CSV results, uploads/download permissions, forbidden Admin access and negative balance checks. It intentionally does not delete audit/financial records. Use a test database, not real school data. `SCHOOL_URL` optionally changes the API base URL (default http://127.0.0.1:5000).

## Production build and Windows publish

From `school-management`, after `npm ci` in frontend:

```bash
node scripts/build.mjs -r win-x64 --self-contained true -o publish/windows
```

This builds React, copies all frontend assets into `backend/wwwroot`, and calls:

```bash
dotnet publish backend/School.Api.csproj -c Release -r win-x64 --self-contained true -o publish/windows
```

The equivalent framework-dependent local build is `node scripts/build.mjs -o publish/local`. The backend serves the real React production assets and API from one origin. No Vite or Node process is needed on the school server. The Windows publish contains `School.Api.exe` plus supporting files; copy the **whole output directory**, not just the exe.

On the Windows school server, install/start PostgreSQL, create the database and a dedicated login, and configure connection credentials. Set an absolute storage root outside the publish directory; database document paths remain relative:

```powershell
$env:ConnectionStrings__School='Host=localhost;Database=mission_school;Username=mission_app;Password=YOUR_PASSWORD'
$env:StoragePath='C:\MissionSchool\storage'
$env:ASPNETCORE_URLS='http://0.0.0.0:5000'
Set-Location C:\MissionSchool\app
.\School.Api.exe --migrate
# On a fresh, non-demo install: set Admin__Email/Admin__Password then --create-admin
.\School.Api.exe
```

Allow inbound port 5000 only for the school's private network, keep PostgreSQL local, and resolve `school-server` to that machine. Other computers can use `http://school-server:5000`. For real financial use, configure a trusted LAN HTTPS certificate and HTTPS listener; cookies follow the request scheme. Use a Windows service wrapper or a scheduled task under a dedicated Windows account, with working directory set to the application folder. Preserve its ASP.NET Data Protection key directory across updates. Do not run the service as an administrator.

## Backups and operation

Back up the PostgreSQL database with `pg_dump -Fc` and the **entire** configured storage directory to a separate protected device. Pause application writes while taking the paired backup, to keep database metadata and files consistent. Restore to a test machine periodically using `pg_restore`. Back up before migrations. Keep the server clock correct and use Africa/Nairobi for school dates. The app stores audit timestamps as UTC and displays dates in an understandable day/month/year format. Financial entries and evidence should be reconciled by management regularly.

## Current boundaries

- This is a single-school, single-currency system. It does not include tuition billing, payroll processing, bank reconciliation or automatic M-Pesa/bank integrations.
- Restricted donations are explicitly labelled by purpose; automatic fund-by-fund disbursement limits are not implemented.
- Roles and permissions are centrally maintained in `RolePermissions.cs`. There is no per-user permission-grant editor or custom-role management screen; changing the mapping requires a code deployment and invalidates existing sessions.
- PDF export uses the browser's Save as PDF; no server PDF engine is installed.
- Reporting is transaction-level with live allocation totals; large datasets currently load scoped records before table pagination. Database-side pagination/aggregation should be added before multi-year high-volume use.
- Evidence accepts PDF/JPEG/PNG rather than editable Office documents. Antivirus scanning, scheduled backups and Windows service installation are operational additions.
- Builds and browser/API workflows are verified on Ubuntu. A Windows-targeted publish can be generated on Ubuntu, but Windows runtime/LAN/service operation requires testing on the actual Windows server.

## Verified delivery

On Ubuntu, 22 November 2026: backend build passed with zero warnings/errors; 15 xUnit tests passed; the twelve live PostgreSQL acceptance workflows and additional integrity checks passed; all 3 Playwright browser tests passed; React production build passed; self-contained `win-x64` publish passed. Build/test output is retained in `tests/verification-results.txt`. Windows runtime operation still needs testing on Windows.

## Live charts

The dashboard includes an interactive monthly spending/cash-income graph, budget breakdown, stacked department comparisons, income mix and purchase-request stages. Select a department bar, stage or month to open the relevant records. The monthly chart has a year selector and does not draw invented future observations.

Budgets, requests, expenses, payments, income, petty cash, inventory and assets also include visual overviews. Reports show charts calculated from the same filtered records as their CSV exports. Module charts summarise all authorised module records; table search operates on the record table. All chart monetary aggregation uses C# decimal on the server. Charts honour department permissions, support keyboard access and exact-value disclosures, print locally and require no external chart service.

Chart verification: 15 backend tests passed; all 6 browser tests passed on the isolated verification database, including chart totals, report filters, drill-down navigation and mobile/department scope. The final responsive chart changes also passed the 3 focused chart tests.

## Authorization and role assignment

Admin assigns `ADMIN`, `FINANCE`, `STOREKEEPER`, `DEPARTMENT_HEAD`, `STAFF` or `AUDITOR` under Users. Department is mandatory for Staff and Department Head; it is optional for the school-wide roles and Storekeeper. All access is checked by ASP.NET Core, independently of the UI. Effective permissions and read/create/edit/scope capabilities are returned by `/api/auth/me` and login. The UI uses these capabilities rather than role-name checks.

| Capability | Admin / Owner | Finance | Storekeeper | Department Head | Staff | Auditor |
|---|---|---|---|---|---|---|
| Users, roles, departments, budget changes | Manage | No writes | No | No | No | No writes |
| Budget override / final purchase approval | Yes, audited | No | No | No | No | No |
| Create/edit/submit purchase requests | All | View all | Receiving view | Department | Own only | View all |
| Invoices, payments, expenses, suppliers, income | Manage all | Manage all | Supplier contacts only | Read department expenses/invoices/payments | No detailed financial posting/read | Read all |
| Petty cash movements and funding decisions | Manage all | Manage all | No | View department, request funding | Request/track own funding | Read all |
| Stock items, locations and movements | Manage all | Read inventory values | Manage school-wide | Read department items | Read department items | Read all |
| Permanent stock → asset | Yes | No | Yes, recorded stock cost only | No | No | No |
| General asset editing | Yes | Read all | Read all / convert stock | Read department | Read department | Read all |
| Documents | Read/upload all | Read/upload authorised financial/stock evidence | Delivery/receiving evidence only | Read/upload on permitted department records | Read/upload on own requests and permitted department stock/assets | Read only |
| Reports | All | Financial school-wide | Stores / permitted purchasing | Permitted department reports | Permitted budget/own-request/stock reports | All, read only |
| Audit history | All, read only | Financial events school-wide, read only | No | Permitted department events | Permitted own/department events | All, read only |

Additional safeguards:

- Role permissions live in `backend/Services/RolePermissions.cs`; resource authorization and independent department/ownership checks live in `Access.cs`. Controllers use permission/resource gates, with no `RequireAdmin()` calls remaining.
- Storekeeper's request view includes approved/fulfilment-stage orders only. Budget IDs, approved/estimated/paid amounts, item prices and unrelated notes are removed server-side. Supplier bank/M-Pesa details and financial totals are also removed from the basic supplier view.
- A non-Admin cannot approve their own request/funding request. Admin self-approval requires a reason and creates an audit event. Finance cannot approve purchases or override a budget/approval ceiling by entering a reason alone.
- Payment `CreatedBy`/`CreatedAt` identify the recorder and timestamp; `ApprovedBy` identifies the original request approver from approval history. Their identities remain separate.
- Document listing, upload and download recheck the current linked entity and allowed evidence kinds. Moving an asset/item between departments removes the former department's access even if old document metadata still names that department.
- Role, department, status and password changes increment SessionVersion. Cookies also validate the current database role/department and a permission-mapping fingerprint. A permission-map deployment revokes old sessions. Everyone must sign in again after this upgrade.
- Audit endpoints remain immutable for every role, including Admin. Security events are not exposed through Finance's financial-history view.

### Migration and verification

`20261122155036_ExpandRolePermissions` maps legacy `USER` accounts to `STAFF` without changing IDs/passwords or financial records, increments their session versions, records the migration in audit history and adds role/department constraints. If a malformed legacy USER lacks a department, it is preserved but deactivated and assigned to an inactive migration holding department for Admin review. Apply with `dotnet ef database update` or `dotnet run -- --migrate` after a backup. Do not seed over an existing installation.

Fresh demo seeds additionally provide `finance@missionschool.local`, `storekeeper@missionschool.local`, `head@missionschool.local` and `auditor@missionschool.local`, all with the development password `SchoolDemo123!`. Existing installations retain their existing accounts; Admin creates/assigns any new officers explicitly.

Authorization verification uses a disposable seeded PostgreSQL server, not the real school database:

```bash
SCHOOL_URL=http://127.0.0.1:5001 python3 tests/permissions_acceptance.py
SCHOOL_URL=http://127.0.0.1:5001 python3 tests/acceptance.py
# From frontend, with the compiled app served on the verification API origin:
SCHOOL_BROWSER_URL=http://127.0.0.1:5001 SCHOOL_CHROME=/usr/bin/google-chrome npx playwright test
```

PowerShell uses `$env:SCHOOL_URL=...`, `$env:SCHOOL_BROWSER_URL=...` and `py` for the equivalent commands. Use downloaded Playwright Chromium if `SCHOOL_CHROME` is unset.

Limitations: role definitions are code-managed, there are no custom per-user grants or multi-role assignments, and Staff requests are private to their creator rather than selectively shared. Storekeeper receiving access is based on approved workflow status; there is no separate goods-versus-services classification in the existing request schema. These defaults keep access narrow and can be extended centrally.

Permission refactor verification: backend build passed with zero warnings/errors; all 55 backend tests and all 13 Playwright tests passed, both live PostgreSQL suites passed, local migration preserved all five existing user IDs and financial totals, and Windows self-contained publishing passed. Windows execution remains untested on this Ubuntu machine.
