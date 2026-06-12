namespace SGI.Api.Contratos.Autenticacao;

/// <summary>
/// Corpo do POST /autenticacao/renovar e do POST /autenticacao/sair.
/// Carrega apenas o token de renovação — neste fluxo, POSSUIR o
/// token É a prova de identidade (por isso /renovar é anônimo:
/// quem o chama está justamente sem access token válido).
/// </summary>
public record RequisicaoRenovacao(string? TokenRenovacao);
