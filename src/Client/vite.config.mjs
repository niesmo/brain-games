import { defineConfig } from "vite";
import fable from "vite-plugin-fable";

export default defineConfig({
  plugins: [fable()],
  server: {
    host: "0.0.0.0",
    port: 5173,
    strictPort: true
  }
});
