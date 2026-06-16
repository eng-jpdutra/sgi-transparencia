namespace SGI.Api.Contratos.Pessoas;

/// <summary>
/// ENTRADA do cadastro unificado de ADMISSÃO. Cria, numa só transação:
///   - a Pessoa (dados civis),
///   - a ficha do papel escolhido (Servidor ou Vereador),
///   - o exercício inicial (Vínculo, se servidor; Mandato, se vereador).
///
/// O campo Tipo decide qual caminho seguir ("servidor" | "vereador").
/// Os blocos abaixo são preenchidos conforme o tipo; o que não se
/// aplica vem nulo e é ignorado. Validação Fail Fast na rota.
/// </summary>
public record AdmissaoEntrada(
    // ----- Dados civis (sempre) -----
    string? NomeCompleto,
    string? Matricula,

    // ----- Papel escolhido -----
    string? Tipo, // "servidor" | "vereador"

    // ----- Se vereador: nome legislativo da ficha -----
    string? NomeLegislativo,

    // ----- Exercício de SERVIDOR (vínculo) -----
    int? CargoId,
    int? RegimeId,

    // ----- Exercício de VEREADOR (mandato) -----
    int? LegislaturaId,

    // ----- Período do exercício (comum a vínculo e mandato) -----
    DateOnly? DataInicio,
    DateOnly? DataFim);
