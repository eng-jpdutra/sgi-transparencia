namespace SGI.Api.Contratos.Pessoas;

/// <summary>
/// ENTRADA do cadastro unificado: dados civis da pessoa + indicação
/// de quais fichas de papel criar. Quando ehVereador é true, o
/// nomeLegislativo passa a ser obrigatório (validado na rota).
///
/// Anuláveis para validarmos com Fail Fast, conforme o padrão.
/// </summary>
public record PessoaEntrada(
    string? NomeCompleto,
    string? Matricula,
    bool EhServidor,
    bool EhVereador,
    string? NomeLegislativo);

/// <summary>Resumo de um papel para exibição.</summary>
public record PapelSaida(int Id, bool Ativo);

/// <summary>Resumo do papel vereador (inclui nome legislativo).</summary>
public record VereadorSaida(int Id, bool Ativo, string NomeLegislativo);

/// <summary>
/// SAÍDA — a pessoa com seus papéis. As fichas vêm nulas quando a
/// pessoa não as possui, permitindo ao frontend saber o que existe.
/// </summary>
public record PessoaSaida(
    int Id,
    string NomeCompleto,
    string Matricula,
    bool Ativo,
    PapelSaida? Servidor,
    VereadorSaida? Vereador);
