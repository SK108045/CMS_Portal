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

- **Requests:** New request → add item quantities/prices → save draft → Open → submit. Admin opens Pending Approvals, chooses an active supplier, approved amount and comments. A return or rejection needs a reason. Returned requests can be edited and submitted again.
- **Delivery/payment:** Open approved request → Ordered → Partially Received / Received. Record inventory receipts under Inventory → Stock Movements and link the request. Under Payments → Invoices, record the supplier's invoice and attach evidence. Under Payments, record each actual payment with the matching invoice and a unique reference. Partial payments reduce commitment only by the amount paid. Choose **Final payment** when all goods are received and every recorded invoice is settled; this releases any unused approval. Open the request to complete it. Its history, invoices and payments remain visible.
- **Expenses:** Admin records direct authorised payments against a department allocation, with supplier/person, payment reference and responsible person. Attach receipts from Open → Supporting evidence. The budget is charged immediately because this records an actual payment.
- **Petty cash:** Staff use Request funds / decisions to submit a funding request; Admin approves with comments/reference, posting the replenishment atomically, or rejects with a reason. Admin creates a departmental account, then an Opening movement. Department staff may record Expense or Withdrawal movements in their own account. Expense requires a department budget and affects spending. Replenishment restores the cash balance without charging the budget again. Withdrawal records accountable cash issued, not an expense. To settle it, create an Expense and select the original withdrawal; this charges the budget without debiting petty cash twice. Return unused cash using a Return movement linked to that withdrawal. Settlements cannot exceed the outstanding advance. Account details show unretired advances separately. Attach receipts to spending records. Staff cannot overdraw.
- **Donations:** Record Donation, Grant or Other Income, with source, donor and restricted purpose. For a non-cash donation first create the inventory item, choose Non-cash Donation and its quantity. A stock movement is posted in the same database transaction. Restricted purposes are highlighted in record detail.
- **Inventory/assets:** Record receipts, issues, returns, damage, loss, adjustments or transfers. Quantities come only from movements. Positive Adjusted movements add units; use Damaged/Lost for reductions. Transfers create a destination stock item and corresponding receipt. Open a Permanent Item and assign one unit to the asset register; this removes it from stock and records a linked asset. Each unit can have its own serial number after editing. Maintenance expenses linked to the asset appear in its lifetime cost.
- **Evidence:** Open a request, invoice, payment, expense, petty cash movement, donation, inventory item or asset; attach a PDF, PNG or JPEG, up to 10 MB. Extensions and signatures are validated. Evidence is stored outside the web root. Downloads require login and correct department access. Missing evidence and missing physical files are flagged.
- **Reports:** Choose a report and date/department/category/supplier/status/search filters. CSV uses the same server-side filters. Print / Save PDF uses the browser print dialog. Tables support their own search, department/status/supplier filters, sorting and pagination.

## Financial and security model

Money uses PostgreSQL `numeric(18,2)` and C# `decimal`. The server calculates item totals, commitments, spending, cash balances and stock quantities. Available budget = original allocation + adjustments − unpaid approved commitments − payments − direct expenses − petty cash expenses. Rejections/cancellations release commitments. Completed orders retain their payment history. Budget adjustments and posted financial entries are immutable through the API; corrections require explicit adjustment records. Admin can exceed availability with a recorded reason. Actual payments may differ from approval, with an explanation for excess payments. Duplicate method/reference pairs and duplicate supplier/invoice numbers are rejected.

Financial mutations use PostgreSQL serializable transactions. Concurrent conflicts return an actionable 409 response. Foreign keys prevent orphaned records; ledger entries are not deleted through the UI. Audit records are append-only through the application. The database administrator remains a trusted operator with direct database access.

Passwords use ASP.NET Core's password hasher. Authentication uses encrypted HTTP-only SameSite=Strict cookies and anti-forgery tokens on every mutation, including login. The API checks department/ADMIN access independently of the frontend. Navigation objects from input are discarded before EF binding. There are two roles today, with access decisions centralised for later extension. Financial catalogues and final approvals are Admin-only; staff can submit requests, attach their department's evidence and record authorised imprest usage. Users see department-scoped activity; central inventory is shared only when no department owns the item. No external email, payment provider or cloud dependency exists.

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
- Permissions currently use Admin and department-user roles; configurable officer-level permission sets are a future extension.
- PDF export uses the browser's Save as PDF; no server PDF engine is installed.
- Reporting is transaction-level with live allocation totals; large datasets currently load scoped records before table pagination. Database-side pagination/aggregation should be added before multi-year high-volume use.
- Evidence accepts PDF/JPEG/PNG rather than editable Office documents. Antivirus scanning, scheduled backups and Windows service installation are operational additions.
- Builds and browser/API workflows are verified on Ubuntu. A Windows-targeted publish can be generated on Ubuntu, but Windows runtime/LAN/service operation requires testing on the actual Windows server.

## Verified delivery

On Ubuntu, 22 November 2026: backend build passed with zero warnings/errors; 15 xUnit tests passed; the twelve live PostgreSQL acceptance workflows and additional integrity checks passed; all 3 Playwright browser tests passed; React production build passed; self-contained `win-x64` publish passed. Build/test output is retained in `tests/verification-results.txt`. Windows runtime operation still needs testing on Windows.
