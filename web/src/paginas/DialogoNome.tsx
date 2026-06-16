import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Stack, Alert,
} from '@mui/material'
import { ErroRequisicao } from '../api/clienteHttp'
import type { ItemNome } from '../api/cargoRegime'
import type { UseMutationResult } from '@tanstack/react-query'

/**
 * Diálogo genérico de criar/editar para recursos "nome+ativo"
 * (Cargos e Regimes). Recebe os hooks de mutação da tela-mãe, então
 * serve a qualquer recurso desse formato sem duplicação.
 */
export function DialogoNome({
  aberto,
  item,
  rotulo, // ex.: "cargo" ou "regime" (para títulos e mensagens)
  criar,
  editar,
  aoFechar,
}: {
  aberto: boolean
  item: ItemNome | null
  rotulo: string
  criar: UseMutationResult<ItemNome, Error, string>
  editar: UseMutationResult<ItemNome, Error, { id: number; nome: string }>
  aoFechar: () => void
}) {
  const editando = item !== null
  const [nome, setNome] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const salvando = criar.isPending || editar.isPending

  useEffect(() => {
    if (aberto) {
      setErro(null)
      setNome(item ? item.nome : '')
    }
  }, [aberto, item])

  async function salvar() {
    setErro(null)
    if (!nome.trim()) {
      setErro('O nome é obrigatório.')
      return
    }
    try {
      if (editando) {
        await editar.mutateAsync({ id: item!.id, nome: nome.trim() })
      } else {
        await criar.mutateAsync(nome.trim())
      }
      aoFechar()
    } catch (e) {
      setErro(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível salvar. Tente novamente.')
    }
  }

  return (
    <Dialog open={aberto} onClose={aoFechar} maxWidth="xs" fullWidth>
      <DialogTitle>
        {editando ? `Editar ${rotulo}` : `Novo ${rotulo}`}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Nome"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            fullWidth
            autoFocus
          />
          {erro && <Alert severity="error">{erro}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={aoFechar} disabled={salvando}>Cancelar</Button>
        <Button variant="contained" onClick={salvar} disabled={salvando}>
          {salvando ? 'Salvando…' : 'Salvar'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
