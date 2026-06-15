import { useState, type FormEvent } from 'react'
import {
  Box, Container, Paper, Typography, TextField, Button, Alert, Stack,
} from '@mui/material'
import { useSessao } from '../contextos/useSessao'
import { ErroRequisicao } from '../api/clienteHttp'

/**
 * Tela de login. Visual institucional sóbrio (tema central): a marca
 * em destaque, formulário enxuto, nada de ornamento. Toda a lógica de
 * autenticação fica no contexto de sessão — esta tela só coleta os
 * dados, dispara entrar() e exibe erro/carregamento.
 */
export function PaginaLogin() {
  const { entrar } = useSessao()
  const [login, setLogin] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function aoEnviar(evento: FormEvent) {
    evento.preventDefault()
    setErro(null)
    setEnviando(true)
    try {
      await entrar(login, senha)
      // Sucesso: o contexto muda a situação para 'autenticado' e o
      // roteador (6.4) troca a tela. Nada mais a fazer aqui.
    } catch (e) {
      // Mensagem já amigável, vinda do cliente HTTP.
      setErro(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível conectar. Tente novamente.')
    } finally {
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
          <Typography variant="h1" color="primary">
            SGI Transparência
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Acesso restrito. Entre com suas credenciais.
          </Typography>

          {/* form nativo: dá o "Enter para enviar" de graça e é
              acessível por padrão. */}
          <form onSubmit={aoEnviar}>
            <Stack spacing={2}>
              <TextField
                label="Login"
                value={login}
                onChange={(e) => setLogin(e.target.value)}
                autoFocus
                autoComplete="username"
                fullWidth
              />
              <TextField
                label="Senha"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoComplete="current-password"
                fullWidth
              />

              {erro && <Alert severity="error">{erro}</Alert>}

              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={enviando || !login || !senha}
                fullWidth
              >
                {enviando ? 'Entrando…' : 'Entrar'}
              </Button>
            </Stack>
          </form>
        </Paper>
      </Container>
    </Box>
  )
}
