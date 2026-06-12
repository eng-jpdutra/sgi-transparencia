namespace SGI.Api.Contratos.Autenticacao;

/// <summary>
/// Resposta do login bem-sucedido.
///
/// Repare no que ela NÃO contém: nada da entidade Usuario (nem o
/// hash, nem contadores internos). DTO de saída expõe APENAS o que
/// o cliente precisa — diretriz: entidades do EF jamais saem pela API.
/// </summary>
/// <param name="TokenAcesso">
///   O JWT. O frontend o enviará em toda requisição no cabeçalho:
///   Authorization: Bearer {token}
/// </param>
/// <param name="ExpiraEmUtc">
///   Quando o token vence (UTC). Permite ao frontend renovar
///   proativamente, em vez de descobrir a expiração levando um 401.
/// </param>
/// <param name="DeveTrocarSenha">
///   true = senha provisória em uso; o frontend deve forçar o
///   fluxo de troca de senha antes de liberar o sistema.
/// </param>
public record RespostaLogin(string TokenAcesso, DateTime ExpiraEmUtc, bool DeveTrocarSenha);
