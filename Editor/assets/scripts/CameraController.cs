using System;
using System.Numerics;
using Engine.Core.Input;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;

namespace Editor.assets.scripts;

// TODO: this must be removed from the engine and implemented in the user project
public class CameraController : ScriptableEntity
{
    public float MoveSpeed = 5.0f;
    public float PanSpeed = 0.15f;

    private bool _isPerspective;

    // Perspective orbital state — mirrors EditorCamera fields exactly
    private Vector3 _focalPoint = Vector3.Zero;
    private float _distance = CameraConfig.DefaultEditorDistance;
    private float _pitch;
    private float _yaw;

    // Mouse / key tracking
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouseSample = true;
    private bool _altDown;
    private bool _leftMouseDown;
    private bool _middleMouseDown;
    private bool _rightMouseDown;

    // Orthographic movement accumulator
    private Vector3 _orthoInput = Vector3.Zero;

    public override void OnCreate()
    {
        if (!HasComponent<CameraComponent>())
            return;

        _isPerspective = GetComponent<CameraComponent>().Camera.ProjectionType == ProjectionType.Perspective;

        if (_isPerspective && HasComponent<TransformComponent>())
            _focalPoint = GetComponent<TransformComponent>().Translation;
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (!_isPerspective)
        {
            UpdateOrthographic((float)ts.TotalSeconds);
            return;
        }

        if (!HasComponent<CameraComponent>())
            return;

        var orientation = GetOrientation();
        var position = _focalPoint - GetForwardDir(orientation) * _distance;

        // Exact same matrix construction as EditorCamera.UpdateView
        GetComponent<CameraComponent>().CameraViewTransform =
            Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(position);
    }

    public override void OnDestroy()
    {
        if (_isPerspective && HasComponent<CameraComponent>())
            GetComponent<CameraComponent>().CameraViewTransform = null;
    }

    // --- EditorCamera math (verbatim) ---

    private Quaternion GetOrientation() =>
        Quaternion.CreateFromYawPitchRoll(-_yaw, -_pitch, 0f);

    private static Vector3 GetForwardDir(Quaternion q) =>
        Vector3.Transform(-Vector3.UnitZ, q);

    private static Vector3 GetRightDir(Quaternion q) =>
        Vector3.Transform(Vector3.UnitX, q);

    private static Vector3 GetUpDir(Quaternion q) =>
        Vector3.Transform(Vector3.UnitY, q);

    private void Orbit(Vector2 delta)
    {
        var q = GetOrientation();
        var yawSign = GetUpDir(q).Y < 0 ? -1f : 1f;
        _yaw += yawSign * delta.X * CameraConfig.EditorRotationSpeed;
        _pitch += delta.Y * CameraConfig.EditorRotationSpeed;
    }

    private void Pan(Vector2 delta)
    {
        var q = GetOrientation();
        _focalPoint += -GetRightDir(q) * delta.X * PanSpeed * _distance;
        _focalPoint += GetUpDir(q) * delta.Y * PanSpeed * _distance;
    }

    private void Zoom(float delta)
    {
        _distance -= delta * CalculateZoomSpeed();
        if (_distance < CameraConfig.MinEditorDistance)
        {
            _focalPoint += GetForwardDir(GetOrientation());
            _distance = CameraConfig.MinEditorDistance;
        }
        _distance = MathF.Min(_distance, CameraConfig.MaxEditorDistance);
    }

    private float CalculateZoomSpeed()
    {
        float d = MathF.Max(_distance * 0.2f, 0f);
        return MathF.Min(d * d, 100f);
    }

    private void UpdateOrthographic(float dt)
    {
        if (_orthoInput == Vector3.Zero || !HasComponent<TransformComponent>())
            return;
        GetComponent<TransformComponent>().Translation += _orthoInput * MoveSpeed * dt;
    }

    // --- Input callbacks ---

    public override void OnMouseMoved(float x, float y)
    {
        if (!_isPerspective)
            return;

        if (_firstMouseSample)
        {
            _lastMouseX = x;
            _lastMouseY = y;
            _firstMouseSample = false;
            return;
        }

        var delta = new Vector2(x - _lastMouseX, y - _lastMouseY) * CameraConfig.EditorMouseSensitivity;
        _lastMouseX = x;
        _lastMouseY = y;

        if (!_altDown)
            return;

        if (_leftMouseDown) Orbit(delta);
        else if (_middleMouseDown) Pan(delta);
        else if (_rightMouseDown) Zoom(delta.Y);
    }

    public override void OnMouseScrolled(float xOffset, float yOffset)
    {
        if (_isPerspective)
            Zoom(yOffset * CameraConfig.EditorZoomSensitivity);
    }

    public override void OnMouseButtonPressed(int button)
    {
        switch (button)
        {
            case 0: _leftMouseDown = true; break;
            case 1: _rightMouseDown = true; break;
            case 2: _middleMouseDown = true; break;
        }
    }

    public override void OnMouseButtonReleased(int button)
    {
        switch (button)
        {
            case 0: _leftMouseDown = false; break;
            case 1: _rightMouseDown = false; break;
            case 2: _middleMouseDown = false; break;
        }
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        if (_isPerspective)
        {
            if (key is KeyCodes.LeftAlt or KeyCodes.RightAlt)
                _altDown = true;
        }
        else
        {
            switch (key)
            {
                case KeyCodes.W: _orthoInput += Vector3.UnitY; break;
                case KeyCodes.S: _orthoInput -= Vector3.UnitY; break;
                case KeyCodes.A: _orthoInput -= Vector3.UnitX; break;
                case KeyCodes.D: _orthoInput += Vector3.UnitX; break;
            }
        }
    }

    public override void OnKeyReleased(KeyCodes key)
    {
        if (_isPerspective)
        {
            if (key is KeyCodes.LeftAlt or KeyCodes.RightAlt)
                _altDown = false;
        }
        else
        {
            switch (key)
            {
                case KeyCodes.W: _orthoInput -= Vector3.UnitY; break;
                case KeyCodes.S: _orthoInput += Vector3.UnitY; break;
                case KeyCodes.A: _orthoInput += Vector3.UnitX; break;
                case KeyCodes.D: _orthoInput -= Vector3.UnitX; break;
            }
        }
    }
}
