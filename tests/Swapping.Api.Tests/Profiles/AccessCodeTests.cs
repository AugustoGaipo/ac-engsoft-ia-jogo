using System.Text.RegularExpressions;
using Swapping.Api.Profiles;

namespace Swapping.Api.Tests.Profiles;

public class AccessCodeTests
{
    private static readonly Regex Format = new("^[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$");

    [Fact]
    public void A_generated_code_has_4_groups_of_4_Crockford_symbols()
    {
        var code = AccessCode.Generate();

        Assert.Matches(Format, code);
    }

    [Fact]
    public void A_thousand_generated_codes_are_all_different_and_well_formed()
    {
        var codes = Enumerable.Range(0, 1000).Select(_ => AccessCode.Generate()).ToList();

        Assert.All(codes, c => Assert.Matches(Format, c));
        Assert.Equal(1000, codes.Distinct().Count());
    }

    [Fact]
    public void Generated_codes_use_every_symbol_of_the_alphabet()
    {
        // 1 000 codes × 16 symbols: missing a symbol would mean broken bit extraction.
        var symbols = Enumerable.Range(0, 1000).SelectMany(_ => AccessCode.Generate().Replace("-", "")).ToHashSet();

        Assert.Equal(32, symbols.Count);
    }

    [Fact]
    public void Normalize_ignores_case_dashes_and_spaces()
    {
        Assert.Equal(AccessCode.Normalize("K7QM-4XRT-9WPA-HC3N"), AccessCode.Normalize("k7qm 4xrt-9wpa-hc3n"));
        Assert.Equal("K7QM4XRT9WPAHC3N", AccessCode.Normalize(" k7qm-4xrt 9wpa-hc3n "));
    }

    [Fact]
    public void Normalize_reads_O_as_zero_and_I_and_L_as_one()
    {
        Assert.Equal("0111", AccessCode.Normalize("OIlL"));
        Assert.Equal("0011", AccessCode.Normalize("ooLl"));
    }

    [Fact]
    public void Normalize_of_null_is_empty()
    {
        Assert.Equal("", AccessCode.Normalize(null));
    }

    [Fact]
    public void Matches_accepts_the_same_code_typed_by_hand()
    {
        var code = AccessCode.Generate();
        var hash = AccessCode.Hash(code);

        var typed = code.ToLowerInvariant().Replace('0', 'o').Replace('1', 'l').Replace("-", " ");

        Assert.True(AccessCode.Matches(typed, hash));
    }

    [Fact]
    public void Matches_refuses_a_different_code_and_null()
    {
        var hash = AccessCode.Hash(AccessCode.Generate());

        Assert.False(AccessCode.Matches(AccessCode.Generate(), hash));
        Assert.False(AccessCode.Matches(null, hash));
        Assert.False(AccessCode.Matches("", hash));
    }

    [Fact]
    public void The_hash_is_SHA_256_of_the_normalized_code_and_never_the_code()
    {
        var hash = AccessCode.Hash("k7qm-4xrt-9wpa-hc3n");

        Assert.Equal(32, hash.Length);
        Assert.Equal(System.Security.Cryptography.SHA256.HashData("K7QM4XRT9WPAHC3N"u8.ToArray()), hash);
    }

    [Theory]
    [InlineData("K7QM-4XRT-9WPA-HC3N", true)]
    [InlineData("k7qm4xrt9wpahc3n", true)]
    [InlineData("K7QM-4XRT-9WPA-HC3", false)]     // 15 symbols
    [InlineData("K7QM-4XRT-9WPA-HC3NN", false)]   // 17 symbols
    [InlineData("K7QM-4XRT-9WPA-HC3U", false)]    // U is not in the alphabet
    [InlineData("K7QM-4XRT-9WPA-HC3!", false)]
    [InlineData("", false)]
    public void IsWellFormed_checks_length_and_alphabet_after_normalizing(string input, bool expected)
    {
        Assert.Equal(expected, AccessCode.IsWellFormed(input));
    }
}
