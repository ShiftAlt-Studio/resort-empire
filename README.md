# Resort Empire: Paradise Tycoon

Resort Empire is a mobile resort management game developed as a university team project.

The player develops a tropical resort, manages guest needs, assigns workers, improves buildings and expands the resort.

## Project

- Engine: Unity
- Platform: Mobile
- Orientation: Landscape
- Repository: `resort-empire`
- Team: ShiftAlt Studio

## Development

The project is developed in Unity.

Before working on the project:

1. Clone the repository
2. Open the project through Unity Hub
3. Use the Unity version specified for the project
4. Create a separate branch for your task
5. Do not commit directly to the main branch

## Branch Naming

Use the Jira issue key in the branch name.

Examples:

`feature/RE-12-wave-needs-panel`

`feature/RE-18-worker-allocation`

`fix/RE-25-wave-counter`

## Commit Messages

Include the Jira issue key in commit messages.

Examples:

`RE-12 Add wave needs panel`

`RE-18 Implement worker allocation`

`RE-25 Fix wave counter`

## Unity Version Control

Unity project settings should use:

- Version Control Mode: Visible Meta Files
- Asset Serialization Mode: Force Text

Always commit `.meta` files together with their corresponding Unity assets.

## Project Structure

Main Unity project folders:

`Assets/` — game assets, scripts, scenes and UI

`Packages/` — Unity package configuration

`ProjectSettings/` — shared Unity project settings

Generated Unity folders such as `Library/`, `Temp/`, `Logs/` and `obj/` are not stored in Git.

## Team

ShiftAlt Studio
