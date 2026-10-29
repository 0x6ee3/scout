const path = require('node:path');
async function main() {
  const { packager } = await import('@electron/packager');
  const root = path.resolve(__dirname, '..');
  const output = await packager({ dir: root, out: path.join(root, 'dist'), name: 'Scribe.js',
    executableName: 'Scribe.js', platform: 'win32', arch: 'x64', electronVersion: require('electron/package.json').version,
    icon: path.join(root, 'assets/app.ico'), asar: false, prune: true, overwrite: true,
    ignore: [/^\/dist($|\/)/, /^\/\.cache($|\/)/, /^\/test-results($|\/)/, /^\/tests($|\/)/, /^\/scripts($|\/)/],
    download: { cacheRoot: path.join(root, '.cache/electron') },
    win32metadata: { CompanyName: 'Scout experiments', FileDescription: 'Scribe.js local audio recorder', ProductName: 'Scribe.js' }
  });
  console.log(`Portable application: ${path.join(output[0], 'Scribe.js.exe')}`);
}
main().catch(error => { console.error(error); process.exitCode = 1; });
