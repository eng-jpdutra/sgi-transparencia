namespace SGI.Api.Contratos.Autenticacao;

/// <summary>
/// Corpo da requisição POST /autenticacao/login.
///
/// Por que um "record"? É o tipo perfeito para DTOs (objetos de
/// transferência de dados): imutável, conciso e com igualdade por
/// valor. O construtor posicional abaixo declara as propriedades.
///
/// Por que as propriedades são anuláveis (string?) se são
/// obrigatórias? Porque quem envia o JSON é o MUNDO EXTERNO —
/// e o mundo externo erra e ataca. Se declarássemos "required",
/// um JSON sem o campo geraria um erro feio de desserialização.
/// Aceitando nulo e validando NÓS MESMOS (Fail Fast na rota),
/// devolvemos um 400 limpo e amigável, sob nosso controle.
/// Regra geral do projeto: DTOs de ENTRADA são permissivos no tipo
/// e rigorosos na validação.
/// </summary>
public record RequisicaoLogin(string? Login, string? Senha);
