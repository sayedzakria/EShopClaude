---
name: finish-spec
description: Use when implementing, verifying and merging one spec on its feature branch, covering steps 5 to 9 of the cycle in _agenticdev/WOW.md.
argument-hint: '<service> <NN>'
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Bash(dotnet build *)
  - Bash(git status *)
  - Bash(git log *)
  - Bash(git diff *)
  - Bash(git rev-parse HEAD)
  - Bash(git branch --show-current)
  - Bash(git branch --list feature/*)
  - Bash(git add src/*)
  - Bash(git add _agenticdev/*)
  - Bash(git commit -m *)
  - Bash(git checkout main)
  - Bash(git merge --no-ff feature/*)
  - Bash(git branch -d feature/*)
  - Bash(git push origin main)
---

# Finish spec

Current branch: !`git branch --show-current`

Commits so far: !`git log --oneline main..HEAD`

Working tree (blank means clean): !`git status --porcelain`

Arguments: $ARGUMENTS

`<service>` is a folder under `_agenticdev/microservices/` (`catalog` means `catalog-api`). `<NN>` is two digits.

## Rules

- Stage files by explicit path only. Never use `git add -A` or `git add .`.
- Never push a feature branch. The only push is `git push origin main` in step 7.
- Never use `--no-verify`, `--force` or `git branch -D`.
- Every stop says what failed and what I should do next.

## Steps

1. **Preflight.** Resolve the spec `_agenticdev/microservices/<service>/<NN>-*.md` with Glob. If none matches, tell me to run `/new-spec <service> <NN>`. Derive the branch from the `_agenticdev/WOW.md` rule: `feature/<service-without-api>-<NN>-<short-name>`, where `<short-name>` is the spec filename without `<NN>-` and `.md`.
   - `git branch --list <branch>` prints nothing: stop and tell me to run `/start-spec <service> <NN>` first.
   - Current branch is not that branch, including `main`: stop and tell me to switch to it. Never run on `main`.
   - Spec Status is already `Done` and Working tree is blank: an earlier run stopped at the merge, so go to step 7.
2. **DEVELOP.** Read the spec and `CLAUDE.md`. Implement the approved plan on this branch, continuing from Commits so far, within the spec's Explicit non-goals and the CLAUDE.md conventions. If no approved plan is in this conversation, make one from the spec's Tasks in plan mode and get my approval first. Commit as you go, only when `dotnet build src/EShopClaude.sln` passes. Commit all remaining work before step 3.
3. **VERIFY.** Run `dotnet build src/EShopClaude.sln` and stop on any error. Then walk the spec's Definition of Done line by line:
   - A line that needs only the repo and a shell, such as `dotnet test` or `git diff`: run it and compare with the expected result.
   - A line that needs the running app, the Aspire dashboard, pgAdmin or a request in the `.http` file: quote it, ask me whether it passes, and wait for my answer.
   - A failed check, or any answer other than a clear yes: stop here, name the line, and merge nothing.
4. **Mark the spec Done.** Run `git rev-parse HEAD` and set the Status line to `**Status:** Done (<sha>)`. This is the last implementation commit.
5. **Update the README.** Set this spec's Status cell in `_agenticdev/microservices/README.md` to the same `Done (<sha>)`.
6. **Commit the docs.** Run `git add` once for the spec file and once for that README, by explicit path, then `git commit -m "Mark <service> spec <NN> as Done"`.
7. **Confirm, then merge.** If `git status --porcelain` shows anything, stop and list it. Show me the branch name, the target `main`, and the actions: checkout `main`, merge with `--no-ff`, delete the branch, push `main`. Wait for a clear yes; on anything else, stop and leave the branch as it is. Then run, stopping on any error:
   1. `git checkout main`
   2. `git merge --no-ff <branch> -m "Merge <branch> into main"`. On a conflict, stop, list the conflicted files and leave the merge in progress for me. Do not resolve it.
   3. `git branch -d <branch>`
   4. `git push origin main`
8. **Report** the merge commit SHA from `git rev-parse HEAD`, the files changed from `git diff --name-only HEAD^1 HEAD`, and each Definition of Done line with how it was verified: the command you ran, or my answer.
