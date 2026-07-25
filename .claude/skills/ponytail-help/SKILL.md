---
name: ponytail-help
description: >
  Quick-reference card for all ponytail modes, skills, and commands in Cursor.
  One-shot display, not a persistent mode. Trigger: @ponytail-help,
  "ponytail help", "what ponytail commands", "how do I use ponytail".
disable-model-invocation: true
---

# Ponytail Help

Display this reference card when invoked. One-shot — do NOT change mode,
write flag files, or persist anything.

## Levels

| Level | How to trigger (Cursor) | What changes |
|-------|-------------------------|--------------|
| **Lite** | Say "ponytail lite" or "@ponytail lite" | Build what's asked; name the lazier alternative in one line. |
| **Full** | Say "@ponytail" or enable via user rule | The ladder enforced: YAGNI → stdlib → native → one line → minimum. Default. |
| **Ultra** | Say "ponytail ultra" or "@ponytail ultra" | YAGNI extremist. Deletion before addition. Challenges requirements before building. |

Level sticks for the rest of the chat unless you say otherwise.

## Skills

| Skill | Cursor trigger | What it does |
|-------|----------------|--------------|
| **ponytail** | `@ponytail` or natural language ("be lazy", "yagni") | Lazy mode itself. Simplest solution that works. |
| **ponytail-review** | `@ponytail-review` | Over-engineering review: `L42: yagni: factory, one product. Inline.` |
| **ponytail-audit** | `@ponytail-audit` | Whole-repo over-engineering audit: ranked list of what to delete. |
| **ponytail-debt** | `@ponytail-debt` | Harvest `ponytail:` shortcut comments into a tracked ledger. |
| **ponytail-gain** | `@ponytail-gain` | Measured-impact scoreboard: less code, less cost, more speed. |
| **ponytail-help** | `@ponytail-help` | This card. |

Type `@` in chat and pick a skill, or describe what you want — the agent
matches from each skill's description.

## Always-on lazy mode

For every session without typing `@ponytail`, add a **Cursor user rule** with
the ponytail ladder (or copy the core rules from `ponytail/SKILL.md`). User
rules apply across chats; skills load per invocation or when auto-matched.

## Deactivate

Say "stop ponytail" or "normal mode". Resume with `@ponytail` or your user
rule.

## Where skills live

Project skills: `.claude/skills/ponytail*/SKILL.md` (this repo) or
`.cursor/skills/ponytail*/SKILL.md`. Personal skills: `~/.cursor/skills/`.

## More

Full docs + examples: https://github.com/DietrichGebert/ponytail
