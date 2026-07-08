# README Templates

Copy the matching template, replace placeholders, delete unused sections.

---

## Minimal (any project)

```markdown
# Project Name

One-line verb-first description of what this does.

## Quick Start

\`\`\`bash
# install or clone
git clone https://github.com/user/project.git
cd project
# run
./start.sh
\`\`\`

## Usage

\`\`\`bash
./project --example
# Expected output: ...
\`\`\`

## License

MIT — see [LICENSE](LICENSE).
```

---

## Open Source Library

```markdown
# library-name

Brief one-line description.

[![npm version](https://img.shields.io/npm/v/library-name)](https://www.npmjs.com/package/library-name)
[![CI](https://github.com/user/library-name/actions/workflows/ci.yml/badge.svg)](https://github.com/user/library-name/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Installation

\`\`\`bash
npm install library-name
\`\`\`

## Usage

\`\`\`javascript
import { convert } from 'library-name'

const result = convert('input')
console.log(result)
// => 'expected output'
\`\`\`

## Features

- Does X without Y
- Supports Z out of the box
- Zero config for the common case

## API

### `convert(input, options?)`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `input` | `string` | — | Source data |
| `options.strict` | `boolean` | `false` | Enable strict mode |

## Development

\`\`\`bash
git clone https://github.com/user/library-name.git
cd library-name
npm install
npm test
\`\`\`

## Contributing

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Commit and push
4. Open a pull request

See [CONTRIBUTING.md](CONTRIBUTING.md) for code style and review process.

## License

MIT — see [LICENSE](LICENSE).
```

---

## CLI Tool

```markdown
# tool-name

What it does, in one line.

## Installation

\`\`\`bash
npm install -g tool-name
# or: brew install tool-name
\`\`\`

## Quick Start

\`\`\`bash
tool-name --help
tool-name input.md --output dist/
\`\`\`

## Options

| Flag | Description |
|------|-------------|
| `--output`, `-o` | Output directory |
| `--watch`, `-w` | Watch for file changes |
| `--verbose` | Print debug logs |

## Examples

\`\`\`bash
# Convert a single file
tool-name readme.md -o html/

# Batch convert a directory
tool-name ./docs --output ./site
\`\`\`

## License

MIT — see [LICENSE](LICENSE).
```

---

## Application / Game (Unity, Electron, etc.)

```markdown
# Project Name

> Short tagline — genre, platform, or elevator pitch.

![Screenshot or gameplay GIF](docs/screenshot.png)

## About

One paragraph: what the project is, who it's for, current status (alpha/beta/release).

## Requirements

- Unity 6 LTS (or: Node 20+, etc.)
- Platform: Windows / macOS / Linux

## Getting Started

1. Clone the repository
2. Open `ProjectName/` in Unity Hub (or run `npm install && npm start`)
3. Open the `Main` scene and press Play

\`\`\`bash
git clone https://github.com/user/project.git
\`\`\`

## Features

- Feature one
- Feature two
- Feature three

## Controls

| Input | Action |
|-------|--------|
| WASD | Move |
| E | Interact |

## Development

\`\`\`bash
# Run tests
# Unity: Window → General → Test Runner
\`\`\`

## Contributing

Pull requests welcome. Please open an issue first for large changes.

## License

[License name] — see [LICENSE](LICENSE).

## Credits

- Inspired by [project](https://github.com/example)
- Assets: [source](https://example.com)
```

---

## Full / Mature Project (jehna-style)

Use when the repo needs install, dev, deploy, and config docs in one file.
Prefer splitting to `/docs` once this exceeds ~400 lines.

```markdown
# Project Name

> Tagline

Brief description of what it does and why someone should use it.

## Installing / Getting started

\`\`\`shell
packagemanager install project
project start
\`\`\`

### Initial configuration

Document API keys, env files, or first-run setup here.

## Developing

\`\`\`shell
git clone https://github.com/user/project.git
cd project
packagemanager install
\`\`\`

### Building

\`\`\`shell
make build
\`\`\`

### Deploying

\`\`\`shell
make deploy
\`\`\`

## Features

- Main capability
- Secondary capability

## Configuration

#### `OPTION_NAME`
Type: `String`  
Default: `'default'`

What it does and when to change it.

## Contributing

Fork, feature branch, pull request. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Links

- Homepage: https://example.com
- Issues: https://github.com/user/project/issues
- Security: email security@example.com (do not open public issues)

## License

MIT — see [LICENSE](LICENSE).
```
