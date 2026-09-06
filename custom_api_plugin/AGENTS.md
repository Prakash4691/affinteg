## IMPORTANT: Do not read through the entire codebase at the start of a session.
This project has a Graphify knowledge graph at `graphify-out/graph.json`.
Query the graph first. Read individual files only when the graph points you to them.

## How to get context from the graph

### Before reading source files, use these CLI commands:

1. `graphify query "<your question>"` — scoped subgraph for a plain-language question.
   Returns explicit paths with `file:line` citations.

2. `graphify path "<ComponentA>" "<ComponentB>"` — shortest path between two concepts,
   hop by hop. Use close-enough names, they are matched by label.

3. `graphify explain "<ConceptName>"` — everything linked to one node:
   what it is, where it lives (file:line), what calls it, what it calls.

### Check confidence tags on every result:
- `EXTRACTED` — found directly in the source AST, safe to trust
- `INFERRED` — derived by resolution, verify by reading the actual file if your change depends on it
- `AMBIGUOUS` — could not be fully resolved, treat as a hint only

### Only read raw source files when:
- The graph doesn't cover what you need
- You need exact line numbers or implementation details for editing
- You're verifying an `INFERRED` or `AMBIGUOUS` relationship that your change depends on
