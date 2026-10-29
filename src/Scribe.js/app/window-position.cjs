function panelPosition(cursor, area, width, height) {
  return [
    Math.round(Math.max(area.x, Math.min(cursor.x - width / 2, area.x + area.width - width))),
    Math.round(Math.max(area.y, Math.min(cursor.y - height - 12, area.y + area.height - height)))
  ];
}
module.exports = { panelPosition };
