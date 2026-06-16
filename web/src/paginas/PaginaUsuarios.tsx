import { useMemo, useState } from 'react'
import { Alert, Box, Chip, IconButton, Paper, Stack, Tooltip, Typography } from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import BlockIcon from '@mui/icons-material/Block'
import KeyIcon from '@mui/icons-material/Key'
import {
  DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel,
} from '@mui/x-data-grid'
import { ptBR } from '@mui/x-data-grid/locales'
import {
  usarUsuarios, usarInativarUsuario, usarResetarSenha,
} from '../api/usarUsuarios'
import { useDebounce } from '../componentes/useDebounce'
import { BarraFiltrosUsuario } from '../componentes/BarraFiltrosUsuario'
import { DialogoUsuario } from './DialogoUsuario'
import { DialogoSenhaProvisoria } from './DialogoSenhaProvisoria'
import { DialogoConfirmacao } from '../componentes/DialogoConfirmacao'
import { ErroRequisicao } from '../api/clienteHttp'
import type { FiltrosUsuario, Usuario } from '../tipos/usuario'

/**
 * Tela de gestão de Usuários. A rota inteira vive sob GuardaPerfil
 * "Admin" (ver Rotas.tsx), então tudo aqui é função administrativa.
 */
export function PaginaUsuarios() {
  const [filtros, setFiltros] = useState<FiltrosUsuario>({
    busca: '',
    incluirInativos: false,
  })
  const buscaDebounced = useDebounce(filtros.busca)
  const [paginacao, setPaginacao] = useState<GridPaginationModel>({
    page: 0, pageSize: 20,
  })
  const [ordenacao, setOrdenacao] = useState<GridSortModel>([
    { field: 'login', sort: 'asc' },
  ])

  // Diálogos
  const [dialogoUsuarioAberto, setDialogoUsuarioAberto] = useState(false)
  const [emEdicao, setEmEdicao] = useState<Usuario | null>(null)
  const [aInativar, setAInativar] = useState<Usuario | null>(null)
  const [aResetar, setAResetar] = useState<Usuario | null>(null)
  const [senhaExibida, setSenhaExibida] = useState<{ login: string; senha: string } | null>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const { data, isLoading, isError, error, isFetching } = usarUsuarios({
    pagina: paginacao.page + 1,
    tamanhoPagina: paginacao.pageSize,
    busca: buscaDebounced,
    incluirInativos: filtros.incluirInativos,
    ordenarPor: ordenacao[0]?.field,
    descendente: ordenacao[0]?.sort === 'desc',
  })
  const inativar = usarInativarUsuario()
  const resetar = usarResetarSenha()

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

  async function confirmarReset() {
    if (!aResetar) return
    setErroAcao(null)
    try {
      const r = await resetar.mutateAsync(aResetar.id)
      const login = aResetar.login
      setAResetar(null)
      setSenhaExibida({ login, senha: r.senhaProvisoria })
    } catch (e) {
      setErroAcao(e instanceof ErroRequisicao ? e.message : 'Não foi possível resetar.')
    }
  }

  const colunas = useMemo<GridColDef<Usuario>[]>(() => [
    { field: 'login', headerName: 'Login', flex: 1, minWidth: 160 },
    {
      field: 'perfis', headerName: 'Perfis', flex: 1, minWidth: 160, sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
          {params.row.perfis.map((p) => (
            <Chip key={p.id} label={p.nome} size="small" />
          ))}
        </Stack>
      ),
    },
    {
      field: 'situacao', headerName: 'Situação', width: 160, sortable: false,
      renderCell: (params) => {
        const u = params.row
        if (!u.ativo) return <Chip label="Inativo" size="small" variant="outlined" />
        if (u.bloqueado) return <Chip label="Bloqueado" color="warning" size="small" variant="outlined" />
        if (u.deveTrocarSenha) return <Chip label="Senha provisória" color="info" size="small" variant="outlined" />
        return <Chip label="Ativo" color="success" size="small" variant="outlined" />
      },
    },
    {
      field: 'acoes', headerName: 'Ações', width: 150, sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title="Editar perfil">
            <IconButton size="small" onClick={() => { setEmEdicao(params.row); setDialogoUsuarioAberto(true) }}>
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Resetar senha">
            <IconButton size="small" onClick={() => setAResetar(params.row)}>
              <KeyIcon fontSize="small" />
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
    },
  ], [])

  return (
    <Box>
      <Typography variant="h1" color="primary" gutterBottom>
        Usuários
      </Typography>

      <Paper elevation={1} sx={{ p: 3 }}>
        <BarraFiltrosUsuario
          filtros={filtros}
          aoMudar={setFiltros}
          aoCriar={() => { setEmEdicao(null); setDialogoUsuarioAberto(true) }}
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

        <DataGrid<Usuario>
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

      {/* Criar / editar */}
      <DialogoUsuario
        aberto={dialogoUsuarioAberto}
        usuario={emEdicao}
        aoFechar={() => setDialogoUsuarioAberto(false)}
        aoCriado={(login, senha) => {
          setDialogoUsuarioAberto(false)
          setSenhaExibida({ login, senha }) // mostra a senha provisória
        }}
      />

      {/* Senha provisória (criação e reset compartilham este diálogo) */}
      <DialogoSenhaProvisoria
        aberto={senhaExibida !== null}
        login={senhaExibida?.login ?? ''}
        senha={senhaExibida?.senha ?? ''}
        aoFechar={() => setSenhaExibida(null)}
      />

      {/* Confirmar reset de senha */}
      <DialogoConfirmacao
        aberto={aResetar !== null}
        titulo="Resetar senha"
        mensagem={`Gerar uma nova senha provisória para "${aResetar?.login}"? `
          + 'As sessões abertas dele serão encerradas e ele deverá '
          + 'definir uma nova senha no próximo acesso.'}
        textoConfirmar="Resetar senha"
        processando={resetar.isPending}
        aoConfirmar={confirmarReset}
        aoCancelar={() => setAResetar(null)}
      />

      {/* Confirmar inativação */}
      <DialogoConfirmacao
        aberto={aInativar !== null}
        titulo="Inativar usuário"
        mensagem={`Deseja inativar o usuário "${aInativar?.login}"? `
          + 'Ele perderá o acesso ao sistema (pode ser reexibido '
          + 'marcando "Incluir inativos").'}
        textoConfirmar="Inativar"
        processando={inativar.isPending}
        aoConfirmar={confirmarInativacao}
        aoCancelar={() => setAInativar(null)}
      />
    </Box>
  )
}
