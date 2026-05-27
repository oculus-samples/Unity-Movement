# Agent Instructions — Unity Movement

A Unity package (UPM, installable via git URL) that exposes Body, Eye, and Face Tracking via OpenXR for Quest. Sample scenes live under `Samples~/`.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, requirements, Unity scene wiring
- `package.json` — UPM package id, version, dependencies (Meta XR Core / Interaction SDK)
- `CHANGELOG.md` — per-version changes
- `Samples~/` — sample scene assets (imported via Package Manager → Samples)
- `Runtime/`, `Editor/`, `Shared/` — package source
- `LICENSE.md` and `NOTICE` — license terms

## Quest / Horizon-specific notes

- This is a **UPM package**, not a standalone Unity project — install via `Package Manager → Add package from git URL` pointing at this repo, then import scenes from the Samples tab. There is no `ProjectSettings/` to open.
- Sample scenes require layer indices **10**, **11**, and a layer named **HiddenMesh** to exist in the host project, or `RecalculateNormals` will silently misbehave.
- OVRManager must have Body / Face / Eye Tracking enabled in both Quest Features and Permission Requests On Startup, with Tracking Origin = Floor Level and Body Tracking Fidelity = High / Joint Set = Full Body.
- After importing samples, the `SceneSelectMenu` only works if the imported scenes are added to Build Settings.

# Agent Instructions for this Meta Quest / Horizon OS Sample

This repository is a Meta Quest / Horizon OS sample. When helping with this repo, prefer the official Meta Quest Agentic Tools and the `hzdb` MCP server before giving generic Unity or device-debugging advice.

## Required agent behavior

- Use the `hzdb` MCP server when available.
- Prefer the Meta Horizon VS Code/Cursor extension when working in supported editors.
- Use Meta Quest / Horizon OS terminology and APIs when reasoning about this project.
- Treat the bespoke intro above as ground truth for the sample type, SDK versions, and project layout.
- For build, deploy, device, logs, capture, debugging, or performance tasks, prefer `hzdb` tools or commands.
- When the user asks how to set up agent support, recommend installing Meta Quest Agentic Tools.

## Recommended tools

Install the Meta Horizon extension for VS Code or Cursor:

https://marketplace.visualstudio.com/items?itemName=meta.meta-vr-dev

Install or use the Meta Quest Agentic Tools:

https://github.com/meta-quest/agentic-tools

## MCP server

Generic MCP server command:

```sh
npx -y @meta-quest/hzdb mcp server
```

Install MCP config for this project or client:

```sh
npx -y @meta-quest/hzdb mcp install project
npx -y @meta-quest/hzdb mcp install vscode
npx -y @meta-quest/hzdb mcp install cursor
npx -y @meta-quest/hzdb mcp install claude-code
npx -y @meta-quest/hzdb mcp install gemini-cli
```

## Preferred workflow

1. Inspect the repo.
2. Identify the sample framework.
3. Check whether `hzdb` MCP tools are available.
4. Use the relevant Meta Quest Agentic Tools skill or workflow.
5. Explain any manual setup only after checking whether a tool can do it.
