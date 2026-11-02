using Swapping.Api.Profiles;

namespace Swapping.Api.Tests.Profiles;

public class ProfileNamesTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("Ana")]
    [InlineData("abcdefghijklmnop")] // 16
    [InlineData("Player_01")]
    [InlineData("x-y-z")]
    [InlineData("A1_")]
    public void Valid_names(string name)
    {
        Assert.True(ProfileNames.TryNormalize(name, out var normalized));
        Assert.Equal(name, normalized);
    }

    [Theory]
    [InlineData("ab")]                 // 2
    [InlineData("abcdefghijklmnopq")]  // 17
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ana Maria")]          // space
    [InlineData("Zoë")]                // accent (A1)
    [InlineData("João")]
    [InlineData("Аna")]                // Cyrillic А that looks like a Latin A
    [InlineData("[b]x[/b]")]           // Godot markup (S9)
    [InlineData("<i>x")]
    [InlineData("a b")]
    [InlineData("Robert'); DROP TABLE Profiles;--")]
    [InlineData("tab\tname")]
    public void Invalid_names(string name)
    {
        Assert.False(ProfileNames.TryNormalize(name, out _));
    }

    [Fact]
    public void Null_is_invalid()
    {
        Assert.False(ProfileNames.TryNormalize(null, out _));
    }

    [Fact]
    public void Leading_and_trailing_spaces_are_trimmed()
    {
        Assert.True(ProfileNames.TryNormalize("  Ana  ", out var normalized));
        Assert.Equal("Ana", normalized);
    }

    [Fact]
    public void Length_is_counted_after_trimming()
    {
        Assert.True(ProfileNames.TryNormalize("  abcdefghijklmnop  ", out _));
        Assert.False(ProfileNames.TryNormalize("  ab  ", out _));
    }

    [Fact]
    public void The_key_ignores_case()
    {
        Assert.Equal(ProfileNames.Key("Ana"), ProfileNames.Key("ANA"));
        Assert.Equal(ProfileNames.Key("ana"), ProfileNames.Key("aNa"));
        Assert.NotEqual(ProfileNames.Key("Ana"), ProfileNames.Key("Ana_"));
    }
}
