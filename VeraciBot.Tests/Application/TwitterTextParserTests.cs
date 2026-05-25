using FluentAssertions;
using VeraciBot.Application.External;

namespace VeraciBot.Tests.Application
{
    public class TwitterTextParserTests
    {
        [Fact]
        public void RemoveReferences_ShouldRemoveMentionsAndNormalizeSpaces()
        {
            var result = TwitterTextParser.RemoveReferences("@bot avalie isso @user agora");

            result.Should().Be("avalie isso agora");
        }

        [Fact]
        public void FindUsersReference_ShouldReturnMentionTokens()
        {
            var users = TwitterTextParser.FindUsersReference("oi @first e @second_2");

            users.Should().Equal("@first", "@second_2");
        }
    }
}
