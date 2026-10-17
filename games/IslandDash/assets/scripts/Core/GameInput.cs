using Input;

namespace IslandDash;

/// <summary>
/// Action mapping. Keyboard + mouse only: the engine exposes no gamepad input yet
/// (add a gamepad branch here once an IGamepadInput service exists).
/// </summary>
public readonly record struct GameInput(
    Vector2 Move,
    bool JumpPressed,
    bool JumpHeld,
    bool SpinPressed,
    float OrbitAxis,
    bool OrbitDrag,
    Vector2 MouseDelta,
    bool ConfirmPressed,
    bool PausePressed)
{
    public static GameInput Read(IKeyboardInput keyboard, IMouseInput mouse)
    {
        var move = Vector2.Zero;
        if (keyboard.IsKeyDown(KeyCodes.W) || keyboard.IsKeyDown(KeyCodes.Up)) move.Y += 1f;
        if (keyboard.IsKeyDown(KeyCodes.S) || keyboard.IsKeyDown(KeyCodes.Down)) move.Y -= 1f;
        if (keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right)) move.X += 1f;
        if (keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left)) move.X -= 1f;
        if (move.LengthSquared() > 1f)
            move = Vector2.Normalize(move);

        var orbit = 0f;
        if (keyboard.IsKeyDown(KeyCodes.Q)) orbit -= 1f;
        if (keyboard.IsKeyDown(KeyCodes.E)) orbit += 1f;

        return new GameInput(
            move,
            keyboard.WasKeyPressed(KeyCodes.Space),
            keyboard.IsKeyDown(KeyCodes.Space),
            keyboard.WasKeyPressed(KeyCodes.K) || keyboard.WasKeyPressed(KeyCodes.J) ||
            keyboard.WasKeyPressed(KeyCodes.LeftShift) || mouse.WasButtonPressed(MouseButtons.Left),
            orbit,
            mouse.IsButtonDown(MouseButtons.Right),
            mouse.Delta,
            keyboard.WasKeyPressed(KeyCodes.Enter) || keyboard.WasKeyPressed(KeyCodes.KeyPadEnter),
            keyboard.WasKeyPressed(KeyCodes.P) || keyboard.WasKeyPressed(KeyCodes.Escape));
    }
}
