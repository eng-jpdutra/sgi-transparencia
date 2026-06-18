import { useMemo, useState } from 'react'
import { Alert, Box, Chip, IconButton, Paper, Stack, Tooltip, Typography } from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import BlockIcon from '@mui/icons-material/Block'
import RestoreIcon from '@mui/icons-material/Restore'
import {
  DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel,
} from '@mui/x-data-grid'
import { ptBR } from '@mui/x-data-grid/locales'
import { usarPartidos, usarInativarPartido, usarReativarPartido } from '../api/usarPartidos'
import { useDebounce } from '../componentes/useDebounce'
import { BarraFiltrosPartido } from '../componentes/BarraFiltrosPartido'
import { DialogoPartido } from './DialogoPartido'
import { DialogoConfirmacao } from '../componentes/DialogoConfirmacao'
import { useSessao } from '../contextos/useSessao'
import { ErroRequisicao } from '../api/clienteHttp'
import type { FiltrosPartido, Partido } from '../tipos/partido'

/**
 * Tela de Partidos — segunda instância do template canônico, no
 * formato pleno (cadastro manual + edição). Botões de escrita só p/
 * Admin (RBAC no front, espelhando o backend).
 */
export function PaginaPartidos() {
  const { usuario } = useSessao()
  const podeEscrever = usuario?.perfis.includes('Admin') ?? false

  const [filtros, setFiltros] = useState<FiltrosPartido>({
    busca: '',
    incluirInativos: false,
  })
  const buscaDebounced = useDebounce(filtros.busca)
  const [paginacao, setPaginacao] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  })
  // Ordenação server-side. Inicia por sigla ascendente (o padrão do backend).
  const [ordenacao, setOrdenacao] = useState<GridSortModel>([
    { field: 'sigla', sort: 'asc' },
  ])

  const [dialogoAberto, setDialogoAberto] = useState(false)
  const [emEdicao, setEmEdicao] = useState<Partido | null>(null)
  const [aInativar, setAInativar] = useState<Partido | null>(null)
  const [aReativar, setAReativar] = useState<Partido | null>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const { data, isLoading, isError, error, isFetching } = usarPartidos({
    pagina: paginacao.page + 1,
    tamanhoPagina: paginacao.pageSize,
    busca: buscaDebounced,
    incluirInativos: filtros.incluirInativos,
    ordenarPor: ordenacao[0]?.field,
    descendente: ordenacao[0]?.sort === 'desc',
  })
  const inativar = usarInativarPartido()
  const reativar = usarReativarPartido()

  function abrirCriacao() {
    setEmEdicao(null)
    setDialogoAberto(true)
  }
  function abrirEdicao(p: Partido) {
    setEmEdicao(p)
    setDialogoAberto(true)
  }

  async function confirmarInativacao() {
    if (!aInativar) return
    setErroAcao(null)
    try {
      await inativar.mutateAsync(aInativar.id)
      setAInativar(null)
    } catch (e) {
      setErroAcao(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível inativar.')
    }
  }

  async function confirmarReativacao() {
    if (!aReativar) return
    setErroAcao(null)
    try {
      await reativar.mutateAsync(aReativar.id)
      setAReativar(null)
    } catch (e) {
      setErroAcao(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível reativar.')
    }
  }

  const colunas = useMemo<GridColDef<Partido>[]>(() => {
    const base: GridColDef<Partido>[] = [
      { field: 'numero', headerName: 'Nº', width: 80 },
      { field: 'sigla', headerName: 'Sigla', width: 120 },
      { field: 'nome', headerName: 'Nome', flex: 1, minWidth: 280 },
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
              <IconButton size="small" onClick={() => abrirEdicao(params.row)}>
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            {params.row.ativo ? (
              <Tooltip title="Inativar">
                <IconButton size="small" color="error"
                  onClick={() => setAInativar(params.row)}>
                  <BlockIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : (
              <Tooltip title="Reativar">
                <IconButton size="small" color="success"
                  onClick={() => setAReativar(params.row)}>
                  <RestoreIcon fontSize="small" />
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
      <Typography variant="h1" color="primary" gutterBottom>
        Partidos
      </Typography>

      <Paper elevation={1} sx={{ p: 3 }}>
        <BarraFiltrosPartido
          filtros={filtros}
          aoMudar={setFiltros}
          podeCriar={podeEscrever}
          aoCriar={abrirCriacao}
        />

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

        <DataGrid<Partido>
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

      <DialogoPartido
        aberto={dialogoAberto}
        partido={emEdicao}
        aoFechar={() => setDialogoAberto(false)}
      />

      <DialogoConfirmacao
        aberto={aInativar !== null}
        titulo="Inativar partido"
        mensagem={`Deseja realmente inativar "${aInativar?.sigla} - ${aInativar?.nome}"? `
          + 'Ele deixará de aparecer nas listagens, mas não será apagado '
          + '(pode ser reexibido marcando "Incluir inativos").'}
        textoConfirmar="Inativar"
        processando={inativar.isPending}
        aoConfirmar={confirmarInativacao}
        aoCancelar={() => setAInativar(null)}
      />

      <DialogoConfirmacao
        aberto={aReativar !== null}
        titulo="Reativar partido"
        mensagem={`Deseja reativar "${aReativar?.sigla} - ${aReativar?.nome}"? `
          + 'Ele voltará a aparecer nas listagens e poderá ser usado normalmente.'}
        textoConfirmar="Reativar"
        processando={reativar.isPending}
        aoConfirmar={confirmarReativacao}
        aoCancelar={() => setAReativar(null)}
      />
    </Box>
  )
}
