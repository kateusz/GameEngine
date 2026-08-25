using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Input;
using PaperGui = Prowl.PaperUI.Paper;

namespace Engine.UI.Paper;

public sealed class PaperInputAdapter
{
    private static readonly KeyCodes[] MappedKeys = BuildMappedKeys();

    private float _scrollDelta;
    private bool _left, _right, _middle;

    public void Apply(InputEvent inputEvent)
    {
        if (inputEvent is MouseScrolledEvent scrolled)
            _scrollDelta += scrolled.YOffset;
    }

    public void Forward(
        PaperGui paper,
        IMouseInput mouseInput,
        IKeyboardInput keyboardInput,
        IPointerSurface pointerSurface,
        float contentScale)
    {
        var mapped = PaperPointerMapper.Map(mouseInput.Position, pointerSurface, contentScale);
        if (mapped.IsInside)
        {
            paper.SetPointerState(
                Prowl.PaperUI.PaperMouseBtn.Unknown,
                mapped.X, mapped.Y,
                isPointerBtnDown: false,
                isPointerMove: true);
        }
        else
        {
            paper.SetPointerPosition(-1, -1);
        }

        ForwardButton(paper, mapped, mouseInput, 0, Prowl.PaperUI.PaperMouseBtn.Left, ref _left);
        ForwardButton(paper, mapped, mouseInput, 1, Prowl.PaperUI.PaperMouseBtn.Right, ref _right);
        ForwardButton(paper, mapped, mouseInput, 2, Prowl.PaperUI.PaperMouseBtn.Middle, ref _middle);
        if (_scrollDelta != 0f)
        {
            paper.SetPointerWheel(_scrollDelta);
            _scrollDelta = 0f;
        }

        foreach (var key in MappedKeys)
            paper.SetKeyState(MapKey(key), keyboardInput.IsKeyDown(key));
    }

    private static void ForwardButton(
        PaperGui paper,
        PaperPointerMapper.MappedPointer mapped,
        IMouseInput mouse,
        int button,
        Prowl.PaperUI.PaperMouseBtn paperButton,
        ref bool wasDown)
    {
        var down = mapped.IsInside && mouse.IsButtonDown(button);
        if (down == wasDown)
            return;
        wasDown = down;
        var x = mapped.IsInside ? mapped.X : -1f;
        var y = mapped.IsInside ? mapped.Y : -1f;
        paper.SetPointerState(paperButton, x, y, down, isPointerMove: false);
    }

    private static KeyCodes[] BuildMappedKeys()
    {
        var keys = new List<KeyCodes>();
        foreach (var key in Enum.GetValues<KeyCodes>())
        {
            if (MapKeyOrNull(key) != null)
                keys.Add(key);
        }

        return [.. keys];
    }

    private static Prowl.PaperUI.PaperKey MapKey(KeyCodes key) =>
        MapKeyOrNull(key) ?? Prowl.PaperUI.PaperKey.Unknown;

    private static Prowl.PaperUI.PaperKey? MapKeyOrNull(KeyCodes key) =>
        key switch
        {
            KeyCodes.Space => Prowl.PaperUI.PaperKey.Space,
            KeyCodes.Escape => Prowl.PaperUI.PaperKey.Escape,
            KeyCodes.Enter => Prowl.PaperUI.PaperKey.Enter,
            KeyCodes.Tab => Prowl.PaperUI.PaperKey.Tab,
            KeyCodes.Backspace => Prowl.PaperUI.PaperKey.Backspace,
            KeyCodes.Delete => Prowl.PaperUI.PaperKey.Delete,
            KeyCodes.Left => Prowl.PaperUI.PaperKey.Left,
            KeyCodes.Right => Prowl.PaperUI.PaperKey.Right,
            KeyCodes.Up => Prowl.PaperUI.PaperKey.Up,
            KeyCodes.Down => Prowl.PaperUI.PaperKey.Down,
            KeyCodes.LeftShift => Prowl.PaperUI.PaperKey.LeftShift,
            KeyCodes.RightShift => Prowl.PaperUI.PaperKey.RightShift,
            KeyCodes.LeftControl => Prowl.PaperUI.PaperKey.LeftControl,
            KeyCodes.RightControl => Prowl.PaperUI.PaperKey.RightControl,
            KeyCodes.LeftAlt => Prowl.PaperUI.PaperKey.LeftAlt,
            KeyCodes.RightAlt => Prowl.PaperUI.PaperKey.RightAlt,
            >= KeyCodes.A and <= KeyCodes.Z => (Prowl.PaperUI.PaperKey)((int)Prowl.PaperUI.PaperKey.A +
                                                                        (key - KeyCodes.A)),
            _ => null
        };
}