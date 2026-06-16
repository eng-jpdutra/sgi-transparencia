import {
  Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Button,
} from '@mui/material'

/**
 * Diálogo de confirmação genérico — reutilizável por todo o sistema
 * para ações destrutivas ("tem certeza?"). Mantém num só lugar o
 * padrão de confirmar antes de inativar/remover (DRY).
 */
export function DialogoConfirmacao({
  aberto,
  titulo,
  mensagem,
  textoConfirmar = 'Confirmar',
  processando = false,
  aoConfirmar,
  aoCancelar,
}: {
  aberto: boolean
  titulo: string
  mensagem: string
  textoConfirmar?: string
  processando?: boolean
  aoConfirmar: () => void
  aoCancelar: () => void
}) {
  return (
    <Dialog open={aberto} onClose={aoCancelar} maxWidth="xs" fullWidth>
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <DialogContentText>{mensagem}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={aoCancelar} disabled={processando}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          color="error"
          onClick={aoConfirmar}
          disabled={processando}
        >
          {processando ? 'Processando…' : textoConfirmar}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
