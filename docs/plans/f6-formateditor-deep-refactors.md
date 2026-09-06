## name: F6 FormatEditor review deeper refactors
overview: Ranked follow-ups from MFR skill reviews of F6 PRs A/B/C (engine, FormatEditor, param editors) that were proposed but not applied in review autofixes. Prefer high cost-to-value first.

# F6 FormatEditor — deeper refactors

Synthesized from per-PR findings reviews of:


| PR  | Area                                                           |
| --- | -------------------------------------------------------------- |
| A   | `FormatTokenCatalog` + `FormatStringSyntax.TryValidate`        |
| B   | Shared `FormatEditor` + Formatter filter wiring                |
| C   | Param dialogs / registry / token editor VMs + Edit under caret |


High-confidence autofixes landed in those review passes (see below). This doc lists **not done** work only.

## Already done in reviews (do not re-open)



### PR A

- Closed `TryValidate` / `Compile` mismatch (balanced unknown spans fail like Compile; LooksLike only for unclosed `<`)
- Frozen catalog list (`[.. catalog]`)
- Stronger catalog/syntax tests (`InsertText` validates + compiles; balanced-unknown fail theory)



### PR B

- Insert on list **tap**, not `SelectionChanged` (keyboard highlight no longer inserts)
- Double-tap token select: Bubble routing; do not reset `CaretIndex` after selection
- Suppress Text DP → `TemplateBox` push so Validate does not double-fire
- Single `MaxInlineErrorLength`; details dialog when inline ≠ full message
- Headless facts: tap insert, double-tap select, long-error truncate, search clear on insert



### PR C

- Layering: `CreateBody` moved out of ViewModels → `FormatTokenEditorBodyFactory` in Views
- Edit path: dropped redundant `HasEditor` before `TryCreate`; replace uses `ResultingFormatString`
- Removed unused `TimestampField` from `FileDate` `KindChoice`
- Replaced nested ternary in `FileSize` with `_ResolveUnit`



### Polish (post-review)

- Shared `FormatTokenChoice(string Value, string Label)` under FormatEditor VMs; Counter / FileDate / FileSize nested choice records deleted



## Defer past F6 (feature / host work)


| Item                                                      | Why defer                                      |
| --------------------------------------------------------- | ---------------------------------------------- |
| Nested `FormatEditor` in substr/token `source=` fields    | High engine+UI+gesture cost; not F6 exit       |
| Nested token spans / caret hit-test in `TryValidate`      | Needed mainly for nested Edit; ride with above |
| Inserter / Audio “likely-token” validation policy flag    | No second FormatEditor host yet                |
| Shared OK-only message dialog across app hosts            | Wait for PathMover / Audio / second consumer   |
| Replace `[FormatTokenInfo]` reflection with hand registry | Explicit non-goal (open catalog is correct)    |




## Ranked follow-ups (best cost-to-value first)



### 1. Share named-arg parse with Filters — **high**

- **Sites:** `NamedFormatOptionsBuilder` (`TryParse` / `_SplitNamedArgumentSegments`) ↔ `FormatOptionsParsing.SplitNamedArgumentSegments` / `ParseNamedKeyValuePairs` (identical depth-aware comma split + key=value rules; throw vs bool)
- **Target:** one public owner in `Mfr.Filters` (or `Mfr.Utils`); UI wraps throw→bool (and keeps Join / FormatInt / FormatBool / soft Get helpers)
- **Value:** one depth-aware split; closes UI↔engine drift on nested `<…>` in values
- **Cost:** public API surface + Filters tests; medium churn; keep soft dialog defaults separate from Compile throws
- **Rank:** high — first structural dedup after F6 merge



### 2. One template-walk owner for Compile + TryValidate — **done**

- **Done:** `FormatStringScan.TryWalk` yields ordered literal/token pieces (+ unclosed-likely error); `Compile` / `TryValidate` consume the same walk; shared `UnknownTokenMessage`
- **Left as-is:** `ContainsLikelyFormatTokens` keeps its own trim-aware heuristic walk (see item 6 for name-trim product call)



### 3. Grouped insert picker when search empty — **high** (plan gap)

- **Sites:** `FormatEditor.axaml` flat `ListBox`; `FormatEditorViewModel` filter; plan Phase 2 / PR B step 2
- **Target:** nest by `GroupPath` when query empty; flat list when filtering
- **Value:** MFR7 browse UX; closes documented F6 plan gap
- **Cost:** medium AXAML/VM/headless churn
- **Rank:** high — UX polish; not blocking F6 exit but best product leftover from B



### 4. Flyout UX: focus search + Enter-to-insert — **medium**

- **Sites:** Insert flyout open; insert list keyboard
- **Target:** focus `InsertSearchBox` on open; Enter inserts highlighted row (without SelectionChanged insert regressions from B)
- **Value:** a11y / muscle memory
- **Cost:** low–medium; careful with B’s tap-vs-selection fix
- **Rank:** medium — natural follow-on to grouped picker



### 5. Convention ViewLocator for token bodies — **medium**

- **Sites:** `FormatTokenEditorRegistry` (name→VM) + `FormatTokenEditorBodyFactory` (VM→View) twin switches; mirror `FilterEditorViewLocator`
- **Target:** naming-convention `…ViewModel` → `…View` for bodies; keep **explicit** registry for which tokens are editable (product identity)
- **Value:** one less switch when adding an editor body
- **Cost:** reflection/convention; fail-loud missing view; modest test updates
- **Rank:** medium — later; do not erase explicit editable-token list



### 6. Consistent name trimming in Compile — **medium** (product call)

- **Sites:** `_CompileToken` (no trim) vs any residual trim expectations in UX / docs
- **Target:** trim both or neither; document
- **Value:** fewer “unknown `<file-name >`” surprises
- **Cost:** behavior change for edge templates; low LOC
- **Rank:** medium — only with an explicit product decision



### 7. Theme-aware error link color — **low**

- **Sites:** `Themes/FilterEditor.axaml` hard-coded `#C42B1C`
- **Target:** Light/Dark dictionary brushes
- **Value:** dark-theme readability
- **Cost:** tiny
- **Rank:** low — polish when touching theme resources



### 8. Shared OK-only message dialog — **low**

- **Sites:** `FormatEditorMessageDialog`; similar OK-only windows elsewhere
- **Target:** one small shared dialog when a second FormatEditor host (or peer control) needs it
- **Value:** delete one-off
- **Cost:** low now, weak until reuse
- **Rank:** low — wait for PathMover / Audio / second consumer



### 9. Nested FormatEditor in `source=` + nested spans — **low for F6**

- **Sites:** `SubstrFormatTokenEditorViewModel` / `TokenFormatTokenEditorViewModel` Source text boxes; outer-only `FormatTokenSpan` list; tokens that `Compile` nested `source=`
- **Target:** embed FormatEditor in source fields + optional nested spans / caret hit-test in `TryValidate`
- **Value:** Edit inside nested tokens without a second ad-hoc parser
- **Cost:** high (engine + UI + gestures + tests)
- **Rank:** low for F6 — post-F6 feature; pair engine nested spans with UI



### 10. Inserter/Audio “likely-token” validation mode — **low**

- **Sites:** `FormatStringCompiler.ContainsLikelyFormatTokens` gate vs Formatter always-Compile; FormatEditor always validates
- **Target:** FormatEditor validation policy flag when hosts do not always-Compile
- **Value:** avoid false errors on literal `<3>` in Inserter-style text
- **Cost:** UI API; not needed until those hosts adopt FormatEditor
- **Rank:** low — defer until reuse



## Explicit non-goals / skip


| Idea                                                      | Why skip                                    |
| --------------------------------------------------------- | ------------------------------------------- |
| Hand-written token catalog instead of `[FormatTokenInfo]` | Open catalog + missing-attr fail is correct |
| Erase explicit `FormatTokenEditorRegistry` name list      | Editable-token set is product identity      |
| Force nested FormatEditor into PR C / F6 exit             | Soft Source text boxes are enough for F6    |
| Merge Padding/Reset choices into enums fighting ComboBox  | String Value + Label binds cleanly          |
| Debounce live Validate                                    | Validate is cheap; B left it intentional    |
| Move format string off `Text` DP onto FormatEditor VM     | DP is right for a reusable control          |




## Autofix index (this pass)

Applied inline on branches `f6/formateditor-a`, `f6/formateditor-b`, and `f6/formateditor-c` during the respective PR review gates (see those review transcripts / diffs). No separate autofix PR required for the applied items above.