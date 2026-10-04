# Antital Admin Dashboard API and UI Implementation Plan

## Objective

Replace the hard-coded Antital admin dashboard data with authenticated, database-backed data. The backend API must be completed, tested, and accepted before any UI integration begins.

This plan covers the `/dashboard` command-center view only. It does not silently convert the existing demo-only admin subpages into production features.

## Execution and approval rules

- No implementation starts until the owner approves checklist item **1**.
- Work proceeds one numbered checklist item at a time.
- Before starting an item, wait for explicit owner permission naming that item.
- After completing an item, run its listed verification, change `[ ]` to `[x]`, report the result, and stop.
- Do not begin the next item—even when it is closely related—until the owner explicitly approves it.
- Backend items **1–6** must all be accepted before item **7** (UI work) can begin.
- If implementation reveals a contract or scope change, update this document and obtain approval before continuing.
- A checkbox means implemented and verified, not merely started.

## Engineering principles: reuse, SOLID, and DRY

- Before adding a class, helper, formatter, component, hook, or period parser, inspect the investor and fundraiser dashboard implementations and the shared application/building-block layers for an existing abstraction.
- Reuse the standard `Result<T>` envelope, MediatR query flow, repository conventions, authentication policy, API client, React Query patterns, error feedback, loading/empty-state components, and dashboard formatting utilities wherever their contracts fit.
- Prefer extending a genuinely shared abstraction over copying logic. For example, period-range calculation and currency/count formatting should have one authoritative implementation when the semantics are the same.
- Do not force unlike dashboard behavior into a shared abstraction merely to remove a few lines. Admin snapshot metrics, investor portfolio periods, and fundraiser campaign periods may remain separate when their business meanings differ.
- Keep responsibilities separated: controllers handle HTTP concerns, query handlers orchestrate business rules, repositories own data access, services/hooks own client transport and caching, and presentation components render supplied state.
- Depend on interfaces at application boundaries and keep DTOs separate from EF entities and UI view models.
- Keep functions/components focused, use explicit domain names, and avoid boolean-heavy or role-heavy branching when a typed model or small focused component is clearer.
- Any new shared abstraction must have at least two real consumers or remove an existing duplication immediately; do not introduce speculative framework code.
- During each checklist review, report what was reused, what duplication was removed, and why any similar-looking logic intentionally remained separate.

## Agreed product boundary

### Data available from the current backend

The current schema can truthfully supply:

- individual and corporate investor totals;
- new-investor counts and comparison with a preceding period;
- total and active/published campaign counts;
- current raised amounts from offering funding records;
- submitted or under-review onboarding work;
- draft campaigns;
- failed payment transactions within the selected period;
- recent registrations, onboarding submissions, campaign publications, and successful investments.

### Data not currently modeled

The current hard-coded UI also mentions escrow, unresolved support tickets, expiring documents, and flagged accounts. The database has no authoritative lifecycle/source for these concepts. They will not be fabricated or inferred from unrelated records.

For this delivery:

- remove the fake escrow figure rather than calling another amount “escrow”;
- omit support-ticket, expiring-document, and flagged-account action cards from the API response and rendered dashboard;
- keep their separate demo pages outside this API-integration scope;
- treat payment failures as “payment exceptions in the selected period,” because there is no resolved/unresolved exception state;
- do not make the demo `/activity-logs` page appear production-backed. Recent activity on the dashboard will be live, but “View Log” will be hidden until that page has its own API.

Adding real ticketing, compliance flags, document-expiry, escrow accounting, or a persistent audit log requires separate domain/API work and a separately approved plan.

## Proposed API contract

### Endpoint and access

`GET /api/admin/dashboard?period=last-30-days`

- Requires authentication and the existing `AdminPolicy`/`Admin` role.
- Returns `401` for no valid session, `403` for authenticated non-admin users, and `400` for an unsupported period.
- Supported periods match the existing admin selector: `last-7-days`, `last-30-days`, and `last-90-days`.
- All interval comparisons use UTC and half-open ranges (`start <= timestamp < end`).

### Response shape

The response remains inside the repository's standard `Result<T>` envelope and contains:

```text
summary
  totalInvestors
  newInvestorsInPeriod
  newInvestorsInPreviousPeriod
  investorGrowthPercent (nullable when the previous period is zero)
  activeCampaigns
  activeCampaignRaisedAmount
  totalCampaigns
  totalFundsRaised
  currency
actions[]
  type (`PendingOnboardingReview`, `DraftCampaign`, or `PaymentException`)
  title
  description
  count
  route (nullable)
recentActivity[]
  id
  type
  subject
  description
  occurredAtUtc
  route (nullable)
```

### Metric definitions

- **Investor**: a non-deleted, non-admin user whose user type is `IndividualInvestor` or `CorporateInvestor`.
- **Total investors**: all investors as of request time.
- **New investors**: investors created in the chosen period; the comparison period is the immediately preceding interval of equal length.
- **Investor growth percent**: `((current - previous) / previous) * 100`; `null` when `previous` is zero so the UI can show a count instead of a misleading percentage.
- **Active campaign**: a non-deleted offering with `OfferingStatus.Published`.
- **Total campaigns**: all non-deleted offerings, regardless of status.
- **Raised amounts**: current `OfferingFunding.RaisedAmount` snapshot sums; active raised amount includes published offerings only, while total funds raised includes all offerings. These snapshot figures are not falsely presented as period flows.
- **Pending onboarding reviews**: non-deleted onboardings in `Submitted` or `UnderReview` status. This is not labeled strictly as KYC because the current record represents the whole onboarding flow.
- **Draft campaigns**: non-deleted offerings in `Draft` status. The label will not say “requires approval,” because no pending-approval campaign state exists.
- **Payment exceptions**: current failed, non-deleted payment transactions whose effective failure timestamp (`ProcessedAt`, then `UpdatedAt`, then `CreatedAt`) falls inside the selected period; deleted related orders, users, and offerings are excluded.
- **Recent activity**: a read-time union of the latest real registration, onboarding-submission, campaign-publication, and successful-investment events, ordered newest first and limited to five. This is a dashboard feed, not a durable audit log.

## Ordered implementation checklist

### Backend — must be completed first

- [x] **1. Freeze the API contract and period semantics.** First audit the existing investor/fundraiser dashboard DTOs, period resolvers, query conventions, and shared building blocks. Reuse or safely generalize them where semantics match; create an admin-specific abstraction only where the day-range/comparison behavior is genuinely different. Add admin dashboard DTOs, activity/action discriminators, query object, and focused unit tests for all supported periods, UTC boundaries, invalid values, and previous-period calculation. **Verification:** targeted application unit tests pass; public DTO fields match this document; completion report lists reused and intentionally separate pieces. **Stop for owner review.**

- [ ] **2. Implement read-only admin dashboard data access.** Follow the existing dashboard repository interfaces and EF conventions, extracting shared query specifications/projections only when they are also useful to an existing dashboard. Add `IAdminDashboardRepository` and an EF Core implementation using `AsNoTracking`, explicit soft-delete filters, projections/aggregates, cancellation tokens, and bounded recent-event queries. Reuse current entities only; do not add speculative ticket/flag/escrow tables. Register the repository through the existing dependency-injection pattern. **Verification:** repository-level tests cover empty data, mixed user roles/types, deleted records, campaign statuses, funding totals, payment status/period filtering, and recent-event ordering/limit. Review generated query shape and add a migration/index only if a demonstrated query requires it; confirm no equivalent repository logic was copied unnecessarily. **Stop for owner review.**

- [ ] **3. Implement the application query handler.** Follow the existing investor/fundraiser handler composition and standard result/error conventions. Compose summary metrics, nullable growth, dynamic action cards, and normalized recent activity without controller or repository business logic. Extract only calculations that are demonstrably shared, keep admin-only rules cohesive, and ensure empty databases return a successful zero/empty response with monetary values represented as `decimal` and `NGN`. **Verification:** handler tests cover populated, empty, zero-baseline growth, and cancellation/error paths; duplication/reuse review passes. **Stop for owner review.**

- [ ] **4. Expose and secure the admin endpoint.** Add an `AdminController` (or equivalent admin route), apply `[Authorize(Policy = "AdminPolicy")]`, document the endpoint and status codes in Swagger, and dispatch through MediatR. Do not rely on the UI role gate for security. **Verification:** integration tests prove `401` unauthenticated, `403` authenticated non-admin, `400` invalid period, and `200` admin with the expected envelope and data. **Stop for owner review.**

- [ ] **5. Validate backend behavior and local data.** Confirm current development seeds provide enough real source records for a meaningful admin response; extend local-only seed data only where necessary and make it idempotent. Do not seed fake production operational records. **Verification:** full `dotnet test`, solution build, migration check, and authenticated API smoke tests for all three periods through the running Aspire stack. Save the observed response summary in the completion report. **Stop for owner review.**

- [ ] **6. Backend acceptance gate.** Review the final endpoint contract, authorization results, query behavior, Swagger exposure, test results, and any migrations with the owner. Resolve backend review findings under this item. **Verification:** owner explicitly accepts the backend API. UI implementation remains blocked until acceptance. **Stop for explicit UI authorization.**

### Frontend — begins only after backend acceptance

- [ ] **7. Add the typed admin dashboard client layer.** Audit and reuse the existing dashboard API client, response-unwrapping/error conversion, cache-key structure, React Query hook conventions, and period-option utilities. Add only the admin-specific response types, service/hook, cache key, and mappings that cannot be shared safely. Enable the query only for a hydrated admin session. **Verification:** ESLint and TypeScript checks pass; browser network inspection shows exactly one admin-dashboard request for the selected period and no admin request for other user types; no duplicate request/error/period infrastructure is introduced. **Stop for owner review.**

- [ ] **8. Replace hard-coded admin summary cards.** Reuse or generalize existing dashboard cards, skeletons, and monetary/count formatters where their visual and semantic contracts match; keep admin-specific composition separate. Feed total investors, investor comparison, active campaigns, active raised amount, total campaigns, and total raised amount from the API. Centralize NGN/count/percentage formatting and honest zero/null behavior while preserving the current responsive design. **Verification:** loading skeleton, populated data, zeros, nullable growth, and period changes render correctly with no hard-coded business totals or duplicated equivalent formatters remaining. **Stop for owner review.**

- [ ] **9. Replace hard-coded actions and recent activity.** Render API-provided action items and activity records, support empty states, use only valid API-provided routes, hide the demo “View Log” link, and remove unsupported fake operational categories. Keep “View All/Show less” local display behavior. **Verification:** populated and empty responses render safely; item counts/descriptions are API-derived; no dead or demo route is presented as an operational action. **Stop for owner review.**

- [ ] **10. Add resilient UI states and error handling.** Reuse the established authentication redirect, forbidden flow, API-error conversion/feedback, and applicable loading/empty-state primitives. Provide a dashboard-level loading state, non-destructive retry state, and accessible empty states without duplicate toasts or parallel error-handling paths. Authentication expiry must redirect to sign-in; genuine authorization failure must use the forbidden flow. **Verification:** exercise success, slow response, `401`, `403`, `500`, retry, and period-switch scenarios; confirm shared error behavior remains consistent across dashboards. **Stop for owner review.**

- [ ] **11. End-to-end acceptance.** Run frontend lint/type-check/build and backend build/tests. With Aspire running, use Playwright to validate admin sign-in/OTP behavior as applicable, dashboard data, all periods, refresh persistence, responsive layouts, and absence of console errors. Reconfirm an investor cannot call or view the admin dashboard. **Verification:** record the checked routes, network statuses, console result, and final test commands; update this checkbox only when all pass. **Stop for final owner acceptance.**

## Definition of done

- The admin command center contains no hard-coded business counts or activity records.
- Every displayed operational number has a documented backend source and definition.
- The admin endpoint is protected by server-side role authorization and has `401`/`403` integration coverage.
- Empty data is a valid `200` response and renders intentionally.
- Unsupported operational domains are omitted rather than simulated.
- Backend tests/build and frontend lint/type-check/build pass.
- Playwright confirms the integrated behavior through Aspire.
- Every checklist item is checked, with owner approval recorded between items.
- The completion report identifies reused abstractions and confirms that no equivalent dashboard logic was duplicated without a documented reason.

## Explicitly out of scope

- Implementing the investor-management, fundraiser-management, flags-and-alerts, financial-operations, support-hub, compliance, or activity-log demo pages.
- Mutation/review endpoints behind action cards.
- A persistent audit/event store.
- Support-ticket, compliance-case, account-flag, document-expiry, payout-resolution, or escrow-ledger schemas.
- Changes to the already API-backed individual, corporate, or fundraiser dashboards.
