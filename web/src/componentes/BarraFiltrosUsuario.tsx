import { Stack, TextField, FormControlLabel, Checkbox, InputAdornment, Button, Box } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import type { FiltrosUsuario } from '../tipos/usuario'

/** Barra de filtros da pesquisa de usuários (template canônico). */
export function BarraFiltrosUsuario({
  filtros,
  aoMudar,
  aoCriar,
}: {
  filtros: FiltrosUsuario
  aoMudar: (novos: FiltrosUsuario) => void
  aoCriar: () => void
}) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      alignItems={{ sm: 'center' }}
      sx={{ mb: 2 }}
    >
      <TextField
        label="Buscar por login"
        value={filtros.busca}
        onChange={(e) => aoMudar({ ...filtros, busca: e.target.value })}
        size="small"
        sx={{ minWidth: 260 }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }}
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={filtros.incluirInativos}
            onChange={(e) =>
              aoMudar({ ...filtros, incluirInativos: e.target.checked })
            }
          />
        }
        label="Incluir inativos"
      />

      <Box sx={{ flexGrow: 1 }} />

      {/* A tela inteira já é Admin-only, então o botão sempre aparece. */}
      <Button variant="contained" startIcon={<AddIcon />} onClick={aoCriar}>
        Novo usuário
      </Button>
    </Stack>
  )
}
