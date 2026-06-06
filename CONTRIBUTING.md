# Contributing to GWS Workforce

## Branching and Pull Requests

- Create feature branches from main using: feature/<area>-<short-description>
- Keep pull requests small and focused.
- Link each pull request to a phase and backlog item from the execution plan.
- Do not merge directly to main.

## Required Pull Request Checklist

1. Build passes locally.
2. Tests for changed behavior are added or updated.
3. No cross-user data access paths are introduced.
4. Migration impact is documented if schema changes are included.
5. Pass/fail test report is updated for the active phase.

## Coding Standards

- Use clean architecture boundaries:
  - UI does not access DbContext directly.
  - Application layer owns use-case logic.
  - Infrastructure implements interfaces only.
- Prefer small classes and single-responsibility methods.
- Avoid hidden side effects and static mutable state.

## Defect and Phase Gates

- Any failed required test must create a defect record.
- Blocker and Critical defects must be resolved before phase closure.
- See docs/Defect-Severity-Matrix.md and defects/templates/defect-record-template.md.

## Commit Guidance

- Use descriptive commit messages.
- Include phase tag when relevant: [Phase-0], [Phase-1], etc.

Example:

- [Phase-1] Refactor conversation write path into application service
