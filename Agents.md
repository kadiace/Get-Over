# 🧠 AGENTS.md — Unity Project Coding Rules

## 0. Project Context (MUST READ)

- **Engine**: Unity 6.3 LTS
- **Template**: Core Universal 3D
- **Render Pipeline**: URP (Universal Render Pipeline)
- **Language**: C#
- **Scope**: Runtime game logic (Editor tooling only when explicitly requested)
- **Unity CLI Path**: `C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe`

This repository is a **Unity project**.  
The **Unity Editor** is the only authoritative source for compilation, validation, and runtime behavior.

---

## 1. Project Structure (STRICT)

Agents MUST respect the following directory responsibilities.

```
Assets/
├─ Datas/ # Pure data files (.json, etc.)
├─ Resources/ # Unity resources (art, animation, sound, prefabs)
├─ Scenes/ # Unity .unity scene files only
└─ Scripts/
    ├─ Controllers/ # Object-level behaviors (Player, Camera, NPC, etc.)
    ├─ Datas/ # Interfaces & loaders for Assets/Datas
    ├─ Managers/ # Core systems using the @Manager singleton pattern
    ├─ Scenes/ # Scene-level logic (*Scene.cs)
    └─ UIs/ # UI logic (all must extend UI_Base)
```

❌ **Forbidden**

- Mixing Manager logic into Controllers
- Putting runtime logic into `Assets/Scenes`
- Creating new top-level folders without explicit instruction

---

## 2. Domain Responsibilities

### 2.1 Controllers

- One Controller = one in-game object
- Responsible for:
  - Input handling
  - Animation & transform control
  - Object-specific behavior

❌ Controllers MUST NOT:

- Load scenes
- Instantiate or manage Managers
- Contain global state

---

### 2.2 Managers (`@Manager` Pattern)

- All Managers:
  - Use Singleton pattern
  - Are orchestrated by a central `@Manager`
- Responsibilities:
  - Scene loading
  - Resource loading
  - Global systems & state

❌ Managers MUST NOT:

- Depend on scene-specific objects
- Be instantiated from Controllers
- Rely on undefined initialization order

---

### 2.3 Scene Scripts (`Assets/Scripts/Scenes`)

- Each Unity scene has:
  - Exactly one `*Scene.cs`
  - One GameObject named `@*Scene`
- Responsibilities:
  - Scene bootstrapping
  - Initializing `@Manager`
  - Requesting scene transitions

❌ Scene scripts MUST NOT:

- Contain gameplay logic
- Act as Managers
- Directly manage UI behavior

---

### 2.4 UI Scripts

- All UI scripts MUST:
  - Extend `UI_Base`
- UI responsibilities:
  - Presentation logic
  - User interaction handling

❌ UI MUST NOT:

- Own game state
- Load scenes directly
- Create or destroy Managers

---

## 3. Data & Resource Rules

### Assets/Datas

- Data files are **read-only at runtime**
- Loaded via interfaces in `Assets/Scripts/Datas`

### Assets/Resources

- Use only when explicitly required
- Avoid hardcoded magic strings
- Prefer constants or centralized definitions

---

## 4. 🚫 LSP / Language Server Policy (CRITICAL)

### Absolute Rule

**DO NOT run or rely on any LSP-based diagnostics.**

This includes:

- `lsp diagnose`
- `csharp-ls`
- OmniSharp
- Roslyn language servers
- Any language-server-based validation step

### Enforcement

- Missing or unavailable LSPs MUST NOT cause:
  - Failure
  - Rollback
  - Refusal to modify code

### Rationale

- Unity performs compilation and validation internally
- External C# LSPs are not Unity-aware
- LSP diagnostics often produce false negatives

### Validation Standard

- Assume correctness based on:
  - Unity conventions
  - Existing project patterns
  - Logical consistency

**Final validation occurs only inside the Unity Editor.**

---

## 5. Unity 6 + URP — Common Pitfalls (AVOID)

Agents MUST avoid the following mistakes:

- Using deprecated Built-in Render Pipeline APIs
- Modifying URP assets or pipeline settings without request
- Assuming `Camera.main` is always valid
- Creating Materials at runtime without lifecycle management
- Using `Resources.Load` for logic-critical systems
- Assuming Script Execution Order instead of designing deterministically

---

## 6. Code Modification Rules

When modifying or generating code:

- ✅ Follow existing naming conventions and patterns
- ✅ Prefer minimal, localized changes
- ❌ Do NOT refactor unrelated files
- ❌ Do NOT improve architecture unless explicitly requested
- ❌ Do NOT introduce new dependencies

If uncertain, ask for clarification instead of guessing.

---

## 7. Final Authority

- Unity Editor compilation is the **only source of truth**
- No external static analysis is authoritative
- LSP-based diagnostics are invalid in this repository

---

## 8. 📘 Unity Documentation Reference Policy (CRITICAL)

All code written or modified in this repository MUST be based on **official Unity documentation**, specifically targeting **Unity 6.3 LTS**.

### Primary Source of Truth

- Unity **official manual and scripting API** for **Unity 6.x LTS**
- Documentation hosted on:
  - https://docs.unity3d.com
  - Unity Scripting API corresponding to Unity 6.3 LTS

### Rules

- Prefer APIs, patterns, and behaviors explicitly documented for **Unity 6.x LTS**
- When multiple Unity versions differ, **Unity 6.3 LTS behavior takes priority**
- Avoid relying on:
  - Deprecated APIs
  - Legacy behaviors from older Unity versions
  - Community-only patterns unless explicitly requested

### Forbidden Assumptions

Agents MUST NOT:

- Assume behavior based on pre-Unity 6 versions
- Use obsolete examples from Built-in Render Pipeline
- Rely on undocumented or experimental APIs unless explicitly instructed

### URP-Specific Requirement

- All rendering-related code MUST align with **URP-compatible APIs**
- Do NOT assume Built-in Render Pipeline features or defaults
- Shader, material, and rendering logic must follow URP documentation

### If Uncertain

If the correct approach is unclear:

- Prefer the **simplest solution documented in official Unity manuals**
- Ask for clarification instead of guessing or inventing patterns

### Enforcement

Code that contradicts Unity 6.3 LTS official documentation is considered **incorrect**, even if it compiles.

Unity Editor behavior and official documentation together form the **final authority**.

---

## 🚨 Final Notice for Agents

This is a **Unity-first project**.  
Respect folder boundaries, domain responsibilities, and **never invoke LSP diagnostics**.

🚫 LSP-based diagnostics are explicitly forbidden in this repository.
Any attempt to run or rely on LSP results is considered incorrect behavior.
