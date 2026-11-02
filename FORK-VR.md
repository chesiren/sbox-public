<!-- FORK-VR: file added by the fork, absent from upstream -->
# VR fork: conventions and registry

The `vr-full` branch carries a complete VR UI for s&box. It is never merged into upstream
(Facepunch) and is rebased often onto `master`. The goal is to touch upstream files as little as
possible so rebases stay conflict-free.

Everything in this fork is written in English: code, comments, docs, commit messages.

## Rules

1. **New code goes in new files.** VR logic lives in files the fork creates:
   `game/addons/menu/Code/VR/` and `engine/Sandbox.Engine/Scene/Components/VR/`.
2. **An upstream file only gets a hook.** One to three lines that delegate to fork code, never a
   block of logic. In order of preference:
   - a component added to the scene, or a subclass;
   - an existing event or hook;
   - a `partial class` in a new file;
   - as a last resort, an inline edit.
3. **Scenes.** Prefer new VR scenes or prefabs. Don't resave an upstream scene unless needed:
   their JSON makes large conflicts.
4. **Styles.** `.is-vr` rules go in new imported SCSS files.
5. **No reformatting or renaming** in upstream files.

## Marking

- **Added file:** first line `// FORK-VR: file added by the fork, absent from upstream`.
  In `.razor` use `@* FORK-VR: ... *@`, in `.md` use `<!-- FORK-VR: ... -->`. VR code goes in a
  `VR/` folder or in a file whose name starts with `VR`.
- **Changed line in an upstream file:** `// FORK-VR: <why>` at the end of the line or just above.
- **Changed block in an upstream file:** wrap it in `// FORK-VR begin` and `// FORK-VR end`.
- **Commits:** prefix the message with `vr:`.
- **Finding everything:**
  - `git grep -n "FORK-VR"`
  - `git diff --name-status master...HEAD`
  - `git log --grep "^vr:"`

Every commit that adds or removes a hook updates the registry below.

## Rebasing onto upstream

- Rebase onto `master` at least once a week and before each milestone. Enable `rerere` with
  `git config rerere.enabled true`.
- After each rebase:
  - run build, tests and format;
  - check that `git grep "FORK-VR"` still finds every hook in the registry.

## Registry

### Added files

| File | Purpose |
|---|---|
| `FORK-VR.md` | This document |
| `game/addons/menu/Code/VR/VRModalHost.cs` | Holds modals on a VR world panel, with the overlay's stylesheet |
| `game/addons/menu/Code/VR/VRPointer.cs` | Controller pointer laser in the VR menu scene |
| `game/addons/menu/Code/VR/VRUIHost.cs` | Picks where modals, the pause menu and the overlay's content go in VR: menu world panel, in-game panel or in-game HUD |
| `game/addons/menu/Code/VR/VROverlayRelay.cs` | Moves toasts, chat, subtitles, mic, loading screen and stray modals off the flat overlay in VR, and remakes them when their host goes |
| `engine/Sandbox.Menu/VR/VRGamePanelHost.cs` | World panels in the game's scene for the menu's UI in VR: a world-locked pause menu and a head-following HUD, with pointer, laser, cursor and haptics; menu button edge |
| `engine/Sandbox.Engine/Systems/Input/VR/VRController.Menu.cs` | `VRController.ButtonMenu` |
| `engine/Sandbox.Engine/Systems/Input/VR/VRPanelPlacement.cs` | Pure placement math for VR panels |
| `engine/Tests/Sandbox.Test.Unit/VR/VRPanelPlacementTest.cs` | Unit tests for `VRPanelPlacement` |

The menu addon has no access to engine internals (such as `WorldPanelInput`), but `Sandbox.Menu`
does and the addon references it. Engine-side VR UI code for the menu therefore goes in
`engine/Sandbox.Menu/VR/`, with a public API the addon calls.

The `game/addons/menu/Code/VR/` folder also holds upstream files, such as
`VRSceneSwitcher.cs`. Only the `FORK-VR` header tells fork files apart.

### Upstream files touched

| File | Hook | Reason |
|---|---|---|
| `CLAUDE.md` | `@FORK-VR.md` | Load these conventions in every session |
| `engine/Sandbox.Engine/Scene/Components/UI/WorldInput.cs` | `Game.IsRunningInVR` block moved before the mouse block | The desktop cursor must not steal the ray |
| `game/addons/menu/Code/ModalSystem.cs` | One-line `GetModalParent()` delegating to `VRUIHost`; `Push` and `PauseMenu` call it | The flat overlay isn't drawn in the headset |
| `game/addons/menu/Code/Overlay/MenuOverlay.cs` | `VROverlayRelay.Tick( this )` at the end of `Tick` | Move the overlay's content where the headset sees it |
| `engine/Sandbox.Engine/Platform/VR/VRSystem.Input.cs` | `menu` action string and `ButtonMenu` enum value | Read the menu button declared in the fpxr manifests |
| `engine/Sandbox.Engine/Systems/Input/VR/VRController.cs` | `ButtonMenu` updated in `Update()` | Same |
| `engine/Sandbox.Menu/MenuDll.cs` | `VRGamePanelHost.Tick()` in `Tick`; menu button ORed with Escape in `LateTick` | Drive the in-game VR panel; pause from the controller |
| `game/addons/menu/Code/MenuUI/Home.razor` | `VisibleTabs`, `GetQuery` (`+vr`) | Home feeds only list VR games |
| `game/addons/menu/Code/MenuUI/Components/Multiplayer/MultiplayerServers.razor` | `IsVrGame` filter | Only servers of VR games are listed |
| `game/addons/menu/Assets/scenes/menu-main.scene` | "VR Scene Switcher" object | Loads `menu-vr.scene` in VR |
| `game/addons/menu/Assets/scenes/menu-vr.scene` | Pointers, `VRWorldInput` removed | VR rig of the menu |

Scene JSON can't hold comments, so the two upstream scenes are only tracked in this registry.

## Headset tests to run

The code is written without a headset and without running s&box. Only compiling, unit tests and
formatting are checked. The items below still need testing in a headset (`sbox.exe -vr`).

- [ ] Main menu (`fix-vr-menu`):
  - [ ] modals open on the VR panel;
  - [ ] the laser stops on the panel;
  - [ ] the desktop mouse doesn't move the ray;
  - [ ] only VR games and servers are listed.
- [ ] Milestone A, pause menu in game:
  - [ ] the menu button (left Touch controller) opens and closes the pause menu; no `Facepunch.XR` warning spam in the console from the new `menu` action;
  - [ ] on Index controllers, find which button the `menu` action reaches (`valve_knuckles.json` maps the left hand to `/input/menu/click`, which Index may not have);
  - [ ] the pause menu opens about 1.5 m ahead at eye level, upright, facing the player, and stays put when the head moves;
  - [ ] it's readable: tune `VRGamePanelHost.Distance`, `Width` and `Drop` if not;
  - [ ] the laser and cursor follow the hand that last pulled its trigger, trigger clicks, grip right-clicks, the stick scrolls;
  - [ ] hovering gives a light haptic tick;
  - [ ] modals opened from the pause menu (settings, friends, servers) show on the same panel;
  - [ ] in a game without a `VRAnchor`, the laser still starts at the controller;
  - [ ] changing scene with the pause menu open, and quitting to the menu, leave no stray panel or crash;
  - [ ] the trigger also reaches the game while the menu is open (known: games reading `Input.VR` directly aren't blocked);
  - [ ] flat screen, without `-vr`: Escape, pause menu and modals unchanged.
- [ ] Milestone B, feedback visible in VR:
  - [ ] in the VR menu, toasts (`MenuOverlay.Show`), questions and party invites show on the menu panel and their buttons can be clicked;
  - [ ] a question is answered with the laser before its 10 s timeout;
  - [ ] in game, toasts, chat, subtitles and the mic meter float on the HUD, which stays still for small head moves and catches up past about 25 degrees or 12 units;
  - [ ] the HUD is readable and comfortable: tune `HudDistance`, `HudWidth`, `HudDrop` and the follow settings if not;
  - [ ] the HUD isn't hidden by nearby walls (world panels are drawn with the scene);
  - [ ] the laser shows over the HUD only while something on it can be clicked;
  - [ ] a modal opened right at startup (`-rungame`, invite) or while switching to the VR menu scene ends up on the VR panel;
  - [ ] loading a game from the VR menu: the loading screen shows on the menu panel, then on the HUD once the game's camera exists; note what the headset shows before that;
  - [ ] the avatar editor round trip and quitting a game don't lose toasts (they're remade on the overlay and moved again);
  - [ ] the party deck stays where it is (not relayed yet);
  - [ ] flat screen, without `-vr`: toasts, chat and loading screen unchanged.
