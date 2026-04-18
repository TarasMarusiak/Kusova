using BureauApp.Data;

namespace BureauApp;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 1. Стандартне налаштування конфігурації
        ApplicationConfiguration.Initialize();

        // 2. ВАЖЛИВО: Викликаємо створення бази даних та таблиць перед запуском форми
        DatabaseHelper.InitializeDatabase();

        // 3. Запускаємо головне вікно
        Application.Run(new Form1());
    }
}