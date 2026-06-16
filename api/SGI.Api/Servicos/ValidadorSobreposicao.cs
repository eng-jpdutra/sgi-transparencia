using Microsoft.EntityFrameworkCore;
using SGI.Api.Persistencia;

namespace SGI.Api.Servicos;

/// <summary>
/// Serviço de validação da regra "uma pessoa nunca exerce dois papéis
/// ao mesmo tempo". Centraliza a lógica de sobreposição temporal que
/// cruza VÍNCULOS (servidor) e MANDATOS (vereador) de uma mesma pessoa.
///
/// Vínculo e Mandato usam este mesmo serviço — assim a regra é idêntica
/// nos dois lados e mora num único lugar (DRY). Tanto faz qual papel
/// se está cadastrando: o serviço reúne TODOS os períodos de exercício
/// da pessoa (de ambas as tabelas) e checa interseção.
/// </summary>
public static class ValidadorSobreposicao
{
    /// <summary>Um período de exercício, agnóstico ao papel de origem.</summary>
    public record Periodo(int? IdRegistro, string Origem, DateOnly Inicio, DateOnly? Fim);

    /// <summary>
    /// Dois períodos se sobrepõem se nenhum termina antes de o outro
    /// começar. Fim NULO = aberto (infinito futuro).
    /// </summary>
    public static bool SeSobrepoe(DateOnly iA, DateOnly? fA, DateOnly iB, DateOnly? fB)
    {
        if (fB is not null && iA > fB) return false;
        if (fA is not null && iB > fA) return false;
        return true;
    }

    /// <summary>
    /// Reúne todos os períodos de exercício de uma pessoa — vínculos e
    /// mandatos — exceto o registro que está sendo editado (para ele
    /// não conflitar consigo mesmo).
    /// </summary>
    /// <param name="ignorarVinculoId">Id de vínculo a ignorar (em edição de vínculo).</param>
    /// <param name="ignorarMandatoId">Id de mandato a ignorar (em edição de mandato).</param>
    public static async Task<List<Periodo>> ColetarPeriodosDaPessoaAsync(
        ContextoDados db, int pessoaId,
        int? ignorarVinculoId = null, int? ignorarMandatoId = null)
    {
        // Vínculos da pessoa (via Servidor -> PessoaId).
        var vinculos = await db.Vinculos
            .Where(v => v.Servidor!.PessoaId == pessoaId && v.Id != ignorarVinculoId)
            .Select(v => new Periodo(v.Id, "vínculo", v.DataInicio, v.DataFim))
            .ToListAsync();

        // Mandatos da pessoa (via Vereador -> PessoaId).
        var mandatos = await db.Mandatos
            .Where(m => m.Vereador!.PessoaId == pessoaId && m.Id != ignorarMandatoId)
            .Select(m => new Periodo(m.Id, "mandato", m.DataInicio, m.DataFim))
            .ToListAsync();

        return vinculos.Concat(mandatos).ToList();
    }

    /// <summary>
    /// Verifica se o período proposto colide com algum exercício
    /// existente da pessoa. Retorna a mensagem de erro (com a origem
    /// do conflito) ou null se estiver livre.
    /// </summary>
    public static async Task<string?> ValidarPeriodoLivreAsync(
        ContextoDados db, int pessoaId,
        DateOnly inicio, DateOnly? fim,
        int? ignorarVinculoId = null, int? ignorarMandatoId = null)
    {
        var periodos = await ColetarPeriodosDaPessoaAsync(
            db, pessoaId, ignorarVinculoId, ignorarMandatoId);

        foreach (var p in periodos)
        {
            if (SeSobrepoe(inicio, fim, p.Inicio, p.Fim))
            {
                return $"O período se sobrepõe a um {p.Origem} já existente " +
                       "desta pessoa. Uma pessoa não pode exercer dois papéis " +
                       "ao mesmo tempo.";
            }
        }

        return null;
    }
}
