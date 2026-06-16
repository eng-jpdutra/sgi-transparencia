import { Stack, TextField, FormControlLabel, Checkbox, InputAdornment, Button, Box } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import type { FiltrosLegislatura } from '../tipos/legislatura'

/**
 * Barra de filtros da pesquisa de legislaturas — a "toolbar externa"
 * do template canônico (decidimos NÃO usar o filtro interno do
 * DataGrid: filtragem é sempre server-side, controlada por nós).
 *
 * É um componente "controlado": não guarda estado próprio; recebe os
 * valores e um callback de mudança. A tela-mãe é a dona do estado dos
 * filtros — assim ela pode alimentá-los na query key do TanStack Query.
 *
 * O botão "Nova" só é mostrado a quem pode criar (RBAC): a tela passa
 * podeCriar e o callback aoCriar.
 */
export function BarraFiltrosLegislatura({
  filtros,
  aoMudar,
  podeCriar,
  aoCriar,
}: {
  filtros: FiltrosLegislatura
  aoMudar: (novos: FiltrosLegislatura) => void
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
        label="Buscar por ano"
        value={filtros.ano}
        onChange={(e) =>
          aoMudar({ ...filtros, ano: e.target.value.replace(/\D/g, '').slice(0, 4) })
        }
        size="small"
        placeholder="2025"
        inputMode="numeric"
        sx={{ minWidth: 200 }}
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
        label="Incluir inativas"
      />

      {/* Empurra o botão para a direita. */}
      <Box sx={{ flexGrow: 1 }} />

      {podeCriar && (
        <Button variant="contained" startIcon={<AddIcon />} onClick={aoCriar}>
          Nova legislatura
        </Button>
      )}
    </Stack>
  )
}
