# TruthDoctor Search Verification Matrix

## 1. Purpose

This document records the automated and manual verification evidence for:

**G3.20.8 — Search Verification Matrix**

The authoritative roadmap requirement is:

> Automated and manual validation across every workspace.

G3.20.8 validates the complete G3.20 context-aware search foundation without prematurely implementing later workspace, capability, history, settings, reporting, authorization, or governance milestones.

The verification scope covers:

- Dashboard
- Resources
- Topology
- Operations
- History
- Reports
- Settings
- Unknown-route safety

---

## 2. Verification Principles

The search system must be evaluated according to the current maturity of each workspace.

A workspace may currently be:

- **Live** — runtime search behavior exists and must be exercised manually.
- **Partially Live** — some runtime search behavior exists while later capability layers remain deferred.
- **Contract-Only** — a pure search contract exists, but runtime domain objects and UI search are intentionally deferred.
- **Unknown / Unsupported** — must fail safely without falling back to another workspace.

G3.20.8 must not create fake runtime objects solely to make manual verification appear complete.

G3.20.8 must not pull later roadmap work forward.

Search behavior must remain owned by the currently active workspace.

An unsupported or deferred workspace must never silently fall back to Dashboard.

---

## 3. Current G3.20 Search Architecture

The following production search components exist:

```text
InfrastructureResourceSearch
InfrastructureResourceSearchResolver
WorkbenchGlobalSearchCoordinator
WorkbenchOperationsSearch
WorkbenchHistorySearch
WorkbenchReportsSearch
WorkbenchSettingsSearch
```

The following focused automated test suites exist:

```text
InfrastructureResourceSearchTests
InfrastructureResourceSearchResolverTests
WorkbenchGlobalSearchCoordinatorTests
WorkbenchOperationsSearchTests
WorkbenchHistorySearchTests
WorkbenchReportsSearchTests
WorkbenchSettingsSearchTests
WorkbenchSearchVerificationMatrixTests
```

---

## 4. Workspace Search Ownership

| Workspace | Search Owner / Contract | Current Maturity |
|---|---|---|
| Dashboard | `InfrastructureResourceSearch` | Live |
| Resources | `InfrastructureResourceSearch` | Live |
| Topology | `InfrastructureResourceSearch` + `InfrastructureResourceSearchResolver` | Live |
| Operations | `WorkbenchOperationsSearch` | Partially Live |
| History | `WorkbenchHistorySearch` | Contract-Only |
| Reports | `WorkbenchReportsSearch` | Contract-Only |
| Settings | `WorkbenchSettingsSearch` | Contract-Only |
| Unknown | `WorkbenchGlobalSearchCoordinator` safe route | Safety Contract |

---

## 5. Global Search Routing Matrix

The global search coordinator recognizes the following workspace routes:

| Workspace Name | Expected Route |
|---|---|
| `dashboard` | `Dashboard` |
| `resources` | `Resources` |
| `topology` | `Topology` |
| `operations` | `Operations` |
| `history` | `History` |
| `reports` | `Reports` |
| `settings` | `Settings` |

The following must resolve to:

```text
Unknown
```

Examples:

```text
null
empty string
whitespace
unknown
future-workspace
```

The coordinator must not redirect an unsupported workspace to Dashboard.

---

## 6. Current Runtime Routing Behavior

The current Universal Workbench routing behavior is:

### Dashboard

The global query is copied into the Dashboard resource search control and resource filters are reapplied.

Expected ownership:

```text
Dashboard → canonical resource filtering
```

### Resources

The global query is copied into the Resources workspace search control.

Expected ownership:

```text
Resources → canonical resource filtering
```

### Topology

The global query is used to:

1. inspect discovered resources,
2. attempt unique canonical resource resolution,
3. navigate/select the resource when exactly one match exists,
4. apply the search text to the Topology Find control.

Expected ownership:

```text
Topology → unique resource resolution + topology Find
```

### Operations

The global query is routed to:

```text
WorkbenchOperationsSearch
```

Expected ownership:

```text
Operations → resource/capability search foundation
```

### History

The route currently performs a safe no-op.

Expected current behavior:

```text
Remain in History.
Do not fall back to Dashboard.
Do not mutate unrelated workspace state.
```

Runtime History search remains deferred.

### Reports

The route currently performs a safe no-op.

Expected current behavior:

```text
Remain in Reports.
Do not fall back to Dashboard.
Do not mutate unrelated workspace state.
```

Runtime reporting search remains deferred.

### Settings

The route currently performs a safe no-op.

Expected current behavior:

```text
Remain in Settings.
Do not fall back to Dashboard.
Do not mutate unrelated workspace state.
```

Runtime Settings / Administration search remains deferred.

### Unknown

The route performs a safe no-op.

Expected behavior:

```text
Do not redirect to Dashboard.
Do not mutate unrelated workspace state.
```

---

## 7. Existing Query Re-Routing Contract

The global search text is retained independently of individual workspace controls.

When workspace navigation occurs, the retained global query is routed again according to the newly active workspace.

Required behavior:

```text
Enter query in Workspace A
        ↓
Navigate to Workspace B
        ↓
Retained query is interpreted by Workspace B
```

The retained query must never force a deferred or unsupported workspace back to Dashboard.

---

# 8. Automated Verification Baseline

## 8.1 Cross-Workspace Verification Suite

Test class:

```text
WorkbenchSearchVerificationMatrixTests
```

Current verified result:

```text
26 / 26 PASS
```

Current matrix coverage:

| Verification Area | Status |
|---|---|
| Seven primary workspace routes | PASS |
| Recognized-route completeness | PASS |
| Verification workspace-name uniqueness | PASS |
| Missing / unsupported workspace → Unknown | PASS |
| Non-Dashboard workspace never falls back to Dashboard | PASS |
| Dashboard / Resources canonical search representative behavior | PASS |
| Topology unique resource resolution representative behavior | PASS |
| Operations resource search representative behavior | PASS |
| Operations capability search representative behavior | PASS |
| History contract representative behavior | PASS |
| Reports contract representative behavior | PASS |
| Settings contract representative behavior | PASS |
| Representative collection search does not mutate input | PASS |

---

## 8.2 Canonical Resource Search

Production component:

```text
InfrastructureResourceSearch
```

Verified search behavior includes:

- blank search is safe,
- search text is trimmed,
- matching is case-insensitive,
- partial substring matching is supported,
- canonical resource fields are searchable,
- property keys and values are searchable,
- tag keys and values are searchable,
- unrelated queries do not match,
- null resources are rejected.

Canonical capabilities are intentionally not searched by this matcher.

Capability search belongs to Operations search.

Status:

```text
AUTOMATED PASS
```

---

## 8.3 Topology Search Resolver

Production component:

```text
InfrastructureResourceSearchResolver
```

Verified behavior includes:

- one unique canonical resource match resolves,
- multiple matches do not resolve arbitrarily,
- no match returns null,
- blank search returns null.

Status:

```text
AUTOMATED PASS
```

---

## 8.4 Global Search Coordinator

Production component:

```text
WorkbenchGlobalSearchCoordinator
```

Verified behavior includes:

- Dashboard resolves correctly,
- Resources resolves correctly,
- Topology resolves correctly,
- Operations resolves correctly,
- History resolves correctly,
- Reports resolves correctly,
- Settings resolves correctly,
- route resolution is case-insensitive,
- workspace names are trimmed,
- unknown workspaces resolve to `Unknown`,
- unsupported workspaces never fall back to Dashboard.

Status:

```text
AUTOMATED PASS
```

---

## 8.5 Operations Search

Production component:

```text
WorkbenchOperationsSearch
```

Search categories:

```text
Resource
Capability
Action
Runbook
OperationRequest
ApprovalState
ExecutionState
```

Currently implemented search behavior:

```text
Resource
Capability
```

Live provider-advertised capabilities remain deferred to G3.22.

Future categories remain contract-only and must not be populated with fake runtime objects.

Verified behavior includes:

- blank query returns one resource result per resource,
- canonical resource fields are reused through `InfrastructureResourceSearch`,
- capability matching is separate,
- capability matching is trimmed,
- capability matching is case-insensitive,
- null capabilities are safe,
- empty capabilities are safe,
- resource match takes precedence over capability match,
- one best match is returned per resource,
- unrelated queries return no results,
- result ordering follows input ordering.

Status:

```text
AUTOMATED PASS
```

---

## 8.6 History Search Contract

Production component:

```text
WorkbenchHistorySearch
```

Search document kinds:

```text
Audit
History
Effect
```

Searchable metadata includes:

```text
Actor
Action
Target
DateText
Result
Provider
Environment
Approval
Verification
CorrelationId
OperationalEffect
```

Verified behavior includes:

- blank query matches,
- trimming,
- case-insensitive matching,
- partial substring matching,
- null-safe metadata,
- unrelated queries do not match,
- input ordering is preserved.

Status:

```text
AUTOMATED PASS
```

Runtime History objects and History UI search are intentionally deferred.

Deferred owner:

```text
G3.27 — History
```

---

## 8.7 Reports Search Contract

Production component:

```text
WorkbenchReportsSearch
```

Search document kinds:

```text
ReportDefinition
Template
HistoricalRun
Schedule
Delivery
DateRange
```

Searchable metadata:

```text
Report
Template
Category
Resource
HistoricalRun
DateRange
Schedule
DeliveryStatus
```

Verified behavior includes:

- blank query matches,
- trimming,
- case-insensitive matching,
- partial substring matching,
- null-safe metadata,
- unrelated queries do not match,
- input ordering is preserved.

Status:

```text
AUTOMATED PASS
```

Runtime reporting objects, generation, schedules, delivery, and reporting UI are intentionally deferred.

Deferred owner:

```text
G3.30 — Reports
```

---

## 8.8 Settings Search Contract

Production component:

```text
WorkbenchSettingsSearch
```

Search document kinds:

```text
PersonalPreference
Administration
Provider
Policy
Approval
Integration
Governance
```

Searchable metadata:

```text
PersonalPreference
Appearance
Provider
CredentialReference
Policy
Role
ApprovalRule
MaintenanceWindow
Notification
Integration
GovernanceSetting
```

Verified behavior includes:

- blank query matches,
- trimming,
- case-insensitive matching,
- partial substring matching,
- null-safe metadata,
- unrelated queries do not match,
- input ordering is preserved,
- `DisplayText` is not implicitly searchable,
- `Kind` is not implicitly searchable.

Security boundary:

```text
CredentialReference
```

is searchable metadata.

The search document does not expose raw secret properties such as:

```text
Password
Secret
SecretKey
Token
PrivateKey
CredentialValue
```

This schema boundary does not replace later authorization, redaction, credential-storage, or secret-handling controls.

Status:

```text
AUTOMATED PASS
```

Runtime Settings / Administration behavior is intentionally deferred.

Deferred owner:

```text
G3.29 — Settings & Administration Foundation
```

---

## 8.9 Full Regression Baseline

Full TruthDoctor regression immediately before G3.20.8:

```text
294 / 294 PASS
```

The G3.20.8 matrix adds:

```text
26 focused matrix tests
```

Expected full-suite count after the matrix test file is included:

```text
320 tests
```

This expected count was verified with the final full regression run.

Current G3.20.8 full-regression status:

```text
320 / 320 PASS
```

---

# 9. Manual Verification Matrix

## MV-01 — Dashboard Global Search

Status:

```text
PASS
```

Procedure:

1. Navigate to Dashboard.
2. Confirm discovered resources are visible.
3. Choose an existing resource with a uniquely identifiable name.
4. Enter that exact name in the global search box.
5. Confirm Dashboard resource results are filtered to the expected match.
6. Confirm the active workspace remains Dashboard.
7. Enter an unrelated query such as:

```text
does-not-exist
```

8. Confirm no unrelated resource appears as a match.
9. Clear the global query.
10. Confirm the normal Dashboard resource population is restored.

Expected:

```text
Dashboard owns the query.
Canonical resource filtering occurs.
No workspace navigation occurs.
No unrelated workspace state is mutated.
```

Actual:

```text
49 total resources were discovered. With the existing available-state filter selected, Dashboard initially showed 17 resources.

Searching for TruthDoctor-Test-ENI reduced the Dashboard list to exactly 1 matching resource.

Searching for does-not-exist reduced the Dashboard list to 0 resources.

Clearing the global query restored the Dashboard list to 17 resources while preserving the available-state filter.

Dashboard remained active throughout and no workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-02 — Resources Global Search

Status:

```text
PASS
```

Procedure:

1. Navigate to Resources.
2. Confirm the Resources workspace is active.
3. Enter the exact name of an existing discovered resource in global search.
4. Confirm the Resources workspace search reflects the same query.
5. Confirm the expected resource remains visible.
6. Enter:

```text
does-not-exist
```

7. Confirm no unrelated resource appears as a match.
8. Confirm the workspace remains Resources.
9. Clear the query.
10. Confirm the Resources population is restored.

Expected:

```text
Resources owns the global query.
No fallback to Dashboard occurs.
No unrelated workspace state is mutated.
```

Actual:

```text
Resources initially showed 49 of 49 discovered resources with both global and local search fields blank.

Searching globally for TruthDoctor-Test-ENI propagated the query into the Resources search field and reduced the list to 1 of 49 resources.

Searching globally for does-not-exist propagated the query into the Resources search field and reduced the list to 0 of 49 resources.

Clearing the global query cleared the Resources search field and restored the list to 49 of 49 resources.

Resources remained active throughout and no Dashboard fallback or unrelated workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-03 — Topology Unique Resource Search

Status:

```text
PASS
```

Procedure:

1. Navigate to Topology.
2. Select a uniquely identifiable discovered resource name.
3. Enter that resource name in global search.
4. Confirm the Topology Find value reflects the query.
5. Confirm the resource is resolved uniquely.
6. Confirm Topology navigates to / selects / highlights the resource.
7. Enter:

```text
does-not-exist
```

8. Confirm no arbitrary resource is selected.
9. Confirm the workspace remains Topology.
10. Clear the query.

Expected:

```text
Unique canonical resource matches may drive Topology navigation.
No-match queries must not select arbitrary resources.
Ambiguous matches must not resolve arbitrarily.
```

Actual:

```text
Topology began with TruthDoctor-Test-ENI selected and both global search and Topology Find blank.

Searching globally for TruthDoctor-Test-IGW populated Topology Find, produced 1 of 1 match, uniquely resolved the resource, moved selection to TruthDoctor-Test-IGW, highlighted its node, and updated Resource Details.

Searching globally for does-not-exist populated Topology Find with a No matches result and did not select an arbitrary resource; TruthDoctor-Test-IGW remained selected.

Clearing the global query cleared Topology Find while leaving the graph visible and the existing TruthDoctor-Test-IGW selection intact.

Topology remained active throughout and no unrelated workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-04 — Operations Resource Search

Status:

```text
PASS
```

Procedure:

1. Navigate to Operations.
2. Clear the global query.
3. Confirm the blank Operations view reports the current searchable resource population.
4. Enter the exact name of an existing discovered resource.
5. Confirm an Operations search result appears for that resource.
6. Select the result.
7. Confirm the expected resource remains associated with the result/details.
8. Enter:

```text
does-not-exist
```

9. Confirm zero Operations results are shown.
10. Confirm the workspace remains Operations.
11. Confirm no navigation to Dashboard occurs.

Expected:

```text
Operations owns the query.
Resource search remains provider-neutral.
Unrelated queries produce no result.
```

Actual:

```text
Operations initially showed 49 searchable resources with the global search blank.

Searching globally for TruthDoctor-Test-ENI reduced Operations to exactly 1 resource result while Operations remained active.

Selecting the TruthDoctor-Test-ENI result updated Resource Details and the selected resource context to TruthDoctor-Test-ENI.

Searching globally for does-not-exist reduced Operations to 0 search results without changing to an unrelated resource or workspace.

Clearing the global query restored the blank Operations view with 49 searchable resources while preserving the existing TruthDoctor-Test-ENI detail selection.

Operations remained active throughout and no Dashboard fallback occurred.
```

Result:

```text
PASS
```

---

## MV-05 — Operations Capability Search

Status:

```text
AUTOMATED PASS / LIVE PROVIDER DATA DEFERRED
```

Automated capability-search contract:

```text
PASS
```

Current live-provider limitation:

```text
The current provider-discovery state does not yet advertise
live resource capabilities.
```

Therefore a live manual capability result is not required for G3.20.8.

This must not be treated as:

```text
FAIL
```

Deferred owner:

```text
G3.22 — Provider Capability Advertisement
```

Manual result:

```text
DEFERRED BY ROADMAP
```

---

## MV-06 — History Isolation

Status:

```text
PASS
```

Procedure:

1. Navigate to History.
2. Confirm History is visibly active.
3. Enter a global query such as:

```text
CloudWatch-Metrics-Lab
```

4. Confirm the active workspace remains History.
5. Confirm Dashboard does not become active.
6. Confirm the History workspace is not replaced by Dashboard resource results.
7. Enter:

```text
does-not-exist
```

8. Confirm the workspace still remains History.

Expected:

```text
History remains the active workspace.
Current UI search behavior is a safe no-op.
No Dashboard fallback occurs.
No unrelated workspace state is mutated.
```

Contract status:

```text
AUTOMATED PASS
```

Runtime dependency:

```text
G3.27 — History
```

Actual:

```text
History began active with the global search blank and Discovery history content visible.

Searching globally for CloudWatch-Metrics-Lab left History active, preserved the History content, and did not invoke Dashboard resource filtering.

Searching globally for does-not-exist also left History active without substituting another workspace or changing the existing TruthDoctor-Test-ENI detail context.

Clearing the global query left History active with its original content visible.

No Dashboard fallback or unrelated workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-07 — Reports Isolation

Status:

```text
PASS
```

Procedure:

1. Navigate to Reports.
2. Confirm Reports is visibly active.
3. Enter a global query such as:

```text
CloudWatch-Metrics-Lab
```

4. Confirm the active workspace remains Reports.
5. Confirm Dashboard does not become active.
6. Confirm Reports is not replaced by Dashboard resource results.
7. Enter:

```text
does-not-exist
```

8. Confirm the workspace still remains Reports.

Expected:

```text
Reports remains the active workspace.
Current UI search behavior is a safe no-op.
No Dashboard fallback occurs.
No unrelated workspace state is mutated.
```

Contract status:

```text
AUTOMATED PASS
```

Runtime dependency:

```text
G3.30 — Reports
```

Actual:

```text
Reports began active with the global search blank and Infrastructure reports content showing 49 resources across 2 domains.

Searching globally for CloudWatch-Metrics-Lab left Reports active, preserved the Reports content, and did not invoke Dashboard resource filtering.

Searching globally for does-not-exist also left Reports active without substituting another workspace or changing the existing TruthDoctor-Test-ENI detail context.

Clearing the global query left Reports active with 49 resources and 2 domains still visible.

No Dashboard fallback or unrelated workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-08 — Settings Isolation

Status:

```text
PASS
```

Procedure:

1. Navigate to Settings.
2. Confirm Settings is visibly active.
3. Enter a global query such as:

```text
CloudWatch-Metrics-Lab
```

4. Confirm the active workspace remains Settings.
5. Confirm Dashboard does not become active.
6. Confirm Settings is not replaced by Dashboard resource results.
7. Enter:

```text
does-not-exist
```

8. Confirm the workspace still remains Settings.

Expected:

```text
Settings remains the active workspace.
Current UI search behavior is a safe no-op.
No Dashboard fallback occurs.
No unrelated workspace state is mutated.
```

Contract status:

```text
AUTOMATED PASS
```

Runtime dependency:

```text
G3.29 — Settings & Administration Foundation
```

Actual:

```text
Settings began active with the global search blank and Workbench settings content visible.

Searching globally for CloudWatch-Metrics-Lab left Settings active, preserved the Settings content, and did not invoke Dashboard resource filtering.

Searching globally for does-not-exist also left Settings active without substituting another workspace or changing the existing TruthDoctor-Test-ENI detail context.

Clearing the global query left Settings active with its original content visible.

No Dashboard fallback or unrelated workspace navigation occurred.
```

Result:

```text
PASS
```

---

## MV-09 — Existing Query Re-Routing

Status:

```text
PASS
```

Purpose:

Verify that the retained global query follows the active workspace rather than remaining owned by the workspace in which it was originally entered.

Procedure:

1. Navigate to Dashboard.
2. Enter an existing resource name in global search.
3. Leave the query populated.
4. Navigate to Resources.
5. Confirm the same query is interpreted by Resources.
6. Navigate to Topology without clearing the query.
7. Confirm the same query is interpreted by Topology.
8. Navigate to Operations without clearing the query.
9. Confirm the same query is interpreted by Operations.
10. Navigate to History without clearing the query.
11. Confirm History remains active and performs only its current safe no-op.
12. Navigate to Reports without clearing the query.
13. Confirm Reports remains active and performs only its current safe no-op.
14. Navigate to Settings without clearing the query.
15. Confirm Settings remains active and performs only its current safe no-op.

Expected:

```text
The retained global query is re-routed according to the newly active workspace.
The old workspace does not continue to own the query.
Deferred workspaces remain active.
No unsupported workspace falls back to Dashboard.
```

Actual:

```text
The global query TruthDoctor-Test-ENI was entered while Dashboard was active and then left unchanged throughout workspace navigation.

Navigating to Resources preserved the global query, populated the Resources search field, and showed 1 of 49 matching resources.

Navigating to Topology preserved the query, populated Topology Find, resolved 1 of 1 match, and selected/highlighted TruthDoctor-Test-ENI.

Navigating to Operations preserved the query and showed exactly 1 Operations search result for TruthDoctor-Test-ENI.

Navigating successively to History, Reports, and Settings preserved the same global query while each workspace remained active and performed only its current safe no-op search behavior.

At no point did the retained query force navigation back to Dashboard or leave the previous workspace as the active search owner.
```

Result:

```text
PASS
```

---

## MV-10 — No Dashboard Fallback

Status:

```text
PASS
```

Purpose:

Explicitly verify the architectural protection introduced by the context-aware global search coordinator.

Procedure:

With a nonblank global query, individually navigate to:

```text
Operations
History
Reports
Settings
```

For each workspace confirm:

1. the requested workspace becomes active,
2. the workspace remains active,
3. global search does not navigate back to Dashboard,
4. Dashboard resource search is not presented as the owner of the query.

Expected:

```text
Operations → remains Operations
History    → remains History
Reports    → remains Reports
Settings   → remains Settings
```

Automated route protection:

```text
PASS
```

Actual:

```text
The retained nonblank global query TruthDoctor-Test-ENI was manually carried through Operations, History, Reports, and Settings during MV-09.

Operations remained Operations and showed its matching Operations result.

History remained History and preserved its current safe no-op behavior.

Reports remained Reports and preserved its current safe no-op behavior.

Settings remained Settings and preserved its current safe no-op behavior.

None of these workspaces navigated back to Dashboard or presented Dashboard resource search as the active owner of the query.
```

Result:

```text
PASS
```

---

# 10. Deferred Runtime Dependencies

The following behavior is intentionally outside G3.20.8.

| Capability | Deferred Owner | G3.20.8 Treatment |
|---|---|---|
| Provider-derived Operations capabilities | G3.22 | Automated contract only |
| Runtime History objects and UI search | G3.27 | Contract-only + isolation verification |
| Settings / Administration runtime search | G3.29 | Contract-only + isolation verification |
| Runtime report objects and reporting search | G3.30 | Contract-only + isolation verification |

These items must not be reported as G3.20.8 failures.

---

# 11. Security and Architectural Boundaries

G3.20.8 must not introduce:

```text
raw credential search
password search
secret search
token search
private-key search
authorization bypass
administration mutation
policy enforcement
approval execution
maintenance scheduling
notification delivery
integration mutation
report generation
report scheduling
history persistence
provider-specific search branching
fake capability advertisement
fake History objects
fake Report objects
fake Settings objects
```

Searchability does not imply authorization.

Future runtime projections must expose only objects the current actor is authorized to view.

---

# 12. Production-Code Change Policy

G3.20.8 is a verification milestone.

Expected implementation scope:

```text
Tests/TruthDoctor.Tests/WorkbenchSearchVerificationMatrixTests.cs
docs/TruthDoctor-Search-Verification-Matrix.md
```

Production code should change only if verification reveals a genuine defect.

At the current checkpoint:

```text
Production-code changes required: NONE
```

---

# 13. G3.20.8 Completion Gate

G3.20.8 may be closed only when all applicable items below are satisfied.

## Automated

Required completion state:

```text
Cross-workspace matrix tests             MUST PASS
Canonical resource search               MUST PASS
Topology resolver                       MUST PASS
Global search coordinator               MUST PASS
Operations search                       MUST PASS
History search contract                 MUST PASS
Reports search contract                 MUST PASS
Settings search contract                MUST PASS
Focused search regression               MUST PASS
Full TruthDoctor regression              MUST PASS
dotnet build                             MUST PASS
```

## Manual

Required completion state:

```text
MV-01 Dashboard global search            MUST PASS
MV-02 Resources global search            MUST PASS
MV-03 Topology unique search             MUST PASS
MV-04 Operations resource search         MUST PASS
MV-05 Operations capability search       DEFERRED / AUTOMATED PASS
MV-06 History isolation                  MUST PASS
MV-07 Reports isolation                  MUST PASS
MV-08 Settings isolation                 MUST PASS
MV-09 Existing-query re-routing          MUST PASS
MV-10 No Dashboard fallback              MUST PASS
```

## Repository Quality

Required completion state:

```text
Deferred dependencies documented         MUST PASS
No unintended production changes         MUST PASS
Working-tree scope reviewed              MUST PASS
git diff --cached --check                MUST PASS
Final staged diff reviewed               MUST PASS
Commit                                   MUST PASS
Push                                     MUST PASS
Local / remote synchronization           MUST PASS
Working tree clean                       MUST PASS
```

---

# 14. Current Checkpoint

Current verified automated state:

```text
G3.20.8 matrix tests                     26 / 26 PASS
Focused search regression                185 / 185 PASS
Full post-matrix regression              320 / 320 PASS
Final build                              PASS
Production changes                       NONE
Manual verification                     COMPLETE — MV-01/MV-02/MV-03/MV-04/MV-06/MV-07/MV-08/MV-09/MV-10 PASS; MV-05 DEFERRED/AUTOMATED PASS
Documentation review                     PASS
Working-tree scope review                PASS
git diff --cached --check                PASS
Final staged diff review                 PASS
Commit / push                            PENDING
```

The next required activity is:

```text
Commit the G3.20.8 search verification matrix
```

Manual verification was performed one case at a time, with each actual result recorded only after the case was observed.
