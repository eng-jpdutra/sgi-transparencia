using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SGI.Api.Dominio.Autenticacao;

namespace SGI.Api.Servicos;

/// <summary>
/// Responsável por UMA coisa (princípio S do SOLID): gerar tokens
/// de acesso JWT. Quem valida tokens é o pipeline (Program.cs);
/// quem decide SE o usuário merece um token é a rota de login.
///
/// Anatomia de um JWT (3 partes separadas por ponto):
///   cabeçalho.corpo.assinatura
/// O corpo carrega as "claims" — afirmações sobre o portador
/// (quem é, quais papéis tem, até quando vale). A assinatura,
/// gerada com a chave secreta, garante que NINGUÉM alterou o corpo:
/// mudou uma vírgula, a assinatura não bate e o token é rejeitado.
/// ATENÇÃO: o corpo é legível por qualquer um (é só Base64) —
/// JWT garante INTEGRIDADE, não sigilo. Nunca colocar dado
/// sensível em claim.
/// </summary>
public class ServicoToken
{
    private readonly string _emissor;
    private readonly string _publico;
    private readonly SymmetricSecurityKey _chave;
    private readonly int _expiracaoMinutos;

    /// <summary>
    /// Lê e valida a configuração UMA vez, na inicialização.
    /// FAIL FAST: configuração ausente ou fraca = aplicação não sobe.
    /// Melhor falhar na partida, com mensagem clara, do que emitir
    /// tokens inválidos (ou inseguros) em produção.
    /// </summary>
    public ServicoToken(IConfiguration configuracao)
    {
        _emissor = configuracao["Jwt:Emissor"]
            ?? throw new InvalidOperationException("Configuração 'Jwt:Emissor' ausente.");

        _publico = configuracao["Jwt:Publico"]
            ?? throw new InvalidOperationException("Configuração 'Jwt:Publico' ausente.");

        var chaveSecreta = configuracao["Jwt:ChaveSecreta"]
            ?? throw new InvalidOperationException(
                "Configuração 'Jwt:ChaveSecreta' ausente. Em DEV ela vem do " +
                "appsettings.Development.json; em PROD, da variável de " +
                "ambiente Jwt__ChaveSecreta. NUNCA do código.");

        // HMAC-SHA256 exige chave de no mínimo 256 bits (32 caracteres).
        // Chave curta = assinatura quebrável por força bruta.
        if (chaveSecreta.Length < 32)
            throw new InvalidOperationException(
                "'Jwt:ChaveSecreta' precisa ter no mínimo 32 caracteres.");

        _chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta));
        _expiracaoMinutos = configuracao.GetValue("Jwt:ExpiracaoMinutos", 15);
    }

    /// <summary>
    /// Gera o token de acesso de um usuário já autenticado.
    /// (Conferir a senha NÃO é papel deste método — quando ele é
    /// chamado, a identidade já foi provada.)
    /// </summary>
    public (string Token, DateTime ExpiraEmUtc) GerarTokenAcesso(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            // sub (subject): o identificador do dono do token.
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),

            // unique_name: o login — vira o "nome" do usuário no pipeline.
            new(JwtRegisteredClaimNames.UniqueName, usuario.Login),

            // jti (JWT id): identificador único DESTE token.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // RBAC: uma claim de role por perfil do usuário.
        // É ESTA lista que o RequireAuthorization("...") dos endpoints
        // consultará — sem ir ao banco, pois ela viaja dentro do token.
        // (Os perfis inativos nem chegam aqui: o query filter global
        // do ContextoDados já os filtrou na consulta do login.)
        foreach (var usuarioPerfil in usuario.Perfis)
            claims.Add(new Claim(ClaimTypes.Role, usuarioPerfil.Perfil.Nome));

        var expiraEmUtc = DateTime.UtcNow.AddMinutes(_expiracaoMinutos);

        var token = new JwtSecurityToken(
            issuer: _emissor,                 // quem emitiu
            audience: _publico,               // para quem
            claims: claims,                   // as afirmações
            expires: expiraEmUtc,             // validade curta
            signingCredentials: new SigningCredentials(
                _chave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEmUtc);
    }
}
