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
| `engine/Sandbox.Engine/Systems/Input/VR/VRPanelPlacement.cs` | Pure placement math for VR panels, and `VRHeadFollow` (dead zone then smooth catch-up) |
| `engine/Tests/Sandbox.Test.Unit/VR/VRPanelPlacementTest.cs` | Unit tests for `VRPanelPlacement` |
| `engine/Sandbox.Engine/Systems/Input/VR/VRStick.cs` | Stick to scroll wheel, with dead zone and curve |
| `engine/Tests/Sandbox.Test.Unit/VR/VRStickTest.cs` | Unit tests for `VRStick` and `VRPreferences.CanPoint` |
| `engine/Sandbox.Engine/Platform/VR/VRPreferences.cs` | Saved `vr_ui_*` convars: panel distance and size, HUD follow, pointer hand, scroll speed |
| `engine/Sandbox.Menu/VR/VRDesktopLinks.cs` | Event raised when a link or folder opens on the desktop in VR |
| `game/addons/menu/Code/VR/VRMenuRig.cs` | Mirrors the VR menu scene's right-hand pointers onto the left hand at runtime; applies the pointer hand setting |
| `game/addons/menu/Code/VR/VRPackageFilter.cs` | Adds `+vr` to game queries in VR |
| `game/addons/menu/Code/VR/SettingsCatalog.VR.cs` | The VR settings page; hides the display page in VR |
| `engine/Sandbox.Menu/VR/VRMenuPlacement.cs` | Public `AnchorFor` for the menu addon |
| `game/addons/menu/Code/VR/VRFlatScene.cs` | Rigs the menu's flat scenes (avatar editor, crate opening) for VR at runtime; pointer ray and trigger click for their 3D interactions |
| `engine/Sandbox.Menu/VR/VRKeyboard.cs` | On-screen keyboard for the menu's text fields on world panels |
| `engine/Sandbox.Engine/Scene/Components/UI/ScreenPanel.VR.cs` | In VR, a screen panel is drawn on a head-following world panel; `ShowInVR` opts out |

The menu addon has no access to engine internals (such as `WorldPanelInput`), but `Sandbox.Menu`
does and the addon references it. Engine-side VR UI code for the menu therefore goes in
`engine/Sandbox.Menu/VR/`, with a public API the addon calls.

The `game/addons/menu/Code/VR/` folder also holds upstream files, such as
`VRSceneSwitcher.cs`. Only the `FORK-VR` header tells fork files apart.

### Upstream files touched

| File | Hook | Reason |
|---|---|---|
| `CLAUDE.md` | `@FORK-VR.md` | Load these conventions in every session |
| `engine/Sandbox.Engine/Scene/Components/UI/WorldInput.cs` | `Game.IsRunningInVR` block moved before the mouse block; grip right-clicks; stick through `VRStick` | The desktop cursor must not steal the ray; context menus and usable scrolling in VR |
| `engine/Sandbox.Engine/Scene/Components/UI/ScreenPanel.cs` | `partial`; one-line calls to `ScreenPanel.VR.cs` in `OnAwake`, `OnEnabled`, `OnDisabled`, `OnDestroy`, `GetPanel`, `OnUpdate` | Game HUDs reach the headset |
| `engine/Sandbox.Engine/Systems/UI/UISystem.cs` | `TickWorldInput` also collects screen panels drawn on world panels | World inputs can click game HUDs in VR |
| `game/addons/menu/Code/AvatarEditor/AvatarEditManager.Camera.cs` | `UpdateCamera` stands down in VR; the eyes follow `VRFlatScene.PointerRay` | The headset moves the camera |
| `game/addons/menu/Code/RewardOpening/RewardUnboxCamera.cs` | `OnUpdate` stands down in VR | Same |
| `game/addons/menu/Code/RewardOpening/RewardCard.cs`, `RewardUnboxCrate.cs` | Pointer ray and trigger click alongside the mouse | Pick cards and bump the crate with the controller |
| `engine/Sandbox.Menu/MenuUtility.cs` | `VRDesktopLinks.Opened( path )` in `OpenUrl` and `OpenFolder` | Tell the player a link opened where the headset doesn't show |
| `game/addons/menu/Code/MenuHelpers.cs` | `LaunchGame` VR shortcut removed | The game page shows on the VR menu panel now |
| `game/addons/menu/Code/Settings/SettingsCatalog.cs` | `partial`, `AddVR()` in the constructor | VR settings page |
| `game/addons/menu/Code/MenuUI/Components/PackageList.razor`, `New/HorizontalGameList.razor`, `Front/RecentlyPlayed.razor` | `VRPackageFilter.Apply` around the query | Only VR games listed in VR |
| `game/addons/menu/Code/MenuUI/Components/MenuPanel.razor`, `PackageHoverCard.razor`, `CardVideoRotation.cs`, `Modals/RewardsModal.razor` | Root panel rect instead of `Screen.Width/Height` | A world panel's pixels aren't the window's |
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
- [ ] Milestone C, main menu finished:
  - [ ] the left hand points in the VR menu too: its laser starts at the controller and isn't mirrored the wrong way (`VRMenuRig` mirrors the right hand's offsets);
  - [ ] the grip opens context menus (package cards, friends, chat), and the trigger still clicks;
  - [ ] the stick scrolls at a comfortable speed, in the right direction, with no drift at rest;
  - [ ] context menus, hover cards and the rewards popup stay on the VR panel instead of being clamped to the desktop window size;
  - [ ] clicking a game opens its page instead of launching it straight away;
  - [ ] the hub, group pages, search, recently played and game pickers only list VR games;
  - [ ] Settings shows a VR page and no Display page; each VR setting takes effect and survives a restart (`vr_ui_*` convars);
  - [ ] the pointer hand setting turns the menu's lasers on and off per hand, and picks the in-game hand;
  - [ ] opening a link, a Steam page or a folder shows the "Opened on your desktop" toast;
  - [ ] flat screen, without `-vr`: no VR page, Display page there, lists unfiltered, game cards open as before.

- [ ] Milestone D, typing and game HUDs:
  - [ ] focusing a text field on the VR menu panel or an in-game panel opens the keyboard, above the field when it's low on the panel;
  - [ ] keys type into the field without taking its focus; shift is one-shot; backspace, space and enter work; enter submits search and chat;
  - [ ] the close key and clicking elsewhere close the keyboard;
  - [ ] the server password, report and review fields can be filled in;
  - [ ] a game whose HUD is a `ScreenPanel` shows it ahead of the player, following the head lazily, readable and behind the menu's HUD;
  - [ ] a game `WorldInput` can click buttons on that HUD;
  - [ ] a `ScreenPanel` with `ShowInVR` off stays on the desktop window only;
  - [ ] the menu's own screen panel in `menu-main.scene` doesn't flash in the headset before the VR menu scene loads;
  - [ ] flat screen, without `-vr`: no keyboard, screen panels unchanged.

- [ ] Milestone E, flat menu scenes and comfort:
  - [ ] the avatar editor opens in the headset, standing where its camera was, facing the avatar; its UI floats ahead and the lasers click it;
  - [ ] the avatar's eyes follow the right hand's laser; walking around the avatar works in place of the mouse orbit;
  - [ ] crate opening: the right laser and trigger bump the crate and pick the reward cards; the camera no longer drifts on its own;
  - [ ] leaving either scene back to the VR menu works;
  - [ ] a short press of the menu button pauses; holding it about a second recenters the in-game panels without pausing;
  - [ ] flat screen, without `-vr`: avatar editor and crate opening unchanged.

## Open work

- **Avatar editor in VR** has no orbit or zoom and no hand models; turning the avatar with the stick would help.

- **Key binding capture in VR.** `KeyBind.razor` captures keyboard and gamepad buttons only; VR controller
  buttons can't be bound from the settings page.
- **The game's own text fields.** The keyboard follows the menu's focus only, not text fields in a game's UI.

- **Tooltips on world panels.** `TooltipSystem` is fed by the screen `InputRouter` and only shows while the desktop
  cursor is visible. Feeding it from `WorldPanelInput` needs a block in `UISystem.TickInput` that competes with
  that routing; left until it can be tried in a headset.
- **Back to the menu without the flat hop.** Scenes that return to `menu-main.scene` go through it before
  `VRSceneSwitcher` loads `menu-vr.scene`.
- **VR styling.** No `.is-vr` rules yet (bigger targets and text); better tuned with a headset on.
- **Party deck** isn't relayed to the VR panels.
- **Game input while a VR menu is open.** Games reading `Input.VR` directly still get the trigger.
