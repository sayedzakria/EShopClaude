---
name: new-spec
description: Use when preparing one feature spec for the spec-driven workflow in _agenticdev/WOW.md, either drafting a new spec or filling the gaps in an existing one.
argument-hint: '<service> <NN> [slug] ["title"]'
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob
---

# New spec

Arguments: $ARGUMENTS

`<service>` is a folder under `_agenticdev/microservices/` (`catalog` means `catalog-api`). `<NN>` is two digits. A quoted argument is the title.

## Rules

- Change only Markdown under `_agenticdev/`. `src/` is read-only.
- Never run a shell command, create a branch or commit.
- Write nothing to disk until step 5 is answered.
- Never change a Status value. A new spec and a new table row say `Not started`.

## Steps

1. **Resolve** `_agenticdev/microservices/<service>/<NN>-*.md` with Glob. Stop and say why if `<service>` or `<NN>` is missing, the folder does not exist, or more than one file matches. Read `_agenticdev/WOW.md`.
2. **If no file matches**, plan `<NN>-<slug>.md` from `_agenticdev/templates/spec-template.md`, with `Status: Not started`. Draft each section from the matching parts of `_agenticdev/PROJECT.md` and `_agenticdev/PLAN.md`. If slug or title was not given, take it from the status table, otherwise ask in step 5.
3. **If a file matches**, read it and report each template section as Complete, Stub, Too vague or Missing, with a one-line reason. If its Status is `In progress` or `Done`, stop after the report. Otherwise read `PROJECT.md` and `PLAN.md` and continue.
4. **Read `_agenticdev/microservices/README.md`.** Keep a "Depends on" that is already filled. Otherwise list only the specs this one really needs, as `<service>/<NN>-<slug>`, using the status table and its build-order line, or write `Nothing.` If the table has no row for this spec, plan one: `| <service> | [<NN>-<slug>](<service>/<NN>-<slug>.md) | `feature/<service-without-api>-<NN>-<slug>` | Not started |`, placed in number order within the service.
5. **Ask before writing.** Read `src/` and the specs this one depends on for repo facts. Then ask every design question those documents do not answer, such as field names and their types, how the database schema is created, and how the service gets its connection string. Ask one question per decision and offer any precedent you found as the suggested answer. Wait for the answers.
6. **Write the spec.** For a new spec, use Write once, after confirming with Glob that the path is still free. For an existing spec, use Edit on the gaps only and never Write. Add the table row if planned.
   - Follow each section's comment in the template, then remove the comment.
   - Fill Context, Data model, Tasks and Files touched. Scope and files cover one microservice and its AppHost registration only.
   - Sharpen every Definition of Done line into a check a person can run: the exact command or request and the expected result. Include `dotnet build`, and `aspire run` with every resource healthy.
   - Mark anything left unanswered `TBD`.
7. **Print** the spec path and a list of everything still to be decided by hand, including each `TBD` and any dependency not yet `Done`. If there is nothing, say so.
