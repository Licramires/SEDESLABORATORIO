import { build } from 'esbuild';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('.', import.meta.url));
await build({ absWorkingDir: root, entryPoints: ['src/main.jsx'], bundle: true,
  outfile: '../wwwroot/js/operations.js', minify: true, sourcemap: false,
  target: ['es2020'], jsx: 'automatic', legalComments: 'linked',
  define: { 'process.env.NODE_ENV': '"production"' } });
