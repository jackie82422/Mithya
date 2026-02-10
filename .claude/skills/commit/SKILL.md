---
name: commit
description: Stage changes and create a conventional commit
disable-model-invocation: true
allowed-tools: Bash(git *)
argument-hint: "[scope]"
---

# Commit Workflow

Create a commit for the current changes, following conventional commit format.

## Steps

1. Run `git status` and `git diff --staged` to review changes
2. If nothing is staged, identify related changed files and stage them (do NOT use `git add -A`)
3. Analyze the staged diff to determine:
   - **type**: `feat` | `fix` | `refactor` | `docs` | `test` | `chore` | `perf` | `style` | `ci`
   - **scope**: the module or area affected (e.g., `frontend`, `backend`, `proxy`, `endpoints`, `infra`)
   - **subject**: concise imperative description (max 50 chars)
4. If the user provided `$ARGUMENTS`, use it as the scope hint

## Commit Message Format

```
type(scope): subject

Optional body with more detail (wrap at 72 chars).
Can use multiple paragraphs.

- Bullet points are fine
```

## Rules

- **NEVER** include any AI-related co-author lines, attribution, or mentions
- **NEVER** use `--no-verify` unless explicitly asked
- Subject line: imperative mood, lowercase, no period at end
- Scope should be concise: `frontend`, `backend`, `proxy`, `logs`, `scenarios`, `import-export`, `infra`, `db`
- If changes span multiple scopes, use the primary scope or omit it: `feat: add xxx`
- Body is optional for small changes, required for multi-file changes
- Do NOT commit `.env`, credentials, or large binary files

## Examples

```
feat(proxy): add service-level proxy fallback
fix(backend): handle duplicate path+method on import
refactor(frontend): replace ProxyConfig with ServiceProxy types
docs: update CLAUDE.md with architecture guide
chore(infra): update docker-compose postgres to v16
perf(backend): add index on mock_request_logs timestamp
test(proxy): add integration tests for fallback forwarding
```

## Multi-scope Example (body)

```
feat(backend): add enhanced request matching with AND/OR logic

- Add LogicMode enum (AND/OR) to MockRule entity
- Update MatchEngine to evaluate conditions with OR support
- Add migration for logic_mode column
- Update rule API to accept logicMode parameter
```
