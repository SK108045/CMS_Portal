# Oraforge — Catholic Mission School Finance Portal

A responsive, interactive financial accountability demonstration for Catholic Mission School in Dandora, Kenya.

## Run locally
Open \`index.html\` in a modern browser. No installation, build step, environment variables, or database credentials are required.

Alternatively, serve locally with \`python -m http.server 8080\` and open http://localhost:8080.

## Demo sign-in
- Email: \`admin@catholicmission.ac.ke\`
- Password: \`Admin123\`

This is **mock browser-only authentication**, not production-grade authentication. Do not enter real school financial data.

## Features
- Dashboard with derived financial totals, budget-versus-actual, spending trends, and recent transactions
- Budgets with editable category allocations, automatic spending calculations, donut chart, and limit warnings
- Purchase requests with new-request form, searchable-by-selection details, supplier information, and budget impact
- Management approval, rejection, and return for changes, with actor, timestamp and notes in each request's history
- Transactions and income records, filters, supporting-document tracking, and detailed audit trail
- Petty cash payments and replenishments with automatic balance calculations
- Reports, CSV export, and print view
- Basic school settings and resettable illustrative seed data
- Responsive navigation, and local browser storage persistence

## Important demo limitations
- Browser localStorage is not secure or shared between users/devices. Clearing site data resets the current demo state.
- Uploaded files retain **filename metadata only**. Actual document bytes are not stored, uploaded, or backed up.
- No real payments, bank integration, identity verification, authorization, immutable audit logs, or backend data persistence.
- Approval and transaction changes are demonstration workflows only; production finance controls still need enforcement and reconciliation.
- Example figures and transactions are illustrative, not real school accounts.

## Deploy
This is a static single-page website. For Vercel, import this repository with **Framework Preset: Other**, **Root Directory: .**, and no build command. A static host such as GitHub Pages also works.

## Next steps for production
Integrate a secure database, proper role-based authentication, encrypted document storage, server-side validation, audit logs, reconciliation rules and periodic backups before handling actual financial records.
