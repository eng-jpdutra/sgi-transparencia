namespace SGI.Api.Contratos.Legislaturas;

/// <summary>
/// DADOS DE SAÍDA — inclui os campos derivados já calculados, para o
/// frontend apenas exibir (sem precisar recalcular nada). É a entidade
/// "achatada" num contrato estável e independente do EF.
/// </summary>
public record LegislaturaSaida(
    int Id,
    int Numero,
    int AnoInicio,
    int AnoFim,
    string Nome,
    DateOnly DataInicio,
    DateOnly DataFim,
    bool Ativo);

/// <summary>
/// PREVIEW da próxima legislatura — o que SERÁ criado, calculado sem
/// persistir. Alimenta a tela de confirmação ("será criada a 23ª...").
/// Não tem Id nem Ativo: ainda não existe.
/// </summary>
public record ProximaLegislatura(
    int Numero,
    int AnoInicio,
    int AnoFim,
    string Nome,
    DateOnly DataInicio,
    DateOnly DataFim);
