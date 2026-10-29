const test = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { Coordinator } = require('../app/coordinator.cjs');

function fixture(error = null) {
  const workers = [];
  const preferences = { value: { recordingFolder: 'C:\\test-output' }, error };
  const coordinator = new Coordinator({ preferences, createWorker: () => {
    const worker = new EventEmitter();
    worker.messages = [];
    worker.postMessage = message => worker.messages.push(message);
    worker.kill = () => {};
    workers.push(worker);
    return worker;
  } });
  return { coordinator, workers };
}
test('startup is idle and duplicate Starts cannot create competing workers', () => {
  const { coordinator, workers } = fixture();
  assert.equal(coordinator.state.phase, 'idle');
  assert.equal(workers.length, 0);
  coordinator.start(); coordinator.start();
  assert.equal(workers.length, 1);
  workers[0].emit('message', { type: 'started', directory: 'session-1', startedAt: 10 });
  assert.equal(coordinator.state.phase, 'recording');
  workers[0].emit('message', { type: 'stopped', result: { completed: true, directory: 'session-1' } });
  assert.equal(coordinator.state.phase, 'saved');
});
test('Stop during startup remains finalizing even if a delayed ready event arrives', async () => {
  const { coordinator, workers } = fixture();
  coordinator.start();
  const stop = coordinator.stop();
  workers[0].emit('message', { type: 'started', directory: 'session-2', startedAt: 20 });
  assert.equal(coordinator.state.phase, 'finalizing');
  workers[0].emit('message', { type: 'stopped', result: { completed: true, directory: 'session-2' } });
  await stop;
  assert.equal(coordinator.state.phase, 'saved');
});
test('worker death exposes failure and permits a fresh session', () => {
  const { coordinator, workers } = fixture();
  coordinator.start();
  workers[0].emit('exit', 7);
  assert.equal(coordinator.state.phase, 'failed');
  assert.match(coordinator.state.error, /unexpectedly/);
  coordinator.start();
  assert.equal(workers.length, 2);
  workers[1].emit('message', { type: 'stopped', result: { completed: true } });
});
test('invalid preferences never open capture devices', () => {
  const { coordinator, workers } = fixture('invalid saved settings');
  assert.throws(() => coordinator.start(), /invalid saved/);
  assert.equal(workers.length, 0);
});
test('every capture worker receives the selected microphone ID without default fallback', () => {
  const { coordinator, workers } = fixture();
  coordinator.preferences.value.microphoneDeviceId = 'test:camera';
  coordinator.start();
  assert.equal(workers[0].messages[0].options.microphoneDeviceId, 'test:camera');
  workers[0].emit('message', { type: 'stopped', result: { completed: true } });
});
