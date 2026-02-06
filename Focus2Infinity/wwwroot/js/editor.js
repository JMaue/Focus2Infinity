window.overlayEditor = {
  getCanvasRect: function (elementId) {
    var el = document.getElementById(elementId);
    if (!el) return null;
    var r = el.getBoundingClientRect();
    return { left: r.left, top: r.top, width: r.width, height: r.height };
  }
};
