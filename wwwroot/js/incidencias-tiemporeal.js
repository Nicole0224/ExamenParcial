/* Actualización en tiempo real de incidencias mediante PieSocket.
 * - Se conecta al canal WebSocket indicado en data-tr-url (solo URL pública).
 * - Al recibir IncidenciaActualizada actualiza la fila sin recargar la página.
 * - Al reconectar, reconsulta el estado vigente (data-lista-url) y reintenta
 *   la conexión con espera progresiva. */
(function () {
    var root = document.getElementById('incidencias-tr');
    if (!root) {
        return;
    }

    var wsUrl = root.getAttribute('data-tr-url');
    var listaUrl = root.getAttribute('data-lista-url');
    var estadoEl = document.getElementById('tr-estado');
    var avisosEl = document.getElementById('tr-avisos');
    var cuerpo = document.getElementById('incidencias-cuerpo');
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';

    var socket = null;
    var desconectado = false;
    var reintentoMs = 1000;
    var maxReintentoMs = 15000;

    function setEstado(texto, clase) {
        if (!estadoEl) {
            return;
        }
        estadoEl.textContent = texto;
        estadoEl.className = 'badge ' + clase;
    }

    function avisar(texto) {
        if (!avisosEl) {
            return;
        }
        var div = document.createElement('div');
        div.className = 'alert alert-info alert-dismissible fade show';
        div.setAttribute('role', 'alert');
        div.textContent = texto;
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn-close';
        btn.setAttribute('data-bs-dismiss', 'alert');
        btn.setAttribute('aria-label', 'Cerrar');
        div.appendChild(btn);
        avisosEl.prepend(div);
    }

    function esc(texto) {
        return String(texto)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // Tolera las formas {event, data} y {message: {event, data}}.
    function extraerEvento(mensaje) {
        if (!mensaje || typeof mensaje !== 'object') {
            return null;
        }
        var env = mensaje.message && typeof mensaje.message === 'object' ? mensaje.message : mensaje;
        if (typeof env.event !== 'string') {
            return null;
        }
        return { evento: env.event, datos: env.data || {} };
    }

    function actualizarVacio() {
        var vacio = document.getElementById('incidencias-vacio');
        var hayFilas = cuerpo && cuerpo.querySelector('tr') !== null;
        if (hayFilas && vacio) {
            vacio.remove();
        } else if (!hayFilas && !vacio && cuerpo) {
            var p = document.createElement('p');
            p.id = 'incidencias-vacio';
            p.className = 'text-muted';
            p.textContent = 'No hay incidencias abiertas para mostrar.';
            cuerpo.parentElement.after(p);
        }
    }

    function aplicarEvento(datos) {
        var id = Number(datos.Id !== undefined ? datos.Id : datos.id);
        var estado = String(datos.Estado !== undefined ? datos.Estado : (datos.estado || ''));
        if (!id) {
            return;
        }
        var fila = cuerpo ? cuerpo.querySelector('tr[data-incidencia-id="' + id + '"]') : null;
        if (/cerrada/i.test(estado)) {
            if (fila) {
                fila.remove();
            }
            avisar('La incidencia #' + id + ' fue cerrada.');
            actualizarVacio();
        }
    }

    function filaHtml(item) {
        var fecha = '';
        try {
            fecha = new Date(item.fechaReporte || item.FechaReporte).toLocaleString();
        } catch (e) {
            fecha = '';
        }
        return '<tr data-incidencia-id="' + esc(item.id !== undefined ? item.id : item.Id) + '">' +
            '<td>' + esc(item.id !== undefined ? item.id : item.Id) + '</td>' +
            '<td>' + esc(item.estacion !== undefined ? item.estacion : item.Estacion) + '</td>' +
            '<td>' + esc(item.descripcion !== undefined ? item.descripcion : item.Descripcion) + '</td>' +
            '<td>' + esc(item.prioridad !== undefined ? item.prioridad : item.Prioridad) + '</td>' +
            '<td>' + esc(fecha) + '</td>' +
            '<td><form action="/Operaciones/Incidencias/Cerrar/' + esc(item.id !== undefined ? item.id : item.Id) + '" method="post" class="d-inline">' +
            '<input name="__RequestVerificationToken" type="hidden" value="' + esc(token) + '" />' +
            '<button type="submit" class="btn btn-sm btn-outline-danger">Cerrar</button>' +
            '</form></td></tr>';
    }

    function reconsultar() {
        if (!listaUrl) {
            return Promise.resolve();
        }
        return fetch(listaUrl, { headers: { 'Accept': 'application/json' } })
            .then(function (r) {
                if (!r.ok) {
                    throw new Error('HTTP ' + r.status);
                }
                return r.json();
            })
            .then(function (items) {
                if (!cuerpo) {
                    return;
                }
                cuerpo.innerHTML = '';
                (items || []).forEach(function (item) {
                    cuerpo.insertAdjacentHTML('beforeend', filaHtml(item));
                });
                tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                token = tokenInput ? tokenInput.value : token;
                actualizarVacio();
            });
    }

    function conectar() {
        if (!wsUrl) {
            setEstado('No disponible', 'text-bg-warning');
            return;
        }
        setEstado('Conectando...', 'text-bg-secondary');
        try {
            socket = new WebSocket(wsUrl);
        } catch (e) {
            programarReconexion();
            return;
        }

        socket.onopen = function () {
            setEstado('Conectado', 'text-bg-success');
            reintentoMs = 1000;
            if (desconectado) {
                desconectado = false;
                reconsultar().catch(function () { /* se reintentará en la próxima reconexión */ });
            }
        };

        socket.onmessage = function (ev) {
            var env = null;
            try {
                env = extraerEvento(JSON.parse(ev.data));
            } catch (e) {
                return;
            }
            if (env && env.evento === 'IncidenciaActualizada') {
                aplicarEvento(env.datos);
            }
        };

        socket.onerror = function () {
            try {
                socket.close();
            } catch (e) {
                /* noop */
            }
        };

        socket.onclose = function () {
            desconectado = true;
            setEstado('Reconectando...', 'text-bg-warning');
            programarReconexion();
        };
    }

    function programarReconexion() {
        setTimeout(conectar, reintentoMs);
        reintentoMs = Math.min(reintentoMs * 2, maxReintentoMs);
    }

    conectar();
})();
