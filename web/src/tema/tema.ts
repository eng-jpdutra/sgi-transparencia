import { createTheme } from '@mui/material/styles'
import { ptBR } from '@mui/material/locale'

// Tema central da aplicação — fonte ÚNICA de verdade visual.
// Cores, tipografia e formas vivem AQUI, nunca espalhadas como
// valores soltos pelos componentes (DRY aplicado ao design).
//
// Direção estética: identidade institucional de transparência
// pública. Sóbria, legível e confiável — a credibilidade vem da
// clareza e da consistência, não de ornamento. A cor de apoio é um
// verde-petróleo institucional (governo/seriedade) usado com
// parcimônia; o restante é neutro e disciplinado.
export const tema = createTheme(
  {
    palette: {
      primary: {
        // Azul-petróleo escuro: sério, institucional, alto contraste.
        main: '#1f4e5f',
        light: '#3a6b7d',
        dark: '#143540',
        contrastText: '#ffffff',
      },
      secondary: {
        // Âmbar discreto, reservado para destaques pontuais.
        main: '#b07d2b',
        contrastText: '#ffffff',
      },
      background: {
        // Cinza muito claro no fundo, branco nas superfícies —
        // separa visualmente o "papel" do "ambiente".
        default: '#f5f6f7',
        paper: '#ffffff',
      },
      error: { main: '#b3261e' },
      success: { main: '#2e6b3e' },
    },

    typography: {
      // Pilha de fontes do sistema: carrega instantâneo, legível em
      // qualquer SO, sem dependência externa. Trocaremos por uma
      // tipografia de marca se/quando houver direção visual definida.
      fontFamily: [
        'system-ui', '-apple-system', 'Segoe UI', 'Roboto',
        'Helvetica', 'Arial', 'sans-serif',
      ].join(','),
      h1: { fontSize: '1.9rem', fontWeight: 700 },
      h2: { fontSize: '1.5rem', fontWeight: 700 },
      button: {
        // Botões institucionais não gritam: sem caixa-alta forçada.
        textTransform: 'none',
        fontWeight: 600,
      },
    },

    shape: {
      // Cantos discretamente arredondados: moderno sem ser lúdico.
      borderRadius: 8,
    },

    components: {
      // Botões sem sombra por padrão — superfície mais sóbria.
      MuiButton: {
        defaultProps: { disableElevation: true },
      },
    },
  },
  // Carrega as traduções do MUI em português do Brasil (textos de
  // componentes como paginação, filtros do DataGrid, etc.).
  ptBR,
)
