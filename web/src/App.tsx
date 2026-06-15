import { Rotas } from './rotas/Rotas'

// App do sub-passo 6.4: a orquestração por "if" do 6.3 deu lugar ao
// roteador. Toda a decisão de qual tela mostrar agora vive em
// Rotas.tsx, baseada na URL e nos guardas. O App só delega.
export default function App() {
  return <Rotas />
}
