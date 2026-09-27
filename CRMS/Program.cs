using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using CRMS.winforms; // Login lives here (lowercase "winforms", unlike this file's own namespace)
using CRMS.Winforms.Admin;
using CRMS.winforms.Api;
using CRMS.winforms.ManagerView;
using CRMS.winforms.StaffView;
using CRMS.winforms.SuperAdmin;

namespace CRMS.Winforms
{
    internal static class Program
    {
        // Only set when this process itself started the API (the installed
        // app's own bundled copy under .\api\) — never touched, and never
        // killed on exit, when the API was already running on its own (the
        // usual case during development via `dotnet run`).
        private static Process? _apiProcess;

        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.ToString(), "Unhandled UI Exception");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show((e.ExceptionObject as Exception)?.ToString() ?? "Unknown error",
                    "Unhandled Exception");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => StopBundledApi();

            StartBundledApiIfPresent();

            ApplicationConfiguration.Initialize();

            // Runs the Login screen, then that role's shell, and loops back to
            // Login again after a Logout instead of exiting. Closing the shell
            // any other way (the window's X button) ends the loop and the app.
            bool keepRunning = true;
            while (keepRunning)
            {
                // Login Flow — signs in against CRMS.api's /api/auth/login, then opens
                // the Staff/Admin/Manager/Superadmin shell depending on the account's role.
                LoginResult? loginResult = null;

                using (var login = new Login())
                {
                    login.SignInRequested += async (s, e) =>
                    {
                        login.SetBusy(true);
                        var client = new AuthApiClient();
                        var result = await client.LoginAsync(e.Username, e.Password);
                        login.SetBusy(false);

                        if (result == null)
                        {
                            login.ShowError("Invalid username or password.");
                            return;
                        }

                        loginResult = result;
                        login.DialogResult = DialogResult.OK;
                        login.Close();
                    };

                    login.ForgotPasswordRequested += (s, e) => { };
                    var dialogResult = login.ShowDialog();

                    if (dialogResult != DialogResult.OK || loginResult == null)
                        return;
                }

                if (loginResult.Roles.Contains("Superadmin"))
                {
                    var form = new SuperAdminMainForm(loginResult.FullName, loginResult.Token);
                    Application.Run(form);
                    keepRunning = form.LoggedOut;
                }
                else if (loginResult.Roles.Contains("Admin"))
                {
                    var form = new AdminMainForm(loginResult.FullName, loginResult.Token, loginResult.CompanyId);
                    Application.Run(form);
                    keepRunning = form.LoggedOut;
                }
                else if (loginResult.Roles.Contains("Staff"))
                {
                    var form = new StaffMainForm(loginResult.FullName, loginResult.Token, loginResult.CompanyId);
                    Application.Run(form);
                    keepRunning = form.LoggedOut;
                }
                else if (loginResult.Roles.Contains("Manager"))
                {
                    var form = new ManagerMainForm(loginResult.FullName, loginResult.Token, loginResult.CompanyId);
                    Application.Run(form);
                    keepRunning = form.LoggedOut;
                }
                else
                {
                    MessageBox.Show("This account's role is not yet supported in this app.");
                    keepRunning = false;
                }
            }
        }

        /// <summary>Only the installed app has a bundled .\api\CRMS.api.exe
        /// next to this one (see the Inno Setup script) — during development
        /// there's no such folder, so this quietly does nothing and the API
        /// keeps being started separately via `dotnet run`, same as always.</summary>
        private static void StartBundledApiIfPresent()
        {
            string apiExePath = Path.Combine(AppContext.BaseDirectory, "api", "CRMS.api.exe");
            if (!File.Exists(apiExePath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = apiExePath,
                WorkingDirectory = Path.GetDirectoryName(apiExePath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // Without this, the published exe defaults to "Production" and
            // reads appsettings.json — a different, essentially-empty
            // MasterCRMS database than the one every real account was ever
            // created in. Forcing Development makes it load
            // appsettings.Development.json instead: the same local
            // databases (and same accounts) the dev workflow has always used.
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

            _apiProcess = Process.Start(startInfo);

            WaitForApiReady();
        }

        /// <summary>Blocks briefly while the just-started API finishes binding
        /// its port — without this, the Login screen's first sign-in attempt
        /// would race a Kestrel process that hasn't opened its socket yet.</summary>
        private static void WaitForApiReady()
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            for (int attempt = 0; attempt < 40; attempt++)
            {
                try
                {
                    // Any response at all — even a 404 — means Kestrel is up
                    // and listening; this route doesn't need to exist.
                    http.GetAsync("http://localhost:5029/").GetAwaiter().GetResult();
                    return;
                }
                catch
                {
                    Thread.Sleep(250);
                }
            }
        }

        private static void StopBundledApi()
        {
            try
            {
                if (_apiProcess != null && !_apiProcess.HasExited)
                    _apiProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort — the OS reclaims it on shutdown either way.
            }
        }
    }
}
