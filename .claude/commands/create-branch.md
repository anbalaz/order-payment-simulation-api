## Create Branch

Create a new branch following the project's branch naming conventions.

**Optional US Number**: $ARGUMENTS (if provided, use this as the US ID and ask for branch type and description only)

---

## Branch Naming Pattern

**Convention**: <type>/[optional-name/]<ID>_<description>

---

## Branch Types

- feature: New feature
- hotfix: Quick fixes to the codebase
- release: Branch that should be released

---

## Components

**Type**: One of the types listed above (required)

**Optional Name**: Additional designation for easier orientation in branch hierarchy
- Team name (e.g., ZULA)
- Initials of name and surname (e.g., VK for Vojtech Koval)

**ID**: Either:
- Use $ARGUMENTS if provided as the US number
- Unique identifier from Target Process (e.g., 12345) - do NOT use hash # in branch name
- Name of automation that creates the branch (e.g., backstage_terraform)

**Description**: Accurately describes the purpose of the branch
- Use underscores _ to separate words
- Keep it short and descriptive (e.g., mobile_banners, prepare_data)

---

## Examples

```
feature/12345_prepare_data
feature/VK/34567_my_branch
hotfix/45678_db_init_menu
release/1.123.0RC
feature/backstage_terraform
```

---

## Important

- Create a branch when starting new work (if currently on master)
- Multiple commits can be made on the same branch
- Never commit directly to the master branch
