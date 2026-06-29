# Balance & Funding — API

## Goal

Expose investor wallet ledger and payment-method endpoints for the balance-funding UI. API work only; UI integration is on `antital-ui` branch `feature/balance-funding-ui` (`PLAN-balance-funding-ui.md`).

## Scope

- Wallet transaction list + detail (v1: paid primary-market `InvestmentOrder`s).
- Payment methods CRUD (masked display fields).
- Swagger + integration tests.

## Out Of Scope

- Secondary market (`Buy` / `Sell` ledger types).
- Wallet deposit / Add Funds (product decision).
- Notifications inbox API.
- CSV/PDF export.
- Crypto payment methods.

## API contracts and endpoints available

### Already live (no changes)

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/investors/me/dashboard` | `InvestorWallet.AvailableBalance` — UI uses this for balance card |

### To implement (this plan)

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/investors/me/wallet/transactions` | Paginated ledger; v1 type `Investment` only |
| `GET` | `/api/investors/me/wallet/transactions/{id}` | Detail / invoice payload |
| `GET` | `/api/investors/me/payment-methods` | List |
| `POST` | `/api/investors/me/payment-methods` | Add |
| `PATCH` | `/api/investors/me/payment-methods/{id}/default` | Set default |
| `DELETE` | `/api/investors/me/payment-methods/{id}` | Remove |

**Transaction type enum (v1):** `Investment` — later `Deposit`, `Withdrawal`, `Fee`. No `Buy` / `Sell`.

**v1 data source:** `InvestmentOrder` where `Status = Paid`, join offering + `PaymentTransaction`.

## Checkpoints

| # | Checkpoint | Status |
|---|------------|--------|
| 1 | Wallet transactions list (investment orders) | completed |
| 2 | Wallet transaction detail (invoice) | completed |
| 3 | Payment methods CRUD | completed |
| 4 | Wallet deposit / Add Funds — blocked | pending |

## Permission rule

Implement only the next `pending` checkpoint. Stop and request explicit approval before the next checkpoint.

---

## Checkpoint 1 — Wallet transactions list

- [x] Status: completed

**UI consumer**

- `GET /api/investors/me/wallet/transactions` — Overview recent activity (`pageSize=3`) and Transactions tab.

**API contract**

- Query: `page`, `pageSize`, `type`, `status`, `from`, `to`
- Response item: `id`, `type`, `description`, `subDescription`, `amount`, `fees?`, `occurredAt`, `status`, `orderId?`, `offeringSlug?`
- Empty list → `200` with `[]`, not 404.

**Files to change**

- `Antital.Application/DTOs/Investors/` — wallet transaction DTOs
- `Antital.Application/Features/Investors/GetWalletTransactions/`
- `Antital.Domain/Interfaces/` — repository methods if needed
- `Antital.Infrastructure/Repositories/`
- `Antital.API/Controllers/InvestorsController.cs`
- `Antital.Test/Integration/API/Controllers/` — new tests

**Code plan**

1. Query paid orders for authenticated user with offering join.
2. Map to `Investment` type rows.
3. Paginate and filter.

**Done criteria**

- Integration test: seeded user with paid order returns rows; user with none returns empty array.
- Swagger documented.

---

## Checkpoint 2 — Wallet transaction detail

- [x] Status: completed

**API contract**

- `GET /api/investors/me/wallet/transactions/{id}` — invoice fields: offering, units, share price, fees, bill-to from user profile, payment reference.

**Files to change**

- `Antital.Application/Features/Investors/GetWalletTransaction/`
- `InvestorsController.cs`
- Integration tests

**Done criteria**

- Returns 404 for other users' orders; 200 with breakdown for owner.

---

## Checkpoint 3 — Payment methods CRUD

- [x] Status: completed

**API contract**

- CRUD routes above; new `InvestorPaymentMethod` entity + migration.
- Store masked labels only (e.g. last4, bank name).

**Files to change**

- Domain model, migration, repository, features, controller, tests

**Done criteria**

- Full CRUD integration tests pass.

---

## Checkpoint 4 — Wallet deposit (blocked)

- [ ] Status: pending

**Blocked on:** Product decision (Paystack top-up vs manual transfer).

---

## Readiness checklist

| Item | Status |
|------|--------|
| `InvestmentOrder` + `PaymentTransaction` from checkout | Ready |
| `InvestorWallet` | Ready (balance only) |
| UI balance integration | On `feature/balance-funding-ui` (checkpoint 1–2) |
| Payment method domain | Ready |
