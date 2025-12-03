## Commit Changes

Add all staged changes and create a commit following the Conventional Commits specification.

**Optional US Number**: $ARGUMENTS (if provided, use this as the US number instead of extracting from branch name)

---

## Commit Message Format

**Structure**: <type>[optional scope]: <description>

**Valid Types**:
- **build**: Build system or external dependencies
- **ci**: CI configuration files/scripts
- **chore**: Changes that don't modify src or test sources
- **docs**: Documentation only
- **feat**: New feature
- **fix**: Bug fix
- **hotfix**: Urgent production fixes
- **perf**: Performance improvement
- **refactor**: Code change that neither fixes a bug nor adds a feature
- **revert**: Reverts previous commits
- **style**: Formatting changes (whitespace, comments, etc.)
- **test**: Adding or correcting tests

---

## US Number Rules

**When to include #<US-number>**:
- If $ARGUMENTS is provided, use that as the US number
- Otherwise, extract the US number from the branch name (e.g., feature/12345_description → 12345)
- Append #<US-number> at the end of commit description **ONLY for the first commit** on the branch
- Subsequent commits on the same branch do **NOT** need the US number
- If no US number exists and no $ARGUMENTS provided (e.g., feature/kafka_integration), don't append a number

---

## Breaking Changes

Use ! after type/scope (e.g., feat(api)!: breaking change) or include BREAKING CHANGE: in footer.

---

## Examples

**Correct branch naming (snake_case with underscores)**:
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

# Branch: feature/kafka_integration (no numeric ID)
ci: add Kafka integration to event processing

# Branch: feature/order_processing_notifications
feat: implement event-driven order notifications
```

**Wrong branch naming (kebab-case with hyphens - DO NOT USE)**:
```
# ❌ feature/kafka-integration (should be feature/kafka_integration)
# ❌ hotfix/order-fix (should be hotfix/order_fix)
```

---

## Additional Notes

- **Never commit directly to main branch** - use /create-branch first to create a proper feature branch with **snake_case naming (underscores, not hyphens)**
- **Do NOT add** "Generated with Claude Code" or "Co-Authored-By: Claude" attribution
- **Always use underscores (_)** in branch names, never hyphens (-)
