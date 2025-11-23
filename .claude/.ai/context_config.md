# AI Context Configuration

## Context Sources
- primary: ARCHITECTURE_GUIDE.md
- secondary: coding_conventions.md
- schema: schema.md
- common_code: common_code_guide.md
- location: .ai/ folder
- fallback: README.md

## AI Usage Rules
1. Always read `ARCHITECTURE_GUIDE.md` before any code generation, reasoning, or architectural decision.
2. Use schema, namespace, and conventions from `ARCHITECTURE_GUIDE.md` for all tasks.
3. If more context is needed, fallback to `README.md` or other documentation in `/Docs`.
4. For new features, refactoring, or bug fixes, ensure all changes align with the guide.
5. Never generate code that violates the architecture, naming, or security rules in context files.
6. If context files are updated, always use the latest version.

## Example Workflow
- Step 1: Parse `ARCHITECTURE_GUIDE.md` for architecture, schema, and conventions.
- Step 2: Parse `README.md` for project overview and additional rules.
- Step 3: For any code or reasoning, always reference context sources above.

---
This configuration ensures all AI agents operate with full project context, maximizing accuracy, consistency, and maintainability.
