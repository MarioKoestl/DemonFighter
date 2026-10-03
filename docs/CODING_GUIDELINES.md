# Coding Guidelines

C# in Unity 6. Microsoft .NET conventions where Unity does not force otherwise. When these guidelines and an existing file disagree, follow the guidelines and fix the file in a separate `refactor:` commit.

## Language and compiler

- C# 9 (what Unity 6 supports). No features beyond that: no `record struct`, no file-scoped namespaces, no global usings. `record` classes, `init` setters, pattern matching and target-typed `new` are fine.
- `#nullable enable` at the top of every file in `Simulation`, `Data` and `App`. Presentation files may skip it where Unity's serialization makes it noisy, but new code should enable it.
- Warnings are errors in the Simulation assembly (`csc.rsp` in that folder). Elsewhere, fix warnings before opening a PR.
- `.editorconfig` at the repository root defines formatting. Do not argue with it. Run `dotnet format` before handing a task back; it is on the definition-of-done checklist, not a Git hook.
- Microsoft.Unity.Analyzers is installed in the project (Roslyn analyzers that catch typical Unity mistakes such as null comparisons and `GetComponent` in `Update`). Fix what it reports; do not suppress it without a comment.

## Naming

| Thing | Style | Example |
|---|---|---|
| Namespace | PascalCase, mirrors folder, rooted at `DemonFighter` | `DemonFighter.Simulation.Combat` |
| Class, struct, record, interface, enum | PascalCase, interfaces start with `I` | `BodyPart`, `ISkillBehaviour` |
| Method, property, event | PascalCase | `ApplyDamage`, `IsSevered` |
| Private field | `_camelCase` | `_parts` |
| Serialized private field (Unity) | `_camelCase` with `[SerializeField]` | `[SerializeField] private float _moveSpeed;` |
| Local, parameter | camelCase | `damageType` |
| Constant | PascalCase | `MaxUpgradeLevel` |
| Static readonly | PascalCase | `DefaultSeed` |
| Enum members | PascalCase | `PartCondition.Severed` |
| ScriptableObject asset file | Prefix by type | `BP_Arm_Basic`, `SK_Bite`, `MU_GrowArm`, `AR_Scavenger`, `BI_AshPlain` |
| Prefab | Prefix by role | `P_Demon`, `P_Food_Corpse`, `P_Decal_Blood` |
| Scene | PascalCase | `Run`, `MainMenu`, `Bootstrap` |
| Test | `Method_Condition_ExpectedResult` | `ApplyDamage_PartAtZeroHp_SeversPart` |

One type per file. File name equals type name. Folder equals namespace. Block-scoped namespaces (C# 9).

Formatting: Allman braces (opening brace on its own line), 4 spaces, lines up to 120 characters, no `#region`. Access modifiers are always written out. Types are `internal` by default; `public` only where another assembly needs them. Test assemblies get `InternalsVisibleTo`. `var` only when the type is obvious from the right-hand side; otherwise the explicit type.

## Simulation code

- Plain C#. No `UnityEngine`, no Unity packages. The assembly definition blocks it; do not work around that.
- Immutable specs (`record` classes), mutable state (`class` with clear ownership), small value types as `readonly struct`. State is changed only by the simulation itself in response to commands.
- Use `float` for gameplay math (matches Unity). Never compare floats with `==`; use a tolerance helper.
- Randomness only through the run's `Rng`. No `System.Random` created ad hoc, no `Guid.NewGuid()` for ids.
- No LINQ, no `foreach` over `List<T>` with allocating enumerators, no string concatenation in the tick path. LINQ is fine in setup code and tests.
- Fail fast: invalid commands are rejected with a reason (`CommandResult`), programming errors throw. Do not swallow exceptions.
- Every public type and method in Simulation has a one-line XML doc comment explaining the rule it implements, not restating the signature.

## Unity code (Presentation, Input, UI, App)

- MonoBehaviours are adapters. If a method contains a game rule (damage math, cost check, AI scoring), it is in the wrong place.
- Cache component references in `Awake`. Never call `GetComponent`, `Find`, `FindObjectOfType` or `Camera.main` in `Update` or `FixedUpdate`.
- `[SerializeField] private` instead of public fields. Expose read-only properties when other code needs the value.
- `[RequireComponent]` on components that depend on others on the same object.
- No `DontDestroyOnLoad` outside the bootstrap. No static singletons. Get services from the composition root through constructor injection or an `Initialize(GameServices)` call made by the object that created you.
- Prefer Unity 6 `Awaitable` over coroutines for async flow. Use coroutines only where a Unity API needs one.
- Compare Unity objects to null with `if (obj == null)`, never with `is null` or `?.` (Unity's fake null, see `UNITY_PRIMER.md`).
- Tuning values (speeds, costs, cooldowns, durations) live in ScriptableObjects, not in code. No magic numbers in behaviour code.
- Use `UnityEngine.Pool.ObjectPool<T>` for anything spawned more than a few times per minute (decals, particles, food views).
- Use the Input System only through `PlayerInputAdapter`. No `Input.GetKey`.
- Logging through the project `Log` helper (`Log.Info`, `Log.Warn`, `Log.Error`, each with a category). Remove `Debug.Log` calls before opening a PR. Keep `Debug.Assert` for invariants.

## ScriptableObjects

- Data only. No runtime state. If you feel the need to write to a ScriptableObject at runtime, you need a state class instead.
- `[CreateAssetMenu(menuName = "Demon Fighter/<Category>/<Name>")]`.
- Every definition has a stable string `Id` (lowercase, dotted: `part.arm.basic`). Ids never change after an asset ships; add a new asset instead.
- Validate in `OnValidate` (costs non-negative, required references set) and surface errors with a clear message.

## Scenes and prefabs

- Scenes are small. `Run.unity` contains only what the world builder cannot create: lighting settings, post-processing volume, the camera rig, the HUD root.
- Prefabs for demons, parts and food are generated by editor scripts where possible (`Demon Fighter > Generate > ...`) so they can be regenerated after a change.
- Never hand-edit `.unity`, `.prefab`, `.asset` or `.meta` YAML unless the task is explicitly about that and the diff is tiny. Say so in the commit message.

## Tests

- Framework: Unity Test Framework (NUnit runner). Assertions with FluentAssertions 7.x or the AwesomeAssertions fork (`result.Should().Be(3)`), never FluentAssertions 8 or newer (commercial license). Mocking with NSubstitute where a fake would be noisy; builders and hand-written fakes stay the first choice for simulation state.
- Simulation tests are EditMode, live in `Assets/_Project/Tests/Simulation`, mirror the source folder structure.
- One behaviour per test. Arrange, act, assert, separated by a blank line. Name: `Method_Condition_ExpectedResult`.
- Use the builders (`DemonBuilder`, `RunStateBuilder`) instead of constructing state by hand.
- Every bug fix in the simulation gets a regression test first.
- PlayMode tests only for things that cannot be tested otherwise (scene wiring, view binding). Keep them under ten in total for v1.

## Error handling

- Simulation: exceptions for programmer errors, `CommandResult.Rejected(reason)` for invalid player or AI actions. Never both.
- Unity side: catch at the boundary (`SimulationRunner`), log with category `Sim`, stop the run with a visible error in development builds. Do not let an exception in a view take down the frame loop silently.

## Comments

- Explain why, not what. If the what is unclear, rename or restructure.
- No commented-out code. Git has history.
- `// TODO(name): ...` only with a linked issue number or a ROADMAP item.

## Git

Git belongs to Mario. Claude Code never runs a command that changes repository state: no `commit`, `push`, `branch`, `checkout`, `merge`, `rebase`, `stash`, `reset`, `tag`. Read-only commands (`status`, `diff`, `log`, `show`, `blame`) are fine and encouraged. Mario commits because the history is his safety net when a session goes wrong.

- At the end of a task Claude Code lists the changed files and proposes one commit message. Mario reviews the diff, commits, pushes.
- Branches (created by Mario): `feat/<topic>`, `fix/<topic>`, `chore/<topic>`, `docs/<topic>`. Lowercase, hyphens.
- Conventional Commits with scope: `feat(sim): ...`, `fix(ui): ...`, `chore(project): ...`. Scopes: `sim`, `data`, `presentation`, `input`, `ui`, `app`, `editor`, `tests`, `content`, `project`, `docs`. Subject under 72 characters, imperative, no period. Body explains why when the diff does not.
- One logical change per commit. Squash merge to `main` through a pull request. Tags on `main` per milestone (`v0.1.0` for M1, `v0.2.0` for M2, ...); the build shows the version.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.sln` or `.csproj` files. `.gitignore` handles it; do not force-add.
- Binary assets go through Git LFS (`.gitattributes` is set up). If you add a new binary type, add its pattern there in the same PR.
- Commit new files together with their `.meta` files. A file without its `.meta` breaks references for everyone else.

## Things Claude Code must not do

- Introduce a static singleton, a service locator reachable from anywhere, or a `GameManager` god class.
- Put game rules into a MonoBehaviour.
- Read input outside `PlayerInputAdapter`.
- Use `Resources.Load`. Content goes through the catalog.
- Mutate a ScriptableObject at runtime.
- Copy code from the Asset Store, forums or tutorials without a license note in `docs/ASSET_LICENSES.md`.
- Edit `.meta` files or delete them.
- Rename an asset id that has shipped.
- Skip tests for simulation code because "it is a small change".
- Run any Git command that changes state (see Git above).
- Add FluentAssertions 8 or newer, or any other package with a commercial license, without Mario's explicit ok.
