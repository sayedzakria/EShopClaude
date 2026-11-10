---
name: auto-spec
description: Use when running one spec end to end through new-spec, start-spec and finish-spec, with a human gate between them.
argument-hint: '<service> <NN>'
disable-model-invocation: true
---

# Auto spec

Arguments: $ARGUMENTS

This skill only sequences. Invoke each skill below with the Skill tool, by name, passing `<service> <NN>` as its arguments, and follow its instructions. When it finishes normally, come back here for the next step. Every question those skills ask still goes to me and waits for my answer.

## When not to use this

Run the three skills one at a time instead when:

- the spec introduces a pattern this repository has not built before
- the spec touches more than one service
- the spec's Definition of Done contains a check nobody can actually run

## Steps

1. Invoke `new-spec`.
2. **GATE ONE.** Read the spec, show it to me in full, and ask me to confirm it is buildable. Wait. Anything other than a clear yes stops here.
3. Invoke `start-spec`.
4. **GATE TWO.** The plan approval in `start-spec` is this gate. Anything other than approval stops here. Approval is the go-ahead `start-spec` waits for, so continue to step 5 without a new message.
5. Invoke `finish-spec`.
6. **At any step**, if one of the three refuses or tells you to stop, stop immediately. Report which skill refused and why. Never work around a refusal: do not retry it, fix its cause, or run its steps yourself.
7. Print the close-out summary from the `finish-spec` report.
