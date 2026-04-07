# Contributing to MimicFacility

Thank you for your interest in contributing to MimicFacility! This document will be expanded as the project matures.

## Getting Started

1. Fork the repository
2. Create a feature branch from `main` (`git checkout -b feature/your-feature`)
3. Make your changes
4. Test in Unity Editor (Play Mode)
5. Commit with a clear message describing your change
6. Open a Pull Request against `main`

## Branch Naming

- `feature/` — New features or mechanics
- `fix/` — Bug fixes
- `art/` — Art, audio, or FX asset additions
- `docs/` — Documentation updates
- `refactor/` — Code restructuring without behavior changes

## Code Style

- Follow C# conventions (PascalCase for public members, camelCase for private)
- Use namespaces matching the folder structure (e.g., `MimicFacility.AI.Director`)
- Prefab naming: prefix with `PFB_` as appropriate
- Use `[SerializeField]` for inspector-exposed private fields
- Comment complex logic; skip obvious boilerplate comments

## Reporting Issues

Open a GitHub Issue with:
- Steps to reproduce
- Expected vs. actual behavior
- Unity version and platform

## Code of Conduct

Be respectful, constructive, and collaborative. Detailed code of conduct coming soon.
