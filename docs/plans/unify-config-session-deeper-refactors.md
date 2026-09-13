---
name: Unify config-session deeper refactors
overview: Ranked follow-ups after unifying prefs into soft-loaded config.json (flat root sections). Prefer high cost-to-value first. Done/obsolete items listed so they are not re-opened.
---

# Unify config / session — deeper refactors

Handover from the unify-config-session phase machine and later cleanup (flat root session sections, omit-empty save, drop `session.version`, `PersistedConfigurationReset.Reset`).

Canonical product plan: [unify-config-session.plan.md](unify-config-session.plan.md) (phases complete). Current on-disk shape: root `log` / `ui` / `mainWindow` / `fileList` / `renameList` / `filterEditor` / `filterDefaults` (no nested `session` wrapper; empty sections omitted on write).

______________________________________________________________________

## Already done / obsolete (do not re-open)

| Item                                                                                               | Status                                                                                                                  |
| -------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| Merge `session.json` + `filter-defaults.json` into `config.json`                                   | **Done** — soft-load prefs; presets stay hard-fail                                                                      |
| Delete `SessionStore`                                                                              | **Done**                                                                                                                |
| `FilterDefaultsStore` file path / separate JSON                                                    | **Done** — memory cache + `ConfigStore.FilterDefaultsJson`                                                              |
| Nested `"session": { … }` document wrapper                                                         | **Superseded** — flat root sections (`MainWindowPrefs` etc.)                                                            |
| `SessionState.Version` schema marker                                                               | **Done** — removed (never a migrate/reject gate)                                                                        |
| Write `"mainWindow": null` / empty `session: {}` / empty `filterDefaults: {}` on EnsureDefaultFile | **Done** — omit nulls; omit empty sections and empty `filterDefaults`                                                   |
| MainWindow `ConfigFilePath` / `SaveOnClose` path plumbing                                          | **Done** — `ConfigStore` owns active path                                                                               |
| `DeleteAppDataFiles` naming                                                                        | **Done** — `PersistedConfigurationReset.Reset`                                                                          |
| Full in-memory clear on Reset / collapse `ClearSessionAndFilterDefaults`                           | **Superseded** — Reset is delete-only; UI always exits after delete (`SuppressSessionSaveOnClose`), restart best-effort |
| Shared case-insensitive JSON property get                                                          | **Done** — `JsonObjectProperties` in Utils; ConfigStore / ConfigJsonApplier / ConfigValueReader                         |
| Rename `FilterDefaultsStore.OpenDefault`                                                           | **Done** — `FromConfigStore` (vs file-backed `PresetManager.OpenDefault`)                                               |
| Collapse ConfigStore\* / FilterDefaults test fixtures                                              | **Done** — `ConfigStoreTempFile`; Prefs Delete dupes removed (kept `ConfigStoreDeleteDefaultFileTests`)                 |
| Split `ConfigStore` into separate prefs I/O type                                                   | **Rejected** — tried `ConfigPrefsDocument`; reverted; keep I/O on `ConfigStore`                                         |

______________________________________________________________________

## Still worth doing (best cost-to-value first)

_(none — opportunistic items above are done)_

______________________________________________________________________

## Low / skip unless already touching that code

| #   | Item                                                                                                                       | Why skip / defer          |
| --- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------- |
| 1   | Rewrite historical plan prose elsewhere (`whole-codebase-review`, `applied-filter-editors`) for old `session.json` wording | Archive; leave as history |

______________________________________________________________________

## Keep / do not “simplify”

- Soft prefs (`ConfigStore`) vs hard presets (`PresetManager`) and hard CLI `--set` / missing `--config`.
- Opaque `JsonObject FilterDefaultsJson` in Models + typed cache in Engine (do not deserialize `BaseFilter` in Models for prefs).
- Reset: delete `config.json` only → suppress session close-save → try restart → **always exit** (even if spawn fails). Do not clear in-memory prefs; next process soft-loads defaults. Do not drop suppress without fixing VM/store identity on a surviving process.
- Do **not** reintroduce nested `session` wrapper, `SessionStore`, or readers for legacy `session.json` / `filter-defaults.json`.
- Do **not** split `ConfigStore` into a separate prefs document I/O type.
