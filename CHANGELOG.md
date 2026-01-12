# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added
- Central Package Management (`Directory.Packages.props`) for consistent dependency upgrades.
- `RenderBatchSize` parameter to reduce allocations and render overhead for long animations.
- Initial bUnit test coverage for key behaviors (dir rendering, extraction + completion, seek rendering).
- `CHANGELOG.md`.

### Changed
- Updated Blazor/ASP.NET Core packages to `10.0.1`.
- Updated MinVer to `7.0.0`.
- Demo publish now reliably rewrites `<base href>` at publish time for GitHub Pages.
- Typewriter rendering now avoids per-tick `RenderFragment` allocations and relies on batched renders.
- Demo cleanup: removed unused handlers and added a render-batching example.

### Fixed
- `Dir` now applies as an actual `dir` attribute, so RTL styling works as expected.

