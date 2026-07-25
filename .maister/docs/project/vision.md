# Project Vision

## Overview
GameEngine is a C# ECS game engine with a visual ImGui editor and standalone runtime for building 2D and 3D games on OpenGL (DirectX planned later).

## Current State
- **Age**: Actively developed (multi-year history; modern .NET 10 stack)
- **Status**: Active development toward public 2D alpha (~70% foundation)
- **Users**: Solo developer today; external game developers as alpha audience
- **Tech Stack**: C# / .NET 10, custom ECS, DryIoc, Silk.NET OpenGL, ImGui editor, Roslyn scripting, Box2D, OpenAL

## Purpose
Provide an editor-first, scriptable engine so developers can compose scenes, write C# gameplay, and publish standalone builds without fighting low-level OpenGL plumbing.

## Goals (Next 6-12 Months)
- Finish the 2D workflow including animation
- Import FBX assets with animation
- Create 3D scenes with world content and animation playback
- Keep OpenGL as the primary backend; leave a path for DirectX later via renderer abstraction

## Evolution
The project already has ECS, 2D/3D rendering foundations, physics queries, editor publish pipeline, and sample games. Next focus shifts from pure foundation to animation pipelines and a usable 3D content workflow, while still closing remaining 2D alpha gaps (runtime UI, undo, hierarchy).
