const path = require('node:path');
const { AudioSession } = require('../app/audio-session.cjs');
const args = process.argv.slice(2);
function option(name, fallback) { const index = args.indexOf(name); return index === -1 ? fallback : args[index + 1]; }
const seconds = Number(option('--seconds', 12));
if (!Number.isFinite(seconds) || seconds < 2 || seconds > 5400) throw new Error('--seconds must be between 2 and 5400.');
const root = path.resolve(option('--output', path.join(__dirname, '../test-results/synthetic')));
let ending;
const session = new AudioSession({ root, synthetic: true, notify: message => {
  if (message.type === 'fault') { console.error(message.error); void finish(); }
} });
async function finish() {
  if (ending) return ending;
  ending = session.stop().then(result => {
    console.log(JSON.stringify(result, null, 2));
    process.exitCode = result.completed ? 0 : 1;
  });
  return ending;
}
process.on('SIGINT', () => { void finish(); });
session.start().then(async () => {
  console.log(`Synthetic sources only; no devices or speakers. Output: ${session.directory}`);
  const timeout = setTimeout(() => { void finish(); }, seconds * 1000);
  timeout.unref();
}).catch(async error => { console.error(error); session.failure ??= error.message; await finish(); });
