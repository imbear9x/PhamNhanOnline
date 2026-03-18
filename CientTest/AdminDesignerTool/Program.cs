namespace AdminDesignerTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) => ShowFatalError(eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                ShowFatalError(exception);
                return;
            }

            ShowFatalError(new Exception("Unknown unhandled exception."));
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private static void ShowFatalError(Exception exception)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "admin-designer-tool-error.log");
            var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(logPath, content);

            MessageBox.Show(
                $"Tool bi loi va da ghi log tai:{Environment.NewLine}{logPath}{Environment.NewLine}{Environment.NewLine}{exception}",
                "Admin Designer Tool Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            MessageBox.Show(
                exception.ToString(),
                "Admin Designer Tool Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
