import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Stack, Alert, MenuItem,
} from '@mui/material'
import { usarCriarUsuario, usarEditarUsuario, usarPerfis } from '../api/usarUsuarios'
import { ErroRequisicao } from '../api/clienteHttp'
import type { Usuario } from '../tipos/usuario'

/**
 * Diálogo de criar/editar usuário.
 *  - Criação: informa login + perfil; o backend gera a senha (a tela
 *    mãe mostra a senha provisória num diálogo próprio).
 *  - Edição: login é imutável; troca-se o perfil.
 *
 * Decisão de UX: UM perfil por usuário (Select simples). O backend
 * modela N:N, então enviamos o perfil dentro de uma lista de um item —
 * se um dia liberarmos múltiplos, muda só este componente.
 */
export function DialogoUsuario({
  aberto,
  usuario,
  aoCriado,
  aoFechar,
}: {
  aberto: boolean
  usuario: Usuario | null // null = criação
  aoCriado: (login: string, senhaProvisoria: string) => void
  aoFechar: () => void
}) {
  const editando = usuario !== null

  const [login, setLogin] = useState('')
  const [perfilId, setPerfilId] = useState<number | ''>('')
  const [erro, setErro] = useState<string | null>(null)

  const perfis = usarPerfis()
  const criar = usarCriarUsuario()
  const editar = usarEditarUsuario()
  const salvando = criar.isPending || editar.isPending

  useEffect(() => {
    if (aberto) {
      setErro(null)
      if (usuario) {
        setLogin(usuario.login)
        setPerfilId(usuario.perfis[0]?.id ?? '')
      } else {
        setLogin('')
        setPerfilId('')
      }
    }
  }, [aberto, usuario])

  async function salvar() {
    setErro(null)

    if (!editando && !login.trim()) {
      setErro('O login é obrigatório.')
      return
    }
    if (perfilId === '') {
      setErro('Selecione um perfil.')
      return
    }

    try {
      if (editando) {
        await editar.mutateAsync({ id: usuario!.id, perfilIds: [perfilId] })
        aoFechar()
      } else {
        const resultado = await criar.mutateAsync({
          login: login.trim(),
          perfilIds: [perfilId],
        })
        // A tela-mãe abre o diálogo da senha provisória.
        aoCriado(resultado.usuario.login, resultado.senhaProvisoria)
      }
    } catch (e) {
      setErro(e instanceof ErroRequisicao
        ? e.message
        : 'Não foi possível salvar. Tente novamente.')
    }
  }

  return (
    <Dialog open={aberto} onClose={aoFechar} maxWidth="xs" fullWidth>
      <DialogTitle>{editando ? 'Editar usuário' : 'Novo usuário'}</DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Login"
            value={login}
            onChange={(e) => setLogin(e.target.value.toLowerCase().trim())}
            disabled={editando} // login é imutável após criação
            fullWidth
            autoFocus={!editando}
            helperText={editando ? 'O login não pode ser alterado.' : undefined}
          />

          <TextField
            select
            label="Perfil"
            value={perfilId}
            onChange={(e) => setPerfilId(Number(e.target.value))}
            fullWidth
            disabled={perfis.isLoading}
          >
            {perfis.data?.map((p) => (
              <MenuItem key={p.id} value={p.id}>{p.nome}</MenuItem>
            ))}
          </TextField>

          {!editando && (
            <Alert severity="info">
              Uma senha provisória será gerada automaticamente e exibida
              em seguida, para você repassar ao usuário.
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
