---
name: ui-simple-extensible
description: Designs and implements UI with surface-level simplicity, clear user flows, and extensible foundations. Use when building screens, components, forms, or navigation where the user asks for clean UX now with room for future capabilities.
---

# UI: Simple Surface, Extensible Core

## When to apply this skill

Apply this skill when UI work should feel easy to users while staying ready for future features:

- New page or component creation
- UX cleanup or simplification
- Form, onboarding, or navigation design
- Refactors where basic flows must stay obvious but architecture should scale

## Core principles

1. Keep first interactions obvious.
2. Use progressive disclosure for advanced behavior.
3. Make defaults safe and useful.
4. Separate UI surface from business logic.
5. Add extension points before they are urgent.

## Working approach

Copy this checklist and execute in order:

```markdown
UI Progress
- [ ] Define core user goal in one sentence
- [ ] Keep primary path short and obvious
- [ ] Hide complexity behind optional affordances
- [ ] Design state model for future feature flags/options
- [ ] Validate naming and layout for clarity
- [ ] Verify empty/loading/error/success states
```

### 1) Define the simple path first

- Identify the one primary action users must succeed at.
- Ensure the main call-to-action is unambiguous.
- Avoid exposing advanced settings in the first view unless required.

### 2) Add advanced capability via progressive disclosure

Use one of these patterns:

- Secondary section (`Advanced options`)
- Expand/collapse panel
- Step-up dialog after basic flow completion
- Contextual actions shown only when relevant

### 3) Build extensible internals

- Keep render components focused on presentation.
- Move decision logic and data mapping into reusable utilities/hooks/services.
- Prefer composable props and typed option objects over hardcoded branches.
- Reserve explicit slots/regions for future controls when structure is stable.

### 4) State and behavior rules

- Define clear states: `idle`, `loading`, `success`, `error`, `empty`.
- Prefer deterministic state transitions.
- Ensure advanced state is optional and does not break the base flow.
- Keep validation and error messages plain-language and actionable.

## Output expectations

When delivering UI changes:

1. Explain how the base flow stays simple.
2. Name what extension point was added for future capabilities.
3. Confirm handling of empty/loading/error states.
4. Mention any trade-off made between simplicity and extensibility.

## Guardrails

- Do not let advanced controls dominate the main view.
- Do not entangle visual components with infrastructure logic.
- Do not add speculative complexity without a defined extension point.
- Do not sacrifice clarity of labels, spacing, and hierarchy.

## Done criteria

- A new user can complete the main task without guidance.
- Advanced behavior is discoverable but non-blocking.
- Internals can accept at least one future variant with minimal surface rewrite.
