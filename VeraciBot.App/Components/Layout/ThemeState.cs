namespace VeraciBot.App.Components.Layout
{
    public sealed class ThemeState
    {
        public const string DarkTheme = "dark";
        public const string LightTheme = "light";

        private string currentTheme = LightTheme;

        public event Action Changed = delegate { };

        public string CurrentTheme => currentTheme;

        public bool IsDarkMode => currentTheme == DarkTheme;

        public void SetTheme(string theme)
        {
            var normalizedTheme = theme == DarkTheme ? DarkTheme : LightTheme;

            if (currentTheme == normalizedTheme)
            {
                return;
            }

            currentTheme = normalizedTheme;
            Changed();
        }
    }
}
