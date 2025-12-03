## Create Branch

Create a new branch following the project's branch naming conventions.

**Optional Arguments**: $ARGUMENTS (if provided, can be US number or other identifiers)

---

## Branch Naming Pattern

**Convention**: <type>/[optional-name/]<ID>_<description>

**CRITICAL - SNAKE_CASE ONLY**:
- **ALWAYS use UNDERSCORES (_)** to separate words in branch names
- **NEVER use hyphens/dashes (-)** - this is a strict requirement
- ID must come IMMEDIATELY after the slash (or after optional-name/ if present)
- Format is: `type/ID_description` OR `type/optional-name/ID_description`

---

## Branch Types

- **feature**: New feature development
- **hotfix**: Quick fixes to the codebase
- **release**: Branch that should be released
- **bugfix**: Bug fixes that aren't urgent hotfixes
- **refactor**: Code refactoring without changing functionality

---

## Components

**Type**: One of the types listed above (required)

**Optional Name**: Additional designation for easier orientation in branch hierarchy
- Team name
- Initials of name and surname

**ID**: The numeric identifier for the branch (MUST come right after the slash before description)
- Use $ARGUMENTS if provided as the identifier
- User Story number (e.g., 12345) - do NOT use hash # in branch name
- For non-ticketed work, use descriptive name (e.g., `kafka_integration`)

**Description**: Accurately describes the purpose of the branch
- **Use underscores (_)** to separate words - **NEVER use hyphens (-)**
- Keep it short and descriptive
- Examples: `mobile_banners`, `prepare_data`, `kafka_event_driven_processing`
- Comes AFTER the ID, separated by underscore

---

## Examples

**Correct (snake_case with underscores)**:
```
feature/12345_prepare_data
feature/VK/34567_my_branch
hotfix/45678_db_init_menu
release/1.123.0RC
feature/kafka_integration
feature/order_processing_notifications
bugfix/98765_fix_payment_validation
```

**WRONG (kebab-case with hyphens - DO NOT USE)**:
```
feature/kafka-integration  ❌
feature/order-processing   ❌
hotfix/db-init-menu       ❌
```

---

## Important

- Create a branch when starting new work (if currently on main)
- Multiple commits can be made on the same branch
- Never commit directly to the main branch
- **Always verify you're using underscores (_), not hyphens (-)**
