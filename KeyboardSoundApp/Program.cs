namespace KeyboardSoundApp;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Logger.Log("=== Application Starting ===");
        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Logger.Log("Application configuration initialized");
            
            Logger.Log("Creating MainForm...");
            Application.Run(new MainForm());
            Logger.Log("Application run completed");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal error in Main", ex);
            throw;
        }
    }    
}