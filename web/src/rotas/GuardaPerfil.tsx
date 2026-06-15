import { Navigate, Outlet } from 'react-router-dom'
import { Box, Container, Paper, Typography } from '@mui/material'
import { useSessao } from '../contextos/useSessao'

/**
 * GuardaPerfil — RBAC no frontend: só libera as rotas filhas se o
 * usuário possuir ao menos um dos perfis exigidos.
 *
 * Mesmo aviso do GuardaAutenticacao vale aqui, com ênfase: isto é
 * espelho de UX do RBAC, não a fonte de autoridade. O backend valida
 * o perfil em cada endpoint ([Authorize(Roles=...)]). Este guarda só
 * poupa o usuário de ver uma tela que tomaria 403 — e melhora a
 * experiência ao não oferecer caminhos que ele não pode trilhar.
 */
export function GuardaPerfil({ perfis }: { perfis: string[] }) {
  const { usuario } = useSessao()

  const temPermissao = usuario?.perfis.some((p) => perfis.includes(p)) ?? false

  if (!temPermissao) {
    // Logado, porém sem o perfil necessário: mensagem clara de
    // permissão negada (não redireciona ao login — ele ESTÁ logado).
    return (
      <Box sx={{ minHeight: '60vh', display: 'flex', alignItems: 'center' }}>
        <Container maxWidth="sm">
          <Paper elevation={1} sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="h2" color="error" gutterBottom>
              Acesso negado
            </Typography>
            <Typography color="text.secondary">
              Você não tem permissão para acessar esta área. Caso
              acredite ser um engano, procure um administrador.
            </Typography>
          </Paper>
        </Container>
      </Box>
    )
  }

  return <Outlet />
}
