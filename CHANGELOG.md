# Changelog

All notable changes to this project are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/), versioning
follows [Semantic Versioning](https://semver.org/).
## [3.8.3] - 2026-09-07

### Bug Fixes

- Always include admin in license notice BCC

### Miscellaneous Tasks

- Prevent CHANGELOG.md conflicts by generating it only on main
- Adopt reusable workflows architecture
- Retrigger after making reusable-workflows public
## [3.8.2] - 2026-07-27

### Bug Fixes

- Propagate VersionPrefix to dotnet publish step
- Prevent --upcoming-expirations from becoming the default run mode

### Documentation

- *(changelog)* Preview for v3.8.2

### Miscellaneous Tasks

- Set initial version to 3.8.1 via Nerdbank.GitVersioning
- Migrate versioning to Nerdbank.GitVersioning and split release/build pipeline
## [3.8.1] - 2026-07-24

### Bug Fixes

- *(tests)* Update MailBcc test setup from array to string

### Documentation

- Document --update-license, --upcoming-expirations, --match-cf and --init CLI parameters
- *(changelog)* Update for v3.8.1

### Miscellaneous Tasks

- Ignore appsettings.*.json to prevent local dev secrets from being tracked
- Generate CHANGELOG.md via git-cliff on release

### Refactor

- Extract mode handlers from Program.cs into dedicated command classes

### Testing

- Add unit tests for MatchFiscalCodeCommand
## [3.8.0] - 2026-07-13

### Features

- Add --match-cf option to link fiscal codes to employees
## [3.7.0] - 2026-07-10

### Features

- Add --upcoming-expirations CLI option to list soonest-expiring licenses
## [3.6.2] - 2026-07-09

### Bug Fixes

- *(csv)* Make ReleaseDate nullable to handle in-progress license requests

### Documentation

- *(cli)* Document ReleaseDate sort behavior for incomplete records
## [3.6.1] - 2026-07-09

### Bug Fixes

- *(ci)* Correct workflow_dispatch indentation in release pipeline
## [3.6.0] - 2026-07-09

### Bug Fixes

- Send daily report only when expiration notifications have been sent (#51)
- *(csv)* Populate license cache in GetAll to persist updates

### Documentation

- Update CHANGELOG

### Features

- Improve CLI sorting documentation and alignment
- Change the order of application startup log messages (#52)
- *(cli)* Add license lookup and expiry update by license number or name

### Miscellaneous Tasks

- Stabilize release pipeline
- Remove npm tooling and simplify CI pipeline
- Add semantic versioning to release pipeline, rename deploy to build

### Refactor

- Send report only when licenses are processed (#49)
## [3.5.0] - 2026-05-05

### Documentation

- Update CHANGELOG
- Update CHANGELOG
- Update CHANGELOG for v3.5.0

### Features

- *(cli)* Add license CSV sorting with CLI options

### Other

- Optimize dev workflow and relax commitlint rules
- Implement automated release workflow with git-cliff and husky
## [3.4.0] - 2026-04-25

### Documentation

- Update CHANGELOG
- Update CHANGELOG for v3.4.0

### Features

- *(api)* Add user endpoint

### Miscellaneous Tasks

- *(workflow)* Update release pipeline configuration
- *(workflow)* Fix dotnet command in release pipeline
- *(format)* Apply dotnet format to project
- Move release.sh to script/ folder
- Remove scripts/pre-push file
- Updated Directory.Build.props to remove MinVer
- Remove DocFX and related documentation artifacts
- *(ci)* Correct git-cliff installation for linux runner
## [3.3.1] - 2026-04-24

### Miscellaneous Tasks

- Add version logging to Serilog startup
## [3.3.0] - 2026-04-24

### Miscellaneous Tasks

- Remove dotnetenv dependency and migrate to native .NET configuration

### Refactor

- Automate SMTP security mode selection
## [3.2.1] - 2026-04-23

### Miscellaneous Tasks

- Add GitHub Actions workflow for automated releases
## [3.2.0] - 2026-04-18

### Documentation

- Delete unsafe data
- Configure base URL for GitHub Pages deployment
- Fix invalid API link in index.md and verify TOC structure
- Add DocFx documentation with API reference and index pages
- Create docfx.yml action
- Staged docfx.yml action

### Features

- Add GitHub Actions workflow for automated DocFX deployment

### Miscellaneous Tasks

- *(build)* Add MinVer + Git commit hash to assembly informational version
- *(ci)* Introduce GitHub Actions pipeline for .NET build, test and versioning (MinVer + full git history)
- Initialize DocFX configuration and metadata mapping

### Refactor

- Improve orchestration metrics and SMTP configuration handling
- Relocate AppVersion from Core.Service to Core.Shared to avoid misuse of service layer
- Convert Program.cs to explicit class to support XML documentation
- *(email)* Async email pipeline and cleanup
## [3.1.0] - 2026-04-11

### Bug Fixes

- *(git)* Repair syntax error in pre-push hook

### Features

- Implement unit tests for repositories and orchestrator logic

### Miscellaneous Tasks

- Create test project in tests/AvvisoScadenzaPatenti.Tests directory and add to solution

### Other

- Implement automatic semantic versioning with MinVer
- Resolve duplicate MinVer package references
- Remove all async references
- Remove all async references

### Refactor

- *(infra)* Organize services into logical subfolders
- Introduce Entities layer and update repository references

### Testing

- Resolve nullability warnings in license category assertions
## [3.0.0] - 2026-04-07

### Documentation

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

### Features

- Initial project restructuring and license orchestration logic

### Miscellaneous Tasks

- Remove .DS_Store and add it to .gitignore

### Other

- Handle deleted files conflict

### Refactor

- Implement SOLID architecture and modern .NET 8 entry point
