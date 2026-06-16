import { useEffect } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, Alert, Stack, Typography, CircularProgress, Box,
} from '@mui/material'
import { usarProximaLegislatura, usarCriarLegislatura } from '../api/usarLegislaturas'
import { ErroRequisicao } from '../api/clienteHttp'

/**
 * Diálogo de NOVA legislatura — sem formulário. Ao abrir, busca do
 * servidor o preview da próxima legislatura (número e ano calculados
 * automaticamente) e mostra exatamente o que será criado. O usuário
 * apenas confirma; nada é digitado.
 */
export function DialogoNovaLegislatura({
  aberto,
  aoFechar,
}: {
  aberto: boolean
  aoFechar: () => void
}) {
  const proxima = usarProximaLegislatura()
  const criar = usarCriarLegislatura()

  // Ao abrir, calcula o preview (refetch porque o hook é enabled:false).
  useEffect(() => {
    if (aberto) {
      criar.reset()
      void proxima.refetch()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [aberto])

  async function confirmar() {
    try {
      await criar.mutateAsync()
      aoFechar() // sucesso: fecha; a lista se atualiza pela invalidação
    } catch {
      // O erro fica visível no Alert abaixo (criar.isError).
    }
  }

  const preview = proxima.data
  const carregandoPreview = proxima.isFetching
  const erroPreview = proxima.isError
  const erroCriar = criar.isError
    ? (criar.error instanceof ErroRequisicao
        ? criar.error.message
        : 'Não foi possível criar. Tente novamente.')
    : null

  return (
    <Dialog open={aberto} onClose={aoFechar} maxWidth="xs" fullWidth>
      <DialogTitle>Nova legislatura</DialogTitle>

      <DialogContent>
        {carregandoPreview && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        )}

        {erroPreview && (
          <Alert severity="error">
            Não foi possível calcular a próxima legislatura.
          </Alert>
        )}

        {preview && !carregandoPreview && (
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography color="text.secondary">
              Com base na última legislatura cadastrada, será criada
              automaticamente:
            </Typography>

            <Alert severity="info" icon={false}>
              <Typography variant="h6" color="primary" gutterBottom>
                {preview.nome}
              </Typography>
              <Typography variant="body2">
                Período: <strong>01/01/{preview.anoInicio}</strong> a{' '}
                <strong>31/12/{preview.anoFim}</strong>
              </Typography>
            </Alert>

            {erroCriar && <Alert severity="error">{erroCriar}</Alert>}
          </Stack>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={aoFechar} disabled={criar.isPending}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          onClick={confirmar}
          disabled={!preview || carregandoPreview || criar.isPending}
        >
          {criar.isPending ? 'Criando…' : 'Confirmar criação'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
