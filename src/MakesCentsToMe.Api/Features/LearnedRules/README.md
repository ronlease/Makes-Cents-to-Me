# Learned Rules

Learned rules remember a user's vendor and category corrections so future imports categorize matching transactions automatically.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/learned-rules` | List rules ordered by pattern |
| GET | `/api/v1/learned-rules/{id}` | Get a rule (404 if missing) |
| POST | `/api/v1/learned-rules` | Create a rule (400 on validation failure) |
| PUT | `/api/v1/learned-rules/{id}` | Update a rule (404 / 400) |
| DELETE | `/api/v1/learned-rules/{id}` | Delete a rule (204 / 404) |

## Key Types

- `LearnedRule` entity: pattern (unique, stored normalized), normalized vendor, category, source transaction ID, timestamps
- `LearnedRulePattern`: static `Normalize`, `Derive`, `Matches`, `FindBestMatch`
- `ILearnedRuleService`: CRUD plus `ApplyRulesAsync` and `BuildSuggestionAsync`
- `LearnedRuleSuggestion`: proposed rule derived from a user override; `ExistingLearnedRuleId` is set when the pattern already exists with a different mapping
- `LearnedRuleResult<T>`: wraps `ApiResponse<T>` and flags not-found failures

## Pattern Derivation

Normalize: trim, collapse whitespace, upper-case. Derive: normalize, cut at the first digit or `#`, trim trailing spaces and `* - # . /`. If fewer than 3 characters remain, use the first whitespace token of the normalized description, then the whole normalized description. Patterns must be at least 3 characters.

Examples: `WHOLEFDS MKT 10245` becomes `WHOLEFDS MKT`; `AMZN MKTP US*2K4F7B1G3` becomes `AMZN MKTP US`; `7-ELEVEN 12345` becomes `7-ELEVEN`.

## Matching

A rule matches when the normalized description starts with the pattern and the next character is the end of the string or not a letter or digit (prefix plus word boundary). The longest matching pattern wins; ties go to the most recently updated rule.

## Applying Rules on Import

`ApplyRulesAsync` loads all rules once. For each match it sets category, normalized vendor, suggested category/vendor, confidence 1.0, `IsAutoCategorized = true`, `LearnedRuleId`, and status `Committed`. Description, raw CSV row, and raw data are never modified. Unmatched transactions are returned for Claude analysis. The method does not save; the caller persists changes.

## Deletion Semantics

Deleting a rule clears `LearnedRuleId` on transactions it categorized; they keep their category, vendor, and `IsAutoCategorized` flag. A category referenced by a rule cannot be deleted.
