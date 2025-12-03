## Create Pull Request

Create a pull request using the gh CLI.

**Optional Arguments**: $ARGUMENTS can be:
- A US number (e.g., `12345`) - will be used as `#12345` in PR title
- A type (e.g., `feature`, `hotfix`, `bugfix`) - will be used as the PR type if branch name doesn't clearly indicate it
- Format: `<type> <us-number>` (e.g., `feature 12345`) - provides both type and US number

---

## Workflow

1. **If on main branch**: Use /create-branch to create a new branch first (remember: **snake_case with underscores only**)
2. **If changes are not committed**: Use /commit to commit them first
3. **Create PR**: Use gh pr create with the formatting rules below

---

## PR Title Format

**Structure**: <type>: <description> [#<US-number>]

Use the same commit types and conventions as defined in /commit command.

**Type Determination** (in order of priority):
1. If $ARGUMENTS contains a type (e.g., `feature`, `hotfix`), use that
2. Otherwise, extract type from branch name (first part before `/`)
3. Map to appropriate conventional commit type

**US Number**:
- If $ARGUMENTS contains a number, use that as `#<number>` in the title
- Otherwise, extract the numeric ID from the branch name
- If no number found, omit the `#<number>` from title

**Examples**:
- `feat: implement Kafka event-driven order processing #820747`
- `hotfix: resolve payment validation error #123456`
- `refactor: improve database connection handling` (no US number)

---

## PR Body Format

- Start with a brief summary section
- List the main changes as bullet points
- Keep it concise and factual
- **DO NOT** add the US number as a standalone line at the end
- **DO NOT** include test plans with checkboxes
- **DO NOT** add "Generated with Claude Code" or "Co-Authored-By: Claude" attributions

---

## Examples

### Example 1: Feature with US number

**Command**: `/pr 12345` or `/pr feature 12345`

**Title**: `feat: add user authentication endpoint #12345`

**Body**:
```
## Summary
- Implemented JWT-based authentication
- Added login and logout endpoints
- Created middleware for protected routes
```

### Example 2: Hotfix without US number

**Command**: `/pr hotfix` (branch: `hotfix/fix_payment_bug`)

**Title**: `hotfix: resolve payment validation error`

**Body**:
```
## Summary
- Fixed null reference exception in payment validation
- Added validation for empty payment amounts
- Updated error messages for better clarity
```

### Example 3: Feature without arguments

**Command**: `/pr` (branch: `feature/kafka_integration`)

**Title**: `feat: implement Kafka event-driven architecture`

**Body**:
```
## Summary
- Integrated Apache Kafka for event streaming
- Implemented order creation events
- Added notification consumers
- Created order expiration service
```
