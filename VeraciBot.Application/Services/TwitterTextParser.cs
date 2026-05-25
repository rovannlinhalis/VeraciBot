using System.Text.RegularExpressions;

namespace VeraciBot.Application.External
{
    public static partial class TwitterTextParser
    {
        public static string RemoveReferences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var result = UserReferenceRegex().Replace(text, string.Empty).Trim();
            result = MultipleSpacesRegex().Replace(result, " ");

            return result.Trim();
        }

        public static string[] FindUsersReference(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            return UserReferenceRegex()
                .Matches(text)
                .Select(match => match.Value)
                .ToArray();
        }

        [GeneratedRegex(@"@\w+", RegexOptions.Compiled)]
        private static partial Regex UserReferenceRegex();

        [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
        private static partial Regex MultipleSpacesRegex();
    }
}
