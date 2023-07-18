namespace Engine;

public class Core
{
    public static Application CreateApplication()
    {
        var windowProps = new WindowProps("Game Engine testing!", 1280, 720);
        return new Application(new Window(windowProps));
    }
}