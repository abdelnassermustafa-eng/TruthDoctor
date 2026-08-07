# TruthDoctor Version 2 — Architecture

---

Project:
TruthDoctor Version 2

Future Platform:
Engineering Workbench

Document:
Technical Architecture

Document Version:
0.1

Document Status:
Planning and Early Implementation

Classification:
Software Architecture

Author:
Nasser Abdelghani

Created:
August 2026

Last Updated:
August 2026

---

# Purpose

This document defines the technical architecture for TruthDoctor Version 2.

TruthDoctor Version 2 evolves the existing TruthDoctor desktop application and
TruthApi backend into a modular, extensible, universal engineering platform.

The immediate objective is to provide a live companion for the AWS Master Labs.

The long-term objective is to establish the first executable foundation for
Engineering Workbench.

The architecture must support:

- live AWS discovery
- multiple AWS accounts
- multiple AWS identities
- multiple AWS Regions
- infrastructure operations
- validation
- protection and recovery
- cost visibility
- project tracking
- packaged local engineering knowledge
- automation
- future providers
- multiple presentation formats

The architecture must remain useful as the platform grows beyond AWS.

---

# Architectural Goals

The architecture should be:

- modular
- extensible
- secure
- testable
- cross-platform
- provider-independent
- account-independent
- Region-independent
- presentation-independent
- backward compatible during migration
- capable of incremental development

The system should evolve without requiring a complete rewrite.

---

# Current Version 1 Architecture

TruthDoctor Version 1 uses the following structure:

```text
TruthDoctor Desktop
        ↓
TruthApi
        ↓
AwsEc2Service
        ↓
AWS SDK
        ↓
AWS Infrastructure

Current capabilities include:

JWT authentication
AWS validation
VPC inventory
subnet inventory
route-table inventory
EC2 inventory
Internet Gateway inventory
NAT Gateway inventory
automatic refresh

Version 1 remains operational while Version 2 is introduced.

Version 2 endpoints and services should coexist with Version 1 until migration
is complete.

Version 2 High-Level Architecture
TruthDoctor Desktop
        ↓
TruthApi Version 2
        ↓
Application Services
        ↓
Provider Registry
        ↓
Provider Services
        ↓
Domain Services
        ↓
External APIs or Packaged Local Content

Expanded view:

TruthDoctor Desktop
        │
        ▼
TruthApi
        │
        ├── Environment API
        ├── Provider API
        ├── Discovery API
        ├── Operations API
        ├── Protection API
        ├── Validation API
        ├── Cost API
        ├── Knowledge API
        └── Project API
                │
                ▼
        Application Service Layer
                │
                ▼
          Provider Registry
                │
      ┌─────────┼─────────┐
      ▼         ▼         ▼
     AWS     Knowledge   User Local
   Provider   Provider    Provider
Architectural Layers

TruthDoctor Version 2 should use clearly separated architectural layers.

Presentation Layer

The presentation layer is the TruthDoctor Avalonia desktop application.

Responsibilities include:

top-level navigation
collapsible navigation
resource presentation
table output
text output
JSON output
YAML output
YAML Stream output
forms
dialogs
operation review
confirmation
progress display
validation display
cost display
knowledge reading
project progress
error presentation

The presentation layer should not contain AWS SDK logic.

The presentation layer should not determine AWS dependencies or recovery
strategies.

The presentation layer consumes TruthApi responses.

API Layer

TruthApi provides the controlled interface between the desktop application and
providers.

Responsibilities include:

authentication
authorization
endpoint routing
request validation
response serialization
cancellation handling
error handling
audit context
API versioning
security boundaries

Controllers should remain thin.

Controllers should delegate business logic to application services.

Application Service Layer

The application service layer coordinates workflows.

Examples include:

discover the current environment
discover networking resources
create an operation plan
execute a dry run
create a Restore Point
calculate cost impact
execute an approved operation
validate the result
refresh the inventory
find related knowledge
evaluate Master Labs progress

Application services should not depend on desktop UI implementation.

Provider Layer

Providers connect Engineering Workbench to external systems or packaged local
content.

Examples include:

AWS
packaged Knowledge Library
local user scripts
Terraform
Kubernetes
Docker
Azure
VMware
future providers

Each provider describes:

its identity
its version
its source type
whether network access is required
supported domains
supported discovery operations
supported search operations
supported write operations
supported output capabilities
Provider Source Types

Providers may obtain data from different sources.

RemoteApi
PackagedLocal
UserLocal
OrganizationRemote
Remote API Providers

Examples:

AWS
Azure
Kubernetes clusters
Terraform Cloud
future external systems

Characteristics:

may require credentials
may require network access
may support write operations
may return live state
Packaged Local Providers

Examples:

AWS Manual
AWS Master Labs
Linux content
Bash content
Sed content
Awk content
Python content
Ansible content
Terraform reference content
Kubernetes reference content
Cheat Sheets
Engineering Scripts
Tools
Engineering Operations Articles

Characteristics:

installed with the application
available offline
normally read-only
searchable
versioned
independently updateable in future releases
User Local Providers

Examples:

personal notes
local scripts
imported labs
custom project documentation
custom automation
organization-specific content

Characteristics:

stored on the local machine
may be editable
may be executable
must use explicit security controls
Organization Remote Providers

Examples:

enterprise knowledge services
centralized project systems
team automation repositories
private plugin repositories

Characteristics:

organization-managed
authenticated
role-controlled
centrally updated
Provider Registry

The Provider Registry maintains the providers available to the platform.

Conceptual structure:

Provider Registry
├── AWS Provider
├── Knowledge Library Provider
├── User Local Provider
├── Terraform Provider
├── Kubernetes Provider
└── Future Providers

The platform should not require hard-coded knowledge of every provider.

Each provider should register a descriptor containing:

Provider ID
Name
Version
Source type
Domains
Network requirement
Discovery support
Search support
Operation support
Read-only status

The desktop application may use this metadata to build navigation dynamically.

Common Workbench Model

Infrastructure resources, knowledge articles, scripts, tools, labs, Restore
Points, and reports are different domain objects.

They should not be forced into one weak generic internal model.

The architecture should use:

Strongly Typed Domain Model
        ↓
Workbench Adapter
        ↓
Common Workbench Envelope

The common Workbench envelope supports:

universal navigation
global search
relationships
capabilities
output rendering
export
bookmarks
project association

Strongly typed models support:

compile-time safety
validation
testing
clear documentation
safe refactoring
domain-specific behavior
Workbench Item Classification

Every common Workbench item should include stable classification information.

Recommended classification:

Provider
Domain
Category
Kind

Examples:

AWS
Infrastructure
Networking
Vpc
AWSMasterLabs
Learning
Networking
Lab
KnowledgeLibrary
Knowledge
Linux
Article
UserLocal
Automation
Shell
Script
Terraform
Automation
InfrastructureAsCode
Module
Kubernetes
Infrastructure
Workload
Deployment
Capability-Based Actions

The UI should not assume every item supports the same operations.

Each item should advertise its capabilities.

Possible capabilities include:

Read
Describe
Search
Create
Update
Modify
Rename
Tag
Untag
Associate
Disassociate
Attach
Detach
Start
Stop
Restart
Delete
Validate
Dry Run
Preview
Export
Restore
Execute
Bookmark
Open Related Lab
Open Related Article

Each capability may describe:

whether it is available
the HTTP method
whether confirmation is required
whether Dry Run is supported
whether a Restore Point is required
why the capability is unavailable

The UI should show only applicable actions.

Relationship Architecture

Relationships connect the platform into one engineering workspace.

Examples:

Subnet BELONGS_TO VPC
RouteTable ASSOCIATED_WITH Subnet
InternetGateway ATTACHED_TO VPC
Lab CREATES RouteTable
Article EXPLAINS RouteTable
Script AUTOMATES Lab
RestorePoint PROTECTS Instance
CostEntry CHARGES_FOR NatGateway
Tool ANALYZES CidrBlock

Relationships should support:

resource navigation
dependency analysis
knowledge recommendations
operation planning
Restore Point planning
project progress
architecture views
cost explanation
AWS Service Architecture

The AWS provider is the first remote provider.

Current Version 2 services include:

AwsClientFactory
AwsIdentityService
AwsRegionService
AwsResourceDiscoveryService
AwsNetworkingDiscoveryService

Future AWS services may include:

AwsComputeDiscoveryService
AwsStorageDiscoveryService
AwsIamDiscoveryService
AwsDatabaseDiscoveryService
AwsContainerDiscoveryService
AwsServerlessDiscoveryService
AwsCostService
AwsOperationService
AwsProtectionService
AwsValidationService
AWS Client Factory

AwsClientFactory is responsible for creating and reusing AWS SDK clients.

Responsibilities include:

use the standard AWS SDK credential chain
avoid storing access keys directly
create clients by Region
reuse clients safely
dispose clients correctly
expose the configured default Region
support future service clients

Conceptual structure:

AwsClientFactory
├── STS client
├── EC2 client per Region
├── IAM client
├── Cost Explorer client
├── S3 client per Region
└── Future service clients

The factory should remain independent from UI state.

AWS Identity Discovery

AwsIdentityService uses AWS STS to determine the active identity.

It should discover:

account ID
principal ID
ARN
user or role display name
discovery time

The application must not assume a specific user, role, account, or company.

AWS Region Discovery

AwsRegionService discovers Regions visible to the current AWS identity.

The Region model should include:

Region name
endpoint
opt-in status
enabled status
whether it is the configured default Region

The desktop application should allow the user to select one or more Regions.

The Region selection should be saved as user preference, not hard-coded into
the provider.

Multi-Region Discovery

Resource discovery must support one or more selected Regions.

Conceptual workflow:

Selected Regions
        ↓
Create or reuse client per Region
        ↓
Run regional discovery
        ↓
Collect results
        ↓
Merge inventory
        ↓
Sort and return

A failure in one Region should not necessarily prevent successful results from
other Regions.

Regional failures should be returned as warnings.

The application must clearly identify the Region of every regional resource.

Pagination

AWS Describe APIs may paginate responses.

Discovery services must continue requesting pages until the continuation token
is empty.

The application must not silently truncate large environments.

Pagination should be handled inside provider services, not inside controllers
or the desktop application.

AWS Networking Architecture

The initial networking discovery service supports:

VPCs
subnets
route tables

The next networking resources will include:

Internet Gateways
NAT Gateways
Security Groups
Network ACLs
VPC Endpoints
Elastic IP addresses
network interfaces
peering connections
Transit Gateways
future networking resources

The networking inventory should be strongly typed.

It should include:

account
selected Regions
discovery time
resources
warnings
Route-Table Modeling

Route tables must not be reduced to one route and one association.

The model should include:

route-table ID
name
VPC ID
main status
Region
every route
every association
every tag

Each route should include:

destination
target type
target ID
state
origin

Each association should include:

association ID
subnet ID
gateway ID
main status
state

This corrects the Version 1 limitation where only the first route and first
explicit subnet association were displayed.

Environment API

The Environment API provides lightweight session initialization.

Example:

GET /api/v2/environment

Expected information:

AWS account
active principal
default Region
available Regions
provider summaries
discovery timestamps
warnings

The Environment API should not return every resource in the account.

It should initialize the application quickly.

Lazy Loading

The application should load large domains only when needed.

Example:

Application Starts
        ↓
Load Environment
        ↓
Display Provider Summaries
        ↓
User Expands AWS
        ↓
Display AWS Domains
        ↓
User Expands Networking
        ↓
Load Networking Inventory

Lazy loading reduces:

application startup time
AWS API calls
response size
memory usage
unnecessary cost-service queries
unnecessary knowledge content loading

The same approach applies to packaged local knowledge.

Proposed API Structure

Version 1 remains available during migration.

/api/v1/...

Version 2 may include:

GET /api/v2/environment
GET /api/v2/providers
GET /api/v2/networking
GET /api/v2/compute
GET /api/v2/storage
GET /api/v2/identity
GET /api/v2/knowledge
GET /api/v2/projects
GET /api/v2/costs
GET /api/v2/protection

Write operations may include:

POST
PUT
PATCH
DELETE

Operational commands may use explicit action routes.

Examples:

POST /api/v2/aws/instances/{id}/actions/start
POST /api/v2/aws/instances/{id}/actions/stop
POST /api/v2/aws/restore-points/{id}/actions/restore
Output Rendering Architecture

The same canonical data should support:

Table
Text
JSON
YAML
YAML Stream

Conceptual design:

Provider Result
        ↓
Strong Domain Model
        ↓
Workbench Adapter
        ↓
Canonical Workbench Item
        ↓
Output Renderer
        ├── Table
        ├── Text
        ├── JSON
        ├── YAML
        └── YAML Stream

Changing display format should not trigger another AWS discovery operation.

Renderers should remain separate from provider and domain logic.

Packaged Knowledge Architecture

The Knowledge Library will be installed with the application.

Proposed packaged structure:

Application Package
├── Application Runtime
├── Providers
├── KnowledgeLibrary
│   ├── aws
│   ├── linux
│   ├── bash
│   ├── sed
│   ├── awk
│   ├── python
│   ├── ansible
│   ├── terraform
│   ├── kubernetes
│   ├── cheatsheets
│   ├── scripts
│   ├── tools
│   └── engineering_operations
└── Search Indexes

The Knowledge Library should include a manifest.

The manifest should describe:

resource ID
title
domain
category
kind
relative path
content type
topics
related labs
related resources
version
edition
search metadata

The application should load content on demand.

Knowledge Library Provider

The Knowledge Library Provider should:

read the packaged manifest
build or load a search index
browse by domain
search across content
load content on demand
expose relationships
support bookmarks
work offline
report content version
support future knowledge-pack updates

Packaged content should normally be read-only.

User notes and user-created documents should be stored separately.

Knowledge Update Architecture

Future releases may separate application updates from knowledge updates.

Application Update
Includes executable software and stable bundled content
Knowledge Pack Update
Includes manuals, labs, articles, scripts, and references

Possible knowledge packs:

Community
Professional
Enterprise
Organization Custom
AWS
Linux
Kubernetes
Automation
AI Engineering
Operations Architecture

Write operations should use a dedicated operation layer.

Conceptual interface:

Plan
Dry Run
Preview
Protect
Review
Confirm
Execute
Validate
Refresh

The operation layer should not be mixed with discovery code.

Discovery reads the environment.

Operations change the environment.

This separation improves:

safety
testing
auditability
maintainability
permission control
Safe Operation Workflow

Significant write operations should follow:

Select Operation
        ↓
Enter Parameters
        ↓
Validate Input
        ↓
Analyze Dependencies
        ↓
Determine Reversibility
        ↓
Propose Recovery Preparation
        ↓
Create Restore Point
        ↓
Validate Restore Point
        ↓
Dry Run
        ↓
Preview Expected State
        ↓
Generate Change Plan
        ↓
Explain Impact
        ↓
Estimate Cost
        ↓
Review
        ↓
Confirm
        ↓
Execute
        ↓
Validate Result
        ↓
Refresh Inventory

The architecture must allow resource-specific operation planners.

Protection Architecture

Protection is a separate domain from discovery and operations.

Proposed components:

Protection
├── Restore Points
├── Backups
├── Recovery Plans
├── Recovery Policies
└── Recovery History

A Restore Point protects one operation.

A backup protects a larger scope or environment.

Restore is initiated from the selected Restore Point or backup.

Cost Architecture

Cost information comes from separate AWS billing services.

Cost refresh timing may differ from infrastructure refresh timing.

The Cost architecture should distinguish:

actual billed cost
forecast cost
operation estimate
cleanup savings estimate
protection storage estimate

Cost information should be optional, permission-aware, and privacy-sensitive.

Validation Architecture

Validation should consume strongly typed domain models.

Validation should not need to call AWS independently when current discovery
data is already available.

Validation results should include:

rule
category
resource
current state
expected state
severity
message
recommendation
related knowledge
related lab
whether the state is expected at the current project stage

Validation should teach while evaluating.

Project Architecture

Projects organize engineering work.

Examples:

AWS Master Labs
production environment
customer migration
Terraform deployment
Kubernetes platform
research project

Projects may associate:

infrastructure resources
expected resources
validation rules
labs
knowledge
scripts
Restore Points
backups
reports
costs
milestones

AWS Master Labs should be an optional project template, not a hard-coded
assumption.

Security Architecture

Security requirements include:

least privilege
standard AWS credential resolution
no ordinary storage of raw access keys
JWT authorization between desktop and API
secure token handling
role-based endpoint access
explicit confirmation for destructive actions
audit logging
protected financial information
protected local user content
script execution controls
safe export behavior
future operating-system secure storage

The architecture should eventually use HTTPS for non-development deployments.

Error Architecture

Errors should be structured.

An error response should communicate:

success status
error code
user-facing message
technical detail where appropriate
provider
account
Region
operation
retryability
timestamp
correlation ID

The UI should explain corrective actions when practical.

Audit Architecture

Every significant operation should record:

timestamp
authenticated application user
provider identity
AWS account
AWS principal
Region
operation
resource
parameters
Dry Run result
Restore Point
approval
result
correlation ID

Sensitive values should be redacted.

Caching Policy

The current objective is live discovery.

The system should not present cached state as current state.

Future caching may be used for:

short-lived provider summaries
packaged knowledge indexes
cost data
immutable content
performance optimization

Every cached result must include:

creation time
expiration
source
freshness state

The UI must distinguish:

Live
Refreshing
Stale
Offline
Unavailable
Compatibility Strategy

Version 2 should coexist with Version 1 during migration.

Existing services and endpoints remain available until equivalent Version 2
capabilities are validated.

Migration approach:

Add Version 2 service
        ↓
Build
        ↓
Test
        ↓
Expose Version 2 endpoint
        ↓
Test against AWS
        ↓
Update desktop
        ↓
Verify compatibility
        ↓
Retire Version 1 component later

No large-scale rewrite should be required.

Testing Architecture

Testing should include:

unit tests
provider tests
model-mapping tests
pagination tests
multi-Region tests
authorization tests
API integration tests
desktop integration tests
live AWS verification
regression tests
operation safety tests
Restore Point tests
knowledge-manifest tests
output-rendering tests

Live AWS tests should use controlled lab environments.

Deployment Architecture

The long-term application should provide a native installation experience.

Potential deployment structure:

Engineering Workbench
├── Desktop Application
├── Local API Service
├── Runtime
├── Providers
├── Knowledge Packs
├── Search Indexes
└── Configuration

The installer should detect the operating system and install required
components automatically when permitted.

Future deployment modes may include:

self-contained desktop
local API companion
enterprise remote API
team deployment
managed cloud service
Current Phase 1 Implementation

The current implementation includes:

Version 1 Git tags
Version 2 feature branch
AWS STS package
AWS client factory
AWS identity service
AWS Region service
AWS resource discovery orchestrator
AWS networking discovery service
Environment API
provider-source models
provider descriptor
Workbench item
Workbench relationships
Workbench capabilities
generic discovery request
generic discovery result
strongly typed VPC model
strongly typed subnet model
strongly typed route-table model
complete route modeling
complete route-table association modeling
multi-Region discovery
pagination support
Immediate Next Steps

The next Phase 1 tasks are:

Expose the Version 2 networking endpoint.
Test live VPC, subnet, and route-table output.
Confirm names, tags, routes, associations, account, and Region information.
Add Internet Gateways.
Add NAT Gateways.
Add Security Groups.
Add Network ACLs.
Add VPC Endpoints.
Add Elastic IP addresses.
Add network interfaces.
Create Workbench adapters for networking resources.
Add table, text, JSON, YAML, and YAML Stream renderers.
Integrate the desktop application after the backend is stable.
Architectural Principle

Every domain should retain its natural strongly typed model.

Every domain should also be representable through the common Workbench envelope
when universal search, navigation, relationships, capabilities, rendering, or
export is required.

This principle allows the platform to support:

AWS
Linux
Bash
Sed
Awk
Python
Ansible
Terraform
Kubernetes
Cheat Sheets
Scripts
Tools
Manuals
Labs
Engineering Articles
Projects
Protection
Costs
Automation
future technologies

without forcing every domain into an inappropriate common structure.

Closing Statement

TruthDoctor Version 2 is being built as the first executable implementation of
Engineering Workbench.

The architecture should solve the immediate AWS Master Labs monitoring need
while remaining flexible enough to support future infrastructure providers,
packaged engineering knowledge, automation, protection, cost management,
projects, tools, and AI-assisted engineering.

The platform should grow through carefully separated providers, strongly typed
domain models, common Workbench envelopes, controlled operations, and
incremental implementation.

