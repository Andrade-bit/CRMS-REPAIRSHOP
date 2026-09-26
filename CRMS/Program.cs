using System;
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
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.ToString(), "Unhandled UI Exception");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show((e.ExceptionObject as Exception)?.ToString() ?? "Unknown error",
                    "Unhandled Exception");

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
                    var form = new AdminMainForm(loginResult.Token, loginResult.CompanyId);
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
    }
}
