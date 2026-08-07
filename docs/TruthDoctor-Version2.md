# TruthDoctor Version 2 — Live Engineering Companion

---

Project:
TruthDoctor

Document:
Version 2 Product and Implementation Plan

Document Version:
0.1

Document Status:
Planning

Classification:
Product and Engineering Design

Author:
Nasser Abdelghani

Created:
August 2026

Last Updated:
August 2026

---

# Purpose

This document defines the objectives, scope, development phases, safety model, and implementation strategy for TruthDoctor Version 2.

TruthDoctor Version 2 evolves the existing TruthDoctor desktop application from an infrastructure-validation viewer into a live, universal, executable engineering companion.

The immediate purpose is to support engineers executing the AWS Master Labs by allowing them to observe their infrastructure as it is created, modified, validated, protected, and removed.

TruthDoctor Version 2 also establishes the first practical foundation for the future Engineering Workbench platform.

---

# Current Foundation

TruthDoctor Version 1 currently consists of two applications:

```text
TruthDoctor Desktop
        ↓
TruthApi
        ↓
AWS SDK
        ↓
AWS Infrastructure
```

Current capabilities include:

* user authentication
* JWT-based API access
* live AWS resource discovery
* automatic refresh
* validation results
* VPC inventory
* subnet inventory
* route-table inventory
* EC2 inventory
* Internet Gateway inventory
* NAT Gateway inventory

The current application queries AWS directly during refresh operations.

It does not maintain a historical inventory database.

Deleted AWS resources disappear when the latest AWS response no longer includes them.

---

# Version 2 Vision

TruthDoctor Version 2 should become a universal live engineering companion.

The application should allow an authorized engineer to:

* discover the current AWS identity
* select an AWS profile or authentication method
* select one or more Regions
* inspect current infrastructure
* validate infrastructure
* execute supported operations
* preview proposed changes
* perform dry runs
* protect resources before changes
* restore protected resources when possible
* view output in multiple engineering formats
* monitor project and lab progress
* access related engineering knowledge

TruthDoctor Version 2 is not intended to remain only an inventory application.

Inventory is one capability within a broader executable engineering environment.

---

# Universal User Model

TruthDoctor must not assume a specific:

* AWS account
* AWS user
* AWS role
* AWS profile
* AWS Region
* Availability Zone
* project
* VPC
* resource name
* resource identifier

The application must dynamically discover the current user's authorized environment.

The expected discovery process is:

```text
Select authentication method
        ↓
Resolve AWS credentials
        ↓
Call STS GetCallerIdentity
        ↓
Display account and identity
        ↓
Discover available Regions
        ↓
Allow Region selection
        ↓
Query selected Regions
        ↓
Build current inventory
```

The same application should work for:

* the project author
* another AWS Master Labs learner
* an individual cloud engineer
* a consultant
* a professional team
* a future enterprise deployment

---

# AWS Authentication Sources

TruthDoctor should eventually support the standard AWS credential resolution methods.

Examples include:

* named AWS profiles
* AWS IAM Identity Center
* assumed IAM roles
* environment credentials
* EC2 instance roles
* container task roles
* the default AWS SDK credential chain

TruthDoctor should not request that users store raw AWS access keys directly inside ordinary application configuration files.

Sensitive credentials and tokens must use operating-system secure storage where applicable.

---

# Current Development Priority

The immediate priority is to improve TruthDoctor while continuing the AWS Master Labs.

The initial implementation should allow the application to reflect infrastructure changes produced by the labs.

Examples:

```text
Lab 01
Custom VPC appears
```

```text
Lab 02
Four custom subnets appear
```

```text
Lab 03
Public and private route tables appear
```

```text
Lab 04
Internet Gateway and public route appear
```

```text
Lab 05
NAT Gateway and Elastic IP appear
```

```text
Lab 06
Security Groups appear
```

```text
Lab 07
Network ACL changes appear
```

```text
Lab 08
VPC Endpoints appear
```

Cleanup operations should also be reflected during the next successful refresh.

---

# Core Product Areas

TruthDoctor Version 2 is organized around several major capability areas.

```text
Dashboard
Projects
Infrastructure
Operations
Protection
Validation
Knowledge
Automation
Reports
Tools
AI
Settings
Help
```

Not every capability will be implemented during the first development phase.

The architecture should nevertheless avoid blocking their future introduction.

---

# Development Phases

---

# Phase 0 — Baseline Protection

Status:

Completed

Objectives:

* preserve the existing TruthApi implementation
* preserve the existing TruthDoctor implementation
* create permanent Git tags
* establish a safe comparison point before Version 2 changes

Baseline tags:

```text
truthapi-v1
truthdoctor-v1
```

The Version 1 tags must remain immutable.

---

# Phase 1 — Universal Live Discovery Foundation

Status:

Current

Objectives:

* remove fixed account assumptions
* remove fixed Region assumptions
* identify the active AWS identity
* discover available Regions
* allow selected-Region inventory
* query AWS live
* add pagination
* establish canonical resource models
* preserve compatibility with the current desktop interface

Initial resource categories:

## Networking

* VPCs
* subnets
* route tables
* Internet Gateways
* NAT Gateways
* Security Groups
* Network ACLs
* VPC Endpoints
* Elastic IP addresses
* network interfaces

## Compute

* EC2 instances
* key pairs
* launch templates
* AMIs

## Storage

* EBS volumes
* EBS snapshots

## Identity

* IAM roles
* instance profiles

Additional services will be introduced incrementally.

---

# Phase 2 — Live Engineering Dashboard

Objectives:

* display AWS account information
* display active identity
* display selected profile
* display selected Regions
* display last successful refresh
* display refresh state
* display resource counts
* organize resources through collapsible navigation
* remove dependence on one long scrolling window

Expected refresh states:

```text
Live
Refreshing
Stale
Disconnected
Authentication Required
Permission Denied
```

The application must never present stale information as live information.

---

# Phase 3 — Resource Relationships

Objectives:

* connect resources visually
* show parent and child relationships
* show dependencies
* show associations
* allow navigation between related resources

Example:

```text
VPC
├── Subnets
├── Route Tables
├── Internet Gateway
├── NAT Gateways
├── Security Groups
├── Network ACLs
├── VPC Endpoints
├── Network Interfaces
└── EC2 Instances
```

A resource should not be presented only as an isolated row.

---

# Phase 4 — Executable Operations

Objectives:

Support appropriate read and write operations.

Read operations may include:

* describe
* list
* search
* filter
* compare
* validate
* refresh

Write operations may include:

* create
* update
* modify
* rename
* tag
* untag
* associate
* disassociate
* attach
* detach
* start
* stop
* restart
* delete
* restore

Not every resource supports every operation.

TruthDoctor must expose only operations valid for the selected resource.

---

# HTTP Operation Model

TruthApi Version 2 should use HTTP methods consistently.

```text
GET
Read, list, describe, or search
```

```text
POST
Create a resource or invoke an operational command
```

```text
PUT
Replace a complete editable representation
```

```text
PATCH
Modify selected properties
```

```text
DELETE
Delete a resource
```

Examples:

```text
GET /api/v2/aws/vpcs
```

```text
GET /api/v2/aws/vpcs/{vpcId}
```

```text
POST /api/v2/aws/vpcs
```

```text
PATCH /api/v2/aws/vpcs/{vpcId}
```

```text
DELETE /api/v2/aws/vpcs/{vpcId}
```

Operational actions may use explicit action endpoints.

Examples:

```text
POST /api/v2/aws/instances/{instanceId}/actions/start
```

```text
POST /api/v2/aws/instances/{instanceId}/actions/stop
```

```text
POST /api/v2/aws/route-tables/{routeTableId}/routes
```

```text
POST /api/v2/aws/route-tables/{routeTableId}/associations
```

---

# Engineering Output Formats

Every supported inventory or resource-details view should allow the user to select an output format.

Supported formats:

* Table
* Text
* JSON
* YAML
* YAML Stream

The display-format selector should be available within the expanded resource or resource-category view.

Example:

```text
Route Tables

Output Format:
Table
```

Changing the presentation format should not require a new AWS discovery operation.

The application should use one canonical resource representation and render it through multiple formatters.

```text
AWS Result
    ↓
Canonical Resource Model
    ├── Table Renderer
    ├── Text Renderer
    ├── JSON Renderer
    ├── YAML Renderer
    └── YAML Stream Renderer
```

YAML Stream output should separate documents using:

```yaml
---
```

Users should be able to:

* copy output
* save output
* export output
* compare output
* use output in scripts
* use output in programming workflows

---

# Future Script Generation

Script generation is accepted as a future capability but is not part of the initial Phase 1 implementation.

Possible generated formats include:

* AWS CLI commands
* Bash scripts
* Python scripts
* PowerShell scripts
* Terraform configuration
* CloudFormation templates
* Ansible automation

Possible actions:

```text
Generate Bash
Generate Python
Copy Command
Save Script
Open in Editor
Export Execution Plan
```

Generated scripts must represent the exact reviewed operation.

The application must not generate destructive automation without clearly displaying its effects and required parameters.

---

# Safe Write-Operation Workflow

Every significant write operation should follow a controlled execution process.

```text
Select operation
        ↓
Enter parameters
        ↓
Validate input
        ↓
Analyze dependencies
        ↓
Determine reversibility
        ↓
Propose recovery preparation
        ↓
Create Restore Point
        ↓
Validate Restore Point
        ↓
Dry Run
        ↓
Preview expected state
        ↓
Generate change plan
        ↓
Explain dependencies and impact
        ↓
Estimate cost when applicable
        ↓
Review
        ↓
Confirm
        ↓
Execute
        ↓
Validate result
        ↓
Refresh live inventory
```

Read-only operations do not require the complete write-operation workflow.

---

# Dry Run

Dry Run is a first-class platform capability.

When the AWS API supports a native dry-run request, TruthDoctor should use it.

When AWS does not support native dry-run behavior, TruthDoctor should perform the strongest available application-level validation.

Dry Run should answer questions such as:

* Are the parameters valid?
* Does the identity have permission?
* Does the target resource exist?
* Are prerequisites satisfied?
* Are dependencies present?
* Is the operation currently executable?
* Is the requested state already present?
* Will AWS reject the operation?

Dry Run does not replace final validation or confirmation.

---

# Preview

Preview describes the expected environment after an operation succeeds.

Example:

```text
Current State

VPCs: 2
Subnets: 10
Route Tables: 4
```

```text
Expected State

VPCs: 2
Subnets: 10
Route Tables: 3
```

Preview should identify:

* resources created
* resources changed
* resources removed
* relationships changed
* expected service interruption
* expected cost changes
* expected project-progress changes

---

# Change Plan

Before execution, TruthDoctor should create a structured change plan.

The change plan may contain:

* operation name
* account
* Region
* target resource
* requested parameters
* current state
* expected state
* dependency analysis
* recovery preparation
* reversibility classification
* dry-run result
* cost estimate
* warnings
* execution steps
* validation steps

The user should be able to export the change plan as:

* Text
* JSON
* YAML
* YAML Stream
* Markdown

Future versions may generate automation from the change plan.

---

# Protection Module

The Protection module provides safeguards before infrastructure changes.

Proposed navigation:

```text
Protection
├── Restore Points
├── Backups
├── Recovery Plans
├── Recovery Policies
└── Recovery History
```

Protection should be proactive.

Recovery should not begin only after a failure occurs.

---

# Restore Points

A Restore Point is a saved recovery state created before a significant operation.

Examples:

```text
Before deleting an EC2 instance
```

```text
Before replacing route-table routes
```

```text
Before modifying a Security Group
```

```text
Before deleting an IAM role
```

Restore Points should record:

* creation date and time
* protected operation
* resource type
* resource name
* resource identifier
* account
* Region
* original configuration
* dependencies
* recovery assets
* recovery limitations
* recovery readiness
* retention policy
* creator
* source operation
* audit identifier

---

# Restore-Point Actions

Rollback should not appear as an unrelated top-level operation.

The intended interaction is:

```text
Protection
    ↓
Restore Points
    ↓
Select Restore Point
    ↓
Review
    ↓
Restore
```

Restore is the user-facing action.

Internally, Restore may perform:

* configuration rollback
* resource recreation
* backup restoration
* snapshot restoration
* version restoration
* association reconstruction
* tag restoration
* dependency reconstruction

The user should not be required to understand the internal implementation before choosing Restore.

The Restore review screen should explain exactly what can and cannot be restored.

---

# Recovery Classification

Every protected operation should receive a recovery classification.

```text
Fully Restorable
```

```text
Restorable with Limitations
```

```text
Recreatable from Configuration
```

```text
Recoverable from Backup
```

```text
Configuration Rollback Only
```

```text
Irreversible
```

TruthDoctor must not claim that an operation is fully restorable when AWS limitations prevent complete restoration.

---

# Resource-Specific Protection

Before destructive or irreversible operations, TruthDoctor should create the strongest available recovery assets.

## EC2

Protection may include:

* create an AMI
* snapshot every attached EBS volume
* record instance type
* record subnet
* record Availability Zone
* record Security Groups
* record IAM instance profile
* record key-pair name
* record user data
* record tags
* record network-interface configuration

The restore review must explain that a restored instance may receive:

* a new instance ID
* a new private address
* a new public address
* different underlying hardware
* different capacity availability

Instance-store data may not be recoverable.

## S3

Protection may include:

* verify versioning
* offer to enable versioning
* verify Object Lock when applicable
* identify replication configuration
* record lifecycle configuration
* preserve object versions
* export bucket policy and tags

When versioning is disabled, TruthDoctor should offer:

```text
Enable versioning and continue
Continue without recovery
Cancel
```

## EBS

Protection may include:

* create a snapshot
* record encryption
* record KMS key
* record Availability Zone
* record attachment information
* record tags

## RDS

Protection may include:

* create a manual DB snapshot
* verify automated backups
* record subnet group
* record parameter groups
* record option groups
* record Security Groups
* record tags

## DynamoDB

Protection may include:

* verify point-in-time recovery
* create an on-demand backup
* record indexes
* record capacity settings
* record encryption
* record tags

## IAM

Protection may include:

* export role configuration
* export trust policies
* export attached policies
* export inline policies
* export tags
* record instance-profile relationships

## Networking Resources

Protection may include:

* routes
* associations
* ingress rules
* egress rules
* Network ACL entries
* tags
* dependencies
* VPC relationships

---

# Backup Module

Backups are separate from operation-specific Restore Points.

Restore Points:

* protect one operation
* are commonly created automatically
* are normally short-lived
* preserve the state immediately before a change

Backups:

* protect larger environments
* may be manual or scheduled
* may be retained long term
* may cover projects, services, or complete environments

Proposed backup pattern:

```text
Full Backup
    ↓
Incremental Backup
    ↓
Incremental Backup
    ↓
Incremental Backup
    ↓
Full Backup
```

Backups should support:

* full backups
* incremental backups
* retention policies
* integrity validation
* recovery testing
* storage-cost estimates
* project-level scope
* selective restore
* backup history

---

# Restore Workflow

Selecting Restore should not immediately modify infrastructure.

The restore workflow should be:

```text
Select Restore Point or Backup
        ↓
Review saved state
        ↓
Analyze current environment
        ↓
Compare saved and current states
        ↓
Detect conflicts
        ↓
Generate recovery plan
        ↓
Dry Run
        ↓
Preview expected restored state
        ↓
Review limitations
        ↓
Confirm
        ↓
Execute restoration
        ↓
Validate restored resources
        ↓
Refresh live inventory
        ↓
Record recovery history
```

---

# Restore-Point List

The Restore Points view should display:

* date and time
* protected operation
* resource name
* resource ID
* resource type
* account
* Region
* recovery type
* recovery readiness
* expiration
* creator
* status

Example:

```text
2026-08-03 14:22
Before: Delete EC2 instance
Resource: web-server-01
ID: i-0123456789abcdef0
Region: us-east-1
Recovery: AMI and two snapshots
Status: Ready
```

Available actions may include:

```text
Review
Compare
Restore
Export
Delete Restore Point
```

Restore should be available only after the user reviews the Restore Point.

---

# Retention and Cost

Recovery artifacts may incur AWS charges.

Examples include:

* EBS snapshots
* AMI-backed snapshots
* RDS snapshots
* AWS Backup recovery points
* retained S3 versions

Restore Points and backups should use configurable retention policies.

Examples:

```text
24 hours
7 days
30 days
90 days
Until manually deleted
```

TruthDoctor should show estimated storage cost when practical.

Expired recovery artifacts should not be removed without applying the configured recovery policy and recording the action.

---

# Validation Model

Validation should do more than return PASS or FAIL.

Validation results should explain:

* what was evaluated
* why it matters
* current state
* expected state
* severity
* recommendation
* related documentation
* related Master Labs
* whether the state is currently expected

Example:

```text
PASS

Both public subnets are explicitly associated with the intended
public route table.

This is the expected state after Lab 03.
```

Example:

```text
INFORMATIONAL

The public route table does not yet contain an Internet Gateway route.

This is expected until Lab 04.
```

TruthDoctor should teach while validating.

---

# Master Labs Companion

TruthDoctor should support the AWS Master Labs as an optional project template.

The application must not assume every user is executing the Master Labs.

A user may instead monitor:

* a production environment
* a migration project
* a personal lab
* a customer environment
* a Terraform deployment
* a Kubernetes platform
* another training curriculum

For the AWS Master Labs template, TruthDoctor should compare expected resources with live infrastructure.

Example:

```text
AWS Master Labs — Part 04 Networking

Lab 01 — Custom VPC
Complete

Lab 02 — Subnets
Complete

Lab 03 — Route Tables
Complete

Lab 04 — Internet Gateway
Not Started
```

Project progress must be based on live infrastructure and configurable project rules.

---

# Knowledge Integration

TruthDoctor Version 2 should prepare for native integration of:

* AWS Engineering Manual
* AWS Master Labs
* Engineering Encyclopedia
* scripts
* cheat sheets
* best practices
* architecture guides
* troubleshooting references

Knowledge should be accessible contextually.

Example:

```text
Selected Resource:
Route Table

Related Knowledge:
AWS Manual — Route Tables
Master Labs — Lab 03
Master Labs — Lab 04
Longest-Prefix Matching
Route-Table Troubleshooting
```

Knowledge integration may be implemented after the initial discovery and dashboard phases.

---

# Collapsible Navigation

The Version 2 interface should avoid placing every resource grid into one long scrolling window.

Proposed navigation:

```text
Dashboard

Projects

Infrastructure
├── Networking
├── Compute
├── Storage
├── Identity
├── Containers
├── Serverless
└── Databases

Operations

Protection

Validation

Knowledge

Automation

Reports

Tools

AI

Settings
```

Users should be able to expand and collapse categories and subcategories.

The interface should preserve the user's navigation state when practical.

---

# Proposed Top Menu

The long-term top menu may include:

```text
File
Projects
Infrastructure
Operations
Protection
Engineering
Knowledge
Automation
Reports
Tools
AI
View
Help
```

The menu is organized around engineering workflows rather than one provider's service catalog.

AWS is the first provider.

The product architecture should not assume AWS is the only future provider.

---

# Immediate Phase 1 Deliverables

The first implementation milestone should produce:

* AWS identity discovery
* account display
* profile awareness
* Region discovery
* Region selection
* live inventory from selected Regions
* pagination-safe discovery
* canonical resource models
* resource names and tags
* complete route-table routes
* complete route-table associations
* compatibility with current validation output
* documented API Version 2 conventions
* initial tests
* no regression to Version 1 functionality

Write operations, Protection, backups, and advanced output rendering are accepted requirements but should be implemented incrementally after the universal live-discovery foundation is stable.

---

# Development Discipline

Every implementation phase should follow:

```text
Design
    ↓
Implement
    ↓
Build
    ↓
Test
    ↓
Run
    ↓
Validate against AWS
    ↓
Document
    ↓
Commit
    ↓
Push
    ↓
Continue
```

Changes should be small enough to review and test safely.

The current working application must remain recoverable through the Version 1 Git tags.

---

# Git Milestones

Proposed milestones:

```text
truthapi-v1
truthdoctor-v1
```

```text
truthapi-v2-phase1
truthdoctor-v2-phase1
```

```text
truthapi-v2-dashboard
truthdoctor-v2-dashboard
```

```text
truthapi-v2-operations
truthdoctor-v2-operations
```

```text
truthapi-v2-protection
truthdoctor-v2-protection
```

```text
truthdoctor-v2-beta
```

```text
truthdoctor-v2.0.0
```

Actual tag names may be refined before release.

---

# Success Criteria

TruthDoctor Version 2 will satisfy its immediate purpose when:

* another authorized AWS user can install and run it
* the application identifies that user's account and identity
* the user can select supported Regions
* current resources appear automatically
* deleted resources disappear after refresh
* newly created resources appear after refresh
* the application accurately tracks the AWS Master Labs environment
* validation teaches rather than merely scores
* resources are organized through scalable navigation
* the foundation supports future operations and protection capabilities

---

# Closing Statement

TruthDoctor Version 2 is the practical bridge between the current TruthDoctor application and the future Engineering Workbench platform.

Its immediate mission is clear:

```text
Observe live infrastructure
        ↓
Understand infrastructure
        ↓
Validate infrastructure
        ↓
Protect infrastructure
        ↓
Operate infrastructure safely
        ↓
Learn while engineering
```

The application should evolve incrementally, preserve the working Version 1 foundation, and remain useful throughout every stage of development.



---

# Cost Management

Cost awareness is a core engineering capability.

TruthDoctor Version 2 should provide engineers with accurate, understandable,
and actionable cost information for the current AWS environment whenever the
authenticated identity has permission to access AWS billing services.

The objective is not simply to display monthly charges.

The objective is to help engineers understand the financial impact of their
engineering decisions before and after infrastructure changes.

The Cost Management module should remain optional and collapsible because some
users may not have billing permissions or may prefer not to display financial
information during demonstrations or presentations.

---

# Design Principles

The Cost Management module shall:

- retrieve actual AWS billing information whenever available
- distinguish actual costs from estimated costs
- associate costs with infrastructure resources
- explain why costs changed
- estimate the cost impact of planned operations
- estimate savings during cleanup
- support engineering decision making
- never present estimated values as actual AWS charges

---

# Cost Dashboard

The dashboard should optionally display a high-level financial summary.

Example:

```text
Current Billing Period

Total Cost
$48.72

Today's Cost
$1.56

Forecast
$52.10

Budget
$100.00

Remaining Budget
$47.90

The dashboard should also display the date and time of the most recent billing
information retrieved from AWS.

Infrastructure inventory and billing information may refresh on different
schedules.

The application must clearly distinguish between them.

Example:

Infrastructure Updated

2026-08-04 09:12 UTC

Billing Information Updated

2026-08-03 23:55 UTC
Cost Navigation

The Cost module should become a major engineering area.

Proposed navigation:

Costs

├── Overview
├── By Service
├── By Region
├── By Account
├── By Project
├── Forecast
├── Budgets
├── Cost Trends
├── Cost Anomalies
├── Optimization
└── Cleanup Savings

Users should be able to expand or collapse the entire Cost section.

Cost by AWS Service

TruthDoctor should retrieve and summarize costs by AWS service.

Example:

EC2 Instances              $12.44

EBS Volumes                 $4.10

NAT Gateway                $18.90

Amazon S3                   $0.34

CloudWatch                  $1.82

IAM                         $0.00

Route Tables                $0.00

VPC                         $0.00

────────────────────────────────

Grand Total                $37.60

The displayed grand total must equal the sum of the individual service costs.

Cost by Region

Users should be able to understand where infrastructure costs originate.

Example:

us-east-1

$32.14

us-west-2

$5.46

eu-west-1

$0.00
Cost by Project

Future versions should allow costs to be grouped by project using resource tags.

Example:

AWS Master Labs

$14.82

Production

$483.92

Development

$42.77

This feature will become especially valuable when multiple environments exist
within the same AWS account.

Resource-Level Cost Information

Whenever possible, infrastructure resources should display their associated
cost information.

Example:

NAT Gateway

Monthly Cost

$32.85

Data Processing

Usage Based

Pricing Region

us-east-1

Example:

Route Table

Monthly Cost

Free

Example:

Internet Gateway

Monthly Cost

Free

This immediately teaches engineers which AWS resources generate charges.

Cost Awareness During Operations

Before executing operations that may incur charges, TruthDoctor should estimate
their financial impact.

Example:

Create NAT Gateway

Estimated Monthly Base Cost

$...

Additional Data Processing

Usage Based

Estimated Billing Region

us-east-1

The engineer should understand the expected cost before confirming execution.

Cleanup Savings

Cleanup should include estimated financial savings.

Example:

Resources Selected

NAT Gateway

Estimated Monthly Savings

$...

Elastic IP

Estimated Monthly Savings

$...

Snapshots

Estimated Monthly Savings

$...

────────────────────────────

Estimated Total Savings

$...

Savings should always be presented as estimates.

Cost and Protection

Protection features may themselves generate AWS charges.

Examples include:

AMIs
EBS Snapshots
RDS Snapshots
AWS Backup Recovery Points
retained S3 object versions

Before creating recovery assets, TruthDoctor should display:

estimated storage
estimated recurring cost
retention period
cleanup policy

Engineers should understand the financial implications of protection before
creating recovery assets.

Cost Privacy

Some organizations restrict access to financial information.

TruthDoctor should therefore support:

hiding the Cost module
masking monetary values
role-based cost visibility
excluding cost information from exported reports
excluding cost information from screenshots

The absence of billing permissions must never prevent normal infrastructure
discovery or validation.

Future Enhancements

Future versions of TruthDoctor may include:

AWS Budgets integration
Cost Explorer integration
Savings Plans analysis
Reserved Instance analysis
idle-resource detection
rightsizing recommendations
anomaly detection
project chargeback reporting
multi-account reporting
multi-cloud cost comparison
cost-aware deployment recommendations
cost trend forecasting
AI-assisted cost optimization
Engineering Philosophy

Infrastructure and cost should never be treated as independent concerns.

Every engineering decision has technical consequences and financial
consequences.

TruthDoctor should present both so that engineers can make informed decisions.

The goal is not merely to monitor AWS costs.

The goal is to help engineers understand the relationship between architecture,
operations, performance, protection, and financial impact.

