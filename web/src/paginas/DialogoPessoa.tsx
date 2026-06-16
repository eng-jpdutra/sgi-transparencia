import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Stack, Alert, FormGroup, FormControlLabel,
  Checkbox, Divider, Typography, Collapse,
} from '@mui/material'
import { usarCriarPessoa, usarEditarPessoa } from '../api/usarPessoas'
import { ErroRequisicao } from '../api/clienteHttp'
import type { Pessoa } from '../tipos/pessoa'

/**
 * Diálogo de cadastro UNIFICADO de pessoa. Um único formulário cria
 * (ou edita) a pessoa e suas fichas de papel. Os campos específicos
 * do papel aparecem dinamicamente: o "Nome legislativo" só surge
 * quando "É vereador" está marcado — e aí torna-se obrigatório.
 *
 * É a tradução visual da criação transacional do backend: o que
 * estiver marcado aqui é criado junto, tudo ou nada.
 */
export function DialogoPessoa({
  aberto,
  pessoa,
  aoFechar,
}: {
  aberto: boolean
  pessoa: Pessoa | null // null = criação
  aoFechar: () => void
}) {
  const editando = pessoa !== null

  const [nomeCompleto, setNomeCompleto] = useState('')
  const [matricula, setMatricula] = useState('')
  const [ehServidor, setEhServidor] = useState(false)
  const [ehVereador, setEhVereador] = useState(false)
  const [nomeLegislativo, setNomeLegislativo] = useState('')
  const [erro, setErro] = useState<string | null>(null)

  const criar = usarCriarPessoa()
  const editar = usarEditarPessoa()
  const salvando = criar.isPending || editar.isPending

  useEffect(() => {
    if (aberto) {
      setErro(null)
      if (pessoa) {
        setNomeCompleto(pessoa.nomeCompleto)
        setMatricula(pessoa.matricula)
        setEhServidor(pessoa.servidor !== null)
        setEhVereador(pessoa.vereador !== null)
        setNomeLegislativo(pessoa.vereador?.nomeLegislativo ?? '')
      } else {
        setNomeCompleto('')
        setMatricula('')
        setEhServidor(false)
        setEhVereador(false)
        setNomeLegislativo('')
      }
    }
  }, [aberto, pessoa])

  async function salvar() {
    setErro(null)

    if (!nomeCompleto.trim()) { setErro('O nome completo é obrigatório.'); return }
    if (!matricula.trim()) { setErro('A matrícula é obrigatória.'); return }
    if (ehVereador && !nomeLegislativo.trim()) {
      setErro('O nome legislativo é obrigatório para vereadores.')
      return
    }

    const dados = {
      nomeCompleto: nomeCompleto.trim(),
      matricula: matricula.trim(),
      ehServidor,
      ehVereador,
      nomeLegislativo: ehVereador ? nomeLegislativo.trim() : undefined,
    }

    try {
      if (editando) {
        await editar.mutateAsync({ id: pessoa!.id, dados })
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
    <Dialog open={aberto} onClose={aoFechar} maxWidth="sm" fullWidth>
      <DialogTitle>{editando ? 'Editar pessoa' : 'Nova pessoa'}</DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {/* Dados civis */}
          <TextField
            label="Nome completo"
            value={nomeCompleto}
            onChange={(e) => setNomeCompleto(e.target.value)}
            fullWidth
            autoFocus
          />
          <TextField
            label="Matrícula"
            value={matricula}
            onChange={(e) => setMatricula(e.target.value.slice(0, 10))}
            fullWidth
            helperText="Identificação funcional única."
          />

          <Divider />

          {/* Papéis */}
          <Typography variant="subtitle2" color="text.secondary">
            Papéis desta pessoa
          </Typography>
          <FormGroup>
            <FormControlLabel
              control={
                <Checkbox
                  checked={ehServidor}
                  onChange={(e) => setEhServidor(e.target.checked)}
                />
              }
              label="É servidor(a)"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={ehVereador}
                  onChange={(e) => setEhVereador(e.target.checked)}
                />
              }
              label="É vereador(a)"
            />
          </FormGroup>

          {/* Campo específico do papel vereador — aparece só quando marcado */}
          <Collapse in={ehVereador}>
            <TextField
              label="Nome legislativo"
              value={nomeLegislativo}
              onChange={(e) => setNomeLegislativo(e.target.value)}
              fullWidth
              placeholder="Ex.: Dr. João, Profa. Maria"
              helperText="Nome pelo qual o vereador é conhecido na atividade legislativa."
            />
          </Collapse>

          {/* Aviso sobre a regra temporal, para alinhar a expectativa */}
          {ehServidor && ehVereador && (
            <Alert severity="info">
              Esta pessoa terá as duas fichas. O exercício de cada papel
              ao longo do tempo é controlado pelos vínculos e mandatos.
            </Alert>
          )}

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
