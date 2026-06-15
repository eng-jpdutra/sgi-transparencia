import { Paper, Typography } from '@mui/material'

/**
 * Página de exemplo restrita ao perfil Admin — existe apenas para
 * demonstrar o GuardaPerfil. Vira (ou dá lugar a) um módulo de
 * administração real mais adiante.
 */
export function PaginaAdmin() {
  return (
    <Paper elevation={1} sx={{ p: 4 }}>
      <Typography variant="h1" color="primary" gutterBottom>
        Área administrativa
      </Typography>
      <Typography color="text.secondary">
        Se você está lendo isto, o GuardaPerfil confirmou que sua conta
        possui o perfil “Admin”. Um usuário sem esse perfil veria a tela
        de acesso negado em vez desta.
      </Typography>
    </Paper>
  )
}
