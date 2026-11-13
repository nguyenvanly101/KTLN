/* Nen ban do dung chung: neu co khoa VietMap thi dung ban do vector VietMap (goc nhin Viet Nam), neu khong hoac khong tai duoc thi dung anh ban do cau hinh san. */
(function () {
  'use strict';
  var meta = function (n) { return (document.querySelector('meta[name="' + n + '"]') || {}).content || ''; };
  var styleUrl = meta('ds-map-style-url');
  window.htVnBounds = (function () { var v = (meta('ds-map-bounds') || '7.5,101.5,24.0,110.5').split(',').map(Number); return v.length === 4 && v.every(isFinite) ? [[v[0], v[1]], [v[2], v[3]]] : [[7.5, 101.5], [24.0, 110.5]]; })();
  window.htMapMinZoom = styleUrl ? (parseInt(meta('ds-map-minzoom'), 10) || 0) : 0;
  var libs = null;
  var hasWebGL = function () { try { var c = document.createElement('canvas'); return !!(window.WebGLRenderingContext && (c.getContext('webgl') || c.getContext('experimental-webgl'))); } catch (e) { return false; } };
  var script = function (src) { return new Promise(function (ok, fail) { var s = document.createElement('script'); s.src = src; s.async = false; s.onload = ok; s.onerror = function () { fail(new Error('Không tải được ' + src)); }; document.head.appendChild(s); }); };
  var loadLibs = function () {
    if (libs) return libs;
    libs = new Promise(function (ok, fail) {
      var l = document.createElement('link'); l.rel = 'stylesheet'; l.href = '/lib/vietmap/vietmap-gl.css'; document.head.appendChild(l);
      script('/lib/vietmap/vietmap-gl-csp.js').then(function () {
        if (!window.vietmapgl) throw new Error('Thiếu thư viện VietMap.');
        if (window.vietmapgl.setWorkerUrl) window.vietmapgl.setWorkerUrl('/lib/vietmap/vietmap-gl-csp-worker.js?v=20261113'); else window.vietmapgl.workerUrl = '/lib/vietmap/vietmap-gl-csp-worker.js?v=20261113';
        return script('/lib/vietmap/leaflet-vietmap-gl.js');
      }).then(function () { return window.L && window.L.vietmapGL ? ok() : fail(new Error('Thiếu lớp VietMap cho Leaflet.')); }).catch(fail);
    });
    return libs;
  };
  var rasterLayer = function () {
    var url = meta('ds-tile-url') || 'https://tile.openstreetmap.de/{z}/{x}/{y}.png', attribution = meta('ds-tile-attribution') || '© OpenStreetMap';
    var native = (function (u) { try { return new URL(u, location.href).hostname === 'tile.openstreetmap.de' ? 16 : 19; } catch (e) { return 19; } })(url);
    return L.tileLayer(url, { maxZoom: 19, maxNativeZoom: native, updateWhenZooming: false, referrerPolicy: 'strict-origin-when-cross-origin', attribution: attribution });
  };
  /** Tra ve mot nhom lop (L.LayerGroup) da gan vao ban do; goi group.whenReady(function (ok) {...}) de biet khi nen san sang. */
  window.htBaseMap = function (map) {
    var group = L.layerGroup().addTo(map), waiting = [], done = null;
    var finish = function (ok) { if (done !== null) return; done = ok; waiting.forEach(function (f) { try { f(ok); } catch (e) { } }); waiting = []; };
    group.whenReady = function (cb) { if (done !== null) cb(done); else waiting.push(cb); };
    var useRaster = function () {
      var t = rasterLayer(); t.addTo(group);
      t.on('load', function () { finish([].some.call(map.getContainer().querySelectorAll('img.leaflet-tile-loaded'), function (i) { return i.naturalWidth > 0; })); });
      t.on('tileerror', function () { setTimeout(function () { finish(false); }, 4000); });
    };
    if (!styleUrl || !hasWebGL()) { useRaster(); return group; }
    loadLibs().then(function () {
      var gl = L.vietmapGL({ style: styleUrl, padding: 0.1 });
      gl.addTo(group);
      var m = gl.getVietmapMap && gl.getVietmapMap(), gotTile = false, bad = 0, fellBack = false;
      var fallback = function () { if (fellBack || gotTile) return; fellBack = true; try { group.removeLayer(gl); } catch (e) { } if (done === null) useRaster(); };
      if (!m || !m.on) { finish(true); return; }
      // chi coi la thanh cong khi da co it nhat mot o ban do (tile) tai duoc; neu khoa bi gioi han (loi 401/403/423/429) thi quay ve anh ban do cau hinh san
      m.on('data', function (e) { if (e && e.dataType === 'source' && e.tile && !gotTile) { gotTile = true; finish(true); } });
      m.on('error', function (e) { if (!gotTile && ++bad >= 2) fallback(); });
      setTimeout(fallback, 12000);
    }).catch(function () { useRaster(); });
    return group;
  };
})();
