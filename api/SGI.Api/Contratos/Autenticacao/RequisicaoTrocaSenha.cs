namespace SGI.Api.Contratos.Autenticacao;

/// <summary>
/// Corpo do POST /autenticacao/trocar-senha.
/// Exigir a senha ATUAL é defesa em profundidade: mesmo que alguém
/// roube um access token válido (15 min de janela), não consegue
/// trocar a senha e sequestrar a conta sem conhecer a senha vigente.
/// </summary>
public record RequisicaoTrocaSenha(string? SenhaAtual, string? NovaSenha);
