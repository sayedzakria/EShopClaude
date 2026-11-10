---
name: start-spec
description: Use when starting work on one ready spec from _agenticdev/microservices, covering steps 2 to 4 of the cycle in _agenticdev/WOW.md.
argument-hint: '<service> <NN>'
allowed-tools:
  - Read
  - Glob
  - Bash(git status *)
  - Bash(git branch --show-current)
  - Bash(git checkout -b feature/*)
  - Bash(git add _agenticdev/microservices/*)
  - Bash(git commit -m *)
---

# Start spec

Current branch: !`git branch --show-current`

Working tree (blank means clean): !`git status --porcelain`

Arguments: $ARGUMENTS

`<service>` is a folder under `_agenticdev/microservices/` (`catalog` means `catalog-api`). `<NN>` is two digits.

## Rules

- Run one git command per call, and only the forms in the steps below. Never push, merge, rm, amend or use `--no-verify`. Feature branches stay local.
- Change no file. Every refusal changes nothing, names the failed check and says what to do next.

## Steps

1. **Resolve** `_agenticdev/microservices/<service>/<NN>-*.md` with Glob. If `<service>` or `<NN>` is missing or no file matches, stop and tell me to run `/new-spec <service> <NN>`. If several match, stop and tell me to remove the duplicate.
2. **Preflight.** Read the spec, `_agenticdev/WOW.md`, `_agenticdev/microservices/README.md` and `_agenticdev/templates/spec-template.md`. Stop at the first failed check:
   - Current branch is not `main`: tell me to switch to `main`.
   - Working tree lists anything other than this spec file: list those paths and tell me to commit, stash or revert them. `_agenticdev/microservices/README.md` also passes when the spec file is untracked (`??`), because `/new-spec` adds its status table row.
   - Spec Status is `Done`: tell me to pick the next spec from the status table.
   - A "Depends on" entry is not `Done` in the status table, or has no row there: name it and tell me to finish it first. `Nothing.` passes.
   - A template section is missing or empty, or still holds a template `<!-- -->` comment or `TBD`: name the sections and tell me to run `/new-spec <service> <NN>`. `n/a` with a reason is filled, and run-time values such as `<catalogapi-url>` are not placeholders.
3. **Derive the branch** from the WOW.md rule: `feature/<service-without-api>-<NN>-<short-name>`, where `<short-name>` is the spec filename without `<NN>-` and `.md`. Never invent one and never ask me for one. If the spec's Branch cell in the status table differs, stop and show both.
4. **Create it:** `git checkout -b <branch> main`. If that fails, stop and report the error.
5. **Commit the spec** if the Working tree lists it. Run `git add <spec path>`, and `git add _agenticdev/microservices/README.md` if the Working tree lists it. Then run `git commit -m "<service> spec <NN> <short-name>: spec commit, before implementation" -- <the staged paths>`. If the Working tree is clean, the spec is already on `main`, so skip the commit and say so. If the commit fails, stop and report the error, leaving the branch in place.
6. **Plan.** Only now call EnterPlanMode. Plan the implementation of the spec's Tasks, in order, and nothing beyond them. Bound it by Explicit non-goals, and flag any Task that conflicts with a non-goal instead of resolving it.
7. **Stop at the plan.** Present it with ExitPlanMode for my approval. Approval ends this skill: confirm the branch name and stop. Do not implement anything until I ask in a new message.
