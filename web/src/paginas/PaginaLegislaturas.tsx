import { useMemo, useState } from 'react'
import { Alert, Box, Chip, IconButton, Paper, Tooltip, Typography } from '@mui/material'
import BlockIcon from '@mui/icons-material/Block'
import RestoreIcon from '@mui/icons-material/Restore'
import {
  DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel,
} from '@mui/x-data-grid'
import { ptBR } from '@mui/x-data-grid/locales'
import { usarLegislaturas, usarInativarLegislatura, usarReativarLegislatura } from '../api/usarLegislaturas'
import { useDebounce } from '../componentes/useDebounce'
import { BarraFiltrosLegislatura } from '../componentes/BarraFiltrosLegislatura'
import { DialogoNovaLegislatura } from './DialogoNovaLegislatura'
import { DialogoConfirmacao } from '../componentes/DialogoConfirmacao'
import { useSessao } from '../contextos/useSessao'
import { ErroRequisicao } from '../api/clienteHttp'
import type { FiltrosLegislatura, Legislatura } from '../tipos/legislatura'

/**
 * Tela de Legislaturas. Cadastro automático: "Nova legislatura" abre
 * um diálogo que mostra a próxima (calculada pelo backend) para o
 * usuário apenas confirmar. Não há edição (a sequência é a regra);
 * a única ação por linha é inativar. Botões de escrita só p/ Admin.
 */
export function PaginaLegislaturas() {
  const { usuario } = useSessao()
  const podeEscrever = usuario?.perfis.includes('Admin') ?? false

  // ----- Filtros e paginação -----
  const [filtros, setFiltros] = useState<FiltrosLegislatura>({
    ano: '',
    incluirInativos: false,
  })
  const anoDebounced = useDebounce(filtros.ano)
  const [paginacao, setPaginacao] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  })
  // Ordenação server-side. Inicia por número decrescente (padrão do backend:
  // a legislatura mais recente no topo).
  const [ordenacao, setOrdenacao] = useState<GridSortModel>([
    { field: 'numero', sort: 'desc' },
  ])

  // ----- Estado dos diálogos -----
  const [dialogoNovaAberto, setDialogoNovaAberto] = useState(false)
  const [aInativar, setAInativar] = useState<Legislatura | null>(null)
  const [aReativar, setAReativar] = useState<Legislatura | null>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  // ----- Dados e mutação -----
  const { data, isLoading, isError, error, isFetching } = usarLegislaturas({
    pagina: paginacao.page + 1,
    tamanhoPagina: paginacao.pageSize,
    ano: anoDebounced,
    incluirInativos: filtros.incluirInativos,
    ordenarPor: ordenacao[0]?.field,
    descendente: ordenacao[0]?.sort === 'desc',
  })
  const inativar = usarInativarLegislatura()
  const reativar = usarReativarLegislatura()

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

  // ----- Colunas -----
  const colunas = useMemo<GridColDef<Legislatura>[]>(() => {
    const base: GridColDef<Legislatura>[] = [
      {
        field: 'numero', headerName: 'Nº', width: 70,
        valueFormatter: (v: number) => `${v}ª`,
      },
      { field: 'nome', headerName: 'Legislatura', flex: 1, minWidth: 260 },
      {
        field: 'dataInicio', headerName: 'Início', width: 120,
        valueFormatter: (v: string) => formatarData(v),
      },
      {
        field: 'dataFim', headerName: 'Fim', width: 120,
        valueFormatter: (v: string) => formatarData(v),
      },
      {
        field: 'ativo', headerName: 'Situação', width: 120,
        renderCell: (params) =>
          params.value
            ? <Chip label="Ativa" color="success" size="small" variant="outlined" />
            : <Chip label="Inativa" size="small" variant="outlined" />,
      },
    ]

    // Ação de inativar: só para quem pode escrever, e só em ativas.
    if (podeEscrever) {
      base.push({
        field: 'acoes', headerName: 'Ações', width: 90, sortable: false,
        renderCell: (params) =>
          params.row.ativo ? (
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
          ),
      })
    }

    return base
  }, [podeEscrever])

  return (
    <Box>
      <Typography variant="h1" color="primary" gutterBottom>
        Legislaturas
      </Typography>

      <Paper elevation={1} sx={{ p: 3 }}>
        <BarraFiltrosLegislatura
          filtros={filtros}
          aoMudar={setFiltros}
          podeCriar={podeEscrever}
          aoCriar={() => setDialogoNovaAberto(true)}
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

        <DataGrid<Legislatura>
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

      {/* Diálogo de confirmação da nova legislatura (com preview) */}
      <DialogoNovaLegislatura
        aberto={dialogoNovaAberto}
        aoFechar={() => setDialogoNovaAberto(false)}
      />

      {/* Confirmação de inativação */}
      <DialogoConfirmacao
        aberto={aInativar !== null}
        titulo="Inativar legislatura"
        mensagem={`Deseja realmente inativar "${aInativar?.nome}"? `
          + 'Ela deixará de aparecer nas listagens, mas não será apagada '
          + '(pode ser reexibida marcando "Incluir inativas").'}
        textoConfirmar="Inativar"
        processando={inativar.isPending}
        aoConfirmar={confirmarInativacao}
        aoCancelar={() => setAInativar(null)}
      />

      {/* Confirmação de reativação */}
      <DialogoConfirmacao
        aberto={aReativar !== null}
        titulo="Reativar legislatura"
        mensagem={`Deseja reativar "${aReativar?.nome}"? `
          + 'Ela voltará a aparecer nas listagens normalmente.'}
        textoConfirmar="Reativar"
        processando={reativar.isPending}
        aoConfirmar={confirmarReativacao}
        aoCancelar={() => setAReativar(null)}
      />
    </Box>
  )
}

function formatarData(iso: string): string {
  if (!iso) return ''
  const [ano, mes, dia] = iso.split('-')
  return `${dia}/${mes}/${ano}`
}
