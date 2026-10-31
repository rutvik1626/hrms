## What & why
<!-- What changed and which requirement/prompt it implements (e.g. docs/prompts/08, Stage 1 "Core HR"). -->

## How it was tested
<!-- Commands run, new tests, manual checks. -->

## Checklist (CLAUDE.md "Definition of done")
- [ ] Tests added/updated (unit, integration incl. tenant-isolation + permission tests where relevant)
- [ ] Migrations added and applied cleanly on an empty DB; seed data (permissions/defaults) updated
- [ ] OpenAPI updated and clients regenerated if the API changed; no unversioned breaking change
- [ ] i18n keys added for new UI text
- [ ] No secrets, PII, or real employee data in code, logs, or fixtures
- [ ] Docs/ADR updated if a decision changed
