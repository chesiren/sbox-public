<!-- FORK-VR: file added by the fork, absent from upstream -->
# VR fork: conventions and registry

The `vr-full` branch carries a complete VR UI for s&box. It is never merged into upstream
(Facepunch) and is rebased often onto `master`. The goal is to touch upstream files as little as
possible so rebases stay conflict-free.

Everything in this fork is written in English: code, comments, docs, commit messages.

## Rules

1. **New code goes in new files.** VR logic lives in files the fork creates:
   `game/addons/menu/Code/VR/`, `engine/Sandbox.Menu/VR/` and `engine/Sandbox.Engine/Scene/Components/VR/`.
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
- **Commits:** written the way Facepunch writes theirs, so the fork's history reads like upstream's:
  - the title starts with `VR: `, so the fork's commits are easy to find, then a short imperative
    sentence, capitalised, with no full stop ("VR: Open the pause menu on a world panel in VR");
  - the body is empty, or a list of `* ` bullets separated by blank lines;
  - no test notes in the message: what's left to test lives in the checklist below.
- **Finding everything:**
  - `git grep -n "FORK-VR"`
  - `git diff --name-status master...HEAD`
  - `git log --grep "^VR: "`

Every commit that adds or removes a hook updates the registry below.

## Rebasing onto upstream

- Rebase onto `master` at least once a week and before each milestone. Enable `rerere` with
  `git config rerere.enabled true`.
- After each rebase:
  - run build, tests and format;
  - check that `git grep "FORK-VR"` still finds every hook in the registry;
  - the menu addon (`game/addons/menu`) is compiled by s&box at startup, not by the build: check
    by hand that its VR files still match upstream's API.

## Registry

### Added files

| File | Purpose |
|---|---|
| `FORK-VR.md` | This document |
| `engine/Sandbox.Engine/Systems/UI/RootPanel.VR.cs` | Projects a flat root panel into a scene world, like a world panel: bounds, scale, transform, ray hits |
| `engine/Sandbox.Menu/VR/VRMenuHost.cs` | Projects the menu's roots on one plane in VR: world-locked while clickable, head-following HUD otherwise; pointer, laser, cursor, haptics; menu button short press, and hold to recenter |
| `engine/Sandbox.Menu/VR/VRMenuScene.cs` | Bare scene with a tracked stereo camera for the headset while no game has a scene |
| `engine/Sandbox.Engine/Systems/Input/VR/VRController.Menu.cs` | `VRController.ButtonMenu` |
| `engine/Sandbox.Engine/Systems/Input/VR/VRPanelPlacement.cs` | Pure placement math for VR panels, and `VRHeadFollow` (dead zone then smooth catch-up) |
| `engine/Tests/Sandbox.Test.Unit/VR/VRPanelPlacementTest.cs` | Unit tests for `VRPanelPlacement` |
| `engine/Sandbox.Engine/Systems/Input/VR/VRStick.cs` | Stick to scroll wheel, with dead zone and curve |
| `engine/Tests/Sandbox.Test.Unit/VR/VRStickTest.cs` | Unit tests for `VRStick` and `VRPreferences.CanPoint` |
| `engine/Sandbox.Engine/Platform/VR/VRPreferences.cs` | Saved `vr_ui_*` convars: menu and HUD distance and size, HUD follow, pointer hand, scroll speed |
| `engine/Sandbox.Menu/VR/VRDesktopLinks.cs` | Event raised when a link or folder opens on the desktop in VR |
| `game/addons/menu/Code/VR/SettingsCatalog.VR.cs` | The VR settings page; hides the display page in VR |
| `engine/Sandbox.Menu/VR/VRKeyboard.cs` | On-screen keyboard for the menu's text fields on projected roots |
| `engine/Sandbox.Engine/Scene/Components/UI/ScreenPanel.VR.cs` | In VR, a screen panel is drawn on a head-following world panel; `ShowInVR` opts out |
| `engine/Sandbox.Engine/Platform/VR/VRGameFilter.cs` | In VR, keeps only Workshop games whose published `ControlModes` support VR |
| `engine/Tests/Sandbox.Test.Unit/VR/VRGameFilterTest.cs` | Unit tests for `VRGameFilter` |

The menu addon has no access to engine internals (such as `WorldPanelInput`), but `Sandbox.Menu`
does and the addon references it. Engine-side VR UI code for the menu therefore goes in
`engine/Sandbox.Menu/VR/`, with a public API the addon calls.

Since upstream's platform menu (November 2026) the menu has no scene of its own: it is only flat
root panels (`PlatformRoot`, `MenuOverlay`, `LoadingRoot`, ...). The fork doesn't touch them.
`VRMenuHost` projects every root of the menu's UI system into the game's scene, or into
`VRMenuScene` when there's none.

### Upstream files touched

| File | Hook | Reason |
|---|---|---|
| `CLAUDE.md` | `@FORK-VR.md` | Load these conventions in every session |
| `engine/Sandbox.Engine/Scene/Components/UI/WorldInput.cs` | `Game.IsRunningInVR` block moved before the mouse block; grip right-clicks; stick through `VRStick` | The desktop cursor must not steal the ray; context menus and usable scrolling in VR |
| `engine/Sandbox.Engine/Scene/Components/UI/ScreenPanel.cs` | `partial`; one-line calls to `ScreenPanel.VR.cs` in `OnAwake`, `OnEnabled`, `OnDisabled`, `OnDestroy`, `GetPanel`, `OnUpdate` | Game HUDs reach the headset |
| `engine/Sandbox.Engine/Systems/UI/UISystem.cs` | `TickWorldInput` also collects screen panels drawn on world panels | World inputs can click game HUDs in VR |
| `engine/Sandbox.Engine/Systems/UI/RootPanel.cs` | One-line calls to `RootPanel.VR.cs` in `UpdateBounds` and `UpdateScale` | A projected root keeps its own size instead of the window's |
| `engine/Sandbox.Engine/Systems/UI/RootPanel.Render.cs` | `ProjectedViewport()` in place of `null` in `BuildCommandList` | A projected root is painted in world space |
| `engine/Sandbox.Menu/MenuUtility.Platform.cs` | `VRGameFilter.Keep` in `DetailsOfGamesAsync` | Only VR games in the menu's game lists in VR |
| `engine/Sandbox.Menu/WorkshopGameSearch.cs` | `NextAsync` fetches up to 10 pages until one has a game left after the filter | A page filtered empty would stop a list that loads more as it's scrolled |
| `engine/Sandbox.Menu/MenuUtility.cs` | `VRDesktopLinks.Opened( path )` in `OpenUrl` and `OpenFolder` | Tell the player a link opened where the headset doesn't show |
| `engine/Sandbox.Menu/MenuDll.cs` | `VRMenuHost.Tick()` in `Tick`; menu button ORed with Escape in `LateTick`; `VRMenuScene.Render` in `OnRender` | Drive the VR menu; pause from the controller; draw the headset's scene when no game has one |
| `game/addons/menu/Code/MenuHelpers.cs` | `LaunchGame` VR shortcut removed | The game page shows in VR like on the desktop |
| `game/addons/menu/Code/Settings/SettingsCatalog.cs` | `partial`, `AddVR()` in the constructor | VR settings page |
| `game/addons/menu/Code/MenuUI/Components/MenuPanel.razor` | Root panel rect instead of `Screen.Width/Height` | A projected root's pixels aren't the window's |
| `engine/Sandbox.Engine/Platform/VR/VRSystem.Input.cs` | `menu` action string and `ButtonMenu` enum value | Read the menu button declared in the fpxr manifests |
| `engine/Sandbox.Engine/Systems/Input/VR/VRController.cs` | `ButtonMenu` updated in `Update()` | Same |

## Headset tests to run

The code is written without a headset and without running s&box. Only compiling, unit tests and
formatting are checked. The menu addon is compiled by s&box at startup, not by the build, so watch the
console for its errors first. The items below still need testing in a headset (`sbox.exe -vr`).

- [ ] Platform menu in VR (after the rebase onto upstream's platform menu):
  - [ ] the menu addon compiles at startup, with no error from `SettingsCatalog.VR.cs`;
  - [ ] on a game without a menu scene, the headset shows the bare VR scene (dark background) with the menu floating ahead, upright, at eye level;
  - [ ] the menu is readable and sharp: tune `vr_ui_distance`, `vr_ui_width` and `VRMenuHost.Drop` if not;
  - [ ] the menu, the overlay's toasts and the loading screen stack in the right order, and an empty root in front doesn't block clicks on the dock or a shelf;
  - [ ] the laser and cursor follow the hand that last pulled its trigger, trigger clicks, grip right-clicks, the stick scrolls; hovering gives a light haptic tick;
  - [ ] the dock, shelves, friends and rewards panels, settings and modals (review, report, password) can be clicked;
  - [ ] context menus and popups open where they should and stay on the panel;
  - [ ] the avatar shelf and the reward unbox, both scene panels inside the menu, show and react to the laser and trigger;
  - [ ] on a game with its own menu scene, the headset shows that scene; the dock floats ahead and the game's own screen panel UI follows the head behind it; both can be clicked;
  - [ ] with nothing to click, toasts and chat float as a HUD that stays still for small head moves and catches up past about 25 degrees or 12 units;
  - [ ] when a toast with buttons or a question shows, the plane comes in front and its buttons can be clicked before its timeout;
  - [ ] playing a game: the loading screen shows ahead, then the game's camera takes over with no frame where both scenes draw, and no bare scene left behind;
  - [ ] the menu button (left Touch controller) pauses and resumes; on Index, find which button the `menu` action reaches; no `Facepunch.XR` warning spam;
  - [ ] holding the menu button about a second recenters without pausing; the Recenter setting does the same;
  - [ ] leaving a game back to the menu and switching games leave no stray panel, laser or scene;
  - [ ] note what the desktop window shows in VR (the menu's roots are no longer drawn there);
  - [ ] the dev console (`DevLayer`) is projected too: note whether it's usable;
  - [ ] `PlatformRoot.WantsMouse` reads the desktop mouse over a game's own menu: check the game still gets the trigger while the dock isn't pointed at;
  - [ ] flat screen, without `-vr`: menu, overlay, loading screen and modals unchanged.
- [ ] Only VR games in VR:
  - [ ] browse, search, featured, "Your games" and favourites list only games that support VR;
  - [ ] scrolling a search keeps loading more, and a search with no VR game ends without hanging;
  - [ ] map pickers and the maps page are unfiltered;
  - [ ] flat screen, without `-vr`: lists unchanged.
- [ ] Settings and links:
  - [ ] Settings shows a VR page and no Display page; each VR setting takes effect and survives a restart (`vr_ui_*` convars);
  - [ ] the pointer hand setting picks which hands point;
  - [ ] opening a link, a Steam page or a folder shows the "Opened on your desktop" toast;
  - [ ] flat screen, without `-vr`: no VR page, Display page there.
- [ ] Typing and game HUDs:
  - [ ] focusing a text field on the menu opens the keyboard, above the field when it's low on the panel;
  - [ ] keys type into the field without taking its focus; shift is one-shot; backspace, space and enter work; enter submits search and chat;
  - [ ] the close key and clicking elsewhere close the keyboard;
  - [ ] a game whose HUD is a `ScreenPanel` shows it ahead of the player, following the head lazily, readable;
  - [ ] a game `WorldInput` can click buttons on that HUD;
  - [ ] a `ScreenPanel` with `ShowInVR` off stays on the desktop window only;
  - [ ] flat screen, without `-vr`: no keyboard, screen panels unchanged.

## Open work

- **Only VR games in the server lists.** `MultiplayerShelf` and `ServersPage` only know a lobby's game
  ident, so filtering them needs each game's package or Workshop item fetched.
- **Starting a flat game in VR.** Nothing stops it, and the game the menu is on stays shown even if
  it isn't a VR game.
- **A nicer VR scene.** `VRMenuScene` is a dark void with a light; a floor, a skybox and hand models
  would help, ideally from a prefab rather than code.
- **Key binding capture in VR.** `KeyBind.razor` captures keyboard and gamepad buttons only; VR controller
  buttons can't be bound from the settings page.
- **The game's own text fields.** The keyboard follows the menu's focus only, not text fields in a game's UI.
- **Tooltips on projected roots.** `TooltipSystem` is fed by the screen `InputRouter` and only shows while
  the desktop cursor is visible.
- **VR styling.** No `.is-vr` rules yet (bigger targets and text); better tuned with a headset on.
- **Game input while a VR menu is open.** Games reading `Input.VR` directly still get the trigger.
