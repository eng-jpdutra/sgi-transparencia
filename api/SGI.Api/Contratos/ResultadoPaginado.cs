namespace SGI.Api.Contratos;

/// <summary>
/// Resultado paginado padrão de TODA listagem do sistema (diretriz
/// v2.1). Genérico em T para servir a qualquer entidade: legislaturas,
/// vereadores, servidores... um único contrato, reutilizado.
///
/// O frontend usa TotalRegistros para configurar o rowCount do
/// DataGrid (paginação server-side); sem ele, o grid não saberia
/// quantas páginas existem.
/// </summary>
/// <typeparam name="T">O tipo de item da página (geralmente um DTO).</typeparam>
/// <param name="Itens">Os registros DESTA página.</param>
/// <param name="TotalRegistros">Total de registros que satisfazem o
///   filtro (não apenas os desta página).</param>
/// <param name="Pagina">Número da página atual (base 1).</param>
/// <param name="TamanhoPagina">Quantos itens por página.</param>
public record ResultadoPaginado<T>(
    IReadOnlyList<T> Itens,
    int TotalRegistros,
    int Pagina,
    int TamanhoPagina);
