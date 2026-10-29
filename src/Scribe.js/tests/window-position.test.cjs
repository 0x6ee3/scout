const test = require('node:test');
const assert = require('node:assert/strict');
const { panelPosition } = require('../app/window-position.cjs');
test('odd window widths and fractional DPI coordinates produce integer Electron positions', () => {
  const position = panelPosition({ x: 501, y: 600 }, { x: 0, y: 0, width: 1920, height: 1040 }, 397, 438);
  assert.deepEqual(position, [303, 150]);
  assert.ok(panelPosition({ x: -799.5, y: 300.5 }, { x: -1920, y: 0, width: 1920, height: 1040 }, 397, 438).every(Number.isInteger));
});
test('recording panel stays inside a negative-coordinate monitor work area', () => {
  assert.deepEqual(panelPosition({ x: -1919, y: 10 }, { x: -1920, y: 0, width: 1920, height: 1040 }, 397, 438), [-1920, 0]);
});
