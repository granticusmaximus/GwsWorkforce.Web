# Definition of Done

A backlog item is Done only when all criteria below are met.

## Functional Completion

1. Acceptance criteria are fully implemented.
2. User-facing behavior is validated for success and failure paths.

## Architecture and Code Quality

1. Clean architecture boundaries are respected.
2. No duplicated business rules across layers.
3. New code is readable and maintainable.
4. Presentation code does not directly reference persistence infrastructure (for example DbContext).
5. Repository contains no duplicate source files and no accidental copy artifacts.
6. Build output and transient files are excluded from source control (bin, obj, test output, temporary files).

## Testing

1. Required unit and integration tests are added/updated.
2. Relevant tests pass locally and in CI.
3. Failure paths are covered for critical operations.

## Security and Privacy

1. User data access is scoped server-side by ApplicationUserId.
2. No sensitive data is leaked in logs or error messages.
3. Authorization requirements are enforced.
4. Dependency vulnerability scan reports no known vulnerabilities for application and test projects.

## Microsoft Standards and Conventions

1. Code formatting and analyzer checks pass (`dotnet format --verify-no-changes`).
2. Nullable reference types remain enabled and warnings are addressed in modified code.
3. Public APIs and naming follow .NET and ASP.NET Core conventions.

## Operations

1. Logging is sufficient to diagnose failures.
2. Schema changes include EF migrations when required.
3. Migration applies cleanly in development validation.
4. Build and test run cleanly for touched projects before phase closure.

## Documentation

1. Phase test report is updated with pass/fail evidence.
2. Defect records are created for failed required tests.
3. Any known limitations are documented.
