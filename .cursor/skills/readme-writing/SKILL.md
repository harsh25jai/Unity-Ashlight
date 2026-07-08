---
name: readme-writing
description: >-
  Writes and improves README.md files using README-driven development, cognitive
  funneling, and open-source best practices. Use when creating, rewriting, or
  reviewing a README, project documentation front page, or when the user asks
  how to document a repo.
---

# README Writing

## Philosophy

A README is the **front door** of a repository. Visitors decide in under 60 seconds whether to star, fork, use, or leave. A good README answers four questions fast:

1. **What** does this do?
2. **How** do I install it?
3. **How** do I use it?
4. **Why** should I trust it?

Principles (synthesized from [Akash's guide](https://meakaakka.medium.com/a-beginners-guide-to-writing-a-kickass-readme-7ac01da88ab3), [Art of README](https://github.com/noffle/art-of-readme), [jehna/readme-best-practices](https://github.com/jehna/readme-best-practices), [OpenMark](https://openmarkapp.com/blog/how-to-write-readme-md)):

- **README-driven development**: Clarify purpose and API in the README before (or alongside) code. The README defines the interface.
- **Brevity**: As short as possible without being shorter. Move deep reference to `/docs` or a wiki.
- **Cognitive funneling**: Broad, high-signal content first → specifics later → credits/background last.
- **Show, don't tell**: Working code examples beat paragraphs. Test every snippet.
- **Respect skimmers**: Headings, bullets, tables. Developers jump to what they need.

Target length: **200–800 words** for most projects. Over ~400 lines → add a Table of Contents or split into `/docs`.

---

## Workflow

Copy this checklist and track progress:

```
Task Progress:
- [ ] Step 1: Understand the project
- [ ] Step 2: Pick template tier and project type
- [ ] Step 3: Draft README (cognitive funnel order)
- [ ] Step 4: Run quality checklist
- [ ] Step 5: Verify code examples work
```

### Step 1: Understand the project

Before writing, gather from the repo:

- Project name, one-sentence purpose, target audience
- Install/run commands (read `package.json`, `Makefile`, `Cargo.toml`, `ProjectSettings`, CI, etc.)
- License file
- Existing docs, CONTRIBUTING.md, screenshots
- What makes this project different from alternatives

If the repo is empty or new, ask the user for the four questions above.

### Step 2: Pick template tier

| Tier | When | Sections |
|------|------|----------|
| **Minimal** | Hackathon, WIP, tiny utility | Title, tagline, install, usage, license |
| **Standard** | Most OSS repos | + badges, demo visual, features, contributing |
| **Full** | Mature libs, frameworks | + API reference, config tables, dev setup, FAQ |

Project-type templates: see [templates.md](templates.md).

### Step 3: Draft (recommended section order)

Use this order unless the project type template says otherwise:

1. **Title** (`#`) — project name
2. **Tagline** — one line, verb-first, no jargon. Not "This project is…"
3. **Badges** (optional) — 3–5 max: license, CI/build, version. Use [shields.io](https://shields.io). Skip badge walls.
4. **Demo** — screenshot, GIF, or Mermaid diagram above the fold
5. **Pitch** (1 short paragraph) — what, who it's for, why it's different
6. **Quick Start** — 3 steps max, copy-paste commands, expected output
7. **Features** — 3–7 bullets, verb-led, scannable (table OK for comparisons)
8. **Usage** — 2–4 realistic code examples covering 80% of use cases
9. **Configuration** — env vars / options table, or link to docs
10. **API Reference** — inline for small projects; link to `/docs` for large ones
11. **Development** — clone, install deps, run tests (or link to CONTRIBUTING.md)
12. **Contributing** — fork → branch → PR; link CONTRIBUTING.md if non-trivial
13. **License** — one line linking to LICENSE file

**Library-specific funnel** (Art of README): Name → One-liner → Usage → API → Installation → License. Put license early if non-permissive.

### Step 4: Quality checklist

Before finishing, verify:

- [ ] Tagline answers "what is this?" without repeating the project name
- [ ] First code block is install or minimal runnable example
- [ ] All code examples are copy-pasteable and tested
- [ ] No broken links, outdated version pins, or stale screenshots
- [ ] Unfamiliar terms are defined or linked
- [ ] TOC added if README is long (GitHub auto-TOC works with proper headings)
- [ ] Critical info not trapped only in images (images can break)
- [ ] License section present
- [ ] Contributing section exists (even one sentence) for OSS

### Step 5: Tone and formatting

- Lead with verbs: "Converts markdown to HTML" not "A tool that helps you convert…"
- Use `##` headings consistently; one H1 only
- Tables for API params, env vars, CLI flags
- Link aggressively: docs, issues, related projects, inspirations
- Avoid walls of prose — break every 3–4 lines

---

## Anti-patterns (fix these)

| Problem | Fix |
|---------|-----|
| Empty or missing README | At minimum: title, tagline, install, usage, license |
| Broken example code | Run examples; include expected output |
| Outdated requirements | Sync with actual deps and CI |
| 12+ badges | Keep 3–5 meaningful ones |
| 5000-word README | Move reference to `/docs`; keep front door scannable |
| "Contributions welcome" only | Add concrete fork/branch/PR steps or link CONTRIBUTING.md |
| Jargon tagline | Plain language; define acronyms on first use |
| All prose, no code | Add at least one runnable snippet |

---

## Project-specific notes

**Games / Unity / creative projects**: Lead with a screenshot or GIF. Cover engine version, how to open the project, how to play/build. Link to design docs separately.

**Internal tools**: Emphasize setup, env vars, and who to contact. Trust signals = CI badge + last-updated note if no public CI.

**CLI tools**: Show `command --help` and 2–3 real invocations with stdout.

**Libraries**: Minimal import + one-liner usage in the first 200 words.

---

## Additional resources

- Copy-paste templates by project type: [templates.md](templates.md)
- Annotated good README patterns: [examples.md](examples.md)
- External references: [Art of README](https://github.com/noffle/art-of-readme), [awesome-readme](https://github.com/matiassingers/awesome-readme), [README-driven development](http://tom.preston-werner.com/2010/08/23/readme-driven-development.html)
