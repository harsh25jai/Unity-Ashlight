# README Examples & Patterns

Annotated patterns from well-regarded guides. Use as reference when drafting.

---

## Tagline examples

**Good** (verb-first, specific):

```markdown
# pageres

Capture website screenshots at multiple viewport sizes.
```

```markdown
# bittorrent-dht

BitTorrent DHT library — queries the distributed hash table for peers.
```

**Bad** (vague, meta, jargon-heavy):

```markdown
# my-project

This project is a tool that helps you do stuff with files.
```

```markdown
# foco

A WIP Electron app.
```

**Fix**: State the concrete outcome. "Stay focused by blocking distracting sites during work sessions."

---

## Quick Start pattern

Three steps max. Show expected output.

```markdown
## Quick Start

\`\`\`bash
npm install my-lib
\`\`\`

\`\`\`javascript
import { parse } from 'my-lib'
console.log(parse('{ "a": 1 }'))
// => { a: 1 }
\`\`\`
```

---

## Features pattern

Scannable bullets. Verb or noun phrase first. Specific, not marketing fluff.

```markdown
## Features

- Parses JSON5 with trailing commas and comments
- Streaming API for files larger than RAM
- TypeScript types included
- Zero dependencies
```

**Avoid**:

```markdown
## Features

- Easy to use
- Fast and efficient
- Great developer experience
```

---

## Configuration table pattern

```markdown
## Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `3000` | HTTP server port |
| `DATABASE_URL` | — | PostgreSQL connection string (required) |
| `LOG_LEVEL` | `info` | `debug` \| `info` \| `warn` \| `error` |
```

---

## Architecture diagram (complex projects)

Mermaid renders on GitHub without external image hosting:

```markdown
## Architecture

\`\`\`mermaid
flowchart LR
  Client[CLI] --> Parser[Parser]
  Parser --> AST[AST]
  AST --> Renderer[HTML Renderer]
  Renderer --> Output[Output]
\`\`\`
```

---

## Contributing (minimal but concrete)

```markdown
## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing`)
5. Open a Pull Request

Please run tests before submitting: `npm test`
```

For non-trivial projects, link out:

```markdown
## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, code style, and PR guidelines.
```

---

## Cognitive funnel in practice

**Wide end (everyone reads)**:

```markdown
# collide-2d-aabb-aabb

Determines whether a moving axis-aligned bounding box (AABB) collides with other AABBs.

\`\`\`javascript
var collide = require('collide-2d-aabb-aabb')
var hit = collide(box1, box2)
\`\`\`
```

**Middle (interested readers)**:

```markdown
## API

### `collide(a, b)`

Returns `true` if boxes `a` and `b` overlap.

- `a`, `b`: `{ x, y, width, height }`
```

**Narrow end (deep context)**:

```markdown
## Background

AABBs are axis-aligned bounding boxes — rectangles that don't rotate...
```

---

## Exemplar repositories

Study these for structure and tone ([Art of README](https://github.com/noffle/art-of-readme)):

- [feross/bittorrent-dht](https://github.com/feross/bittorrent-dht) — clear one-liner, usage, API
- [sindresorhus/pageres](https://github.com/sindresorhus/pageres) — screenshot, install, examples
- [substack/tape](https://github.com/substack/tape) — minimal, no-nonsense
- [matiassingers/awesome-readme](https://github.com/matiassingers/awesome-readme) — curated gallery of great READMEs

---

## Akash's extended section list

From the [kickass README gist](https://gist.github.com/akashnimare/7b065c12d9750578de8e705fb4771d2f). Include only what applies — not every project needs all sections:

| Section | Include when |
|---------|--------------|
| Motivation | Problem isn't obvious from the name |
| Build status | CI exists; badges add trust |
| Code style | Contributors need to match a linter |
| Screenshots | UI, game, or visual tool |
| Tech/framework | Stack isn't obvious |
| Code example | Always for libraries |
| API reference | Public API exists |
| Tests | Test suite exists; show how to run |
| Credits | Forks, inspirations, contributors |
