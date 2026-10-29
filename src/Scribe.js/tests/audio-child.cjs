const { AudioSession } = require('../app/audio-session.cjs');
const session = new AudioSession({ root: process.argv[2], synthetic: true, notify: message => {
  if (message.type === 'fault') { console.error(message.error); process.exitCode = 1; void session.stop(); }
} });
session.start().then(() => process.send({ directory: session.directory })).catch(error => { console.error(error); process.exit(1); });
