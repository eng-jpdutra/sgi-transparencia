import { useMemo, useState } from 'react'
import {
  Alert, Box, Checkbox, Chip, FormControlLabel, IconButton, InputAdornment,
  MenuItem, Paper, Stack, TextField, Tooltip, Typography, Button,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import BlockIcon from '@mui/icons-material/Block'
import SearchIcon from '@mui/icons-material/Search'
import {
  DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel,
} from '@mui/x-data-grid'
import { ptBR } from '@mui/x-data-grid/locales'
import { usarPessoas, usarInativarPessoa } from '../api/usarPessoas'
import { useDebounce } from '../componentes/useDebounce'
import { DialogoAdmissao } from './DialogoAdmissao'
import { DialogoConfirmacao } from '../componentes/DialogoConfirmacao'
import { useSessao } from '../contextos/useSessao'
import { ErroRequisicao } from '../api/clienteHttp'
import { formatarCpf } from '../util/cpf'
import type { FiltrosPessoa, Pessoa } from '../tipos/pessoa'

/**
 * Tela de Pessoas. O cadastro é feito pelo fluxo de ADMISSÃO (pessoa +
 * papel + exercício inicial). Nesta fatia: criar (admissão) + listar +
 * inativar. Editar pessoa e gerir vínculos/mandatos virão em telas
 * próprias. Escrita só para Admin.
 */
export function PaginaPessoas() {
  const { usuario } = useSessao()
  const podeEscrever = usuario?.perfis.includes('Admin') ?? false

  const [filtros, setFiltros] = useState<FiltrosPessoa>({
    busca: '', papel: '', incluirInativos: false,
  })
  const buscaDebounced = useDebounce(filtros.busca)
  const [paginacao, setPaginacao] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [ordenacao, setOrdenacao] = useState<GridSortModel>([
    { field: 'nomeCompleto', sort: 'asc' },
  ])

  const [admissaoAberta, setAdmissaoAberta] = useState(false)
  const [aInativar, setAInativar] = useState<Pessoa | null>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const { data, isLoading, isError, error, isFetching } = usarPessoas({
    pagina: paginacao.page + 1,
    tamanhoPagina: paginacao.pageSize,
    busca: buscaDebounced,
    papel: filtros.papel,
    incluirInativos: filtros.incluirInativos,
    ordenarPor: ordenacao[0]?.field,
    descendente: ordenacao[0]?.sort === 'desc',
  })
  const inativar = usarInativarPessoa()

  async function confirmarInativacao() {
    if (!aInativar) return
    setErroAcao(null)
    try {
      await inativar.mutateAsync(aInativar.id)
      setAInativar(null)
    } catch (e) {
      setErroAcao(e instanceof ErroRequisicao ? e.message : 'Não foi possível inativar.')
    }
  }

  const colunas = useMemo<GridColDef<Pessoa>[]>(() => {
    const base: GridColDef<Pessoa>[] = [
      {
        field: 'cpf', headerName: 'CPF', width: 150,
        renderCell: (params) => formatarCpf(params.row.cpf),
      },
      { field: 'nomeCompleto', headerName: 'Nome completo', flex: 1, minWidth: 200 },
      {
        field: 'papeis', headerName: 'Papéis', width: 190, sortable: false,
        renderCell: (params) => {
          const p = params.row
          return (
            <Stack direction="row" spacing={0.5}>
              {p.servidor && <Chip label="Servidor" size="small" color="primary" variant="outlined" />}
              {p.vereador && <Chip label="Vereador" size="small" color="secondary" variant="outlined" />}
              {!p.servidor && !p.vereador && (
                <Typography variant="body2" color="text.disabled">—</Typography>
              )}
            </Stack>
          )
        },
      },
      {
        field: 'ativo', headerName: 'Situação', width: 110,
        renderCell: (params) =>
          params.value
            ? <Chip label="Ativa" color="success" size="small" variant="outlined" />
            : <Chip label="Inativa" size="small" variant="outlined" />,
      },
    ]

    if (podeEscrever) {
      base.push({
        field: 'acoes', headerName: 'Ações', width: 90, sortable: false,
        renderCell: (params) =>
          params.row.ativo ? (
            <Tooltip title="Inativar">
              <IconButton size="small" color="error" onClick={() => setAInativar(params.row)}>
                <BlockIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          ) : null,
      })
    }
    return base
  }, [podeEscrever])

  return (
    <Box>
      <Typography variant="h1" color="primary" gutterBottom>Pessoas</Typography>

      <Paper elevation={1} sx={{ p: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}
          alignItems={{ sm: 'center' }} sx={{ mb: 2 }}>
          <TextField
            label="Buscar por nome ou CPF"
            value={filtros.busca}
            onChange={(e) => setFiltros({ ...filtros, busca: e.target.value })}
            size="small"
            sx={{ minWidth: 280 }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>
              ),
            }}
          />
          <TextField
            select label="Papel" value={filtros.papel}
            onChange={(e) => setFiltros({ ...filtros, papel: e.target.value as FiltrosPessoa['papel'] })}
            size="small" sx={{ minWidth: 160 }}
          >
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="servidor">Servidores</MenuItem>
            <MenuItem value="vereador">Vereadores</MenuItem>
          </TextField>
          <FormControlLabel
            control={
              <Checkbox
                checked={filtros.incluirInativos}
                onChange={(e) => setFiltros({ ...filtros, incluirInativos: e.target.checked })}
              />
            }
            label="Incluir inativas"
          />
          <Box sx={{ flexGrow: 1 }} />
          {podeEscrever && (
            <Button variant="contained" startIcon={<AddIcon />}
              onClick={() => setAdmissaoAberta(true)}>
              Nova admissão
            </Button>
          )}
        </Stack>

        {isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error instanceof Error ? error.message : 'Falha ao carregar.'}
          </Alert>
        )}
        {erroAcao && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErroAcao(null)}>
            {erroAcao}
          </Alert>
        )}

        <DataGrid<Pessoa>
          rows={data?.itens ?? []}
          columns={colunas}
          getRowId={(linha) => linha.id}
          paginationMode="server"
          rowCount={data?.totalRegistros ?? 0}
          paginationModel={paginacao}
          onPaginationModelChange={setPaginacao}
          pageSizeOptions={[10, 20, 50]}
          sortingMode="server"
          sortModel={ordenacao}
          onSortModelChange={setOrdenacao}
          disableColumnFilter
          loading={isLoading || isFetching}
          localeText={ptBR.components.MuiDataGrid.defaultProps.localeText}
          autoHeight
          disableRowSelectionOnClick
          sx={{ '--DataGrid-overlayHeight': '300px' }}
        />
      </Paper>

      <DialogoAdmissao
        aberto={admissaoAberta}
        aoFechar={() => setAdmissaoAberta(false)}
      />

      <DialogoConfirmacao
        aberto={aInativar !== null}
        titulo="Inativar pessoa"
        mensagem={`Deseja inativar "${aInativar?.nomeCompleto}"? `
          + 'As fichas de papel associadas também serão inativadas '
          + '(pode ser reexibida marcando "Incluir inativas").'}
        textoConfirmar="Inativar"
        processando={inativar.isPending}
        aoConfirmar={confirmarInativacao}
        aoCancelar={() => setAInativar(null)}
      />
    </Box>
  )
}
