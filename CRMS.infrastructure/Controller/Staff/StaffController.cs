using System;

namespace CRMS.infrastructure.Controller.Staff
{
    public class StaffController
    {
        private string currentPage;

        public StaffController()
        {
            currentPage = "Customer";
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