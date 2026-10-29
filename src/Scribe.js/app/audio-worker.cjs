const { AudioSession } = require('./audio-session.cjs');
// Electron utilityProcess gives native capture a separate process and crash boundary.
const port = process.parentPort;
let recording;
let startPromise;
let finishing;
const send = message => port.postMessage(message);
async function stop(error) {
  if (finishing) return finishing;
  finishing = (async () => {
    if (error && recording) recording.failure ??= error;
    await startPromise?.catch(() => {});
    let result;
    try { result = await recording?.stop(); }
    catch (failure) { result = { completed: false, error: failure.message, directory: recording?.directory }; }
    send({ type: 'stopped', result });
    setTimeout(() => process.exit(result?.completed ? 0 : 1), 30);
  })();
  return finishing;
}
port.on('message', ({ data }) => {
  if (data.type === 'probe-native' && !recording) {
    const { openPlayback } = require('./wasapi.cjs');
    openPlayback(data.excludedPid).then(source => {
      const metadata = source.metadata;
      source.close();
      send({ type: 'probe-result', metadata });
      setTimeout(() => process.exit(0), 30);
    }).catch(error => { send({ type: 'probe-result', error: error.message }); setTimeout(() => process.exit(1), 30); });
  } else if (data.type === 'start' && !recording) {
    recording = new AudioSession({ ...data.options, notify: message => {
      send(message);
      if (message.type === 'fault') void stop(message.error);
    } });
    startPromise = recording.start();
    startPromise.then(result => { if (!finishing) send({ type: 'started', ...result }); })
      .catch(error => { send({ type: 'fault', error: error.message }); void stop(error.message); });
  } else if (data.type === 'stop') void stop();
});
process.on('uncaughtException', error => { send({ type: 'fault', error: error.message }); void stop(error.message); });
process.on('unhandledRejection', error => { send({ type: 'fault', error: String(error) }); void stop(String(error)); });
