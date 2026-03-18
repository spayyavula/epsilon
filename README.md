# Epsilon

**AI-powered mathematics research and learning assistant for Windows.**

Epsilon bridges the gap between informal mathematical reasoning and formal rigor. Chat with LLMs, build proofs step by step, solve equations with live display, explore concepts deeply, and get introduced to Lean 4 theorem proving.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue) ![Windows](https://img.shields.io/badge/platform-Windows-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

### AI Math Chat
- Chat with **OpenAI**, **Anthropic (Claude)**, **Google Gemini**, or **Ollama** (local)
- Beautiful LaTeX equation rendering via KaTeX
- Math-optimized system prompt with formal notation support

### Equation Solver
- **Live step-by-step display** — watch the solution unfold in real-time
- Solves: algebraic, trigonometric, differential equations, integrals, limits, series, systems, and more
- Each step labeled and explained with LaTeX rendering
- Final answer boxed with verification
- Clickable examples for quick start

### Document Library & RAG
- Upload PDFs, Word docs, textbooks, and lecture notes
- **Link folders** and **OneDrive integration**
- Automatic text extraction, chunking, and full-text search (SQLite FTS5)
- Every chat and solver query searches your documents for grounded answers

### Web Search
- Toggle Exa web search to find academic content
- **Save to Library** — save web results as PDFs, automatically indexed

### Research Toolkit
Five guided tools for mathematical work:

| Tool | Steps | Purpose |
|------|-------|---------|
| **Proof Builder** | 5 | Statement, strategy, construction, verification, formal write-up |
| **Problem Solver** | 2 | Full step-by-step solution or hint mode for learning |
| **Concept Explorer** | 2 | Deep dives: definitions, theorems, examples, connections |
| **Lean Bridge** | 3 | Translate informal proofs to Lean 4 formal verification code |
| **Practice Mode** | 2 | Progressive exercises with feedback on your proof attempts |

### Welcome Tour
- Interactive animated walkthrough on first launch
- Accessible anytime via "? Tour & Help"

## Getting Started

### Prerequisites
- Windows 10/11 (x64)
- WebView2 Runtime (pre-installed on Windows 11)
- At least one API key: [OpenAI](https://platform.openai.com/api-keys), [Anthropic](https://console.anthropic.com/), [Google Gemini](https://aistudio.google.com/apikey), or [Ollama](https://ollama.com)

### Download
Download `Epsilon.exe` from the [Releases](../../releases) page. No installation required.

### Build from Source
```bash
git clone https://github.com/spayyavula/epsilon.git
cd epsilon
dotnet build
dotnet run --project src/Epsilon.App
```

### Publish
```bash
dotnet publish src/Epsilon.App/Epsilon.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o build/publish
```

## Quick Start

1. Launch Epsilon
2. Go to **Settings** → enter your API key
3. **Solver** → type `x^2 + 5x + 6 = 0` and hit Enter
4. **Chat** → ask "Prove that √2 is irrational"
5. **Research** → try the Proof Builder or Lean Bridge

## Tech Stack

- **.NET 8** / WPF with [WPF-UI](https://github.com/lepoco/wpfui)
- **WebView2** with KaTeX + marked.js for LaTeX rendering
- **SQLite** with FTS5 for document search
- **QuestPDF** for PDF export
- **CommunityToolkit.Mvvm** for MVVM architecture
- Multi-LLM: OpenAI, Anthropic, Gemini, Ollama
- [Exa](https://exa.ai) for semantic web search

## License

[MIT](LICENSE)
