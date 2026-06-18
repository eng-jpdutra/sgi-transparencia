import { useState } from 'react'
import {
  AppBar, Toolbar, Typography, Box, Container, IconButton,
  Drawer, List, ListItemButton, ListItemIcon, ListItemText, Divider,
} from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft'
import HomeIcon from '@mui/icons-material/Home'
import GavelIcon from '@mui/icons-material/Gavel'
import FlagIcon from '@mui/icons-material/Flag'
import WorkIcon from '@mui/icons-material/Work'
import DescriptionIcon from '@mui/icons-material/Description'
import PeopleIcon from '@mui/icons-material/People'
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts'
import LogoutIcon from '@mui/icons-material/Logout'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'

const LARGURA_GAVETA = 260

interface ItemMenu {
  rotulo: string
  caminho: string
  icone: React.ReactNode
  somenteAdmin?: boolean
}

const ITENS: ItemMenu[] = [
  { rotulo: 'Início', caminho: '/', icone: <HomeIcon /> },
  { rotulo: 'Legislaturas', caminho: '/legislaturas', icone: <GavelIcon /> },
  { rotulo: 'Partidos', caminho: '/partidos', icone: <FlagIcon /> },
  { rotulo: 'Cargos', caminho: '/cargos', icone: <WorkIcon /> },
  { rotulo: 'Regimes', caminho: '/regimes', icone: <DescriptionIcon /> },
  { rotulo: 'Pessoas', caminho: '/pessoas', icone: <PeopleIcon /> },
  { rotulo: 'Usuários', caminho: '/usuarios', icone: <ManageAccountsIcon />, somenteAdmin: true },
]

/**
 * Moldura das telas autenticadas: cabeçalho institucional e uma GAVETA
 * lateral PERSISTENTE. A gaveta abre empurrando o conteúdo para a
 * direita e só recolhe pelo botão (clicar fora NÃO fecha) — comporta-se
 * como um menu lateral de painel administrativo.
 */
export function LayoutAutenticado() {
  const { usuario, sair } = useSessao()
  const navegar = useNavigate()
  const local = useLocation()
  const ehAdmin = usuario?.perfis.includes('Admin') ?? false
  const [gavetaAberta, setGavetaAberta] = useState(true) // começa aberta

  async function aoSair() {
    await sair()
    navegar('/login', { replace: true })
  }

  function irPara(caminho: string) {
    navegar(caminho)
    // Gaveta persistente: NÃO fecha ao navegar; permanece aberta.
  }

  const itensVisiveis = ITENS.filter((i) => !i.somenteAdmin || ehAdmin)

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', display: 'flex' }}>
      {/* ----- Cabeçalho institucional (fixo no topo, sobre tudo) ----- */}
      <AppBar
        position="fixed"
        elevation={0}
        sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            edge="start"
            onClick={() => setGavetaAberta((v) => !v)}
            aria-label="alternar menu"
            sx={{ mr: 2 }}
          >
            <MenuIcon />
          </IconButton>

          {/* Brasão: imagem maior e sem recorte, para não pixelar.
              object-fit contain mostra o brasão inteiro sem distorção.
              Coloque o arquivo em web/public/brasao.png. */}
          <Box
            component="img"
            src="/brasao.png"
            alt="Brasão da Câmara"
            sx={{ height: 48, width: 'auto', mr: 1.5, objectFit: 'contain' }}
          />

          <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: 0.5 }}>
            CÂMARA MUNICIPAL DE MARÍLIA
          </Typography>
        </Toolbar>
      </AppBar>

      {/* ----- Gaveta lateral PERSISTENTE ----- */}
      <Drawer
        variant="persistent"
        anchor="left"
        open={gavetaAberta}
        sx={{
          width: gavetaAberta ? LARGURA_GAVETA : 0,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: LARGURA_GAVETA,
            boxSizing: 'border-box',
          },
        }}
      >
        {/* Espaçador para a gaveta começar abaixo do cabeçalho fixo */}
        <Toolbar />
        <Box sx={{ overflow: 'auto' }} role="navigation">
          <List>
            {itensVisiveis.map((item) => (
              <ListItemButton
                key={item.caminho}
                selected={local.pathname === item.caminho}
                onClick={() => irPara(item.caminho)}
              >
                <ListItemIcon>{item.icone}</ListItemIcon>
                <ListItemText primary={item.rotulo} />
              </ListItemButton>
            ))}
          </List>
          <Divider />
          <List>
            <ListItemButton onClick={aoSair}>
              <ListItemIcon><LogoutIcon /></ListItemIcon>
              <ListItemText primary="Sair" />
            </ListItemButton>
          </List>
        </Box>
      </Drawer>

      {/* ----- Área de conteúdo: cede espaço quando a gaveta abre ----- */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          // Transição suave do conteúdo ao abrir/fechar a gaveta.
          transition: (t) => t.transitions.create('margin', {
            easing: t.transitions.easing.sharp,
            duration: t.transitions.duration.leavingScreen,
          }),
        }}
      >
        {/* Espaçador para o conteúdo começar abaixo do cabeçalho fixo */}
        <Toolbar />
        <Container maxWidth="lg" sx={{ py: 4 }}>
          <Outlet />
        </Container>
      </Box>
    </Box>
  )
}
