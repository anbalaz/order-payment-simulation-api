## Commit Changes

Add all staged changes and create a commit following the Conventional Commits specification.

**Optional US Number**: $ARGUMENTS (if provided, use this as the user story number instead of extracting from branch name)

---

## Commit Message Format

**Structure**: <type>[optional scope]: <description>

**Valid Types**:
- build: Build system or external dependencies
- ci: CI configuration files/scripts
- chore: Changes that don't modify src or test sources
- docs: Documentation only
- feat: New feature
- fix: Bug fix
- perf: Performance improvement
- refactor: Code change that neither fixes a bug nor adds a feature
- revert: Reverts previous commits
- style: Formatting changes (whitespace, comments, etc.)
- test: Adding or correcting tests

---

## User Story Number Rules

**When to include #<ID>**:
- If $ARGUMENTS is provided, use that as the US number
- Otherwise, extract the numeric ID from the branch name (e.g., feature/12345_description → 12345)
- Append #<ID> at the end of commit description **ONLY for the first commit** on the branch
- Subsequent commits on the same branch do **NOT** need the US number
- If no numeric ID exists and no $ARGUMENTS provided (e.g., feature/backstage_terraform), don't append a number

---

## Breaking Changes

Use ! after type/scope (e.g., feat(api)!: breaking change) or include BREAKING CHANGE: in footer.

---

## Examples

```
# Branch: feature/12345_user_authentication
# First commit only
feat: add user authentication endpoint #12345

# Subsequent commits
test: add unit tests for authentication
docs: update authentication documentation

# Branch: feature/VK/34567_cart_fix
# First commit only
fix(api): resolve null reference in cart controller #34567

# Subsequent commits
refactor: improve cart service error handling

# Branch: hotfix/45678_wishlist_breaking_change
feat(wishlist)!: breaking change to wishlist API #45678

# Branch: release/1.123.0RC
chore: prepare release 1.123.0RC

# Branch: feature/backstage_terraform (no numeric ID)
ci: add Confluent validation to PR workflow
```

---
