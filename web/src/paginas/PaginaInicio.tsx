import { Paper, Typography, Stack, Chip, Alert } from '@mui/material'
import { Link as LinkRouter } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'

/**
 * Página inicial da área autenticada (o "painel"). Por enquanto é uma
 * boas-vindas que mostra os perfis do usuário; vira o dashboard real
 * quando os módulos de domínio existirem.
 */
export function PaginaInicio() {
  const { usuario } = useSessao()
  const ehAdmin = usuario?.perfis.includes('Admin') ?? false

  return (
    <Stack spacing={3}>
      <Paper elevation={1} sx={{ p: 4 }}>
        <Typography variant="h1" color="primary" gutterBottom>
          Bem-vindo, {usuario?.login}
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          Fundação concluída: autenticação, sessão persistente, rotas
          protegidas e controle por perfil. Os módulos de domínio
          (legislaturas, vereadores, servidores…) entram a partir daqui.
        </Typography>

        <Typography variant="body2" sx={{ mb: 1 }}>Seus perfis:</Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {usuario?.perfis.map((p) => (
            <Chip key={p} label={p} color="primary" variant="outlined" />
          ))}
        </Stack>
      </Paper>

      <Paper elevation={1} sx={{ p: 4 }}>
        <Typography variant="h2" color="primary" gutterBottom>
          Teste do controle por perfil
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          A rota <code>/admin</code> é protegida pelo GuardaPerfil
          (exige o perfil “Admin”).
          {ehAdmin
            ? ' Como você é Admin, terá acesso.'
            : ' Como você NÃO é Admin, verá a tela de acesso negado.'}
        </Typography>
        <Alert severity="info">
          Acesse <LinkRouter to="/admin">/admin</LinkRouter> para ver o
          guarda de perfil em ação.
        </Alert>
      </Paper>
    </Stack>
  )
}
