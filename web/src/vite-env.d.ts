/// <reference types="vite/client" />

// Tipagem das variáveis de ambiente — para o TypeScript conhecer
// import.meta.env.VITE_API_URL e autocompletar/validar seu uso.
interface ImportMetaEnv {
  readonly VITE_API_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
