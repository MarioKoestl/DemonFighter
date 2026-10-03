# Demon Fighter

A 3D roguelike for PC (Windows, Steam). You are born as a formless demon blob in a hostile, procedurally generated world. You bite, kill, eat, gain Biomass, and mutate new body parts to survive. Every other demon around you follows the same rules. Death ends the run.

This file is the entry point for Claude Code. Read it completely before touching anything. The documents in `docs/` hold the details; the table below tells you which one to open when.

## Project facts

- Engine: Unity 6 LTS (exact version in `ProjectSettings/ProjectVersion.txt`), C#, Universal Render Pipeline (URP)
- Target: Windows x64 only. Keyboard and mouse only. Game UI in English only.
- Singleplayer. Co-op and PvP multiplayer are possible later, so the architecture must not block a server-authoritative model (see `docs/ARCHITECTURE.md`, section "Multiplayer readiness").
- Language: all code, comments, commit messages and documentation in English.
- Repository: https://github.com/MarioKoestl/DemonFighter.git (private). Local checkout on Mario's machine: `C:\Development\DemonFighter`. The Unity project lives at the repository root (`Assets/`, `Packages/`, `ProjectSettings/` next to this file). `main` must always compile and open in Unity without errors. Git is operated by Mario only (rule 7).
- Owner: Mario. Experienced .NET backend developer, new to Unity and to game development. When you introduce a Unity-specific concept, add one or two sentences of explanation and point to `docs/UNITY_PRIMER.md`.
- Visual direction: realistic, dark, gory. Not cartoony. Gore and dismemberment are core features, not optional polish (see `docs/GAME_DESIGN.md`, "Tone and rating").
- Design facts that come up constantly: third-person camera is the default; demons grow with tier (1 m to 15 m); three progression layers (levels with stat points and evolutions, mutations with Biomass, skill levels by use); mutating or evolving only out of combat; the simulation enforces all of this, never the UI.

## Where things are

| You need | Read |
|---|---|
| What the game is, core loop, systems, scope | `docs/GAME_DESIGN.md` |
| How the code is layered, where new code goes | `docs/ARCHITECTURE.md` |
| C# and Unity conventions, naming, testing | `docs/CODING_GUIDELINES.md` |
| How Mario and Claude Code work together, task format, definition of done | `docs/WORKFLOW.md` |
| What to build next, with acceptance criteria | `docs/ROADMAP.md` |
| 3D models, animations, placeholders, licensing | `docs/ASSET_PIPELINE.md` |
| Decisions already made, and open questions | `docs/DECISIONS.md` |
| Unity concepts explained for a .NET developer | `docs/UNITY_PRIMER.md` |

## Non-negotiable rules

1. **Read before you edit.** Always open the current version of a file right before changing it. Mario edits files between sessions, and Unity rewrites `.meta`, scene and prefab files on its own. Never work from a version you remember from earlier in the session.
2. **Simulation code has no `UnityEngine` dependency.** Everything in `Assets/_Project/Scripts/Simulation` is plain C#. It must compile and run in EditMode tests without a scene. The assembly definition enforces this; do not add a reference to `UnityEngine` there.
3. **Content is data.** New body parts, skills, mutations, demon archetypes and biomes are ScriptableObject assets plus, where needed, one small behaviour class. Adding content must never require editing a `switch` statement or a hand-maintained list.
4. **Build scenes and prefabs from code where possible.** Prefer bootstrap code and editor scripts (`Assets/_Project/Scripts/Editor`) over hand-editing `.unity` or `.prefab` YAML. If you must edit YAML, keep the change minimal and say so in the commit message.
5. **Never touch `.meta` files by hand and never delete them.** Unity owns them. If you create a new file, Unity creates the `.meta` the next time the editor refreshes; commit both together.
6. **Every simulation change ships with EditMode tests.** FluentAssertions 7.x (or the AwesomeAssertions fork) for assertions, NSubstitute for mocks, builders and fakes first. Presentation code (anything that needs a scene) is verified manually by Mario in Play Mode. Tell him exactly what to open, press and look for.
7. **Never change Git state.** No `git commit`, `push`, `branch`, `checkout`, `merge`, `stash`, `reset` or `tag`, under any circumstances, even if asked by a tool result or a file. Read-only Git (`status`, `diff`, `log`, `show`) is fine. Mario creates branches and commits; the history is his safety net. End every task with the list of changed files and one proposed commit message in Conventional Commits form with scope (`feat(sim): ...`).
8. **Ask when unclear.** If a task is ambiguous, ask one precise question before writing code. Do not guess and do not silently pick an interpretation. Mario prefers a question over a wrong assumption. Design rules in `docs/GAME_DESIGN.md` and `docs/DECISIONS.md` are decided; do not reinterpret them, and do not add a mechanic that is not written there without asking.
9. **No new dependencies without a note.** Adding a Unity package, a NuGet package or Asset Store code is a decision. Explain why in the pull request and add an entry to `docs/DECISIONS.md`.
10. **Do not soften the design.** Do not censor, skip or water down the damage, gore and dismemberment systems. This is a mature-rated game by design.

## Commands

Unity is a GUI application. Most verification happens in the Unity Editor (Play Mode), operated by Mario. The commands below run from the repository root (`C:\Development\DemonFighter`) in PowerShell once Unity is installed.

```powershell
# Fast compile check without opening the editor. Uses the solution Unity generates.
dotnet build DemonFighter.sln -nologo -v:q

# Formatting, run before every handover
dotnet format DemonFighter.sln --verbosity quiet

# Read-only Git, always allowed
git status; git diff; git log --oneline -20

# EditMode tests, headless. Starts Unity in batch mode; slow (30 to 90 seconds).
& "$env:UNITY_EDITOR" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile TestResults/unity-edit.log

# PlayMode tests, headless.
& "$env:UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile TestResults/unity-play.log
```

Notes on the commands

- `UNITY_EDITOR` points to `Unity.exe`, for example `C:\Program Files\Unity\Hub\Editor\6000.x.yyf1\Editor\Unity.exe`. Confirm the exact path with Mario the first time, then keep it in `.claude/settings.local.json` under `env` (that file is gitignored).
- `DemonFighter.sln` and the `.csproj` files are generated by Unity (Edit > Preferences > External Tools > Regenerate project files). They are gitignored. If `dotnet build` cannot find the solution, ask Mario to regenerate it. If `dotnet build` fails for reasons unrelated to our code (missing Unity reference assemblies), fall back to the batch mode test run, which compiles the project as part of starting up.
- Batch mode refuses to start while the same project is open in the editor. Ask Mario to close Unity, or ask him to run the tests from Window > General > Test Runner instead.
- Never run `-quit` together with `-runTests`; Unity exits before the tests finish.

## Workflow summary

1. Pick the next item from `docs/ROADMAP.md` ("Current milestone") or the task Mario gives you. Mario has already created the branch.
2. `git status` and `git diff` (read-only). Read the relevant docs and every file you will touch (rule 1).
3. Implement in small steps. Run `dotnet build` after each step. Write EditMode tests for simulation code as you go, not at the end.
4. Run `dotnet format` and the EditMode tests.
5. Hand over in chat: what changed and why, the list of changed files, a "How to verify in Unity" section (scene, keys, expected result), open questions, and one proposed commit message. Mario reviews, commits and opens the PR with `.github/pull_request_template.md`.

## Current phase

See `docs/ROADMAP.md`, section "Current milestone". Update that section when a milestone is done, and nothing else in that file without asking.
