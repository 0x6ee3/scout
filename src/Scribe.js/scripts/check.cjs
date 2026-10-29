const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
for (const directory of ['app', 'ui', 'scripts', 'tests']) {
  for (const file of fs.readdirSync(path.join(__dirname, '..', directory))) {
    if (!/\.(cjs|js)$/.test(file)) continue;
    const target = path.join(__dirname, '..', directory, file);
    const result = spawnSync(process.execPath, ['--check', target], { encoding: 'utf8' });
    if (result.status !== 0) { process.stderr.write(result.stderr); process.exit(1); }
  }
}
console.log('JavaScript syntax checks passed.');
