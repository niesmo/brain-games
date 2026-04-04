import { defineConfig } from "vite";
import fable from "vite-plugin-fable";

export default defineConfig({
  plugins: [fable()],
  server: {
    port: 5173
  }
});
