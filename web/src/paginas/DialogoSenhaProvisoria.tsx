import { useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, Alert, Stack, Typography, TextField, InputAdornment, IconButton, Tooltip,
} from '@mui/material'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import CheckIcon from '@mui/icons-material/Check'

/**
 * Diálogo que mostra a senha provisória gerada — UMA única vez. O
 * Admin copia e repassa ao usuário; depois de fechar, a senha não é
 * mais recuperável (está só como hash no banco). Daí o aviso enfático.
 */
export function DialogoSenhaProvisoria({
  aberto,
  login,
  senha,
  aoFechar,
}: {
  aberto: boolean
  login: string
  senha: string
  aoFechar: () => void
}) {
  const [copiado, setCopiado] = useState(false)

  async function copiar() {
    try {
      await navigator.clipboard.writeText(senha)
      setCopiado(true)
      setTimeout(() => setCopiado(false), 2000)
    } catch {
      // Se o navegador bloquear a área de transferência, o usuário
      // ainda pode selecionar e copiar o texto manualmente do campo.
    }
  }

  return (
    <Dialog open={aberto} onClose={aoFechar} maxWidth="xs" fullWidth>
      <DialogTitle>Senha provisória gerada</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography color="text.secondary">
            Repasse estas credenciais ao usuário <strong>{login}</strong>.
            Ele deverá trocar a senha no primeiro acesso.
          </Typography>

          <TextField
            label="Senha provisória"
            value={senha}
            InputProps={{
              readOnly: true,
              endAdornment: (
                <InputAdornment position="end">
                  <Tooltip title={copiado ? 'Copiado!' : 'Copiar'}>
                    <IconButton onClick={copiar} edge="end">
                      {copiado ? <CheckIcon color="success" /> : <ContentCopyIcon />}
                    </IconButton>
                  </Tooltip>
                </InputAdornment>
              ),
            }}
            sx={{ '& input': { fontFamily: 'monospace', fontSize: '1.1rem' } }}
          />

          <Alert severity="warning">
            Anote agora: por segurança, esta senha não poderá ser
            consultada novamente. Se for perdida, use “Resetar senha”.
          </Alert>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={aoFechar}>Entendi</Button>
      </DialogActions>
    </Dialog>
  )
}
