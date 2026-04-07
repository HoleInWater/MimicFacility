# Contributing to MimicFacility

Thank you for your interest in contributing to MimicFacility! This document will be expanded as the project matures.

## Getting Started

1. Fork the repository
2. Create a feature branch from `main` (`git checkout -b feature/your-feature`)
3. Make your changes
4. Test in Unreal Editor (Development Editor, Win64)
5. Commit with a clear message describing your change
6. Open a Pull Request against `main`

## Branch Naming

- `feature/` — New features or mechanics
- `fix/` — Bug fixes
- `art/` — Art, audio, or FX asset additions
- `docs/` — Documentation updates
- `refactor/` — Code restructuring without behavior changes

## Code Style

- Follow Unreal Engine coding standards for C++ (prefix classes with appropriate UE letters: `A` for Actors, `U` for UObjects, `F` for structs, etc.)
- Blueprint naming: prefix with `BP_`, `WBP_`, `BT_`, `BB_`, `EQS_` as appropriate
- Keep C++ headers and implementations paired
- Comment complex logic; skip obvious boilerplate comments

## Reporting Issues

Open a GitHub Issue with:
- Steps to reproduce
- Expected vs. actual behavior
- Engine version and platform

## Code of Conduct

Be respectful, constructive, and collaborative. Detailed code of conduct coming soon.
