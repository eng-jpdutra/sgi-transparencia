namespace SGI.Api.Contratos.Cargos;

/// <summary>Entrada para criar/editar um cargo (anulável: validamos na rota).</summary>
public record CargoEntrada(string? Nome);

/// <summary>Saída — DTO dedicado (não a entidade do EF).</summary>
public record CargoSaida(int Id, string Nome, bool Ativo);
