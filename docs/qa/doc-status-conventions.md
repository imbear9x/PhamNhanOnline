# Documentation Status Conventions

## Canonical fields

Recommended frontmatter fields for second-brain docs:

- `title`
- `doc_type`
- `status`
- `owner` or `owners`
- `last_verified`
- `source_of_truth`
- `related_docs`
- `related_code`
- `tags`

## Verification rule

A doc should not claim `verified` unless it has been checked against at least one of:

- code path
- config path
- DB artifact
- runtime/log evidence
- test output
