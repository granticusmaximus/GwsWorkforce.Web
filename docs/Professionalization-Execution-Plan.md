# GWS Workforce Professionalization Execution Plan

## 1) Mission and Operating Rules

This plan defines the exact phased roadmap to deliver a production-grade GWS Workforce application with:

- Clean Architecture
- Professional UX/UI
- Complete and resilient backend
- Comprehensive unit and integration test coverage
- Strict phase quality gates with defect handling

Mandatory rule for phase progression:

- A phase may only close when all required tests pass.
- Any failing required test creates a defect record.
- Defects tagged as Blocker or Critical must be fixed and re-tested before moving to the next phase.

## 2) Target Architecture (Clean Architecture)

## 2.1 Layers

- Presentation Layer
  - Blazor pages/components, view models, UI state, validation feedback
- Application Layer
  - Use cases, orchestrators, commands/queries, DTOs, policies
- Domain Layer
  - Entities, value objects, domain rules, invariants
- Infrastructure Layer
  - EF Core persistence, Ollama HTTP client, logging, caching, resilience pipelines

## 2.2 Dependency Rule

- Domain has no dependency on Application, Infrastructure, or UI.
- Application depends on Domain abstractions.
- Infrastructure implements Application abstractions.
- UI depends on Application contracts only.

## 2.3 Initial Project Structure Goal

- src/Domain
- src/Application
- src/Infrastructure
- src/Web
- tests/Domain.Tests
- tests/Application.Tests
- tests/Infrastructure.Tests
- tests/Web.IntegrationTests

## 3) Delivery Phases

## Phase 0: Baseline and Governance

Objective:

- Freeze current baseline and establish release governance.

Scope:

- Branching and CI strategy
- Coding standards and analyzer rules
- Definition of Done and defect severity matrix
- Test reporting template and dashboards

Deliverables:

- Contribution guide
- Severity rubric
- PR checklist
- Test report template

Required test units:

1. CI Pipeline Smoke
   - Validate restore, build, and test execution pipeline
2. Analyzer Gate
   - Validate warnings-as-errors policy for configured rules
3. Migration Safety Check
   - Validate migration apply against clean local DB

Pass criteria:

- CI runs green on main
- Analyzer gate active and enforced
- Migration check passes

Fail handling:

- Create defects for pipeline instability, analyzer misconfiguration, or migration failure

## Phase 1: Clean Architecture Refactor Foundation

Objective:

- Remove page-level business logic and direct data access.

Scope:

- Introduce Application services:
  - IConversationService
  - IKnowledgeService
  - IWorkerCatalogService
  - IChatOrchestrationService
- Introduce repository interfaces and implementations
- Move business rules from Razor pages into Application layer

Deliverables:

- Layered solution structure
- Dependency inversion interfaces
- Refactored Workforce and Knowledge pages using application services only

Required test units:

1. Domain Entity Invariants
   - WorkerDefinition key required and unique behavior assumptions
   - Conversation requires user ownership
   - Message role constraints
2. Application Service Tests
   - Create conversation for user
   - Rename/delete conversation with ownership checks
   - Knowledge CRUD with ownership checks
3. Dependency Rule Verification
   - Static check or architectural test ensuring layer boundaries

Pass criteria:

- No page directly uses DbContext
- All Application service tests pass
- Dependency rule check passes

Fail handling:

- Defect type: Architecture Drift
- Severity: Critical if boundary violations remain

## Phase 2: UX/UI Professionalization

Objective:

- Deliver polished, production-ready user experience.

Scope:

- Workspace layout
  - Worker panel
  - Conversation panel with paging and search
  - Chat thread panel
- Visual system
  - Typography hierarchy
  - Color tokens and spacing scale
  - Component states (loading, empty, error, success)
- Accessibility
  - Keyboard navigation
  - Focus states
  - ARIA labels
  - Contrast compliance

Deliverables:

- Design tokens CSS
- Reusable components
- Accessible UI interactions

Required test units:

1. UI Component Tests
   - Conversation list renders paging correctly
   - Rename/delete modals and controls render and behave as expected
   - Knowledge form validates required fields
2. Accessibility Tests
   - Keyboard tab order and focus traps
   - ARIA role and label assertions on key components
3. Visual Regression Baseline
   - Snapshot tests for Workforce and Knowledge core states

Pass criteria:

- Accessibility checks pass for primary flows
- No unresolved visual regressions in accepted baseline
- All UI component tests pass

Fail handling:

- Defect type: UX Regression or Accessibility Defect
- Severity: Critical for keyboard/accessibility blockers

## Phase 3: Backend Completion and Data Integrity

Objective:

- Complete backend behavior and enforce data integrity standards.

Scope:

- Command/query handlers for conversations, messages, knowledge
- Database constraints and indexes finalization
- Concurrency handling for rename/edit operations
- Transaction boundaries for chat write operations

Deliverables:

- Full application service behavior mapped to use cases
- EF configuration aligned with invariants
- Documented migration strategy and rollback notes

Required test units:

1. Repository Integration Tests
   - User scoping in every query path
   - Paging queries return deterministic results
2. Data Integrity Tests
   - Unique worker key enforced
   - Foreign key behaviors validated
3. Concurrency Tests
   - Simulate conflicting updates and verify expected behavior

Pass criteria:

- Integration tests pass with SQLite test database
- Integrity and concurrency tests pass
- No cross-user data leakage paths detected

Fail handling:

- Defect type: Data Integrity Defect or Security Isolation Defect
- Severity: Blocker for any cross-user leakage

## Phase 4: Ollama Reliability and Operational Resilience

Objective:

- Ensure robust, user-safe model interaction under failure conditions.

Scope:

- Timeout and cancellation policies
- Friendly error categorization and user messaging
- Retry policy for transient network failures
- Model availability checks and fallback policy

Deliverables:

- Resilience policy implementation
- User-facing error catalog
- Health checks for Ollama and DB

Required test units:

1. Ollama Client Unit Tests
   - Timeout returns expected domain error
   - Connection failure returns service-unavailable message
   - Model-not-found returns actionable model message
2. Orchestration Tests
   - Chat flow writes consistent conversation/message states under failure and success
3. Health Check Tests
   - Ready and degraded states reported correctly

Pass criteria:

- All resilience-path tests pass
- User-visible errors map to defined catalog
- No unhandled exceptions in chat request path

Fail handling:

- Defect type: Reliability Defect
- Severity: Critical if user session can crash or hang

## Phase 5: Security and Privacy Hardening

Objective:

- Achieve production-grade user isolation, auth hardening, and abuse protection.

Scope:

- Policy-based authorization checks at service boundary
- Identity hardening settings (lockout, password policies, MFA-ready)
- Rate limiting and request-size constraints
- Audit logging for sensitive actions

Deliverables:

- Authorization policy map
- Security configuration profile
- Audit event list

Required test units:

1. Authorization Tests
   - Forbidden access for other-user resources
2. Rate Limit Tests
   - Exceed threshold and verify controlled response
3. Audit Logging Tests
   - Delete and rename actions create audit entries

Pass criteria:

- Unauthorized access tests pass
- Abuse controls verified
- Audit trail for sensitive events verified

Fail handling:

- Defect type: Security Defect
- Severity: Blocker for privilege or isolation breaches

## Phase 6: Performance and Scale Readiness

Objective:

- Validate acceptable performance for concurrent users on target hardware.

Scope:

- Query profiling and optimization
- Response-time budget enforcement
- Background task strategy for heavy operations

Deliverables:

- Performance benchmark report
- Query optimization list and applied changes

Required test units:

1. Performance Test Pack
   - Conversation list paging latency
   - Chat round-trip baseline under load
2. Data Growth Tests
   - Large conversation and knowledge datasets still page correctly

Pass criteria:

- Meets target SLAs for P50/P95
- No major regression against baseline

Fail handling:

- Defect type: Performance Defect
- Severity: Critical if SLA breach exceeds agreed threshold

## Phase 7: Release Readiness and Go-Live

Objective:

- Final production validation and operational handoff.

Scope:

- Release candidate freeze
- End-to-end UAT
- Backup and restore rehearsal
- Runbook completion

Deliverables:

- Release checklist signed
- UAT report
- Rollback and recovery runbook

Required test units:

1. End-to-End Acceptance Suite
   - Register, login, chat, rename, delete, knowledge CRUD
2. Backup/Restore Test
   - Restore from backup and verify data integrity
3. Deployment Verification
   - Startup checks, migration checks, health checks

Pass criteria:

- UAT approved
- Backup/restore rehearsal successful
- Production readiness checklist complete

Fail handling:

- Defect type: Release Blocker
- Severity: Blocker

## 4) Test Strategy Matrix

Test layers:

- Unit: Domain and Application logic
- Integration: EF, repositories, service boundaries
- Component/UI: Blazor component behavior and validation
- E2E: User journey validation
- Non-functional: Performance, resilience, security checks

Minimum coverage targets:

- Domain + Application unit tests: 85 percent line coverage
- Critical use case branch coverage: 90 percent
- Security and user-isolation scenarios: 100 percent required paths

## 5) Pass/Fail Recording Template (Per Phase)

For each phase, create a report file at:

- reports/phase-{n}-test-report.md

Template:

- Phase: {number and name}
- Date:
- Build ID:
- Scope covered:
- Test units executed:
  - Test unit name
  - Result: Pass or Fail
  - Evidence: logs, screenshot path, test output path
- Summary:
  - Total passed
  - Total failed
  - Total blocked
- Gate decision:
  - Approved or Rejected
- Required fixes before next phase:
  - List of defect IDs

## 6) Defect Management Policy

Defect severities:

- Blocker: Prevents progression or causes security/data risk
- Critical: Major broken functionality or severe UX/accessibility break
- Major: Significant defect with workaround
- Minor: Low impact issue

Required response:

- Blocker or Critical
  - Fix mandatory before phase close
- Major
  - Fix before next phase unless explicitly accepted as risk
- Minor
  - Can defer with documented rationale

Defect record format:

- ID: DEF-{phase}-{sequence}
- Title:
- Severity:
- Area: UI, Application, Domain, Infrastructure, Security, Performance
- Repro steps:
- Expected result:
- Actual result:
- Root cause:
- Fix plan:
- Status: Open, In Progress, Resolved, Verified, Deferred
- Linked failed test unit:

## 7) Detailed Initial Backlog by Phase

Phase 0 backlog:

1. Add CI pipeline stages for restore, build, test, and migration check
2. Create Definition of Done and severity rubric
3. Add report templates under reports/

Phase 1 backlog:

1. Introduce application service interfaces and implementations
2. Refactor Workforce and Knowledge pages to use services
3. Add domain invariants and validators
4. Add unit tests for service ownership checks

Phase 2 backlog:

1. Build professional workspace layout and reusable components
2. Add accessibility improvements and validation
3. Add visual regression baselines

Phase 3 backlog:

1. Finalize persistence mappings and constraints
2. Add concurrency handling
3. Add integration tests for all ownership and paging queries

Phase 4 backlog:

1. Add resilient Ollama policies and health checks
2. Implement friendly error mapping catalog
3. Add resilience-path tests

Phase 5 backlog:

1. Add policy authorization checks in service layer
2. Add abuse protections and audit logging
3. Add security tests

Phase 6 backlog:

1. Build performance benchmark suite
2. Optimize hot-path queries
3. Validate SLA thresholds

Phase 7 backlog:

1. Run full UAT and release checklist
2. Validate backup/restore drill
3. Final go-live signoff

## 8) Exit Criteria for Full Program Completion

Program is complete when:

1. All phase gates approved
2. No open Blocker or Critical defects
3. Required tests pass in CI and release candidate environments
4. Operational runbooks and rollback procedures are validated
5. Product owner signs off on UX/UI and functional acceptance
