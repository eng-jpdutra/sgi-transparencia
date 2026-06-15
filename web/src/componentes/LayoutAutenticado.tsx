import { AppBar, Toolbar, Typography, Button, Box, Container } from '@mui/material'
import { Outlet, useNavigate } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'

/**
 * Moldura das telas autenticadas: cabeçalho fixo (marca + usuário +
 * sair) e a área de conteúdo onde cada página interna é renderizada
 * (<Outlet />). Login e troca de senha NÃO usam este layout — são
 * telas "cheias", sem moldura.
 *
 * É aqui que, nos módulos de domínio, entrará o menu de navegação
 * lateral. Por ora, só o cabeçalho.
 */
export function LayoutAutenticado() {
  const { usuario, sair } = useSessao()
  const navegar = useNavigate()

  async function aoSair() {
    await sair()
    navegar('/login', { replace: true })
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            SGI Transparência
          </Typography>

          <Typography sx={{ mr: 2, opacity: 0.9 }}>
            {usuario?.login}
          </Typography>

          <Button color="inherit" onClick={aoSair}>
            Sair
          </Button>
        </Toolbar>
      </AppBar>

      {/* Área de conteúdo: cada página interna renderiza aqui. */}
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Outlet />
      </Container>
    </Box>
  )
}
