using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SGI.Api.Infraestrutura;

/// <summary>
/// A última linha de defesa da aplicação: QUALQUER exceção que nenhum
/// try/catch capturou termina aqui — em vez de derrubar a requisição
/// com um stack trace exposto.
///
/// O contrato de governança que este arquivo cumpre:
///   - NO SERVIDOR: o erro é logado POR COMPLETO (tipo, mensagem,
///     stack trace, rota) — diagnóstico total para nós.
///   - PARA O CLIENTE: sai apenas um JSON anônimo e padronizado.
///     Stack trace, nomes de classes, versões de biblioteca — tudo
///     isso é mapa de ataque de graça, e não sai daqui.
///
/// IExceptionHandler é o mecanismo nativo do .NET 8 para isso;
/// o pipeline o invoca via app.UseExceptionHandler().
/// </summary>
public class ManipuladorErrosGlobal(ILogger<ManipuladorErrosGlobal> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excecao,
        CancellationToken cancelamento)
    {
        // --------------------------------------------------------------
        // CASO 1: requisição malformada (ex.: JSON inválido no corpo).
        // A culpa é do CLIENTE -> 400, não 500. (Você já viu este erro
        // ao vivo: o PowerShell comendo as aspas do JSON. Era isto que
        // estourava com stack trace na tela — agora vira um 400 limpo.)
        // --------------------------------------------------------------
        if (excecao is BadHttpRequestException)
        {
            // Warning, não Error: cliente mandando lixo é rotina da
            // internet, não incidente nosso.
            logger.LogWarning(
                "Requisição malformada em {Caminho}: {Motivo}",
                contexto.Request.Path, excecao.Message);

            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new
            {
                mensagem = "Requisição malformada. Verifique o formato " +
                           "dos dados enviados."
            }, cancelamento);

            return true; // true = "tratei; pipeline, siga em frente"
        }

        // --------------------------------------------------------------
        // CASO 2: qualquer outra exceção = falha NOSSA -> 500.
        // --------------------------------------------------------------
        // O log carrega a exceção INTEIRA (o ILogger imprime o stack
        // trace completo) + a rota onde ocorreu.
        logger.LogError(excecao,
            "Erro não tratado em {Metodo} {Caminho}",
            contexto.Request.Method, contexto.Request.Path);

        // ProblemDetails (RFC 7807): o formato padrão da indústria para
        // erros HTTP — o mesmo que Results.Problem() produz nas rotas.
        // Repare no que ele NÃO contém: nenhum detalhe interno.
        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexto.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro interno do servidor.",
            Detail = "Ocorreu uma falha inesperada. Tente novamente; " +
                     "se persistir, contate o suporte."
        }, cancelamento);

        return true;
    }
}
