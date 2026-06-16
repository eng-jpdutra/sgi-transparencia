import { AppBar, Toolbar, Typography, Button, Box, Container, Stack } from '@mui/material'
import { Outlet, useNavigate, Link as LinkRouter } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'

/**
 * Moldura das telas autenticadas: cabeçalho fixo (marca + navegação +
 * usuário + sair) e a área de conteúdo onde cada página interna é
 * renderizada (<Outlet />). Login e troca de senha NÃO usam este
 * layout — são telas "cheias", sem moldura.
 */
export function LayoutAutenticado() {
  const { usuario, sair } = useSessao()
  const navegar = useNavigate()
  const ehAdmin = usuario?.perfis.includes('Admin') ?? false

  async function aoSair() {
    await sair()
    navegar('/login', { replace: true })
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ fontWeight: 700, mr: 4 }}>
            SGI Transparência
          </Typography>

          {/* Navegação. Cresce e ocupa o espaço; usuário/sair à direita.
              Quando houver muitos módulos, isto vira um menu lateral. */}
          <Stack direction="row" spacing={1} sx={{ flexGrow: 1 }}>
            <Button color="inherit" component={LinkRouter} to="/">
              Início
            </Button>
            <Button color="inherit" component={LinkRouter} to="/legislaturas">
              Legislaturas
            </Button>
            <Button color="inherit" component={LinkRouter} to="/partidos">
              Partidos
            </Button>
            <Button color="inherit" component={LinkRouter} to="/cargos">
              Cargos
            </Button>
            <Button color="inherit" component={LinkRouter} to="/regimes">
              Regimes
            </Button>
            {/* "Usuários" só aparece para Admin — espelha a proteção da
                rota (GuardaPerfil). Esconder o link é UX; a segurança
                real é o guarda + o backend. */}
            {ehAdmin && (
              <Button color="inherit" component={LinkRouter} to="/usuarios">
                Usuários
              </Button>
            )}
          </Stack>

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
