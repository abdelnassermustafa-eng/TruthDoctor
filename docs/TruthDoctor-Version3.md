# TruthDoctor Authoritative Hybrid Infrastructure Intelligence & Operations Roadmap v3

**Version:** 3.0
**Date:** October 17, 2026
**Status:** Authoritative long-term product and engineering roadmap
**Builds Upon:** TruthDoctor Authoritative Hybrid Multicloud Roadmap v2
**Historical Preservation:** Roadmap v2 remains preserved as the prior authoritative architectural baseline.
**Current Verified Development Baseline:** `3f239c7` — `fix: route global search into topology`
**Current Automated Test Baseline:** 158 passed, 0 failed
**Repository State at Adoption:** `main` synchronized with `origin/main`

---

# Executive Purpose

TruthDoctor is a provider-neutral **Hybrid Infrastructure Intelligence and Operations** platform.

Its mission is broader than infrastructure discovery, inventory, visualization, or monitoring alone. Those capabilities are necessary, but incomplete.

TruthDoctor must help operators and administrators understand, govern, safely change, verify, and audit infrastructure across public cloud, private cloud, datacenters, sites, edge, networking, Kubernetes, storage, security infrastructure, applications, and services.

The authoritative TruthDoctor lifecycle is:

```text
DISCOVER
   ↓
MONITOR
   ↓
UNDERSTAND
   ↓
DECIDE
   ↓
PLAN
   ↓
APPROVE / SCHEDULE
   ↓
ACT
   ↓
VERIFY
   ↓
AUDIT
```

**Governance surrounds and continuously controls the entire lifecycle.**

Governance is not a single sequential stage. It determines, throughout the lifecycle:

- who may see infrastructure;
- who may act;
- which policies apply;
- what risk is acceptable;
- which approvals are required;
- when an operation may execute;
- what evidence is required;
- how history is retained;
- which actions are prohibited;
- when exceptions or emergency procedures may apply.

TruthDoctor must be able to answer not only:

- What exists?
- Where is it?
- How is it connected?
- Is it healthy?

but also:

- What depends on it?
- What changed?
- What will happen if I change it?
- Am I authorized to change it?
- Does the action require confirmation, approval, or scheduling?
- What is the likely blast radius?
- Can the change be rolled back?
- What application or business service may be affected?
- Did the provider actually perform the requested operation?
- Did the resulting infrastructure state become healthy?
- What evidence proves the outcome?
- Who requested, approved, executed, and verified the change?

---

# Part I — Continuity With Roadmap v2

## 1.1 Preservation Rule

Roadmap v3 is an **architectural expansion and clarification** of Roadmap v2.

It does not erase completed work, rewrite historical milestones, or reset TruthDoctor as though development were beginning again.

Roadmap v2 remains preserved as the historical architectural baseline.

## 1.2 Preserved Mission

TruthDoctor remains provider-neutral and continues to target:

- Public clouds
- Private clouds and virtualization platforms
- On-premises and bare-metal infrastructure
- Physical and virtual networks
- Kubernetes and container platforms
- Security infrastructure
- Application-delivery infrastructure
- Engineering and laboratory environments
- Imported and offline infrastructure representations
- Declarative infrastructure definitions

## 1.3 Preserved Architectural Commitments

The strongest v2 principles remain binding:

1. Universal engines come before provider-specific convenience.
2. Providers contribute capabilities; the core must not hard-code vendors.
3. Observation and evidence come before action.
4. Operations must be authorized, confirmed where appropriate, auditable, and verifiable.
5. A successful UI message must correspond to confirmed provider success.
6. Missing, stale, unavailable, or partially discovered infrastructure must fail safely.
7. Hybrid environments must be understood as one connected system.
8. Credentials remain isolated from plugins and user-interface code.
9. AI answers, recommendations, and scores must expose supporting evidence.
10. Each milestone requires build, automated tests, manual verification, commit, push, and synchronization.

---

# Part II — Product Mission

TruthDoctor will become a provider-neutral **Hybrid Infrastructure Intelligence and Operations** platform spanning:

- Public cloud
- Multicloud
- Private cloud
- Datacenters
- Sites
- Edge
- Bare metal
- Physical and virtual networking
- Kubernetes and container platforms
- Storage
- Databases
- Security infrastructure
- Identity
- Applications
- Business and technical services
- Engineering/lab environments
- Imported, offline, and declarative infrastructure

TruthDoctor will normalize these environments into universal models for resources, relationships, capabilities, operations, policies, telemetry, evidence, history, sites, applications, and services.

It will support both human-driven and policy-driven workflows while enforcing truthful state, safe execution, evidence-based verification, and operational accountability.

---

# Part III — Governing Principles

1. **Provider-neutral core** — Core application behavior must not depend on vendor-specific assumptions.
2. **Universal models before provider convenience** — Core abstractions must remain stable as providers expand.
3. **Observe before acting**.
4. **Understand dependencies before acting**.
5. **Every action has intent**.
6. **Every meaningful action has a plan**.
7. **Risk is explicit**.
8. **Govern high-risk change**.
9. **No false success**.
10. **Execution is incomplete until verification**.
11. **Everything important is auditable**.
12. **Uncertainty is explicit**.
13. **Search is contextual**.
14. **Infrastructure is one connected system**.
15. **AI is evidence-grounded**.
16. **Safety enforcement belongs in execution layers**.
17. **Least privilege is the default**.
18. **Separation of duties is supported and enforceable**.
19. **Provider capability drives available actions**.
20. **Every milestone remains independently testable and recoverable**.
21. **Governance continuously controls the lifecycle**.
22. **Operations exist to enable the operator to do real infrastructure work safely**.

---

# Part IV — Core Universal Models

TruthDoctor v3 converges on provider-neutral core models.

## 4.1 Resource
Represents a manageable or observable infrastructure object such as a VM, bare-metal server, network interface, subnet, route table, gateway, firewall, database, Kubernetes workload, hypervisor, switch, storage system, application component, or site.

## 4.2 Relationship
Represents dependency, connectivity, ownership, containment, association, service linkage, or operational relationship. Examples include `contains`, `depends-on`, `connected-to`, `routes-through`, `secured-by`, `attached-to`, `hosted-on`, `member-of`, `serves`, and `owned-by`.

## 4.3 Capability
Represents what TruthDoctor can observe or do for a resource, such as Discover, Inspect, Retrieve configuration, Read telemetry, Start, Stop, Restart, Resize, Scale, Attach, Detach, Backup, Restore, Failover, Patch, Upgrade, or Delete.

## 4.4 Operation
Represents a proposed, approved, scheduled, executing, verifying, completed, failed, cancelled, or prohibited change.

## 4.5 Policy
Represents governance, compliance, authorization, or operational intent.

## 4.6 Evidence
Represents observable proof supporting a conclusion, recommendation, operation result, or verification outcome.

## 4.7 History Event
Represents a durable event in TruthDoctor's audit, action, change, discovery, and verification timeline.

## 4.8 Site
Represents a physical or logical infrastructure location.

## 4.9 Application / Service
Represents business and technical services that depend on infrastructure.

---

# Part V — Discovery & Inventory

TruthDoctor should discover and normalize:

## 5.1 Public Cloud
AWS, Azure, GCP, OCI, and future validated providers.

## 5.2 Private Cloud / Virtualization
VMware, Hyper-V, OpenStack, Nutanix, Proxmox, and future validated platforms.

## 5.3 Bare Metal / Datacenter
Physical servers, chassis, racks, storage, network devices, out-of-band management, and facility/location metadata.

## 5.4 Networking
Routers, switches, firewalls, load balancers, interfaces, VLANs, VRFs, routes, neighbors, tunnels, VPNs, DNS, and IPAM.

## 5.5 Kubernetes / Containers
Clusters, nodes, namespaces, workloads, pods, services, ingress, policies, persistent volumes, controllers, and identities.

## 5.6 Security / Identity
Roles, policies, security groups, firewall policy, key-management relationships, identity associations, and exposure state.

## 5.7 Imported / Offline
TruthDoctor snapshots, Terraform state, CloudFormation, ARM/Bicep, Kubernetes manifests, JSON/YAML, vendor configuration, and other declarative sources.

---

# Part VI — Datacenter, Site, and Edge Model

Datacenters and sites are first-class scope.

```text
Organization
 └── Business Unit
      └── Environment
           ├── Cloud Estate
           │    ├── Provider
           │    ├── Account / Subscription / Project
           │    ├── Region
           │    └── Zone
           ├── Datacenter
           │    ├── Site
           │    ├── Building
           │    ├── Room
           │    ├── Rack
           │    ├── Device
           │    ├── Hypervisor
           │    ├── VM
           │    ├── Storage
           │    └── Network
           └── Edge Site
                ├── Compute
                ├── Network
                └── Devices
```

Capabilities should eventually include site inventory, rack/device hierarchy, physical-to-virtual mapping, site topology, WAN connectivity, redundancy analysis, maintenance domains, outage analysis, dependency impact, capacity/utilization, and environmental integration where available.

---

# Part VII — Monitoring & Observability

Monitoring is a first-class subsystem.

## 7.1 Universal Signal Types
Metrics, logs, events, alerts, alarms, health state, traces, provider status, hardware health, network telemetry, and synthetic checks.

## 7.2 Monitoring Capabilities
Real-time and historical health, trend analysis, alert correlation, noise reduction, health scoring, dependency-aware alerting, anomaly detection, threshold detection, service-impact analysis, and provider telemetry integration.

## 7.3 Truthful Monitoring States
TruthDoctor should distinguish Healthy, Degraded, Unhealthy, Unknown, Stale, Partially observed, and Unavailable.

---

# Part VIII — Understanding & Infrastructure Intelligence

Capabilities include dependency analysis, dependents, neighborhood analysis, blast radius, reachability, path analysis, failure-domain analysis, topology traversal, configuration interpretation, root-cause assistance, change comparison, drift detection, service impact, risk estimation, simulation, and recommendations.

Example questions:

```text
What depends on this subnet?
What happens if this switch is restarted?
Why is this application unhealthy?
Can this workload reach the database?
Which production services depend on this route table?
```

---

# Part IX — Application & Service Model

Applications and services are first-class scope.

```text
Business Service
   ↓
Application
   ↓
Application Component
   ↓
Runtime / Container / VM
   ↓
Infrastructure
   ↓
Network
   ↓
Storage / Database
```

The model should support ownership, environment, criticality, SLA/SLO metadata, business importance, infrastructure dependencies, cross-provider dependencies, health, change impact, and service topology.

---

# Part X — Context-Aware Global Search

## 10.1 Search Architecture

```text
One global search control
        ↓
Context-aware search coordinator
        ↓
Workspace-specific search engine
```

## 10.2 Dashboard Search
Search resources, IDs, states, high-level health, and major status data.

## 10.3 Resources Search
Search resource names, native IDs, universal IDs, resource types, domains, locations, accounts, states, properties, and tags.

## 10.4 Topology Search
Preserve the implemented behavior: resolve globally, navigate/select when unique, build topology, synchronize local find, highlight, and focus.

## 10.5 Operations Search
Search resources, capabilities, operations, actions, runbooks, targets, pending requests, approval state, and failure state.

## 10.6 History Search
Search actor, action, target, date, result, provider, environment, approval, verification, correlation ID, and operational effect.

## 10.7 Reports Search
Search report, template, category, resource, historical run, date range, schedule, and delivery status.

## 10.8 Settings Search
Search personal preferences, appearance, providers, credentials, policies, roles, approval rules, maintenance windows, notifications, integrations, and governance settings.

---

# Part XI — Universal Provider Capability Contract

Every provider should advertise what it supports, including resource type, capability, operation, required permissions, risk characteristics, reversibility, dry-run support, scheduling support, rollback support, verification support, progress/cancellation, telemetry, configuration retrieval, and optional cost/security evidence.

Example universal capabilities:

```text
RestartResource
StopResource
StartResource
ResizeResource
ScaleResource
AttachResource
DetachResource
CreateResource
DeleteResource
FailoverResource
BackupResource
RestoreResource
PatchResource
UpgradeResource
ChangeRoute
ChangeSecurityPolicy
```

Provider adapters translate universal intent into native APIs.

---

# Part XII — Operations Engine

Operations is the operator's universal command center. Its purpose is to **enable the operator to do real infrastructure work safely**.

```text
Intent
  ↓
Resolve Target
  ↓
Resolve Capability
  ↓
Preconditions
  ↓
Impact Analysis
  ↓
Risk Classification
  ↓
Operation Plan
  ↓
Authorization
  ↓
Approval
  ↓
Schedule
  ↓
Revalidate
  ↓
Execute
  ↓
Observe
  ↓
Verify
  ↓
Evidence
  ↓
Audit
```

Operation categories include lifecycle, compute, networking, storage, database, Kubernetes, security, datacenter, and application operations.

---

# Part XIII — Operation Risk & Safety Model

The authoritative risk scale is:

```text
R0 — Read Only
R1 — Low
R2 — Moderate
R3 — High
R4 — Critical
R5 — Prohibited
```

Risk evaluation may consider production status, resource criticality, protection flags, affected-resource count, dependency count, application/service impact, reversibility, backup state, redundancy, security impact, network impact, financial impact, maintenance windows, provider health, and verification confidence.

---

# Part XIV — Approval, Change, and Scheduling Engine

Approval and scheduling are native TruthDoctor concepts even when external ITSM systems are integrated.

TruthDoctor should maintain an `OperationRequest` or `ChangeRequest` containing request ID, requester, action, targets, intent, expected outcome, risk, blast radius, preconditions, rollback plan, required approvals, requested schedule, maintenance window, state, execution result, verification result, evidence, and audit trail.

Approval may be surfaced through native in-app approval, email notification/link, Teams, Slack, ServiceNow, Jira, and API/webhook.

Separation of duties is mandatory for selected high-risk operations, with controlled policy-based flexibility. Scheduled operations must be revalidated immediately before execution.

---

# Part XV — Verification Engine

Verification is mandatory whenever technically possible before TruthDoctor reports success.

```text
Provider API accepted request
       ↓
Observed resource state
       ↓
Health validation
       ↓
Dependency validation
       ↓
Application validation
       ↓
Expected-state comparison
       ↓
Verified / Failed / Indeterminate
```

Verification may use rediscovery, state polling, metrics, logs, events, reachability, topology comparison, application probes, provider health APIs, policy checks, and configuration comparison.

---

# Part XVI — History, Evidence, and Audit Ledger

History is TruthDoctor's durable record of:

> **who did what, when they did it, and what effect the action had.**

Each significant event should support timestamp, actor, workspace, action, target, provider, environment, intent, operation plan, risk, authorization, approvals, schedule, provider response, before state, after state, operational effect, verification, evidence, result, and correlation ID.

History covers discoveries, resource changes, configuration changes, operations, approvals, denials, policy findings, drift, verification, failures, recovery, and user/admin changes.

---

# Part XVII — Policy & Governance Engine

Governance surrounds and continuously controls the entire lifecycle.

Policy applies to both observed state and proposed actions. Capabilities include policy definition, provider-neutral assertions, scope, severity, exceptions, expiry, evidence, compliance status, proposed-operation evaluation, automated remediation, approval-based remediation, and audit.

---

# Part XVIII — Identity, Authorization, and Administration

Potential roles include Viewer, Operator, Network Operator, Cloud Engineer, Application Operator, Security Operator, Approver, Change Manager, Auditor, Administrator, and Platform Owner.

Authorization combines:

```text
RBAC
+
policy conditions
+
resource scope
+
environment
+
operation
+
risk
```

Settings is divided into:

## Personal Preferences
Appearance, theme, density, display preferences, personal workspace defaults, and other non-security-sensitive preferences.

## Platform Administration
Users, groups, roles, permissions, provider access, credential references, policies, approval chains, maintenance windows, notifications, site connectors, external integrations, audit retention, security settings, and functional application behavior.

---

# Part XIX — Event-Driven Automation

```text
Event
 ↓
Rule
 ↓
Correlation
 ↓
Diagnosis
 ↓
Recommended Action
 ↓
Policy
 ↓
Automatic / Approval Required
 ↓
Execute
 ↓
Verify
 ↓
Audit
```

Automation remains policy-controlled and evidence-driven.

---

# Part XX — Runbooks & Workflow Orchestration

Runbooks evolve into controlled workflows containing checks, actions, verification, rollback, waits, conditions, approvals, parallel branches, retries, and human tasks.

---

# Part XXI — Distributed Execution & Site Connectors

Private environments should not require unsafe inbound management exposure.

```text
TruthDoctor Control Plane
        ↓
Secure Site / Execution Connector
        ↓
Private Datacenter / Edge / Network
```

Connectors should support outbound-only secure sessions, mutual authentication, scoped credentials, local provider access, discovery, execution, telemetry, job expiry, cancellation, replay protection, evidence return, health, upgrades, rotation, and high availability where needed.

---

# Part XXII — Configuration, Drift, and Desired State

Capabilities include configuration capture/versioning, comparison, secret redaction, drift detection, desired-state import, intended-vs-actual comparison, unauthorized-change detection, rollback, and governed remediation.

---

# Part XXIII — FinOps & Capacity Intelligence

Capabilities include cost inventory/allocation, budgets, forecasts, idle-resource detection, rightsizing, commitments, private-cloud capacity, licensing, showback/chargeback, cost-aware operation planning, and capacity forecasting.

---

# Part XXIV — Lifecycle Management

Lifecycle management includes operating systems, hypervisors, Kubernetes, network OS, firmware, agents, server software, and application/runtime versions.

```text
Discover versions
→ identify unsupported/outdated components
→ dependency analysis
→ upgrade plan
→ maintenance window
→ approval
→ execute
→ verify
→ audit
```

---

# Part XXV — Reports

Reports are a complete reporting subsystem.

## 25.1 Report Types
Inventory, topology, security, compliance, risk, availability, history, change, operations, failed operations, cost, capacity, backup, drift, lifecycle, application/service impact, and lab validation.

## 25.2 Historical Reports
Previously generated reports remain available with generation date/time, report period, scope, requester, delivery status, and evidence/freshness metadata.

## 25.3 Date-Range Reporting
Reports may be generated from one date/time to another, with presets such as Last 24 hours, Last 7 days, Last 30 days, This month, Previous month, Custom range, Since last deployment, and Between infrastructure snapshots.

## 25.4 Scheduled Future Reports
Authorized Operators and Administrators can schedule future reports.

Examples:

- Generate an operations report after a maintenance window finishes.
- Send a maintenance-completion report to management and administrators.
- Generate a weekly security/compliance report.
- Generate a monthly capacity and cost report.
- Generate a report after a scheduled change window.
- Generate and distribute a verification summary after planned maintenance.

A scheduled report definition should include report/template, scope, from/to calculation rule, schedule or triggering event, recipient list, delivery channel, authorization owner, retention policy, failure/retry behavior, and audit record.

## 25.5 Report Delivery
Future delivery channels may include the in-application archive, email, management distribution lists, administrator distribution lists, Teams/Slack, external document repositories, and API/webhooks.

Scheduled report delivery must be auditable and respect authorization/redaction rules.

## 25.6 Report Features
Templates, saved reports, generated reports, scheduled reports, historical runs, export, sharing, search, authorization, evidence timestamps, freshness metadata, and delivery tracking.

---

# Part XXVI — Cloud Operations Assistant / AI Intelligence

The assistant must operate on verified TruthDoctor engines using resource graph, service graph, telemetry, history, capabilities, policy, runbooks, reports, and evidence.

It must distinguish observed fact, inference, recommendation, simulation, proposed operation, approved operation, executed operation, and verified outcome.

A request such as `Fix it` should generate an operation plan subject to normal risk, authorization, approval, execution, verification, and audit controls.

---

# Part XXVII — External Enterprise Integrations

Potential integrations include ServiceNow, Jira, Teams, Slack, email, SIEM, CMDB, monitoring platforms, identity providers, ticketing/change systems, webhooks, and notifications.

TruthDoctor retains its own operation/evidence model even when external systems provide enterprise approval or workflow.

---

# Part XXVIII — Provider Ecosystem

## Public Cloud
AWS, Azure, GCP, OCI.

## Private Cloud / Virtualization
VMware, Hyper-V, OpenStack, Nutanix, Proxmox.

## Kubernetes
EKS, AKS, GKE, OpenShift, Rancher-managed clusters, vanilla Kubernetes.

## Networking
Cisco, Juniper, Arista, firewalls, load balancers, SD-WAN platforms, network controllers.

## Labs
EVE-NG and future engineering/simulation systems.

## Datacenter
Server platforms, storage platforms, out-of-band management, and environmental/power systems where practical.

---

# Part XXIX — Competitive Capability Adoption Matrix

The Competitive Capability Adoption Matrix is a permanent roadmap tool.

| Capability Pattern | Industry Example | TruthDoctor Status | v3 Treatment |
|---|---|---|---|
| Hybrid resource governance | Azure Arc | Overlapping | Strengthen |
| Managed-node execution | AWS Systems Manager | Partial/planned | Formalize |
| Automation workflows | Ansible | Planned | Expand |
| Human approval gates | Ansible / ServiceNow | Planned | Formalize |
| Dynamic service mapping | ServiceNow ITOM | Not explicit enough | Add |
| Fleet lifecycle management | VMware VCF Operations | Partial | Add |
| Distributed execution | Automation Mesh pattern | Site connectors existed broadly | Expand |
| Continuous remediation | Policy-driven platforms | Policy broad | Add |
| ITSM/change integration | ServiceNow / Jira | Not explicit | Add |
| Cost/capacity intelligence | Cloud/operations tooling | Planned | Deepen |
| Alert/event correlation | ITOM/observability platforms | Not explicit enough | Add |
| Application/service impact | Service mapping platforms | Graph enables it | Strongly add |
| Predictive capacity | Operations platforms | FinOps broad | Add later |
| Fleet upgrade orchestration | Infrastructure lifecycle tools | Not explicit enough | Add |

For every adopted capability, document why it is useful, its universal abstraction, owning engine, provider dependencies, security/safety implications, verification requirements, implementation phase, and whether it is core, optional, or provider-specific.

---

# Part XXX — Workspace Ownership Architecture

## Dashboard
Mission control: health, discovery state, important alerts, counts, scope, and high-level operational status.

## Resources
Universal inventory: search, filtering, selection, details, capabilities, properties/tags, relationships.

## Topology
Relationship/dependency exploration: graph visualization, traversal, search/focus, reachability, blast radius, saved views, and later topology history.

## Operations
Universal command center: resources/actions, operation requests, plans, risk, approvals, schedules, execution, verification, active/pending operations, and runbooks.

## History
Audit/evidence/change timeline: discovery history, changes, operations, approvals, verification, failures, recovery, configuration history, user/admin actions, and operational effects.

## Reports
Reproducible intelligence: templates, saved/generated reports, historical reports, date-range reporting, scheduled future reporting, post-maintenance reporting, distribution, historical runs, and export.

## Settings
Personal preferences plus authorized platform administration/governance.

---

# Part XXXI — Release and Safety Gates

A capability is not complete until appropriate gates pass:

1. Main build.
2. Test-project build.
3. Unit tests.
4. Integration tests where applicable.
5. Provider-contract tests where applicable.
6. Failure behavior.
7. Cancellation behavior where applicable.
8. Authorization behavior.
9. Operation safety review where applicable.
10. Audit/evidence verification where applicable.
11. Manual UI verification.
12. Documentation.
13. Git diff review.
14. `git diff --cached --check`.
15. Commit.
16. Push.
17. Synchronization confirmation.

High-risk operation features additionally require rollback, partial-failure, provider-uncertainty, restart/recovery, bypass, audit-integrity, and verification-correctness testing.

---

# Part XXXII — Program Execution Layers

## Layer A — Universal Foundation
Resource model, relationship graph, topology, search, workspace stabilization, state integrity, UI truthfulness.

## Layer B — Operations Safety Foundation
Capabilities, operation request/plan, risk, authorization, approval, verification, evidence, audit.

## Layer C — AWS Reference Provider
Complete discovery, relationships, configuration, operations, telemetry, verification, cost/security evidence.

## Layer D — Hybrid Provider Expansion
Azure, GCP, OCI, private cloud, Kubernetes, networking, labs, enterprise hierarchy.

## Layer E — Datacenter / Site / Edge
Site model, physical hierarchy, connectors, execution nodes, physical/virtual topology, edge.

## Layer F — Intelligence & Observability
Telemetry, health, correlation, drift, impact, simulation, reachability, root-cause assistance.

## Layer G — Governance
Policy, RBAC, conditional authorization, separation of duties, approvals, maintenance windows, compliance.

## Layer H — Automation & Orchestration
Runbooks, workflows, event-driven operations, scheduling, remediation, rollback.

## Layer I — Applications & Services
Application graph, business-service model, ownership, health, impact.

## Layer J — Enterprise Integrations
ITSM, CMDB, SIEM, identity, messaging, notifications, webhooks.

## Layer K — Reports / FinOps / Lifecycle
Reports, historical reporting, scheduled future reporting, distribution, cost, capacity, licensing, versions, upgrades.

## Layer L — Ecosystem & Release
SDK, provider certification, dynamic plugins, packaging, security hardening, compatibility, upgrades, release management.

---

# Part XXXIII — Definition of a Genuine TruthDoctor Platform

TruthDoctor should claim full Hybrid Infrastructure Intelligence and Operations capability only when it can demonstrate:

- At least two public clouds through the same universal core.
- At least one private/on-premises environment.
- Kubernetes.
- Physical and virtual networking.
- Datacenter/site hierarchy.
- Applications/services related to infrastructure.
- Cross-provider identity and relationships.
- Monitoring/telemetry correlated to graph state.
- Context-aware global search.
- Dynamic capability advertisement.
- Policy-controlled operations.
- Risk assessment.
- Approval and scheduling.
- Separation of duties.
- Secure private-site execution.
- Verification before operation success is declared.
- Durable searchable history/evidence.
- Drift and configuration history.
- Reproducible authorized reports.
- Scheduled and distributed reports.
- Runbooks/workflows.
- Truthful handling of stale, partial, unavailable, or failed states.
- Provider expansion without core application forks.

Until these criteria are met, product language should say TruthDoctor is **evolving toward comprehensive Hybrid Infrastructure Intelligence and Operations**.

---

# Part XXXIV — Current Verified Development Checkpoint

## G3.19 — Topology Saved Views and Workspace Presets
**Status:** Complete

```text
865809f feat: manage topology saved views
```

## G3.20.1 — Canonical Provider-Neutral Resource Search
**Status:** Complete

```text
7286464 feat: unify workbench resource search
```

## G3.20.2 — Topology Global Search
**Status:** Complete

```text
3f239c7 fix: route global search into topology
```

Verified behavior includes top-bar routing into Topology, unique resource resolution, topology navigation, local-search synchronization, graph focus/highlight, selected-resource/details synchronization, and navigation-history preservation.

## Current Regression Baseline

```text
158 total
158 passed
0 failed
```

## Current Repository Baseline

```text
main synchronized with origin/main
HEAD: 3f239c7
```

## Current Active Milestone

> **G3.20 — Context-Aware Global Search and Operational Navigation Foundation**

---

# Part XXXV — Near-Term Development Sequence

## G3.20 — Context-Aware Global Search and Operational Navigation Foundation

### G3.20.1 — Canonical Resource Search
**Complete**

### G3.20.2 — Topology Global Search
**Complete**

### G3.20.3 — Context-Aware Global Search Coordinator
Define explicit routing and workspace ownership without encoding temporary placeholder behavior as permanent architecture.

### G3.20.4 — Operations Search Foundation
Define search across resources, capabilities, actions, runbooks, operation requests, approval state, and execution state.

### G3.20.5 — History Search Contract
Define searchable audit/history/effect objects.

### G3.20.6 — Reports Search Contract
Define searchable report definitions, templates, historical runs, schedules, delivery, and date-range metadata.

### G3.20.7 — Settings Search Contract
Define searchable personal preferences, administration, providers, policy, approvals, integrations, and governance settings.

### G3.20.8 — Search Verification Matrix
Automated and manual validation across every workspace.

## G3.21 — Workspace Ownership Contracts

### G3.21.1 — Dashboard Ownership Contract
### G3.21.2 — Resources Ownership Contract
### G3.21.3 — Topology Ownership Contract
### G3.21.4 — Operations Ownership Contract
### G3.21.5 — History Ownership Contract
### G3.21.6 — Reports Ownership Contract
### G3.21.7 — Settings Ownership Contract
### G3.21.8 — Cross-Workspace Navigation Contract
### G3.21.9 — Empty / Loading / Error State Contract
### G3.21.10 — Workspace Verification Matrix

## G3.22 — Universal Operation Capability Contract

### G3.22.1 — Universal Capability Identity
### G3.22.2 — Capability-to-Resource Contract
### G3.22.3 — Provider Capability Advertisement
### G3.22.4 — Capability Preconditions
### G3.22.5 — Reversibility & Rollback Metadata
### G3.22.6 — Verification Capability Metadata
### G3.22.7 — Cancellation / Progress Contract
### G3.22.8 — Unsupported-Capability Behavior
### G3.22.9 — Capability Contract Tests

## G3.23 — Operation Request & Plan Model

### G3.23.1 — Operation Intent Model
### G3.23.2 — Target Model
### G3.23.3 — Desired Outcome Model
### G3.23.4 — Preconditions
### G3.23.5 — Planned Steps
### G3.23.6 — Dependency Ordering
### G3.23.7 — Rollback / Compensation Plan
### G3.23.8 — Estimated Impact
### G3.23.9 — Evidence Requirements
### G3.23.10 — Plan Review Representation
### G3.23.11 — Operation Correlation ID
### G3.23.12 — Persistence / Serialization Tests

## G3.24 — Risk Classification Foundation

### G3.24.1 — R0–R5 Risk Model
### G3.24.2 — Environment Risk
### G3.24.3 — Blast-Radius Contribution
### G3.24.4 — Reversibility Contribution
### G3.24.5 — Service Criticality Contribution
### G3.24.6 — Security Impact Contribution
### G3.24.7 — Cost Impact Contribution
### G3.24.8 — Backup / Redundancy Contribution
### G3.24.9 — Risk Explanation
### G3.24.10 — Policy Overrides / Escalation
### G3.24.11 — Risk Regression Tests

## G3.25 — Authorization, Approval & Scheduling Contract

### G3.25.1 — Actor Identity Contract
### G3.25.2 — RBAC Foundation
### G3.25.3 — Resource Scope Authorization
### G3.25.4 — Conditional Authorization
### G3.25.5 — Native Approval Request
### G3.25.6 — Approval Chains
### G3.25.7 — Separation of Duties
### G3.25.8 — Flexible Separation-of-Duties Policy
### G3.25.9 — Maintenance Windows
### G3.25.10 — Scheduled Execution
### G3.25.11 — Revalidation Before Execution
### G3.25.12 — Expired / Revoked Approval
### G3.25.13 — External Approval Adapter Contract
### G3.25.14 — Authorization Bypass Tests

## G3.26 — Verification & Evidence Contract

### G3.26.1 — Expected Outcome Definition
### G3.26.2 — Provider Response Evidence
### G3.26.3 — Rediscovery Verification
### G3.26.4 — Resource-State Verification
### G3.26.5 — Health Verification
### G3.26.6 — Dependency Verification
### G3.26.7 — Reachability Verification
### G3.26.8 — Application / Service Verification
### G3.26.9 — Verification Outcomes
### G3.26.10 — Evidence Model
### G3.26.11 — No-False-Success Enforcement
### G3.26.12 — Verification Regression Tests

## G3.27 — Operational History Foundation

### G3.27.1 — History Event Model
### G3.27.2 — Actor / Who
### G3.27.3 — Timestamp / When
### G3.27.4 — Action / What
### G3.27.5 — Target / Where
### G3.27.6 — Intent / Why
### G3.27.7 — Before State
### G3.27.8 — After State
### G3.27.9 — Effect / Impact
### G3.27.10 — Approval History
### G3.27.11 — Verification Evidence
### G3.27.12 — Correlation IDs
### G3.27.13 — Search / Filtering
### G3.27.14 — Retention Contract
### G3.27.15 — Immutable / Audit Integrity Rules
### G3.27.16 — History UI v1

## G3.28 — Operations Workspace v1

### G3.28.1 — Operations Dashboard
### G3.28.2 — Resource Search
### G3.28.3 — Capability / Action Search
### G3.28.4 — Selected Target Context
### G3.28.5 — Available Operations
### G3.28.6 — Operation Request Creation
### G3.28.7 — Operation Plan Review
### G3.28.8 — Risk Presentation
### G3.28.9 — Approval Status
### G3.28.10 — Scheduling Controls
### G3.28.11 — Execution Progress
### G3.28.12 — Verification Status
### G3.28.13 — Failure / Recovery Presentation
### G3.28.14 — Operation History Link
### G3.28.15 — Safe First AWS Operation

## G3.29 — Settings & Administration Foundation

### G3.29.1 — Personal Preferences Model
### G3.29.2 — Appearance Settings
### G3.29.3 — User Defaults
### G3.29.4 — Administrator Authorization Boundary
### G3.29.5 — User / Group Administration
### G3.29.6 — Roles & Permissions
### G3.29.7 — Provider Configuration
### G3.29.8 — Credential Reference Administration
### G3.29.9 — Policy Administration
### G3.29.10 — Approval Rule Administration
### G3.29.11 — Maintenance Window Administration
### G3.29.12 — Connector Administration
### G3.29.13 — Integration Administration
### G3.29.14 — Notification Administration
### G3.29.15 — Audit / Retention Administration
### G3.29.16 — Context-Aware Settings Search
### G3.29.17 — Unauthorized-Administration Tests

## G3.30 — Reports Foundation

### G3.30.1 — Report Definition Model
### G3.30.2 — Report Template Model
### G3.30.3 — Report Run Model
### G3.30.4 — Historical Report Catalog
### G3.30.5 — Date / Time Range Model
### G3.30.6 — Preset Time Ranges
### G3.30.7 — Report Scope
### G3.30.8 — Search
### G3.30.9 — Report Generation
### G3.30.10 — Historical Run Retrieval
### G3.30.11 — Evidence / Freshness Metadata
### G3.30.12 — Authorization / Redaction
### G3.30.13 — Export Foundation
### G3.30.14 — Scheduled Report Contract
Support future, recurring, event-triggered, post-maintenance, and post-operation verification reports.
### G3.30.15 — Report Recipient / Delivery Contract
Support authorized delivery to Operators, Administrators, Management, distribution lists, and approved external recipients.
### G3.30.16 — Post-Maintenance Automatic Reporting

```text
Maintenance window ends
        ↓
TruthDoctor gathers operation/history/verification evidence
        ↓
Generates maintenance completion report
        ↓
Applies authorization/redaction
        ↓
Archives report
        ↓
Sends to management and administrators
        ↓
Records delivery result in audit history
```

### G3.30.17 — Report Delivery Failure / Retry
### G3.30.18 — Report Scheduling Audit
### G3.30.19 — Reproducibility Tests

---

# Part XXXVI — Explicit Deferred Scope

Strategically approved but not immediate:

- Full datacenter environmental monitoring
- Advanced ML anomaly detection
- Fully autonomous remediation
- Production-grade ServiceNow/Jira integration
- Automatic large-scale application dependency discovery
- Full physical rack visualization
- Predictive capacity AI
- Multi-site disaster-recovery orchestration
- Full graphical workflow designer
- Advanced FinOps optimization
- Automated fleet firmware lifecycle
- Full CMDB replacement
- Broad unattended destructive remediation
- Cross-provider transactional orchestration before operation/verification foundations stabilize

---

# Part XXXVII — Change-Control Summary From v2 to v3

## Preserved
Provider-neutral architecture, universal graph, topology, saved views, AWS reference-provider strategy, hybrid/multicloud mission, safety modes, capability contracts, operations direction, History, drift, reachability, configuration management, query language, Reports, Cloud Operations Assistant, plugin validation, dynamic providers, release hardening, private cloud, Kubernetes, hybrid connectivity, enterprise hierarchy, governance, observability, FinOps, secure site connectors, and cross-provider orchestration.

## Expanded
Product positioning to **Hybrid Infrastructure Intelligence and Operations**, operations lifecycle, risk, authorization, approval, scheduling, verification, audit/evidence, datacenter/site hierarchy, applications/services, administration, Reports, search ownership, policy remediation, lifecycle management, and scheduled future reporting/distribution.

## Added Explicitly
Context-aware search coordinator, workspace-specific search engines, service graph, operation risk model, native operation/change request, separation of duties, distributed execution fabric, event-driven automation, enterprise ITSM integration, application/service impact analysis, permanent competitive capability matrix, revalidation before scheduled execution, dedicated verification engine, first-class administration, workspace ownership architecture, historical report catalog, date-range reporting, scheduled future reports, post-maintenance automatic reporting, authorized report delivery, and report-delivery audit.

## Removed
**None.**

---

# Part XXXVIII — Authoritative Decisions Adopted in v3

1. The expanded product mission is correct.
2. Product positioning evolves toward **Hybrid Infrastructure Intelligence and Operations**.
3. The lifecycle is `Discover → Monitor → Understand → Decide → Plan → Approve/Schedule → Act → Verify → Audit`.
4. **Governance surrounds and continuously controls the lifecycle** rather than appearing as a single stage.
5. Datacenters, sites, and edge are first-class scope.
6. Applications and services are first-class scope.
7. Operations is the universal command center and exists to enable the operator to do real work.
8. History is the audit/evidence/change ledger and records who did what, when, and what effect resulted.
9. Reports is a complete reporting subsystem with historical reports, date-range generation, future scheduling, and automated delivery.
10. Settings separates operator/user personal preferences from administrator-controlled application functionality and governance.
11. Approval and scheduling remain native TruthDoctor concepts even when external ITSM systems are integrated.
12. Separation of duties is mandatory for selected high-risk operations, with controlled policy-based flexibility.
13. Verification is mandatory whenever technically possible before reporting success.
14. R0–R5 is the authoritative operation-risk model.
15. The Competitive Capability Adoption Matrix is a permanent roadmap tool.
16. G3.20–G3.30 is the approved near-term development sequence.
17. The deferred capabilities in Part XXXVI remain deferred.
18. No additional enterprise capability is currently required before adoption of v3.

---

# Part XXXIX — Development Methodology

TruthDoctor development continues using the established strict methodology:

1. Start from a clean synchronized baseline.
2. Inspect before editing.
3. Define the behavioral contract.
4. Prefer test-first implementation for pure logic and critical contracts.
5. Make isolated changes.
6. Run focused tests.
7. Run full regression tests.
8. Perform manual UI verification where appropriate.
9. Review staged diff.
10. Run `git diff --cached --check`.
11. Commit with a narrow descriptive message.
12. Push.
13. Confirm local/remote synchronization.
14. Update roadmap/handover only when the verified checkpoint changes materially.

No milestone should silently mix unrelated architectural changes.

---

# Part XL — Change-Control Rule

This roadmap is authoritative and must never be silently rewritten.

Any future roadmap revision must:

1. Preserve this version.
2. State the reason for the change.
3. Identify added, modified, deferred, or removed scope.
4. Record architectural and sequencing impact.
5. Increment the roadmap version and date.
6. Update the verified development checkpoint.
7. Review the revised roadmap before adoption.
8. Commit the new roadmap separately.
9. Push and confirm synchronization.

---

# Final Authoritative Statement

TruthDoctor evolves from:

> **Universal infrastructure discovery and management**

into:

> **Universal Hybrid Infrastructure Intelligence and Governed Operations**

without abandoning the architecture already built.

Its defining operational promise is:

> **TruthDoctor understands the infrastructure context before change, governs the decision to act, executes through provider capabilities, verifies the resulting reality, and preserves the evidence.**

That promise governs every future provider, workspace, operation, report, automation, integration, administration function, and AI capability.

---

**END OF AUTHORITATIVE ROADMAP v3**
