# EShopClaude — Way of Working

How our team and Claude Code work on this repository. Scope is in [`PROJECT.md`](./PROJECT.md), build order in [`PLAN.md`](./PLAN.md), standing rules in `CLAUDE.md`.

## Principles

- One spec is one sub-feature, one feature branch, one merge to main.
- A spec changes exactly one microservice and its own AppHost registration.
- `main` always builds and runs. Every merge leaves a working solution.
- Every spec is self-contained, so a fresh session only needs to read that one file.

## Branches and merges

- One spec is one feature branch, named `feature/<service>-<NN>-<short-name>`, branched from `main`.
  - Example: `feature/catalog-02-decrement-stock`.
- The spec is the first commit on the branch, before any code.
- Merge into `main` with `git merge --no-ff`, then delete the branch.
- Feature branches stay local and are never pushed.
- No direct commits to `main`.

## Folder layout

- Specs live in `_agenticdev/microservices/<service>/NN-<short-name>.md`.
  - Numbered per service, and never renumbered.
- Every spec follows [`_agenticdev/templates/spec-template.md`](./templates/spec-template.md).
- `_agenticdev/microservices/README.md` holds the status table for all specs.

## The cycle, per spec

1. Pick the next spec from the status table.
2. Create the feature branch from `main`.
3. Start a fresh Claude Code session.
4. Plan in plan mode, and approve the plan.
5. Implement in auto mode.
6. Verify manually.
7. Commit on the branch, then merge to `main` with `git merge --no-ff`.
8. Mark the spec `Done` in the spec file and in the microservices README status table.
9. Delete the feature branch.

## Ordering rules

- Specs run in number order inside a service.
- A spec does not start until every spec it depends on is merged to `main`.

## Session hygiene

- One spec, one fresh session. Clear the context between specs.
- Plan mode before implementing, and a human approves the plan.
- Before any commit:
  - the build and `aspire run` gate in `CLAUDE.md` passes;
  - the spec's Definition of Done passes.
