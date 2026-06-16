import { useMemo, useState } from 'react'
import {
  Alert, Box, Button, Checkbox, Chip, FormControlLabel, IconButton,
  InputAdornment, Paper, Stack, TextField, Tooltip, Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import BlockIcon from '@mui/icons-material/Block'
import SearchIcon from '@mui/icons-material/Search'
import {
  DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel,
} from '@mui/x-data-grid'
import { ptBR } from '@mui/x-data-grid/locales'
import { criarHooksNome } from '../api/usarCargoRegime'
import { useDebounce } from '../componentes/useDebounce'
import { DialogoNome } from './DialogoNome'
import { DialogoConfirmacao } from '../componentes/DialogoConfirmacao'
import { useSessao } from '../contextos/useSessao'
import { ErroRequisicao } from '../api/clienteHttp'
import type { ItemNome } from '../api/cargoRegime'
import type { FiltrosNome } from '../tipos/cargoRegime'

/**
 * Página GENÉRICA de listagem para recursos "nome+ativo". Cargos e
 * Regimes a reutilizam passando título, rótulo singular e os hooks
 * (criados por criarHooksNome). Um só componente, dois módulos —
 * o ganho máximo do template canônico.
 */
export function PaginaNome({
  titulo,        // ex.: "Cargos"
  rotulo,        // ex.: "cargo" (singular, minúsculo)
  hooks,         // resultado de criarHooksNome(chave, base)
}: {
  titulo: string
  rotulo: string
  hooks: ReturnType<typeof criarHooksNome>
}) {
  const { usuario } = useSessao()
  const podeEscrever = usuario?.perfis.includes('Admin') ?? false

  const [filtros, setFiltros] = useState<FiltrosNome>({
    busca: '', incluirInativos: false,
  })
  const buscaDebounced = useDebounce(filtros.busca)
  const [paginacao, setPaginacao] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [ordenacao, setOrdenacao] = useState<GridSortModel>([{ field: 'nome', sort: 'asc' }])

  const [dialogoAberto, setDialogoAberto] = useState(false)
  const [emEdicao, setEmEdicao] = useState<ItemNome | null>(null)
  const [aInativar, setAInativar] = useState<ItemNome | null>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const { data, isLoading, isError, error, isFetching } = hooks.usarLista({
    pagina: paginacao.page + 1,
    tamanhoPagina: paginacao.pageSize,
    busca: buscaDebounced,
    incluirInativos: filtros.incluirInativos,
    ordenarPor: ordenacao[0]?.field,
    descendente: ordenacao[0]?.sort === 'desc',
  })
  const criar = hooks.usarCriar()
  const editar = hooks.usarEditar()
  const inativar = hooks.usarInativar()

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

  const colunas = useMemo<GridColDef<ItemNome>[]>(() => {
    const base: GridColDef<ItemNome>[] = [
      { field: 'nome', headerName: 'Nome', flex: 1, minWidth: 240 },
      {
        field: 'ativo', headerName: 'Situação', width: 120,
        renderCell: (params) =>
          params.value
            ? <Chip label="Ativo" color="success" size="small" variant="outlined" />
            : <Chip label="Inativo" size="small" variant="outlined" />,
      },
    ]
    if (podeEscrever) {
      base.push({
        field: 'acoes', headerName: 'Ações', width: 110, sortable: false,
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="Editar">
              <IconButton size="small" onClick={() => { setEmEdicao(params.row); setDialogoAberto(true) }}>
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            {params.row.ativo && (
              <Tooltip title="Inativar">
                <IconButton size="small" color="error" onClick={() => setAInativar(params.row)}>
                  <BlockIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        ),
      })
    }
    return base
  }, [podeEscrever])

  return (
    <Box>
      <Typography variant="h1" color="primary" gutterBottom>{titulo}</Typography>

      <Paper elevation={1} sx={{ p: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}
          alignItems={{ sm: 'center' }} sx={{ mb: 2 }}>
          <TextField
            label="Buscar por nome"
            value={filtros.busca}
            onChange={(e) => setFiltros({ ...filtros, busca: e.target.value })}
            size="small"
            sx={{ minWidth: 260 }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>
              ),
            }}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={filtros.incluirInativos}
                onChange={(e) => setFiltros({ ...filtros, incluirInativos: e.target.checked })}
              />
            }
            label="Incluir inativos"
          />
          <Box sx={{ flexGrow: 1 }} />
          {podeEscrever && (
            <Button variant="contained" startIcon={<AddIcon />}
              onClick={() => { setEmEdicao(null); setDialogoAberto(true) }}>
              Novo {rotulo}
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

        <DataGrid<ItemNome>
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

      <DialogoNome
        aberto={dialogoAberto}
        item={emEdicao}
        rotulo={rotulo}
        criar={criar}
        editar={editar}
        aoFechar={() => setDialogoAberto(false)}
      />

      <DialogoConfirmacao
        aberto={aInativar !== null}
        titulo={`Inativar ${rotulo}`}
        mensagem={`Deseja realmente inativar "${aInativar?.nome}"? `
          + 'Deixará de aparecer nas listagens, mas não será apagado.'}
        textoConfirmar="Inativar"
        processando={inativar.isPending}
        aoConfirmar={confirmarInativacao}
        aoCancelar={() => setAInativar(null)}
      />
    </Box>
  )
}
