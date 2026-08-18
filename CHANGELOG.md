# Changelog

All notable changes to K-Setting are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.3] - 2026-08-18

### Added

- K-Reference now folds its settings into **Tools > KTools Setting**.

### Changed

- Moved the `Tools/KTools Setting` menu item to priority 911, grouping it at
  the bottom of the Tools menu alongside the other Kingfisher tools' settings
  items.

## [1.0.2] - 2026-08-13

### Added

- Published as a `.unitypackage` on this tag's GitHub Release, for anyone who
  would rather not use git.

## [1.0.1] - 2026-08-13

### Fixed

- Fixed dragging a slider or color setting causing the whole editor to
  stutter and write the settings file to disk on nearly every frame of the
  drag, instead of only when needed.

## [1.0.0] - 2026-08-13

### Added

- **Tools > KTools Setting**, one window that every installed Kingfisher tool
  folds its settings into.
- Tools are discovered by reflection at load time, so there is nothing to wire
  up and no tool has to reference K-Setting. Add a tool, and its section
  appears in the window.
- Each tool describes its own section - headings, toggles, radio groups,
  sliders and color fields - so the window needs no per-tool code here.
- Per-tool controls to open the tool, disable it, reset its settings and delete
  its stored data, listing the files that would be removed.
- Shared settings store alongside the tools' own data in a `.KData` folder
  beside `Assets`, which gitignores itself on creation.
- Installs as a UPM package from its git URL, or as a plain folder under
  `Assets/`.
- Editor-only: the assembly is `Editor`-platform only, so nothing is compiled
  into player builds.
