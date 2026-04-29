# Corporate Onboarding API Implementation Plan

## Goal
Implement backend support for the Corporate Investment Account flow shown in UI screens:
1. Company Details
2. Company Address
3. Account Representative Details

This plan adds API contracts, persistence, validation, and tests so the UI can save and resume these fields through `api/onboarding`.

## Scope
In scope:
- Save/read all corporate company + representative fields currently present in the UI.
- Keep existing individual onboarding flow working.
- Support resume/review via `GET /api/onboarding`.

Out of scope (for this phase):
- KYC document storage redesign.
- External business verification (CAC lookup, phone/email OTP, sanctions checks).
- Admin review/approval workflow changes.

## Field Mapping (UI -> API)
### Company Details
- `companyName` -> `CompanyLegalName`
- `brandName` -> `TradingBrandName`
- `registrationType` -> `RegistrationType`
- `registrationNumber` -> `RegistrationNumber`
- `loginEmail` -> `CompanyLoginEmail`
- `password` / `confirmPassword` -> handled in auth flow only (not persisted in onboarding profile)

### Company Address
- `registrationDate` -> `DateOfRegistration`
- `companyWebsite` -> `CompanyWebsite`
- `businessAddress` -> `BusinessAddress`
- `registeredAddress` -> `RegisteredAddress`
- `companyEmail` -> `CompanyEmail`
- `companyPhone` -> `CompanyPhone`

### Account Representative
- `repFullName` -> `RepresentativeFullName`
- `repJobTitle` -> `RepresentativeJobTitle`
- `repPhoneNumber` -> `RepresentativePhoneNumber`
- `repDob` -> `RepresentativeDateOfBirth`
- `repEmail` -> `RepresentativeEmail`
- `repNationality` -> `RepresentativeNationality`
- `repResidence` -> `RepresentativeCountryOfResidence`
- `repAddress` -> `RepresentativeAddress`

### QII Investment Profile (Corporate)
- `institutionTypes[]` (multi-select):
  - Bank
  - Asset management company
  - Pension fund administrator
  - Insurance company
  - Venture capital/private equity fund
  - Corporate finance institution
  - Other regulated institution (specify)
- `otherInstitutionType` -> free text when "Other regulated institution" selected
- `hasValidQiiRegistrationOrLicense` -> boolean
- `hasApprovedAlternativeInvestmentMandate` -> boolean
- `confirmsSecNigeriaQiiCriteria` -> boolean

### OCI Investment Profile (Corporate)
- `hasBoardResolutionOrInternalMandate` -> boolean
- `netAssetValueRange` -> enum/range:
  - Below ₦10 million
  - ₦10 million – ₦50 million
  - ₦50 million – ₦100 million
  - ₦100 million – ₦500 million
  - Above ₦500 million
- `hasFinancialCapacityToWithstandLoss` -> boolean
- `understandsCrowdfundingHighRiskLoss` -> boolean
- `hasQualifiedInvestmentProfessionalsAccess` -> boolean

### QII KYC Additional Documents
- `recentStatusReportDocumentPathOrKey`
- `qiiLicenseEvidenceDocumentPathOrKey`
- `boardResolutionDocumentPathOrKey`

### OCI KYC Additional Documents
- `incorporationCertificateDocumentPathOrKey`
- `recentStatusReportDocumentPathOrKey`
- `boardResolutionDocumentPathOrKey`

## API Contract Plan
Use existing onboarding endpoint pattern and add corporate payload support.

## Reuse vs New (Implementation Decision)
### Reuse (no new endpoint needed)
- Keep existing auth/signup flow:
  - `POST /api/auth/signup`
  - `POST /api/auth/verify-email`
  - `POST /api/auth/resend-verification`
- Keep existing onboarding endpoints:
  - `GET /api/onboarding` (resume + review hydration)
  - `PUT /api/onboarding` (save by step)
  - `POST /api/onboarding/submit` (final submit gate)
- Keep existing onboarding orchestration pattern in command/handler; extend logic for corporate branch.

### New contracts needed (DTOs/records)
- Add request payload DTOs:
  - `CorporateCompanyPayload`
  - `CorporateAddressPayload`
  - `CorporateRepresentativePayload`
  - `CorporateQiiProfilePayload`
  - `CorporateOciProfilePayload`
  - `CorporateQiiDocumentsPayload`
  - `CorporateOciDocumentsPayload`
- Extend response DTOs:
  - Add corporate sections to `OnboardingResponse` for all corporate steps and review cards.

### New endpoint decision
- Phase 1: **No new REST endpoint** for corporate flow.
- Reuse `PUT /api/onboarding` with additional payload slots and step guards.
- Optional future phase: separate file-upload endpoints only if we move away from `PathOrKey` pattern.

### Base class / inheritance decision
- Default: **no inheritance hierarchy for onboarding payload DTOs**.
- Prefer composition + focused validators to reduce coupling and avoid rigid DTO trees.
- If shared shape is needed, use small shared value objects/enums/helpers (not deep base classes).
- Exception allowed: a tiny shared `DocumentRef` value object if multiple payloads carry the same document metadata.

### Email Verification
- Corporate uses the same email verification process as individual onboarding.
- Reuse existing endpoints/flow (`/api/auth/resend-verification`, `/api/auth/verify-email`) with no new corporate-specific email verification API.

### `PUT /api/onboarding`
Extend `SaveOnboardingRequest` to optionally accept:
- `CorporateCompanyPayload`
- `CorporateAddressPayload`
- `CorporateRepresentativePayload`

Rule:
- Save only the payload matching the current corporate sub-step.
- Reject mismatched payloads with `400` and field errors.

### `GET /api/onboarding`
Extend `OnboardingResponse` with:
- `CorporateCompanyInfo`
- `CorporateAddressInfo`
- `CorporateRepresentativeInfo`

### Flow control
- Keep `OnboardingStep` as top-level step progression.
- Add corporate sub-step progression in onboarding state (new enum/int in persisted onboarding aggregate), e.g.:
  - `CompanyDetails`
  - `CompanyAddress`
  - `AccountRepresentativeDetails`
- After email verification, corporate categorization step allows selecting either:
  - `QualifiedInstitutionalInvestor (QII)`
  - `OtherCorporateInvestor (OCI)`
- This maps to existing `InvestorCategory` save behavior for corporate users.
- For QII path, onboarding continues with:
For corporate paths, onboarding continues with:
  1. Category-specific investment profile (QII or OCI)
  2. Account representative KYC (ID + proof of address + selfie)
  3. Category-specific additional document uploads
  4. Review and submit

- For QII additional docs:
  1. QII investment profile questionnaire
  2. Upload recent status report
  3. Upload proof/evidence of QII registration/license
  4. Upload board resolution authorizing registration, investment, and account representative

- For OCI additional docs:
  1. OCI investment profile questionnaire
  2. Upload incorporation certificate
  3. Upload recent status report
  4. Upload board resolution authorizing registration, investment, and account representative

## Domain and Persistence Plan
### Option selected (recommended)
Create a new entity/table for corporate onboarding profile:
- `UserCorporateOnboardingProfile`
- FK: `UserId` (unique, one profile per user)
- Includes all corporate company/address/representative columns.

Reason:
- Clear separation from individual investment profile and KYC tables.
- Easier validation and migration path.
- Minimizes risk of null-heavy unrelated columns.

### Repository
Add:
- `IUserCorporateOnboardingRepository`
- EF implementation in Infrastructure
- Register in DI + UnitOfWork

### Migration
Create EF migration to add `UserCorporateOnboardingProfiles` table and indexes:
- Unique index on `UserId`
- Optional index on `RegistrationNumber` if needed for future verification

Add columns/tables for QII-specific profile and docs (if not already in current investment profile table):
- Institution types (string list or normalized child table)
- Other institution type text
- QII boolean confirmations
- QII document keys/paths
- OCI-specific fields:
  - board mandate boolean
  - net asset value range
  - financial capacity boolean
  - high-risk understanding boolean
  - qualified professionals access boolean
  - incorporation certificate document key/path

## Validation Rules (Phase 1)
- Required: all fields except `CompanyWebsite`.
- Email format: `CompanyLoginEmail`, `CompanyEmail`, `RepresentativeEmail`.
- Date rules:
  - `DateOfRegistration` cannot be in future.
  - `RepresentativeDateOfBirth` must meet minimum age (e.g., 18).
- Enum constraints:
  - `RegistrationType` from allowed list.
  - `RepresentativeJobTitle` from allowed list (or free text with max length if business wants flexibility).
- Length constraints for all strings (define max lengths in one constants class).
- QII rules:
  - At least one institution type required.
  - If "Other regulated institution" selected, `otherInstitutionType` is required.
  - `hasValidQiiRegistrationOrLicense`, `hasApprovedAlternativeInvestmentMandate`, and `confirmsSecNigeriaQiiCriteria` are required (must be explicitly true/false).
  - QII additional docs required before final submit for QII category.
- OCI rules:
  - All OCI questionnaire booleans required (explicit true/false).
  - `netAssetValueRange` required.
  - OCI additional docs required before final submit:
    - incorporation certificate
    - recent status report
    - board resolution

## Application Layer Changes
- Add DTO records for corporate payloads listed in "Reuse vs New (Implementation Decision)".
- Extend `SaveOnboardingCommand` and validator.
- Extend `SaveOnboardingCommandHandler` to:
  - detect corporate flow (`OnboardingFlowType.CorporateInvestor`)
  - save the correct corporate payload by sub-step
  - advance corporate sub-step
- Extend `GetOnboardingQueryHandler` + mapper to hydrate new corporate fields.
- Ensure review payload includes:
Ensure review payload includes:
  - company summary
  - categorization + category-specific answers (QII/OCI)
  - account rep KYC status
  - category-specific document upload statuses

## Endpoint Additions/Adjustments for QII
- Keep using `PUT /api/onboarding` for save/resume by step.
- Reuse existing document upload approach (path/key in payload) unless direct upload API is introduced later.
- `POST /api/onboarding/submit` must enforce QII completion rules before submission.
- `POST /api/onboarding/submit` must enforce completion rules per selected category:
  - QII: QII profile + QII required docs
  - OCI: OCI profile + OCI required docs

## Backward Compatibility
- Do not break existing individual payload contracts.
- New corporate payload properties should be optional for non-corporate users.
- Existing clients can continue sending current payloads unchanged.

## Security and Data Handling
- Treat representative PII as sensitive data.
- Avoid logging raw payloads in info/error logs.
- Validate and normalize phone/date/email formats before persist.

## Testing Plan
## Implementation Order (Enforced)
1. Write/adjust **unit tests first** for new corporate validators, step guards, and submit gating.
2. Implement application/domain/infrastructure changes to make unit tests pass.
3. Add and run **integration tests last** to verify end-to-end API behavior.

This sequence is mandatory for this workstream (test-first for unit level, then implementation, then integration verification).

## Build & Verification Gates
Run these checks before moving to the next phase:

1. After unit test changes:
   - `dotnet build AntitalAPI.sln`
   - `dotnet test Antital.Test/Antital.Test.csproj --filter "FullyQualifiedName~Onboarding|FullyQualifiedName~Corporate"`
2. After implementation changes:
   - `dotnet build AntitalAPI.sln`
   - `dotnet test Antital.Test/Antital.Test.csproj`
3. After integration test additions:
   - `dotnet test Antital.Test/Antital.Test.csproj -c Release`

Stop-and-fix rule:
- Do not continue to the next phase while build/tests are failing.
- Fix failures first, then rerun the same gate.

## Testing Plan
### Unit tests
- Command validation for each corporate payload.
- Save handler tests per sub-step progression.
- Guard tests for wrong payload/flow mismatch.
- QII validation tests (institution type, dependent fields, required booleans).
- QII submit gate tests (cannot submit without required QII docs/answers).
- OCI validation tests (required booleans + required net asset value range).
- OCI submit gate tests (cannot submit without required OCI docs/answers).

### Integration tests
- `PUT /api/onboarding` save + `GET /api/onboarding` roundtrip for all 3 corporate sections.
- Resume behavior after partial completion.
- Ensure individual onboarding tests remain green.
- Full QII flow: company -> email verified -> categorization(QII) -> profile -> kyc -> qii docs -> review -> submit.
- Full OCI flow: company -> email verified -> categorization(OCI) -> profile -> kyc -> oci docs -> review -> submit.

## Rollout Plan
1. Add/adjust **unit tests** for corporate flow and failure cases.
2. Run Gate 1 (build + targeted unit tests).
3. Add domain model + EF migration.
4. Add DTOs, validators, command handler updates.
5. Add GET mapper updates.
6. Run Gate 2 (full build + full test suite), refactor/fix until green.
7. Add/expand **integration tests** for QII and OCI full flows.
8. Run Gate 3 (release test run).
9. Update Swagger examples for corporate requests.
10. Coordinate with UI to switch from local-only state to API-backed save for each company sub-step.

## Open Decisions
- Should representative `JobTitle` be enum or free text?
- Should we enforce uniqueness on `RegistrationNumber` globally?
- Do we need audit fields for who changed corporate data and when (beyond TrackableEntity)?
- For `institutionTypes`, should we store as normalized rows or comma-separated enum values?
- Should QII additional docs be hard-required before submit, or can they be deferred with a pending compliance status?

## Deliverables
- New markdown plan file (this file)
- Code implementation PR in follow-up phase
