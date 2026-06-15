import { Box, CircularProgress, Typography } from '@mui/material'

/**
 * Tela de carregamento de página inteira. Usada enquanto a sessão é
 * reconstruída na abertura do app — evita o "piscar" da tela de login
 * por uma fração de segundo antes de descobrirmos que há sessão válida.
 */
export function TelaCarregando({ mensagem = 'Carregando…' }: { mensagem?: string }) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        bgcolor: 'background.default',
      }}
    >
      <CircularProgress />
      <Typography color="text.secondary">{mensagem}</Typography>
    </Box>
  )
}
