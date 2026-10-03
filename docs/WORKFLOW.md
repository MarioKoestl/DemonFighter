# Workflow

How work gets done on Demon Fighter. Three roles:

- **Mario**: owner, designer, tester, Unity Editor operator, reviewer. Decides.
- **Claude Code**: implements tasks in the working tree, writes tests, proposes commit messages, asks questions. Never touches Git state.
- **Claude (chat)**: design discussions, planning, writing and updating the documents in `docs/`.

## The reload rule

Claude Code reads the current version of a file right before editing it. Every time. Mario edits files in VS Code and in the Unity Editor between sessions, and Unity rewrites scene, prefab and `.meta` files on its own. Working from a cached or remembered version has overwritten Mario's changes in other projects. It must not happen here.

Practical form: run `git status` and `git diff` (read-only) at the start of a session, and open a file again before each edit even if it was read earlier in the session.

## Session start (Claude Code)

0. You are in `C:\Development\DemonFighter`, the checkout of https://github.com/MarioKoestl/DemonFighter.git.
1. `git status` and `git diff` (read-only). If the working tree has uncommitted changes, ask Mario whether they are his work in progress before touching anything.
2. Read `CLAUDE.md`.
3. Read the "Current milestone" section of `docs/ROADMAP.md`.
4. Read the task from Mario, or pick the next unchecked item in the current milestone.
5. List the files you expect to touch and read them.
6. If anything is unclear, ask one precise question. Then start.

## Task format

Mario writes tasks in this shape. Short is fine; the headings matter more than the length.

```
Goal: what should be true when this is done
Why: one sentence of context, or a ROADMAP item reference
Acceptance: bullet list, each verifiable in Play Mode or by a test
Constraints: anything that must not change, or a design choice already made
Notes: links, ideas, non-binding hints
```

If a task arrives without acceptance criteria, Claude Code proposes them in its first reply and waits for a yes before implementing.

## Definition of done

A task is done when all of this holds:

- `dotnet build` passes with no warnings in Simulation.
- EditMode tests pass, and new simulation behaviour has tests.
- The acceptance criteria are met, and the PR says how Mario can verify each one in Unity.
- No `Debug.Log` left behind, no commented-out code, no TODOs without a reference.
- New content assets have ids and pass `OnValidate`.
- New decisions or dependencies are recorded in `docs/DECISIONS.md`.
- `docs/ROADMAP.md` checkboxes are updated for the items the task completes.
- `dotnet format` has been run.
- The handover message lists every changed file and proposes one Conventional Commit message with scope.

## Verification protocol

Claude Code cannot run Play Mode. Every handover that touches Presentation, Input, UI or App includes a section like this (Mario pastes it into the PR):

```
How to verify in Unity
1. Open Assets/_Project/Scenes/Run.unity
2. Press Play
3. Press V. Expected: camera switches to third-person behind the demon, the demon's body is visible.
4. Walk into a rock. Expected: the demon stops, no jitter.
Known gaps: third-person camera clips through walls (M5 item).
```

Mario runs the steps and replies with what he saw. A step that fails goes back to Claude Code with the observation and, if useful, a screenshot or the relevant lines from the Console.

## Git, branches and pull requests

Git is Mario's tool and his safety net. Claude Code never commits, pushes, branches, checks out, stashes or resets. The flow for one task:

1. Mario creates the branch (`feat/<topic>` and friends) and starts the Claude Code session.
2. Claude Code edits files, runs builds and tests, and hands over: what changed, which files, how to verify, open questions, and a proposed commit message.
3. Mario reviews the diff in VS Code. If something is off, he tells Claude Code, which fixes it in the working tree.
4. Mario commits with the proposed message (or his own), pushes, opens the PR using `.github/pull_request_template.md`, and squash merges when the Unity verification passed.
5. `main` is protected; nothing goes there without a PR. Mario reviews every change himself, also small ones; that is part of learning the codebase.

If a session goes wrong, Mario discards the working tree (`git checkout .` / `git clean`) and starts over. This is why Claude Code must not create commits: a half-finished state must never be in history.

## What Claude Code can and cannot do in a Unity project

Can do from the repository:

- Write and edit C# in all assemblies, assembly definitions, `csc.rsp`, `.editorconfig`.
- Create and edit ScriptableObject assets as YAML when the schema is simple (or better: write an editor script that creates them).
- Edit `Packages/manifest.json` to add Unity packages (needs a DECISIONS entry).
- Edit project settings files under `ProjectSettings/` when the task calls for it, carefully.
- Write editor scripts (`Scripts/Editor`) that build scenes, prefabs and catalogs, so that Mario triggers them from a menu instead of clicking through the editor by hand.
- Run `dotnet build`, `dotnet format` and the batch mode test commands from `CLAUDE.md`.
- Read-only Git: `status`, `diff`, `log`, `show`, `blame`.

Cannot do, Mario does it:

- Anything that changes Git state: commit, push, branch, merge, stash, reset, tag.
- Install Unity, open the editor, press Play, look at the screen.
- Import a model and judge whether it looks right.
- Click through the Inspector to set a reference that only exists after an import.
- Accept a package's license dialog.
- Run anything that needs the GPU.

When a task needs an editor action, Claude Code writes the exact steps for Mario in the handover, or writes a menu item that does it, and prefers the menu item.

Optional later: a Unity MCP server that lets Claude Code drive the editor directly. Not part of M0 to M2; we revisit it when the manual round trips become the bottleneck. Add it to `DECISIONS.md` when evaluated.

## Questions and decisions

- Questions that block a task: ask immediately in the session.
- Design questions that do not block: add them under "Open" in `docs/DECISIONS.md` with a proposed default, continue with the default, and mention it in the handover.
- Task tracking: milestones and their items live in `docs/ROADMAP.md`; bugs and small follow-ups are GitHub Issues.
- Decisions Mario makes in chat are written into `docs/DECISIONS.md` by Claude (chat) or by Claude Code on request. A decision that is not in that file does not exist for the next session.

## Communication style

- English in the repository. German in chat with Mario is fine; code, commits, comments, PR text and docs stay English.
- Short and specific. One screen per handover is the target.
- When something went wrong, say what, why, and what the fix is. No apologies, no padding.
