# Defect Severity Matrix

## Severity Levels

## Blocker

Definition:

- Prevents release or phase progression.
- Causes security/privacy breach, data corruption, or complete outage.

Examples:

- Cross-user data leakage.
- Authentication bypass.
- Migration failures that block startup.

Required action:

- Must be fixed and verified before phase closure.

## Critical

Definition:

- Core feature unusable without acceptable workaround.
- Severe UX/accessibility failure for primary user flow.

Examples:

- Chat send path fails for valid requests.
- Conversation operations fail for all users.

Required action:

- Must be fixed and verified before phase closure.

## Major

Definition:

- Significant impact, workaround exists, no direct security compromise.

Examples:

- Pagination misbehaves on edge cases.
- Non-blocking reliability degradation.

Required action:

- Fix before next phase unless explicitly accepted as risk.

## Minor

Definition:

- Low impact issue or cosmetic defect.

Examples:

- Layout misalignment on non-critical screens.
- Wording inconsistencies.

Required action:

- Can be deferred with rationale and owner.

## Phase Gate Rule

- Any failed required test must map to a defect record.
- Open Blocker or Critical defects automatically fail the phase gate.
