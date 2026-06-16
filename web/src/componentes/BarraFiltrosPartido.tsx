import { Stack, TextField, FormControlLabel, Checkbox, InputAdornment, Button, Box } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import type { FiltrosPartido } from '../tipos/partido'

/**
 * Barra de filtros da pesquisa de partidos — toolbar externa do
 * template canônico. Componente controlado: a tela-mãe é dona do
 * estado dos filtros (alimenta a query key do TanStack Query).
 * O botão "Novo" só aparece para quem pode criar (RBAC).
 */
export function BarraFiltrosPartido({
  filtros,
  aoMudar,
  podeCriar,
  aoCriar,
}: {
  filtros: FiltrosPartido
  aoMudar: (novos: FiltrosPartido) => void
  podeCriar: boolean
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
        label="Buscar por sigla ou nome"
        value={filtros.busca}
        onChange={(e) => aoMudar({ ...filtros, busca: e.target.value })}
        size="small"
        sx={{ minWidth: 280 }}
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

      {podeCriar && (
        <Button variant="contained" startIcon={<AddIcon />} onClick={aoCriar}>
          Novo partido
        </Button>
      )}
    </Stack>
  )
}
