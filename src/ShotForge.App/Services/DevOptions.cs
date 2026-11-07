using System;
using System.Linq;

namespace ShotForge.App.Services
{
    /// <summary>
    /// Developer-only startup switches, read from the command line. Compiled to
    /// do nothing in Release builds, so none of this can ship in the installer.
    ///
    ///   dotnet run --project src/ShotForge.App -- --force-pro
    ///
    /// (In Visual Studio: project Debug Properties > "Command line arguments".)
    /// </summary>
    public static class DevOptions
    {
        public static bool ForceProRequested { get; private set; }

        public static void Initialize(string[] args)
        {
#if DEBUG
            ForceProRequested = args.Any(a =>
                string.Equals(a.Trim(), "--force-pro", StringComparison.OrdinalIgnoreCase));
#endif
        }
    }
}
