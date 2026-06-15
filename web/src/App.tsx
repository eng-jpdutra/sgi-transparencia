import { Box, Container, Paper, Typography, Chip, Stack } from '@mui/material'

// App provisório do sub-passo 6.1: apenas confirma visualmente que a
// fundação está de pé — Vite, React, MUI e o tema institucional.
// Será substituído pelo roteamento real (login + guards) nos
// sub-passos 6.3 e 6.4.
export default function App() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        bgcolor: 'background.default',
      }}
    >
      <Container maxWidth="sm">
        <Paper elevation={1} sx={{ p: 4 }}>
          <Typography variant="h1" color="primary" gutterBottom>
            SGI Transparência
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Fundação do frontend no ar. Vite, React, Material UI e o tema
            institucional estão configurados e funcionando.
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip label="Vite" color="primary" variant="outlined" />
            <Chip label="React 18" color="primary" variant="outlined" />
            <Chip label="TypeScript" color="primary" variant="outlined" />
            <Chip label="Material UI" color="primary" variant="outlined" />
            <Chip label="TanStack Query" color="primary" variant="outlined" />
          </Stack>
        </Paper>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ mt: 2, textAlign: 'center' }}
        >
          Próximo: cliente HTTP e autenticação (sub-passo 6.2).
        </Typography>
      </Container>
    </Box>
  )
}
