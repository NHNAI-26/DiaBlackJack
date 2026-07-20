# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**데블랙잭 / DiaBlackJack** — a single-player, blackjack-based deck-building roguelite (PC). The player spends *souls* as both HP and currency, and wins blackjack rounds either by number or by using card effects (pistol, knife, hammer, orb) to bust the opponent directly.

Unity **6000.3.10f1**, URP 17.3.0, C#. Team of 3.

Documentation is written in Korean; **code, identifiers, and comments are English**. Keep that split.

## Commands

Unity Editor lives at `F:\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`.

There is no CI, no build script, and no test runner script. Tests are Unity **EditMode** tests run one of two ways:

**1. MCP for Unity (primary — use this).** The `com.coplaydev.unity-mcp` package is in `Packages/manifest.json`. It runs tests **inside the already-running Editor** via the Test Runner API, compiles, reads Console, and validates scenes — **nothing needs to be closed.** This is how every `job <id>` cited in the progress logs was produced. The Editor exposes its bridge on `127.0.0.1:6400` while open. (`Docs/project-structure-and-mcp-reference.md` says HTTP `8080/mcp`; that port is closed — the doc is stale.)

The MCP *client* is registered **per-developer, not in the repo** — there is deliberately no `.mcp.json`, so each person configures their own and nobody's setup disturbs the team. If `mcp__*` Unity tools are missing from your tool surface, the client just isn't registered on this machine; check with `claude mcp list`. The progress logs record several sessions where they were unavailable (§7.14), so verify rather than assume.

**2. Unity batch mode (fallback only — when MCP is unavailable).**

```powershell
& "F:\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" -runTests -batchmode `
  -projectPath "F:\Unity Project\DiaBlackJack" `
  -testPlatform EditMode `
  -testResults "$env:TEMP\dbj-results.xml" `
  -logFile "$env:TEMP\dbj-unity.log"
```

- **Close the Editor first.** This spawns a *second* Unity process, and Unity single-instance-locks the project (`Temp/UnityLockfile`), so it fails while the Editor holds the lock. Check for a running `Unity` process before invoking. This restriction applies **only to this path** — path 1 above reuses the live Editor and needs no such thing.
- Do **not** add `-quit`; `-runTests` exits on its own. Read pass/fail from the results XML, not stdout.
- One assembly: `-assemblyNames "DiaBlackJack.CoreLoop.Tests.EditMode"`
- One test: `-testFilter "DiaBlackJack.CoreLoop.Tests.CoreLoopFlowTests.CL_U01_..."` (also accepts a regex, and `;`-separated names)

Baseline as of the last recorded run: **315 EditMode tests, 0 failures** (CoreLoop 193 + StageProgression 122). Any change is expected to keep the full suite green — the progress logs record exact counts per task, so a drop is visible.

## Architecture

Everything under `Assets/01. Scripts/Runtime/` compiles into **one** assembly, `Border` (`Border.asmdef`). Layering is by namespace and discipline, not by asmdef:

```
Border/
  Core/              DeterministicRng, Log, ScreenshotManager   (Log/Screenshot use UnityEngine)
  CoreLoop/          one battle: rules, state, cards, enemy AI  — PURE C#
  StageProgression/  one run: stages, player state, rewards     — PURE C#
  UI/                MonoBehaviours + IMGUI                     — Unity-facing
```

### The purity rule (most important constraint)

`CoreLoop/` and `StageProgression/` contain **zero `UnityEngine` references**. This is what makes ~315 tests runnable as plain NUnit against real game rules. **Nothing enforces it** — `Border.asmdef` itself references `Unity.TextMeshPro` and `UnityEngine.UI`, so a stray `using UnityEngine;` would compile fine. Preserve it manually. Rules go in CoreLoop/StageProgression; anything needing a `GameObject`, `Time`, `Debug`, or `SceneManager` goes in `UI/`.

Known existing violation of layer direction: `CoreLoop/EnemyProfiles/EnemyBattleConfiguration.cs` imports `DiaBlackJack.StageProgression` for `BattleRewardTier`. CoreLoop otherwise does not depend upward.

### `Try*` is the universal entry-point pattern

Every state-mutating public method is `bool TryXxx(...)`. Invalid input returns `false` and **leaves all state untouched** — it does not throw. Failure atomicity is tested explicitly across the suite (bad offer ids, stale offers, duplicate confirms, insufficient state). When adding an operation, follow this: validate fully, construct anything that might fail, *then* commit the transition.

`throw` is reserved for genuine invariant breaches (e.g. `StageProgressionSession` throws `InvalidOperationException` if `RunProgress` rejects an already-validated advance).

### CoreLoop — one battle

`CoreLoopState`: `Initializing → StartingRound → PlayerTurn ⇄ {PlayerChoosingChangeCard, PlayerResolvingCardEffect} ⇄ EnemyTurn → ResolvingRound → {StartingRound | BattleEnded}`.

- `CoreLoopBattle` holds all battle state. `CoreLoopSession` is a thin facade over it plus a `Func<CoreLoopBattle>` restart factory — the factory must return an *unstarted* battle; the session calls `Start()`.
- **A player action synchronously runs the enemy to completion.** `TryPlayerHit()` → player draws → `RunEnemyTurn()` loops `while (State == EnemyTurn)` → round may resolve → `CompleteRound()` applies damage and **auto-starts the next round**. By the time `TryPlayerHit()` returns, the enemy has already acted and the round may be over. Do not write code expecting to "step" the enemy separately.
- Mutators on `BattleParticipant` (`Draw`, `Stand`, `ClearRound`) are `internal` so only `CoreLoopBattle` can mutate participant state.

### Enemy AI fairness (a hard design invariant, not a nicety)

The core pitch of the game is that the AI infers rather than cheats. This is enforced structurally — **do not weaken it**:

- `IEnemyBehaviorPolicy.Decide(EnemyObservation)` is the entire AI surface. **A policy never receives a `CoreLoopBattle` reference**, so it cannot reach hidden state.
- `EnemyObservationFactory` is the enforcing type. `EnemyObservation` has `PlayerFaceUpCards` and `PlayerHiddenCardCount` — there is **no field carrying a hidden rank**. Collections are deep-copied into `ReadOnlyCollection<T>`.
- Policies substitute `EnemyNumberInferenceCalculator`, which computes rank probabilities from public info only.
- Decisions are validated **twice**: `EnemyObservation.ActionCandidates` is the legal-move whitelist, and `TryExecuteEnemyDecision` rebuilds a fresh observation and re-validates before executing. An invalid decision retries twice then falls back to a `Stand`-preferring candidate.

The UI mirrors this: `CoreLoopPresenter` calls `FormatCards(..., revealAll: true)` for the player and `revealAll: false` for the enemy, emitting `"?"` — the hidden rank never enters the UI layer. `EnemyCombatDisplaySnapshot` has an `internal` constructor so only its factory can build one, and it exposes grade-gated projections (Normal: top-3 with probabilities; Elite: top numbers, no probabilities, coarse confidence; Boss: telegraph state) — never the policy object.

### Card effects

`CardDefinition` (key, rank, display name, `CardActivationKind`, `CardEffectKind`) lives in the static `CardDefinitionCatalog` — 10 hardcoded definitions, one default per rank 1–10.

Handlers never touch `CoreLoopBattle`; they receive `CardEffectContext`, an **actor-relative** facade (`Actor`/`Opponent` resolved from `ActorSide`), so one handler serves both player and enemy. A handler returns a `CardEffectStep` — either `AwaitChoice(...)` or `Complete(...)`.

**To add a card effect:** add a `CardEffectKind` value → implement `internal ICardEffectHandler` → register it in `CardEffectResolver.CreateDefault()` → add the `CardDefinition` to the catalog. Registration is explicit; there is no reflection or attribute scanning. Duplicate or `None` kinds throw at resolver construction.

### Enemy profiles

`EnemyCombatProfileCatalog.Default` has 5 profiles: `gunslinger` (Normal/3), `cultist` (Normal/3), `trickster` (Normal/4), `enforcer` (Elite/5), `final-boss` (Boss/7).

**Two distinct key namespaces — do not conflate:** profile keys (`"gunslinger"`) vs behavior-policy keys (`EnemyBehaviorPolicyCatalog.Gunslinger == "gunslinger-public-inference"`, `"simple-16-stand-17"`, `"final-boss-three-phase"`). Profile constructors validate their policy key and every deck key against the catalogs, so a bad entry **fails at static-constructor time** — i.e. as a type-initializer exception in unrelated tests, not at the call site.

`profileKey` → `EnemyBattleConfigurationFactory.Create` → `StageBattleFactory.Create` → `CoreLoopBattle`. Grade also selects `ExpectedRewardTier` and the UI information mode.

### StageProgression — one run

`StageProgressionState`: `NotStarted, OpponentSelection, InBattle, RewardSelection, StageCleared, RunVictory, RunDefeat`.

- **`RunProgress` is the pure state machine** — no battle knowledge at all. It owns state, stage index, pending reward, and `PlayerRunState`.
- **`StageProgressionSession` is the orchestrator** — owns `RunProgress` + the current `CoreLoopSession`, with injectable battle factory, reward generator, reward-tier selector, and opponent-selection generator (all injectable for tests).
- **Reward resolution — not battle end — is what clears a stage.** Winning transitions to `RewardSelection`; only `TrySelectBattleReward`/`TrySkipBattleReward` reaches `StageCleared` or `RunVictory`. This includes the final boss: beating it does *not* win the run until its high-grade reward is resolved.
- `PlayerRunState` (soul + deck) persists across stages; `CoreLoopBattle` is per-battle and discarded. `SynchronizeFinishedBattle()` copies surviving soul back and guards re-entry by reference identity.
- Path invariant: only the last stage may be `FinalBossCombat`, and it must be.

### Two card types

`CardDefinition` is the shared **archetype** (catalog, keyed by string). `RunCardDefinition` is a **per-run instance**: `Id` + `DefinitionKey`. Both exist because a run deck holds multiple copies of the same archetype (the prototype deck is ranks 1–10 ×2).

The **run card id** is a stable identity threaded all the way through: `PlayerRunState` rejects duplicate ids and issues new ones monotonically; `StageBattleFactory` forwards it into `new BlackjackCard(card.Id, definition)`; so the `cardId` in `TryBeginPlayerCardUse(int cardId)` and in `PlayerCardViewModel` **is** the run card id. Rewards travel as `definitionKey` strings only — a `RunCardDefinition` is minted solely at `TrySelectBattleReward`.

### UI — the Presentation / View / Controller triple

Both `UI/CoreLoop/` and `UI/StageProgression/` use the same three-part shape:

| File | Type | Responsibility |
| --- | --- | --- |
| `*Presentation.cs` | **pure C#**, no MonoBehaviour | Immutable view-models + a static presenter. **All formatting happens here** — strings arrive pre-baked (`"STAGE 1 / 3"`, `"HIGH-GRADE REWARD"`). Unit-tested. |
| `*View.cs` | MonoBehaviour | Dumb renderer. `Render(model)` stores the model; raises `event Action<...>` upward. Never touches `RunProgress` or `CoreLoopBattle`. |
| `*Controller.cs` | MonoBehaviour, `[RequireComponent(typeof(*View))]` | Subscribes to view events, funnels every input through `ProcessInput(Func<bool>)` (locks input → calls session → re-presents). **The only type that talks to the session.** |

New display logic belongs in `*Presentation.cs` so it can be tested; the view should stay free of domain logic.

**The UI is IMGUI (`OnGUI` + `GUILayout`), not uGUI.** There are no prefabs, no Canvas, no serialized `UnityEngine.UI` references — styles are constructed in code and the scenes hold only bare GameObjects with the two scripts. A UI change is therefore a pure code edit with no `.unity`/`.prefab` churn and no risk of breaking serialized references. Views are responsive by hand: they branch on screen height for 720p vs 1080p, and both resolutions are part of the manual verification checklist.

### Scenes

Build settings: `StageTest` (0), `CoreLoopTest` (1), `SampleScene` (2). `LevelDesign*` scenes are art work-in-progress and are not in the build.

`StageProgressionRuntime` (`[DefaultExecutionOrder(-100)]`) is the cross-scene carrier: **static `Instance` + `DontDestroyOnLoad`**, self-destroying on duplicate. It builds the session once from a serialized seed and the hardcoded 3-stage prototype path (Ash Gate/gunslinger → Blood Hall/enforcer → Black Throne/final-boss). `StageTest` contains it; `CoreLoopTest` does **not** — the instance survives the load.

`CoreLoopController` is **dual-mode**: in `Awake` it probes `StageProgressionRuntime.Instance` for a live `InBattle` session and adopts it; otherwise it falls back to a standalone `CoreLoopSession` with its own seed. That is why `CoreLoopTest` is independently playable, and why every action forks on `IsStageBattle`.

### Determinism

`Border.Core.DeterministicRng` (xorshift32, GC-free, `Reseed(int)`) is the only RNG — do not introduce `System.Random` or `UnityEngine.Random` into rules code. `BlackjackDeck` takes a **mandatory** seed; seeds flow from `StageDefinition.PlayerDeckSeed`/`EnemyDeckSeed`.

Tests bypass RNG entirely with `BlackjackDeck.CreateInDrawOrder(cards)`, which yields the exact declared draw order. Reach for that rather than hunting for a seed that produces the hand you want.

## Conventions

- **Test naming: `{TaskId}_{U##}_{BehaviorDescription}`** — e.g. `RW02_U01_NormalVictoryBeginsRewardWithoutClearingStage`, `CL_U03_...`, `EUI04_U07_...`. The prefix ties the test to a task ID in `Docs/`, which is how the progress logs cite evidence. Match the prefix of whatever task you are implementing.
- NUnit constraint model (`Assert.That(x, Is.EqualTo(y))`), not classic asserts.
- `sealed` classes by default (67 of 116). Explicit types over `var` in rules code.
- Tests live in `Assets/06.Packages/Tests/EditMode/{CoreLoop,StageProgression}/`. Note that `Docs/project-structure-and-mcp-reference.md` §4.2 still says `Assets/Tests/EditMode/` — that path does not exist; the doc is stale.
- `AssemblyInfo.cs` grants `InternalsVisibleTo` to both test assemblies. Keep mechanism `internal` and drive it from tests rather than widening the public API for testability.
- Every new `.cs` needs its Unity-generated `.meta` committed alongside it.
- Asset serialization is ForceText, but there is no YAML merge driver configured — avoid concurrent scene/prefab edits.
- `DiaBlackJack.slnx` and `*.csproj` are Unity-regenerated. Reordering there is noise; exclude it from commits.

## Docs workflow

`Docs/` is not background reading — it is the working spec, and it is expected to be updated as part of a change.

`Docs/rule.md` is the **authoritative game-rules baseline**; `game-design-document.md` derives from it. If code and `rule.md` conflict, do not silently pick one — record the decision and its rationale.

Each feature carries a **4-document set** plus a work-ID series:

| Feature | ID series | Status |
| --- | --- | --- |
| core loop | `CL` / 1–4단계 | complete |
| stage progression | `SP-00`–`SP-04` | complete |
| combat actions (fold/change) | `BA-00`–`BA-05` | complete |
| card use | `CU-00`–`CU-06` | complete |
| battle rewards | `RW-00`–`RW-05` | complete |
| enemy combat profiles | `EP-00`–`EP-06` | complete |
| opponent selection + combat info UI | `EUI-00`–`EUI-05` | complete |
| **formal run flow (gold + shop)** | **`RF-00`–`RF-05`** | **RF-00 done, RF-01 next** |

The four documents per feature are `*-design.md` (scope + provisional decisions), `*-development-spec.md` (types, states, test plan), `*-implementation-plan.md` (ordered tasks + verification gate per task), `*-progress-log.md` (what was actually built and verified).

**Record only what was actually done.** The logs deliberately separate planned from completed work and cite concrete evidence (test counts, job ids, scene checks, Console-clean status). Do not mark something complete that was not verified.

## Current work and ownership

File ownership between team members is explicit and consistently respected — progress logs repeatedly record "no changes to other owners' files."

- **이천서** — docs, CoreLoop, StageProgression, enemy profiles/AI, opponent selection + combat UI. All complete through EUI-05.
- **Shim0Hwan** — art, shaders, `LevelDesign` scenes, lighting (`Assets/05. Arts`).
- **HONG** — gold, shop, and the formal `battle → shop → battle → shop → boss` run flow. **This is the active work**, and the git user here is HONG.

**RF-01 is the next task**: add gold to `PlayerRunState` (hold/grant/spend/reset), a `GoldRewardCatalog` with per-profile values (3/3/4/6/10), and grant gold exactly once after card-reward resolution — awarded on both select and skip, never on failure, duplicate, or defeat; 0 on restart. UI, shop, and scenes are explicitly out of scope for RF-01.

Read both before starting: `Docs/formal-run-flow-development-spec.md` (already specifies the intended type signatures, e.g. `GoldRewardCatalog.CreatePrototype()`, and the per-test expectations) and `Docs/formal-run-flow-implementation-plan.md` (task order and the verification gate for each step).

## Commits

From `README.md`, and as practiced in history:

```
(TAG) : 제목            e.g.  feat : 상대 선택 UI 연결
                              docs : 이벤트 문서 작성
                              art : 셰이더 수정, 텍스처 수정
```

Tags: `feat`, `fix`, `docs`, `art`, `merge`, `chore`. Branches: `(TAG)/(주요내용)/(ISSUE NUMBER)`, e.g. `feat/player/#99`. Commit subjects are written in Korean.
