namespace SGI.Api.Contratos.Regimes;

/// <summary>Entrada para criar/editar um regime (anulável: validamos na rota).</summary>
public record RegimeEntrada(string? Nome);

/// <summary>Saída — DTO dedicado (não a entidade do EF).</summary>
public record RegimeSaida(int Id, string Nome, bool Ativo);
