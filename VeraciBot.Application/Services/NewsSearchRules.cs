using System.Text.RegularExpressions;

namespace VeraciBot.Application.External
{
    public sealed record NewsSearchTweet(string AuthorId, string Text);

    public static partial class NewsSearchRules
    {
        private static readonly string[] ForceSearchMarkers =
        [
            "!avaliar",
            "!falso",
            "avaliar",
            "avalia",
            "avalie",
            "verdade",
            "verdadeiro",
            "falso",
            "mentira",
            "fake",
            "noticia",
            "not\u00edcia",
            "fonte",
            "fontes",
            "checar",
            "cheque",
            "verificar",
            "verifique",
            "confere",
            "boato"
        ];

        public static bool ShouldForceNewsSearch(string mentionText, string threadDialog = "")
        {
            var content = NormalizeSearchText($"{mentionText}\n{threadDialog}");

            return !string.IsNullOrWhiteSpace(content)
                && ForceSearchMarkers.Any(marker => content.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }

        public static string BuildNewsSearchQuery(
            string mentionText,
            IEnumerable<NewsSearchTweet> threadTweets,
            string botUserId,
            IEnumerable<string> fallbackThreadTexts = null)
        {
            var cleanedMention = CleanNewsSearchCandidate(mentionText);
            var threadCandidates = (threadTweets ?? [])
                .Where(tweet => !string.Equals(tweet.AuthorId, botUserId, StringComparison.Ordinal))
                .Select(tweet => CleanNewsSearchCandidate(tweet.Text))
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Where(candidate => !AreEquivalentSearchTexts(candidate, cleanedMention))
                .ToArray();

            var bestThreadCandidate = SelectBestNewsSearchCandidate(threadCandidates);
            var query = !string.IsNullOrWhiteSpace(bestThreadCandidate)
                ? bestThreadCandidate
                : cleanedMention;

            if (string.IsNullOrWhiteSpace(query))
            {
                query = SelectBestNewsSearchCandidate(
                    (fallbackThreadTexts ?? []).Select(CleanNewsSearchCandidate).ToArray());
            }

            return TruncateForPrompt(query, 220);
        }

        public static string CleanNewsSearchCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = TwitterTextParser.RemoveReferences(value);
            cleaned = NewsSearchUrlRegex().Replace(cleaned, " ");
            cleaned = NewsSearchCommandRegex().Replace(cleaned, " ");
            cleaned = NewsSearchRequestRegex().Replace(cleaned, " ");
            cleaned = NewsSearchRequestPrefixRegex().Replace(cleaned, " ");
            cleaned = cleaned
                .Replace("?", " ", StringComparison.Ordinal)
                .Replace("!", " ", StringComparison.Ordinal);

            return NormalizeSearchText(cleaned);
        }

        public static string SelectBestNewsSearchCandidate(IReadOnlyCollection<string> candidates)
        {
            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .OrderBy(LooksLikeOnlyFactCheckRequest)
                .ThenByDescending(candidate => candidate.Length)
                .FirstOrDefault() ?? string.Empty;
        }

        public static bool LooksLikeOnlyFactCheckRequest(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var cleaned = CleanNewsSearchCandidate(value);
            return cleaned.Length < 12;
        }

        public static bool AreEquivalentSearchTexts(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return NormalizeSearchText(left).Equals(NormalizeSearchText(right), StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeSearchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(
                ' ',
                value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        public static string TruncateForPrompt(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;

            return trimmed[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
        }

        [GeneratedRegex(@"(^|\s)!(avaliar|falso|verificar|checar)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex NewsSearchCommandRegex();

        [GeneratedRegex(@"\b(pode\s+)?(avaliar|avalia|avalie|verificar|verifique|checar|cheque|confere|procede|isso\s+[e\u00e9]\s+(verdade|falso|fake|mentira)|essa?\s+not[i\u00ed]cia\s+[e\u00e9]\s+(verdade|falsa|fake)|verdadeiro\s+ou\s+falso)\b[?.!,;:]*", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex NewsSearchRequestRegex();

        [GeneratedRegex(@"^\s*(sobre|analise|analisa|olha|veja|diz\s+se|me\s+diz\s+se|quero\s+saber\s+se|essa?\s+not[i\u00ed]cia|esta?\s+not[i\u00ed]cia|not[i\u00ed]cia)\b[?.!,;:\-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex NewsSearchRequestPrefixRegex();

        [GeneratedRegex(@"(?:https?://|www\.)[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex NewsSearchUrlRegex();
    }
}
