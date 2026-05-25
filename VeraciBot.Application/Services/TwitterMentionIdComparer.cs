namespace VeraciBot.Application.Services
{
    public static class TwitterMentionIdComparer
    {
        public static bool IsGreater(string candidateId, string currentId)
        {
            if (string.IsNullOrWhiteSpace(candidateId))
                return false;

            if (string.IsNullOrWhiteSpace(currentId))
                return true;

            if (ulong.TryParse(candidateId, out var candidate)
                && ulong.TryParse(currentId, out var current))
            {
                return candidate > current;
            }

            return string.CompareOrdinal(candidateId, currentId) > 0;
        }
    }
}
