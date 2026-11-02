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
| `game/addons/menu/Code/VR/VRModalHost.cs` | Hosts modals on the VR menu's world panel |
| `game/addons/menu/Code/VR/VRPointer.cs` | Controller pointer laser |

The `game/addons/menu/Code/VR/` folder also holds upstream files, such as
`VRSceneSwitcher.cs`. Only the `FORK-VR` header tells fork files apart.

### Upstream files touched

| File | Hook | Reason |
|---|---|---|
| `CLAUDE.md` | `@FORK-VR.md` | Load these conventions in every session |
| `engine/Sandbox.Engine/Scene/Components/UI/WorldInput.cs` | `Game.IsRunningInVR` block moved before the mouse block | The desktop cursor must not steal the ray |
| `game/addons/menu/Code/ModalSystem.cs` | `Push` calls `GetModalParent()`, plus the `GetModalParent` block | The flat overlay isn't drawn in the headset |
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
