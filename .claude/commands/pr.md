## Create Pull Request

Create a pull request using the gh CLI.

**Optional US Number**: $ARGUMENTS (if provided, use this as the user story number instead of extracting from branch name)

---

## Workflow

1. **If on master branch**: Use /create-branch to create a new branch first
2. **If changes are not committed**: Use /commit to commit them first
3. **Create PR**: Use gh pr create with the formatting rules below

---

## PR Title Format

**Structure**: <type>: <description> #<US-number>

Use the same commit types and conventions as defined in /commit command.

- If $ARGUMENTS is provided, use that as the US number in the title
- Otherwise, extract the numeric ID from the branch name

**Example**: feat: migrate from GitLab to GitHub CI/CD #820747

---

## PR Body Format

- Start with a brief summary section
- List the master changes as bullet points
- Keep it concise and factual
- **DO NOT** add the US number as a standalone line at the end
- **DO NOT** include test plans with checkboxes
- **DO NOT** add "Generated with Claude Code" or "Co-Authored-By: Claude" attributions

---

## Example

**Title**: feat: add user authentication endpoint #12345

**Body**:
```
## Summary
- Implemented JWT-based authentication
- Added login and logout endpoints
- Created middleware for protected routes
```
