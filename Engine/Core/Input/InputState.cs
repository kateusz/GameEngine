namespace Engine.Core.Input;

public class InputState
{
    private static InputState? _instance;
        
    public static InputState Instance
    {
        get
        {
            if (_instance is not null) 
                return _instance;
                
            _instance = new InputState();
            return _instance;
        }
    }

    public IKeyboardState Keyboard { get; set; } = null!;
    public IMouseState Mouse { get; set; } = null!;

    public static void Init()
    {
        Instance.Keyboard = KeyboardStateFactory.Create();
        Instance.Mouse = MouseStateFactory.Create();
    }
}