namespace Engine.Core.Input;

public class InputState
{
    private static bool _initialized;
    private static InputState _instance;
        
    public static InputState Instance
    {
        get
        {
            if (_initialized) 
                return _instance;
                
            _instance = new InputState();
            _initialized = true;
            return _instance;
        }
    }

    public IKeyboardState Keyboard { get; private set; }
    public IMouseState Mouse { get; private set; }

    public static void Init()
    {
        Instance.Keyboard = KeyboardStateFactory.Create();
        Instance.Mouse = MouseStateFactory.Create();
    }
}