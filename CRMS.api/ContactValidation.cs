using System.Text.RegularExpressions;

namespace CRMS.api
{
    /// <summary>
    /// Shared name/phone format checks — the same rules the WinForms client
    /// blocks at the keyboard (RoundedTextBox.InputMode), enforced again here
    /// so a raw API call can't bypass them.
    /// </summary>
    public static class ContactValidation
    {
        private static readonly Regex NamePattern = new(@"^[A-Za-z][A-Za-z\s'.-]*$");
        private static readonly Regex PhonePattern = new(@"^[0-9][0-9\s\-+()]*$");

        /// <summary>True for an empty/whitespace string too — pair with an
        /// explicit required check where the field isn't optional.</summary>
        public static bool IsValidNameIfPresent(string? value) =>
            string.IsNullOrWhiteSpace(value) || NamePattern.IsMatch(value);

        public static bool IsValidPhoneIfPresent(string? value) =>
            string.IsNullOrWhiteSpace(value) || PhonePattern.IsMatch(value);
    }
}
