# Localization Guidelines

- Use resource files under `Resources/` for all user-facing strings.
- Prefer parameterized strings: e.g., `"Product.InStock": "Product '{0}' is in stock! SKU: {1}"`.
- Apply culture-aware formatting for dates, numbers, and currency using `CultureInfo.CurrentCulture`.
- Avoid hardcoded English; use resource lookups where possible.
- Keep internal logs invariant using `CultureInfo.InvariantCulture`.

## Examples

- Date formatting: `date.ToString("d", CultureInfo.CurrentCulture)`
- Currency formatting: `amount.ToString("C", CultureInfo.CurrentCulture)`
- Resource lookup placeholder usage documented in `Resources/*.json`.

## Keys

- CLI.* for command descriptions
- Notifications.* for toast/console messages
- Errors.* for validation and error messages

## Process

1. Add new keys to `en-CA` JSON first.
2. Reference the key in code (central helper planned in Phase 2).
3. Add translations in future phases.


