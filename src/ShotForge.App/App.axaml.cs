using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using ShotForge.App.Services;
using ShotForge.App.ViewModels;
using ShotForge.App.Views;
using ShotForge.Data;

namespace ShotForge.App
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            // Apply the remembered Light/Dark choice before any window is created,
            // so the app never flashes the wrong theme on launch.
            ThemeService.Apply(ThemeService.LoadIsDark());

            // Ensure the local SQLite database exists with a working
            // schema - and self-heal if it doesn't.
            //
            // EnsureCreated() only checks whether the .db FILE exists,
            // not whether it actually has tables in it. If a previous
            // run left behind an empty/broken file (as happened during
            // development here), EnsureCreated() sees the file, assumes
            // the schema is already there, and does nothing - silently
            // leaving the app pointed at a tableless database.
            //
            // So: actually try a real query against it first. If that
            // fails, the file is broken - delete it and rebuild fresh.
            // This means a broken database fixes itself on next launch,
            // with no manual file deletion ever required.
            using (var db = new ShotForgeDbContext())
            {
                var dbPath = ShotForgeDbContext.GetDatabasePath();
                var isBroken = false;

                if (System.IO.File.Exists(dbPath))
                {
                    try
                    {
                        _ = db.Projects.Any();
                    }
                    catch
                    {
                        isBroken = true;
                    }
                }

                if (isBroken)
                {
                    db.Database.CloseConnection();
                    System.IO.File.Delete(dbPath);
                }

                db.Database.EnsureCreated();
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var licenseService = new LicenseService();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(licenseService)
                };

                // Quiet background re-check of a Pro key (at most once a week, only
                // if online). Runs on the UI thread's context so any tier change
                // updates the screen safely. Never blocks or interrupts startup.
                _ = licenseService.RevalidateIfDueAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
