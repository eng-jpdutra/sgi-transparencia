# Fase 3 — Frontend da admissão com CPF + reaproveitamento de pessoa

Alinha o frontend ao novo contrato da API (Fase 2): CPF como identidade
civil, matrícula como dado do exercício, e a UX de reaproveitar uma
pessoa já cadastrada na admissão (servidor que vira vereador, etc.).

## Como aplicar

1. Copie a pasta `web/` deste pacote **por cima** da raiz do repositório
   (sobrescreve 5 arquivos e adiciona 1 novo, `util/cpf.ts`).
2. **APAGUE** manualmente o arquivo `web/src/paginas/DialogoPessoa.tsx`
   (o zip não consegue remover arquivos). É código morto: não é importado
   por ninguém e ainda referencia hooks que não existem mais
   (`usarCriarPessoa`/`usarEditarPessoa`). No `npm run dev` ele passa
   despercebido, mas quebraria um `npm run build`/`tsc`. O checkpoint já
   o dava como "Removido".
3. `npm run dev` (ou `npm run build` para checar tipos de ponta a ponta).

## Arquivos no pacote (6)

Novo (1):
- `web/src/util/cpf.ts` — normalizar, validar (dígitos) e formatar CPF.

Alterados (5):
- `web/src/tipos/pessoa.ts` — `Pessoa.matricula` → `cpf`; `AdmissaoEntrada`
  ganha `cpf` (civil) e `matricula` passa a ser a do exercício.
- `web/src/api/pessoas.ts` — `buscarPessoaPorCpf` (match exato via busca).
- `web/src/api/usarPessoas.ts` — hook `usarPessoaPorCpf` (dispara com 11 dígitos).
- `web/src/paginas/DialogoAdmissao.tsx` — campo CPF com busca da pessoa
  existente; matrícula no bloco do exercício; avisos de reaproveitamento
  ("já tem ficha de X → novo exercício" / "adiciona papel à pessoa") e
  bloqueio quando o CPF é de uma pessoa inativa.
- `web/src/paginas/PaginaPessoas.tsx` — coluna CPF (formatada) no lugar
  da matrícula; busca por nome ou CPF.

## Observações

- Não consegui rodar `tsc` aqui (sem `node_modules`, restore exige rede).
  Revisão manual feita; a checagem de tipos roda no seu ambiente.
- Telas de gestão de Vínculos/Mandatos (onde a matrícula aparece por
  exercício) continuam como Frente futura — não fazem parte desta fase.
