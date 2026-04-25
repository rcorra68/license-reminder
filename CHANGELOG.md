## [unreleased]

### 🚀 Features

- *(api)* Add user endpoint

### 📚 Documentation

- Update CHANGELOG

### ⚙️ Miscellaneous Tasks

- *(workflow)* Update release pipeline configuration
- *(workflow)* Fix dotnet command in release pipeline
- *(format)* Apply dotnet format to project
- Move release.sh to script/ folder
- Remove scripts/pre-push file
- Updated Directory.Build.props to remove MinVer
- Remove DocFX and related documentation artifacts
- *(ci)* Correct git-cliff installation for linux runner
- *(release)* Remove deprecated release script (migrated to GitHub Actions)
## [3.3.1] - 2026-04-24

### ⚙️ Miscellaneous Tasks

- Add version logging to Serilog startup
## [3.3.0] - 2026-04-24

### 🚜 Refactor

- Automate SMTP security mode selection

### ⚙️ Miscellaneous Tasks

- Remove dotnetenv dependency and migrate to native .NET configuration
## [3.2.1] - 2026-04-23

### ⚙️ Miscellaneous Tasks

- Add GitHub Actions workflow for automated releases
## [3.2.0] - 2026-04-18

### 🚀 Features

- Add GitHub Actions workflow for automated DocFX deployment

### 🚜 Refactor

- Improve orchestration metrics and SMTP configuration handling
- Relocate AppVersion from Core.Service to Core.Shared to avoid misuse of service layer
- Convert Program.cs to explicit class to support XML documentation
- *(email)* Async email pipeline and cleanup

### 📚 Documentation

- Delete unsafe data
- Configure base URL for GitHub Pages deployment
- Fix invalid API link in index.md and verify TOC structure
- Add DocFx documentation with API reference and index pages
- Create docfx.yml action
- Staged docfx.yml action

### ⚙️ Miscellaneous Tasks

- *(build)* Add MinVer + Git commit hash to assembly informational version
- *(ci)* Introduce GitHub Actions pipeline for .NET build, test and versioning (MinVer + full git history)
- Initialize DocFX configuration and metadata mapping
## [3.1.0] - 2026-04-11

### 🚀 Features

- Implement unit tests for repositories and orchestrator logic

### 🐛 Bug Fixes

- *(git)* Repair syntax error in pre-push hook

### 💼 Other

- Implement automatic semantic versioning with MinVer
- Resolve duplicate MinVer package references
- Remove all async references
- Remove all async references

### 🚜 Refactor

- *(infra)* Organize services into logical subfolders
- Introduce Entities layer and update repository references

### 🧪 Testing

- Resolve nullability warnings in license category assertions

### ⚙️ Miscellaneous Tasks

- Create test project in tests/AvvisoScadenzaPatenti.Tests directory and add to solution
## [3.0.0] - 2026-04-07

### 🚀 Features

- Initial project restructuring and license orchestration logic

### 💼 Other

- Handle deleted files conflict

### 🚜 Refactor

- Implement SOLID architecture and modern .NET 8 entry point

### 📚 Documentation

- Cleaning sensible data
- Cleaning sensible data
- Cleaning sensible data
- Add comprehensive README with architecture and usage guide
- Add comprehensive README with architecture and usage guide
- Add comprehensive README with architecture and usage guide
- *(cli)* Add XML comments and documentation to all interfaces and repositories
- Modified README with badge and roadmap
- Fix formatting in README.md
- *(infra)* Finalize logging and email service implementation

### ⚙️ Miscellaneous Tasks

- Remove .DS_Store and add it to .gitignore
## [1.0.0.0] - 2025-09-12
