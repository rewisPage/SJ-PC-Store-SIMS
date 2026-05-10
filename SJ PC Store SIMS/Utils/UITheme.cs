using System.Drawing;

namespace SJ_PC_Store_SIMS.Utils
{
    public static class UITheme
    {
        public static bool IsDarkMode = false;

        // Brand Colors
        public static Color PrimaryDark = ColorTranslator.FromHtml("#0A2440");
        public static Color SecondaryDark = ColorTranslator.FromHtml("#1B4F72");
        public static Color AccentYellow = ColorTranslator.FromHtml("#FFD24A");
        public static Color MutedText = ColorTranslator.FromHtml("#A0AAB2");

        // Dark Mode
        public static Color DarkWorkspace = ColorTranslator.FromHtml("#1e1d22");
        public static Color DarkPanel = ColorTranslator.FromHtml("#2D2A32"); // The container color
        public static Color DarkText = ColorTranslator.FromHtml("#F7F9FC");
        public static Color DarkInputBg = ColorTranslator.FromHtml("#35323A");
        public static Color DarkBorder = ColorTranslator.FromHtml("#4a4852");
        public static Color DarkIcon = ColorTranslator.FromHtml("#A0AAB2");

        // Light Mode
        public static Color LightWorkspace = ColorTranslator.FromHtml("#e9ecef");
        public static Color LightPanel = ColorTranslator.FromHtml("#ffffff");
        public static Color LightText = ColorTranslator.FromHtml("#111111");
        public static Color LightInputBg = ColorTranslator.FromHtml("#f8f9fa");
        public static Color LightBorder = ColorTranslator.FromHtml("#ced4da");
        public static Color LightIcon = ColorTranslator.FromHtml("#495057");

        // Dynamic Getters
        public static Color CurrentWorkspace => IsDarkMode ? DarkWorkspace : LightWorkspace;
        public static Color CurrentPanel => IsDarkMode ? DarkPanel : LightPanel;
        public static Color CurrentText => IsDarkMode ? DarkText : LightText;
        public static Color CurrentInputBg => IsDarkMode ? DarkInputBg : LightInputBg;
        public static Color CurrentBorder => IsDarkMode ? DarkBorder : LightBorder;
        public static Color CurrentIcon => IsDarkMode ? DarkIcon : LightIcon;

        // FINAL FIX: Sidebar uses the exact Container/Panel color in Dark Mode, and Navy in Light Mode
        public static Color CurrentSidebarBg => IsDarkMode ? DarkInputBg : PrimaryDark;

        // Typography 
        public static Font MainFont = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        public static Font InputFont = new Font("Segoe UI", 12F, FontStyle.Regular);
        public static Font HeaderFont = new Font("Segoe UI", 24F, FontStyle.Bold);
        public static Font LabelFont = new Font("Segoe UI", 9F, FontStyle.Bold);

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
    }
}