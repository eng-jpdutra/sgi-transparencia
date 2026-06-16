namespace SGI.Api.Contratos.Usuarios;

/// <summary>
/// DADOS DE ENTRADA para criar um usuário. O Admin informa apenas o
/// login e os perfis; a SENHA NÃO vem daqui — é gerada pelo sistema
/// (provisória) e devolvida uma única vez na resposta da criação.
/// </summary>
public record UsuarioCriacao(string? Login, int[]? PerfilIds);

/// <summary>
/// DADOS DE ENTRADA para editar um usuário: troca-se o conjunto de
/// perfis. (O login é imutável após a criação — é o identificador.)
/// </summary>
public record UsuarioEdicao(int[]? PerfilIds);

/// <summary>
/// DADOS DE SAÍDA — o que a API devolve sobre um usuário.
/// NUNCA inclui senha nem hash (diretriz inegociável). Inclui os
/// perfis para exibição na listagem.
/// </summary>
public record UsuarioSaida(
    int Id,
    string Login,
    bool DeveTrocarSenha,
    bool Bloqueado,
    bool Ativo,
    IReadOnlyList<PerfilSaida> Perfis);

/// <summary>
/// Resposta da CRIAÇÃO e do RESET de senha: traz a senha provisória
/// gerada, em texto puro, EXCLUSIVAMENTE nesta resposta (uma única
/// vez). O Admin a repassa ao usuário; ela não fica recuperável depois.
/// </summary>
public record UsuarioCriadoSaida(UsuarioSaida Usuario, string SenhaProvisoria);

/// <summary>DTO de perfil (para listagem e para o seletor do formulário).</summary>
public record PerfilSaida(int Id, string Nome, string? Descricao);
