import { useState, type FormEvent } from 'react'
import {
  Box, Container, Paper, Typography, TextField, Button, Alert, Stack,
} from '@mui/material'
import { useSessao } from '../contextos/useSessao'
import { requisitar, ErroRequisicao } from '../api/clienteHttp'

/**
 * Tela de troca de senha obrigatória. Aparece quando deveTrocarSenha
 * é true (senha provisória do seed ou definida por um administrador).
 * Fecha o ciclo iniciado lá na Etapa 3.
 *
 * Importante: trocar a senha REVOGA todas as sessões no backend
 * (Etapa 4). Por isso, ao concluir, NÃO tentamos "continuar logado":
 * encerramos a sessão local (sair) e o roteador leva ao login, onde
 * o usuário entra com a senha nova. Isso evita o estado inconsistente
 * "autenticado, porém com sessão já revogada no servidor".
 */
export function PaginaTrocaSenha() {
  const { sair } = useSessao()
  const [senhaAtual, setSenhaAtual] = useState('')
  const [novaSenha, setNovaSenha] = useState('')
  const [confirmacao, setConfirmacao] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [sucesso, setSucesso] = useState(false)
  const [enviando, setEnviando] = useState(false)

  async function aoEnviar(evento: FormEvent) {
    evento.preventDefault()
    setErro(null)

    // Validações no cliente (Fail Fast): espelham as regras do backend
    // para dar resposta imediata, sem ida à rede. O backend revalida.
    if (novaSenha.length < 8) {
      setErro('A nova senha deve ter ao menos 8 caracteres.')
      return
    }
    if (novaSenha !== confirmacao) {
      setErro('A confirmação não corresponde à nova senha.')
      return
    }
    if (novaSenha === senhaAtual) {
      setErro('A nova senha deve ser diferente da atual.')
      return
    }

    setEnviando(true)
    try {
      await requisitar<void>('/autenticacao/trocar-senha', {
        metodo: 'POST',
        corpo: { senhaAtual, novaSenha },
      })

      // Sucesso: mostra a confirmação e, após um instante para o
      // usuário ler, encerra a sessão. O sair() tem try/finally no
      // contexto, então SEMPRE limpa o estado local (mesmo que a
      // chamada ao servidor falhe — a sessão já estava revogada).
      // Ao mudar a situação para 'naoAutenticado', o roteador
      // automaticamente leva à tela de login.
      setSucesso(true)
      setTimeout(() => { void sair() }, 1800)
    } catch (e) {
      setErro(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível concluir. Tente novamente.')
      setEnviando(false)
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        bgcolor: 'background.default',
      }}
    >
      <Container maxWidth="xs">
        <Paper elevation={1} sx={{ p: 4 }}>
          <Typography variant="h2" color="primary">
            Defina uma nova senha
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Sua senha é provisória. Crie uma senha pessoal para continuar.
          </Typography>

          {sucesso ? (
            <Alert severity="success">
              Senha alterada. Redirecionando para um novo acesso…
            </Alert>
          ) : (
            <form onSubmit={aoEnviar}>
              <Stack spacing={2}>
                <TextField
                  label="Senha atual"
                  type="password"
                  value={senhaAtual}
                  onChange={(e) => setSenhaAtual(e.target.value)}
                  autoComplete="current-password"
                  fullWidth
                />
                <TextField
                  label="Nova senha"
                  type="password"
                  value={novaSenha}
                  onChange={(e) => setNovaSenha(e.target.value)}
                  autoComplete="new-password"
                  helperText="Mínimo de 8 caracteres."
                  fullWidth
                />
                <TextField
                  label="Confirme a nova senha"
                  type="password"
                  value={confirmacao}
                  onChange={(e) => setConfirmacao(e.target.value)}
                  autoComplete="new-password"
                  fullWidth
                />

                {erro && <Alert severity="error">{erro}</Alert>}

                <Button
                  type="submit"
                  variant="contained"
                  size="large"
                  disabled={enviando || !senhaAtual || !novaSenha || !confirmacao}
                  fullWidth
                >
                  {enviando ? 'Salvando…' : 'Salvar nova senha'}
                </Button>
              </Stack>
            </form>
          )}
        </Paper>
      </Container>
    </Box>
  )
}
