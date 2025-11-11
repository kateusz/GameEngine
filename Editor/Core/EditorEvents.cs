using ECS;
using Engine.Scene;

namespace Editor.Core;

/// <summary>
/// Common editor events for inter-panel communication.
/// These events allow panels to communicate without direct dependencies.
/// </summary>

// Entity Selection Events
public record EntitySelectedEvent(Entity Entity);
public record EntityDeselectedEvent();

// Scene Events
public record SceneChangedEvent(IScene Scene);
public record SceneLoadedEvent(IScene Scene, string Path);
public record SceneSavedEvent(IScene Scene, string Path);
public record SceneNewEvent(IScene Scene);

// Asset Events
public record AssetImportedEvent(string AssetPath, string AssetType);
public record AssetDeletedEvent(string AssetPath);
public record AssetRenamedEvent(string OldPath, string NewPath);

// Project Events
public record ProjectOpenedEvent(string ProjectPath, string ProjectName);
public record ProjectClosedEvent();
public record ProjectCreatedEvent(string ProjectPath, string ProjectName);

// Editor State Events
public record EditorModeChangedEvent(EditorMode Mode);
public record ViewportFocusChangedEvent(bool IsFocused);
public record CameraPositionChangedEvent(System.Numerics.Vector3 Position);

// Component Events
public record ComponentAddedEvent(Entity Entity, Type ComponentType);
public record ComponentRemovedEvent(Entity Entity, Type ComponentType);
public record ComponentModifiedEvent(Entity Entity, Type ComponentType);

// Animation Events (for AnimationTimeline communication)
public record AnimationTimelineOpenRequestEvent(Entity Entity);
public record AnimationClipChangedEvent(Entity Entity, string ClipName);
