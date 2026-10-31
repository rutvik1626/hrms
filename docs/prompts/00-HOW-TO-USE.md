# How to use these build prompts

## Setup (once)
1. Create an empty Git repository `hrms` on GitHub and clone it locally.
2. Copy into it:
   - `CLAUDE.md` → repo root (Claude Code reads it automatically every session)
   - `HRMS-Requirements.md` → `docs/HRMS-Requirements.md`
   - this `prompts/` folder → `docs/prompts/`
3. Install: .NET 8 SDK, Node LTS, Angular CLI, Flutter stable, Docker Desktop, `dotnet-ef`, Git, VS Code / Rider / Android Studio / Xcode (Mac for iOS builds).
4. Open Claude Code in the repo root.

## Running a prompt
- **One prompt per Claude Code session** (start a fresh session for each, so context stays focused). Large prompts are split into numbered parts (e.g. "Part A / Part B") — run each part in its own session if Claude asks or context gets long.
- Paste: `Execute docs/prompts/NN-name.md` — or paste the file's content.
- Start each session in **plan mode** for prompts 01–05, 08, 10, 13 (foundational / complex): review Claude's plan, then approve.
- Claude will stop at the **"Ask me first"** points — answer them; don't let it guess on business rules.

## Between prompts
1. Read the summary Claude gives (what changed, what's not done).
2. Run the **Verify** commands in the prompt yourself.
3. Review the diff (at least: migrations, permissions, public API changes).
4. Commit on a branch → PR → CI green → merge to `main`.
5. If something is wrong, fix it in the same session before moving on — later prompts build on earlier ones.

## Run order
| # | Prompt | Release | Depends on |
|---|---|---|---|
| 01 | repo-skeleton | 1A | — |
| 02 | building-blocks | 1A | 01 |
| 03 | identity-service | 1A | 02 |
| 04 | gateway | 1A | 03 |
| 05 | workflow-notification | 1A | 03, 04 |
| 06 | angular-shell | 1A | 04 |
| 07 | flutter-foundation | 1A | 04 |
| 08 | corehr-service | 1A | 03, 05 |
| 09 | corehr-web | 1A | 06, 08 |
| 10 | time-service | 1B | 08, 05 |
| 11 | time-web | 1B | 09, 10 |
| 12 | time-mobile | 1B | 07, 10 |
| 13 | payroll-service | 1C | 10 |
| 14 | payroll-web-mobile | 1C | 11, 12, 13 |
| 15 | expense-asset | 1D | 05, 08, 13 |
| 16 | talent | 1E | 08, 13 |
| 17 | mobile-manager-hr | 1B–1E | 12, 15 |
| 18 | reports-dashboards | 1A–1E | all module prompts |
| 19 | devops-deploy | from 1A onwards | 01 (start early, extend per release) |
| 20 | hardening-golive | before launch | all |

> Tip: run **19** right after **04** to get staging live early, then re-run its "extend" section whenever a new service is added.

## Prompt template (all files follow it)
**Goal · Read first · Scope (in / out) · Deliverables · Acceptance criteria · Verify · Ask me first**
