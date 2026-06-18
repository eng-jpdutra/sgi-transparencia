namespace SGI.Api.Contratos.Vinculos;

/// <summary>
/// ENTRADA para criar/editar um vínculo. Referencia o servidor, o
/// cargo e o regime por id; período com fim opcional. Anuláveis para
/// validação Fail Fast na rota.
/// </summary>
public record VinculoEntrada(
    int? ServidorId,
    int? CargoId,
    int? RegimeId,
    string? Matricula,
    DateOnly? DataInicio,
    DateOnly? DataFim);

/// <summary>
/// SAÍDA — o vínculo com os nomes já resolvidos (cargo, regime,
/// pessoa), para o frontend exibir sem buscas adicionais.
/// </summary>
public record VinculoSaida(
    int Id,
    int ServidorId,
    int PessoaId,
    string PessoaNome,
    int CargoId,
    string CargoNome,
    int RegimeId,
    string RegimeNome,
    string Matricula,
    DateOnly DataInicio,
    DateOnly? DataFim,
    bool Vigente,   // derivado: DataFim == null
    bool Ativo);
