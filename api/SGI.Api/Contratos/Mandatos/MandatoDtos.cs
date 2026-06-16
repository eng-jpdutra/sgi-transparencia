namespace SGI.Api.Contratos.Mandatos;

/// <summary>ENTRADA para criar/editar mandato (anuláveis: validação na rota).</summary>
public record MandatoEntrada(
    int? VereadorId,
    int? LegislaturaId,
    DateOnly? DataInicio,
    DateOnly? DataFim);

/// <summary>SAÍDA — mandato com nomes resolvidos para exibição.</summary>
public record MandatoSaida(
    int Id,
    int VereadorId,
    int PessoaId,
    string PessoaNome,
    string NomeLegislativo,
    int LegislaturaId,
    string LegislaturaNome,
    DateOnly DataInicio,
    DateOnly? DataFim,
    bool EmCurso,   // derivado: DataFim == null
    bool Ativo);
