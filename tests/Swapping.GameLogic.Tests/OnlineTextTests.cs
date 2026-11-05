using Swapping.GameLogic.Online;

namespace Swapping.GameLogic.Tests;

/// <summary>What the menu says when the server answers with an error (or does not answer).</summary>
public sealed class OnlineTextTests
{
    [Theory]
    [InlineData(0, "Offline", "servidor fora do ar")]
    [InlineData(401, "InvalidCredentials", "nome ou código errado")]
    [InlineData(401, "InvalidToken", "sessão expirada: entre de novo")]
    [InlineData(423, "ProfileLocked", "muitas tentativas: espere um pouco")]
    [InlineData(409, "NameTaken", "esse nome já existe")]
    [InlineData(400, "InvalidName", "nome inválido: 3 a 16 letras, números, _ ou -")]
    [InlineData(409, "LevelLocked", "termine o nível anterior nesta conta")]
    [InlineData(422, "ImplausibleResult", "resultado recusado pelo servidor")]
    [InlineData(429, "TooManyRequests", "muitas tentativas: espere um pouco")]
    [InlineData(500, "Whatever", "erro no servidor (500)")]
    public void Every_error_has_a_short_message(int status, string code, string text)
    {
        Assert.Equal(text, OnlineText.Error(status, code));
    }
}
