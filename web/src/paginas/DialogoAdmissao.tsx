import { useEffect, useMemo, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  Stack, Alert, Divider, Typography, ToggleButton, ToggleButtonGroup,
  MenuItem,
} from '@mui/material'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import { usarAdmitirPessoa, usarPessoaPorCpf } from '../api/usarPessoas'
import {
  usarCargosSelect, usarRegimesSelect, usarLegislaturasSelect,
} from '../api/usarSelects'
import { ErroRequisicao } from '../api/clienteHttp'
import { cpfValido, formatarCpf, normalizarCpf } from '../util/cpf'

/**
 * Diálogo de ADMISSÃO — cria pessoa + ficha de papel + exercício inicial
 * numa só operação. A matrícula agora é do EXERCÍCIO (não da pessoa), e a
 * pessoa é identificada pelo CPF.
 *
 * REAPROVEITAMENTO: ao digitar um CPF já cadastrado, a admissão deixa de
 * criar pessoa nova e passa a ANEXAR o papel/exercício à pessoa existente
 * (ex.: servidor eleito vereador). O formulário detecta isso e avisa.
 *
 * Campos do exercício mudam conforme o tipo:
 *   Servidor -> cargo + regime + matrícula + datas (vínculo)
 *   Vereador -> nome legislativo + legislatura + matrícula + datas (mandato)
 */
export function DialogoAdmissao({
  aberto,
  aoFechar,
}: {
  aberto: boolean
  aoFechar: () => void
}) {
  // Dados civis
  const [cpf, setCpf] = useState('')            // só dígitos
  const [nomeCompleto, setNomeCompleto] = useState('')
  // Papel
  const [tipo, setTipo] = useState<'servidor' | 'vereador'>('servidor')
  // Exercício
  const [matricula, setMatricula] = useState('')
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

  // Busca a pessoa pelo CPF (só dispara com 11 dígitos).
  const buscaCpf = usarPessoaPorCpf(cpf)
  const existente = normalizarCpf(cpf).length === 11 ? (buscaCpf.data ?? null) : null
  const existenteInativa = existente !== null && !existente.ativo
  const jaTemPapel = tipo === 'servidor' ? !!existente?.servidor : !!existente?.vereador

  // Limpa o formulário ao abrir.
  useEffect(() => {
    if (aberto) {
      setCpf(''); setNomeCompleto(''); setTipo('servidor'); setMatricula('')
      setCargoId(''); setRegimeId(''); setLegislaturaId('')
      setNomeLegislativo(''); setDataInicio(null); setDataFim(null)
      setErro(null)
    }
  }, [aberto])

  // Pessoa existente encontrada: assume o nome civil dela (não se altera
  // na admissão) e, se já for vereadora, o nome legislativo da ficha.
  useEffect(() => {
    const ex = buscaCpf.data
    if (ex) {
      setNomeCompleto(ex.nomeCompleto)
      if (ex.vereador) setNomeLegislativo(ex.vereador.nomeLegislativo)
    }
  }, [buscaCpf.data])

  const avisoExistente = useMemo(() => {
    if (!existente) return null
    if (existenteInativa) {
      return {
        severidade: 'warning' as const,
        texto: `Há uma pessoa INATIVA com este CPF (${existente.nomeCompleto}). `
          + 'Reative-a antes de admitir.',
      }
    }
    return {
      severidade: 'info' as const,
      texto: jaTemPapel
        ? `${existente.nomeCompleto} já tem ficha de ${tipo}. Será adicionado um `
          + 'NOVO exercício (com nova matrícula) a esta pessoa.'
        : `CPF já cadastrado: ${existente.nomeCompleto}. Será adicionado o papel `
          + `de ${tipo} à pessoa existente (o nome civil não muda).`,
    }
  }, [existente, existenteInativa, jaTemPapel, tipo])

  async function salvar() {
    setErro(null)

    // Validações de cliente (espelham o backend).
    if (!cpfValido(cpf)) { setErro('CPF inválido.'); return }
    if (existenteInativa) {
      setErro('Pessoa inativa com este CPF. Reative-a antes de admitir.'); return
    }
    if (!existente && !nomeCompleto.trim()) {
      setErro('O nome completo é obrigatório.'); return
    }
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
      nomeCompleto: (existente?.nomeCompleto ?? nomeCompleto).trim(),
      cpf: normalizarCpf(cpf),
      tipo,
      matricula: matricula.trim(),
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
            label="CPF"
            value={cpf}
            onChange={(e) => setCpf(normalizarCpf(e.target.value).slice(0, 11))}
            fullWidth autoFocus
            inputProps={{ inputMode: 'numeric' }}
            helperText={
              normalizarCpf(cpf).length === 11
                ? (buscaCpf.isFetching ? 'Verificando CPF…' : formatarCpf(cpf))
                : 'Somente números. Identifica a pessoa (reaproveita se já existir).'
            }
          />

          {avisoExistente && (
            <Alert severity={avisoExistente.severidade}>{avisoExistente.texto}</Alert>
          )}

          <TextField
            label="Nome completo"
            value={nomeCompleto}
            onChange={(e) => setNomeCompleto(e.target.value)}
            fullWidth
            disabled={existente !== null}
            helperText={existente ? 'Nome da pessoa já cadastrada (não editável aqui).' : undefined}
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

          {/* ----- Matrícula DO EXERCÍCIO (comum aos dois papéis) ----- */}
          <TextField
            label="Matrícula do exercício"
            value={matricula}
            onChange={(e) => setMatricula(e.target.value.slice(0, 10))}
            fullWidth
            helperText="Número único no sistema; cada exercício (vínculo/mandato) tem a sua."
          />

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
                disabled={!!existente?.vereador}
                helperText={existente?.vereador
                  ? 'Nome legislativo da ficha já existente.' : undefined}
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
        <Button variant="contained" onClick={salvar}
          disabled={admitir.isPending || existenteInativa}>
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
