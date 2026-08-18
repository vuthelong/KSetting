# K-Setting

The shared settings backend for the Kingfisher K-Tools. It adds
**Tools > KTools Setting**, one window that every installed Kingfisher tool
folds its settings into.

K-Setting finds the installed tools by reflection at load time, so there is
nothing to wire up. Add a tool, and its section appears in the window.

It is optional. Without it, each tool falls back to its own settings window at
**Tools > Kingfisher > \<Tool\> > Settings**. Install K-Setting when you have
more than one tool and would rather manage them in one place.

Everything is editor-only - the assembly is `Editor`-platform only, so nothing
here is compiled into player builds.

## Install

K-Setting is its own repository, and that repository *is* the package - the
`.cs` files, the `.asmdef` and the `.meta` files sit at its root, so the folder
drops straight into a project and stays editable.

Clone it into `Assets/ThirdParty/KingfisherTools/`, beside the tools it serves:

```
cd Assets/ThirdParty/KingfisherTools
git clone https://github.com/vuthelong/KSetting.git
```

Or add it as a submodule, which is what the
[kTool](https://github.com/vuthelong/kTool) development project does:

```
git submodule add https://github.com/vuthelong/KSetting.git Assets/ThirdParty/KingfisherTools/KSetting
```

Check out a tag to pin a version:

```
git -C Assets/ThirdParty/KingfisherTools/KSetting checkout 1.0.4
```

Alternatively, download the `.unitypackage` from the
[latest release](https://github.com/vuthelong/KSetting/releases/latest) and
import it via **Assets > Import Package > Custom Package**. This is a
point-in-time snapshot, not a tracked install - re-download it to update.

Keep one copy of the folder per project - Unity rejects a second with
`Assembly with name 'Kingfisher.KSetting' already exists`.

## The tools

- [K-Folders](https://github.com/vuthelong/KFolders) - color-coded folder icons and outlines for the Project window
- [K-Hierarchy](https://github.com/vuthelong/KHierarchy) - navigation, component icons and readability tweaks for the Hierarchy
- [K-Inspector](https://github.com/vuthelong/KInspector) - navigation, component tooling and extra drawers for the Inspector
- [K-Favorites](https://github.com/vuthelong/KFavorites) - a bookmark panel for assets and scene objects
- [K-Tabs](https://github.com/vuthelong/KTabs) - browser-style tabs for editor windows
- [K-Emoji](https://github.com/vuthelong/KEmoji) - TextMeshPro sprite asset generation from a sprite atlas

## Where your data lives

Settings are written to a `.KData` folder at the root of your project, next to
`Assets/` and `Packages/` - not into the tool's folder, so it survives updating
or re-cloning the repository.

The folder carries a `.gitignore` of its own that excludes everything inside it,
so it stays out of version control without your project's `.gitignore` needing
an entry. Delete that file to commit the folder instead.

## License

Proprietary - see [LICENSE.md](LICENSE.md). Licensed per purchase (Unity Asset
Store or a direct agreement with Kingfisher); it is not open source.
