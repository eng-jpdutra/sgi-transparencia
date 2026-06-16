namespace SGI.Api.Contratos.Partidos;

/// <summary>
/// DADOS DE ENTRADA para criar/editar um partido.
/// Permissivo no tipo (anuláveis), rigoroso na validação (Fail Fast
/// na rota): o mundo externo manda o que quiser; nós validamos.
/// </summary>
public record PartidoEntrada(
    string? Sigla,
    string? Nome,
    int? Numero);

/// <summary>
/// DADOS DE SAÍDA — o que a API devolve. DTO dedicado, não a entidade
/// do EF (diretriz: entidades não vazam pela API).
/// </summary>
public record PartidoSaida(
    int Id,
    string Sigla,
    string Nome,
    int Numero,
    bool Ativo);
