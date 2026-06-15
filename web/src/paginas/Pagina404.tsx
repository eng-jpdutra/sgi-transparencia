import { Box, Container, Paper, Typography, Button } from '@mui/material'
import { Link as LinkRouter } from 'react-router-dom'

/** Tela para URLs inexistentes. */
export function Pagina404() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', bgcolor: 'background.default' }}>
      <Container maxWidth="sm">
        <Paper elevation={1} sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h1" color="primary" gutterBottom>
            404
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            A página que você procurou não existe ou foi movida.
          </Typography>
          <Button variant="contained" component={LinkRouter} to="/">
            Voltar ao início
          </Button>
        </Paper>
      </Container>
    </Box>
  )
}
