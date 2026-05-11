# Token Efficiency Policy

Use this only when troubleshooting token bloat or overly heavy retrieval behavior.

## Policy

- retrieve only what the current task needs
- prefer direct canonical docs over long summaries about those docs
- avoid repeating the same heavy reads when the relevant facts are already known
- summarize large outputs instead of replaying them
- do not keep obsolete shadow or legacy rule files in the active mental model when a live source is known
