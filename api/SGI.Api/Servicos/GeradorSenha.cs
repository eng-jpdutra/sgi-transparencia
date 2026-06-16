using System.Security.Cryptography;

namespace SGI.Api.Servicos;

/// <summary>
/// Gera senhas provisórias aleatórias e legíveis para novos usuários.
/// Usa um gerador CRIPTOGRÁFICO (RandomNumberGenerator), não o Random
/// comum — senha previsível seria uma falha de segurança.
///
/// O resultado mistura letras (sem caracteres ambíguos como O/0, l/1)
/// e dígitos, num tamanho que satisfaz a política mínima (8+).
/// </summary>
public static class GeradorSenha
{
    // Alfabeto sem caracteres ambíguos, para o usuário não confundir
    // ao digitar a senha provisória que o Admin lhe passou.
    private const string Alfabeto =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public static string Gerar(int tamanho = 10)
    {
        var caracteres = new char[tamanho];
        for (var i = 0; i < tamanho; i++)
        {
            // GetInt32 sorteia um índice de forma criptograficamente segura.
            var indice = RandomNumberGenerator.GetInt32(Alfabeto.Length);
            caracteres[i] = Alfabeto[indice];
        }
        return new string(caracteres);
    }
}
