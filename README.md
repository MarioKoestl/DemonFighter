# Demon Fighter

A 3D roguelike for Windows (Steam). Born as a demon blob in a glowing cavern, you bite, eat, gain Biomass, mutate new body parts and evolve to survive in a procedurally generated world where every other demon follows the same rules. Realistic, dark and gory. Third-person with a first-person toggle.

Status: pre-production. See `docs/ROADMAP.md`.

Repository: https://github.com/MarioKoestl/DemonFighter.git (private). Local checkout: `C:\Development\DemonFighter`. The Unity project sits at the repository root.

## Documents

- `CLAUDE.md`: instructions for Claude Code. Start here if you are an AI.
- `docs/GAME_DESIGN.md`: what the game is.
- `docs/ARCHITECTURE.md`: how the code is structured.
- `docs/CODING_GUIDELINES.md`: how code is written.
- `docs/WORKFLOW.md`: how work flows between Mario and Claude Code.
- `docs/ROADMAP.md`: milestones and acceptance criteria.
- `docs/ASSET_PIPELINE.md`: models, animation, gore assets, licenses.
- `docs/DECISIONS.md`: decisions and open questions.
- `docs/UNITY_PRIMER.md`: Unity explained for a .NET developer.

## One-time setup (Mario)

1. Install **Git for Windows** (includes Git Bash) and **Git LFS**. Run `git lfs install` once.
2. Install **Unity Hub**, then the latest **Unity 6 LTS** with the module "Windows Build Support (IL2CPP)". Sign in with a Unity account; the Personal plan is free below Unity's revenue threshold (check current terms).
3. Install **VS Code** with the extensions "C# Dev Kit" (Microsoft), "Unity" (Microsoft) and "Claude Code" (Anthropic). Install the .NET SDK if `dotnet --version` does not work.
4. The repository is already cloned at `C:\Development\DemonFighter`. Create a folder `ArtSource/` inside it for generated 3D and music sources; it is gitignored on purpose.
5. Create the Unity project. Unity Hub wants an empty folder, and the repository already has files, so: in Unity Hub choose New project, template **Universal 3D**, project name `DemonFighter`, location `C:\Development\_unity-tmp`. Wait for it to open once, then close Unity. Move the folders `Assets`, `Packages` and `ProjectSettings` from `C:\Development\_unity-tmp\DemonFighter` into `C:\Development\DemonFighter`, delete the temp folder, and open `C:\Development\DemonFighter` through Unity Hub (Add project from disk).
6. In Unity: Edit > Preferences > External Tools: set the external script editor to VS Code and tick "Generate .csproj files for: Embedded packages, Local packages, Registry packages". Click "Regenerate project files".
7. Edit > Project Settings > Editor: confirm Asset Serialization = Force Text and Version Control = Visible Meta Files.
8. Close Unity. In `C:\Development\DemonFighter`: `git add -A`, `git commit -m "chore(project): create Unity project"`, `git push`.
9. Open the repository in VS Code, start Claude Code, and give it the first task below.

## First task for Claude Code

```
Read CLAUDE.md and the docs it points to. Then execute the Claude Code part of milestone M0 in docs/ROADMAP.md.
Work in a branch chore/m0-project-setup and open one PR. Ask before adding anything not listed in M0.
```

## Running tests

- In Unity: Window > General > Test Runner, EditMode tab, Run All.
- From PowerShell, see the commands in `CLAUDE.md`.

## License

All rights reserved. Not open source. Third-party assets and code are listed in `docs/ASSET_LICENSES.md`.
