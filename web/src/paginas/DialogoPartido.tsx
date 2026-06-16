import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Stack, Alert,
} from '@mui/material'
import { usarCriarPartido, usarEditarPartido } from '../api/usarPartidos'
import { ErroRequisicao } from '../api/clienteHttp'
import type { Partido } from '../tipos/partido'

/**
 * Diálogo de criar/editar partido. Um componente para os dois casos
 * (DRY): com 'partido' está editando; sem, criando. Validações de
 * cliente espelham o backend (Fail Fast); o backend revalida.
 */
export function DialogoPartido({
  aberto,
  partido,
  aoFechar,
}: {
  aberto: boolean
  partido: Partido | null // null = criação
  aoFechar: () => void
}) {
  const editando = partido !== null

  const [sigla, setSigla] = useState('')
  const [nome, setNome] = useState('')
  const [numero, setNumero] = useState('')
  const [erro, setErro] = useState<string | null>(null)

  const criar = usarCriarPartido()
  const editar = usarEditarPartido()
  const salvando = criar.isPending || editar.isPending

  useEffect(() => {
    if (aberto) {
      setErro(null)
      if (partido) {
        setSigla(partido.sigla)
        setNome(partido.nome)
        setNumero(String(partido.numero))
      } else {
        setSigla('')
        setNome('')
        setNumero('')
      }
    }
  }, [aberto, partido])

  const numeroNum = Number(numero)

  async function salvar() {
    setErro(null)

    if (!sigla.trim()) { setErro('A sigla é obrigatória.'); return }
    if (!nome.trim()) { setErro('O nome é obrigatório.'); return }
    if (!/^\d{2}$/.test(numero) || numeroNum < 10 || numeroNum > 99) {
      setErro('O número da legenda deve ter dois dígitos (10 a 99).')
      return
    }

    const dados = {
      sigla: sigla.trim().toUpperCase(),
      nome: nome.trim(),
      numero: numeroNum,
    }

    try {
      if (editando) {
        await editar.mutateAsync({ id: partido!.id, dados })
      } else {
        await criar.mutateAsync(dados)
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
        {editando ? 'Editar partido' : 'Novo partido'}
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Sigla"
            value={sigla}
            onChange={(e) => setSigla(e.target.value.toUpperCase().slice(0, 25))}
            placeholder="PT"
            fullWidth
            autoFocus
          />
          <TextField
            label="Nome"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Partido dos Trabalhadores"
            fullWidth
          />
          <TextField
            label="Número da legenda"
            value={numero}
            onChange={(e) => setNumero(e.target.value.replace(/\D/g, '').slice(0, 2))}
            placeholder="13"
            inputMode="numeric"
            fullWidth
            helperText="Dois dígitos (10 a 99)."
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
