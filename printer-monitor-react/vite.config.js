import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    server: {
        port: 5173,
        host: true,       // permite acessar via IP da rede
        open: false,
        cors: true        // aceita requisições de outros hosts
    }
});
