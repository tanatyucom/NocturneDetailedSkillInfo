# Validation CSV notes

These files are the organized public copies of the preserved v1.0.0 release
audit data.

- `skill_validation_ja.csv`: 512 rows / 150 changed
- `skill_validation_en.csv`: 512 rows / 150 changed

They intentionally preserve the raw audit fields so future developers can
re-evaluate old assumptions without repeating the reverse-engineering work.

The added columns `Language`, `ValidationScope`, and
`DirectInGameVerification` are documentation metadata. The historical audits
did not record direct in-game verification per individual row.
