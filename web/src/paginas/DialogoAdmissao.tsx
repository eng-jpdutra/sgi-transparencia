import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  Stack, Alert, Divider, Typography, ToggleButton, ToggleButtonGroup,
  MenuItem,
} from '@mui/material'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import { usarAdmitirPessoa } from '../api/usarPessoas'
import {
  usarCargosSelect, usarRegimesSelect, usarLegislaturasSelect,
} from '../api/usarSelects'
import { ErroRequisicao } from '../api/clienteHttp'

/**
 * Diálogo de ADMISSÃO — o fluxo unificado que cria pessoa + ficha de
 * papel + exercício inicial, numa só operação. Os campos do exercício
 * mudam conforme o tipo escolhido:
 *   Servidor -> cargo + regime + datas (vínculo)
 *   Vereador -> nome legislativo + legislatura + datas (mandato)
 *
 * Só CRIAÇÃO (decisão de escopo da fatia). Editar pessoa e gerir
 * vínculos/mandatos terão telas próprias mais adiante.
 */
export function DialogoAdmissao({
  aberto,
  aoFechar,
}: {
  aberto: boolean
  aoFechar: () => void
}) {
  // Dados civis
  const [nomeCompleto, setNomeCompleto] = useState('')
  const [matricula, setMatricula] = useState('')
  // Papel
  const [tipo, setTipo] = useState<'servidor' | 'vereador'>('servidor')
  // Exercício
  const [cargoId, setCargoId] = useState<number | ''>('')
  const [regimeId, setRegimeId] = useState<number | ''>('')
  const [legislaturaId, setLegislaturaId] = useState<number | ''>('')
  const [nomeLegislativo, setNomeLegislativo] = useState('')
  const [dataInicio, setDataInicio] = useState<Date | null>(null)
  const [dataFim, setDataFim] = useState<Date | null>(null)
  const [erro, setErro] = useState<string | null>(null)

  const cargos = usarCargosSelect()
  const regimes = usarRegimesSelect()
  const legislaturas = usarLegislaturasSelect()
  const admitir = usarAdmitirPessoa()

  // Limpa o formulário ao abrir.
  useEffect(() => {
    if (aberto) {
      setNomeCompleto(''); setMatricula(''); setTipo('servidor')
      setCargoId(''); setRegimeId(''); setLegislaturaId('')
      setNomeLegislativo(''); setDataInicio(null); setDataFim(null)
      setErro(null)
    }
  }, [aberto])

  async function salvar() {
    setErro(null)

    // Validações de cliente (espelham o backend).
    if (!nomeCompleto.trim()) { setErro('O nome completo é obrigatório.'); return }
    if (!matricula.trim()) { setErro('A matrícula é obrigatória.'); return }
    if (!dataInicio) { setErro('A data de início é obrigatória.'); return }
    if (dataFim && dataFim <= dataInicio) {
      setErro('A data de fim deve ser posterior à de início.'); return
    }
    if (tipo === 'servidor') {
      if (cargoId === '') { setErro('Selecione o cargo.'); return }
      if (regimeId === '') { setErro('Selecione o regime de contratação.'); return }
    } else {
      if (!nomeLegislativo.trim()) { setErro('O nome legislativo é obrigatório.'); return }
      if (legislaturaId === '') { setErro('Selecione a legislatura.'); return }
    }

    const dados = {
      nomeCompleto: nomeCompleto.trim(),
      matricula: matricula.trim(),
      tipo,
      dataInicio: paraIso(dataInicio),
      dataFim: dataFim ? paraIso(dataFim) : null,
      ...(tipo === 'servidor'
        ? { cargoId: Number(cargoId), regimeId: Number(regimeId) }
        : { nomeLegislativo: nomeLegislativo.trim(), legislaturaId: Number(legislaturaId) }),
    }

    try {
      await admitir.mutateAsync(dados)
      aoFechar()
    } catch (e) {
      setErro(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível concluir a admissão. Tente novamente.')
    }
  }

  return (
    <Dialog open={aberto} onClose={aoFechar} maxWidth="sm" fullWidth>
      <DialogTitle>Admissão de pessoa</DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {/* ----- Dados civis ----- */}
          <TextField
            label="Nome completo"
            value={nomeCompleto}
            onChange={(e) => setNomeCompleto(e.target.value)}
            fullWidth autoFocus
          />
          <TextField
            label="Matrícula"
            value={matricula}
            onChange={(e) => setMatricula(e.target.value.slice(0, 10))}
            fullWidth
            helperText="Identificação funcional única."
          />

          <Divider />

          {/* ----- Tipo de papel ----- */}
          <Typography variant="subtitle2" color="text.secondary">
            Tipo de admissão
          </Typography>
          <ToggleButtonGroup
            value={tipo}
            exclusive
            onChange={(_, v) => { if (v) setTipo(v) }}
            fullWidth
            color="primary"
          >
            <ToggleButton value="servidor">Servidor</ToggleButton>
            <ToggleButton value="vereador">Vereador</ToggleButton>
          </ToggleButtonGroup>

          {/* ----- Campos do exercício de SERVIDOR ----- */}
          {tipo === 'servidor' && (
            <>
              <TextField
                select label="Cargo" value={cargoId}
                onChange={(e) => setCargoId(Number(e.target.value))}
                fullWidth disabled={cargos.isLoading}
              >
                {cargos.data?.map((c) => (
                  <MenuItem key={c.id} value={c.id}>{c.rotulo}</MenuItem>
                ))}
              </TextField>
              <TextField
                select label="Regime de contratação" value={regimeId}
                onChange={(e) => setRegimeId(Number(e.target.value))}
                fullWidth disabled={regimes.isLoading}
              >
                {regimes.data?.map((r) => (
                  <MenuItem key={r.id} value={r.id}>{r.rotulo}</MenuItem>
                ))}
              </TextField>
            </>
          )}

          {/* ----- Campos do exercício de VEREADOR ----- */}
          {tipo === 'vereador' && (
            <>
              <TextField
                label="Nome legislativo"
                value={nomeLegislativo}
                onChange={(e) => setNomeLegislativo(e.target.value)}
                placeholder="Ex.: Dr. João, Profa. Maria"
                fullWidth
              />
              <TextField
                select label="Legislatura" value={legislaturaId}
                onChange={(e) => setLegislaturaId(Number(e.target.value))}
                fullWidth disabled={legislaturas.isLoading}
              >
                {legislaturas.data?.map((l) => (
                  <MenuItem key={l.id} value={l.id}>{l.rotulo}</MenuItem>
                ))}
              </TextField>
            </>
          )}

          {/* ----- Período (comum) ----- */}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <DatePicker
              label="Início do exercício"
              value={dataInicio}
              onChange={setDataInicio}
              format="dd/MM/yyyy"
              slotProps={{ textField: { fullWidth: true } }}
            />
            <DatePicker
              label="Fim (opcional)"
              value={dataFim}
              onChange={setDataFim}
              format="dd/MM/yyyy"
              minDate={dataInicio ?? undefined}
              slotProps={{ textField: { fullWidth: true } }}
            />
          </Stack>
          <Typography variant="caption" color="text.secondary">
            Deixe o fim em branco para um exercício em aberto (vigente).
          </Typography>

          {erro && <Alert severity="error">{erro}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={aoFechar} disabled={admitir.isPending}>Cancelar</Button>
        <Button variant="contained" onClick={salvar} disabled={admitir.isPending}>
          {admitir.isPending ? 'Admitindo…' : 'Admitir'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

/** Date -> 'YYYY-MM-DD' (sem susto de fuso). */
function paraIso(data: Date): string {
  const ano = data.getFullYear()
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${ano}-${mes}-${dia}`
}
