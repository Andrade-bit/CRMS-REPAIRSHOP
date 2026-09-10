using System;

namespace CRMS.api.Admin
{
    public class AdminController
    {
        private string currentPage;

        public AdminController()
        {
            currentPage = "Dashboard";
        }

        // Changes the current page
        public string NavigateTo(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return currentPage;
            }

            currentPage = pageName;

            return currentPage;
        }

        // Gets the current page
        public string GetCurrentPage()
        {
            return currentPage;
        }
    }
}