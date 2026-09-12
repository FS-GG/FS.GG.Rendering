import { defineConfig } from "vite";
import { resolve } from "node:path";
export default defineConfig({ build: { target: "es2022", rollupOptions: { input: { player: resolve(import.meta.dirname, "index.html"), studio: resolve(import.meta.dirname, "studio.html") } } } });
