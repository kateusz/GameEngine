namespace Sandbox;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var app = new SandboxApplication();
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }
}