// JS de marcador en vivo (externalizado desde la vista VerPartido)
// Requiere que la vista defina `window.marcadorConfig = { partidoId, marcadorJsonUrl }` y que los formularios incluyan __RequestVerificationToken
(function () {
    'use strict';

    function valueFrom(obj, ...aliases) {
        if (!obj) return undefined;
        for (const aliasRaw of aliases) {
            const alias = aliasRaw ?? '';
            if (!alias) continue;
            if (Object.prototype.hasOwnProperty.call(obj, alias)) return obj[alias];
            const camel = alias.charAt(0).toLowerCase() + alias.slice(1);
            if (Object.prototype.hasOwnProperty.call(obj, camel)) return obj[camel];
            const pascal = alias.charAt(0).toUpperCase() + alias.slice(1);
            if (Object.prototype.hasOwnProperty.call(obj, pascal)) return obj[pascal];
        }
        return undefined;
    }

    // Helpers
    async function postForm(form, formData) {
        const fd = formData || new FormData(form);
        const body = new URLSearchParams();
        for (const pair of fd.entries()) {
            const key = pair[0];
            const value = pair[1] == null ? '' : pair[1];
            body.append(key, value);
        }
        const resp = await fetch(form.action, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
            },
            credentials: 'same-origin',
            body: body.toString()
        });
        if (resp.redirected && resp.url) {
            window.location.href = resp.url;
            return Promise.reject(new Error('Redirigiendo…'));
        }
        const contentType = resp.headers.get('content-type') || '';
        let payload = null;
        if (contentType.includes('application/json')) {
            payload = await resp.json();
        }
        else {
            if (resp.ok) {
                window.location.reload();
                return {};
            }
            payload = await resp.text();
        }
        if (!resp.ok) {
            const message = typeof payload === 'string'
                ? payload
                : (payload?.message || payload?.Message || 'Error procesando la acción');
            throw new Error(message);
        }
        return payload;
    }

    async function fetchMarcador(id) {
        const base = window.marcadorConfig && window.marcadorConfig.marcadorJsonUrl ? window.marcadorConfig.marcadorJsonUrl : '/Partidos/MarcadorJson';
        const resp = await fetch(base + '/' + id);
        if (!resp.ok) throw new Error('Error fetching marcador');
        return resp.json();
    }

    function showAlert(message, type = 'info') {
        if (!message) return;
        let alertDiv = document.getElementById('live-alert');
        if (!alertDiv) {
            const container = document.querySelector('.scorekeeper-shell, .page-shell, .container, main');
            if (!container) return;
            alertDiv = document.createElement('div');
            alertDiv.id = 'live-alert';
            container.insertBefore(alertDiv, container.firstChild);
        }
        alertDiv.innerHTML = `<div class="alert alert-${type} alert-dismissible" role="alert">${message}<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>`;
    }

    function clearAlert() {
        const alertDiv = document.getElementById('live-alert'); if (alertDiv) alertDiv.innerHTML = '';
    }

    function updateRunnerEventOptions(b1, b2, b3) {
        const baseSelect = document.getElementById('baseEvento');
        const eventSelect = document.getElementById('eventoCorredor');
        const actionBtn = document.getElementById('btn-evento-corredor');
        const hasRunners = !!(b1 || b2 || b3);

        if (baseSelect) {
            const availability = {
                1: !!b1,
                2: !!b2,
                3: !!b3
            };
            const options = baseSelect.querySelectorAll('option[data-base]');
            options.forEach(option => {
                const base = Number(option.dataset.base);
                if (!base) return;
                option.disabled = !availability[base];
            });
            baseSelect.disabled = !hasRunners;
            if (!hasRunners) {
                baseSelect.value = '';
            }
        }

        if (eventSelect) {
            eventSelect.disabled = !hasRunners;
            if (!hasRunners) {
                eventSelect.value = '';
            }
        }

        if (actionBtn) {
            actionBtn.disabled = !hasRunners;
        }
    }

    function insertBeforeTotals(row, cell) {
        if (!row) return;
        const marker = row.querySelector('[data-total="true"]');
        if (marker) {
            row.insertBefore(cell, marker);
        } else {
            row.appendChild(cell);
        }
    }

    function ensureInningColumns(required) {
        const table = document.getElementById('tabla-entradas');
        if (!table) return;
        const headerRow = table.querySelector('thead tr');
        if (!headerRow) return;
        const current = headerRow.querySelectorAll('th[data-inning]').length;
        if (!required || required <= current) return;
        const bodyRows = table.querySelectorAll('tbody tr');
        for (let inning = current + 1; inning <= required; inning++) {
            const th = document.createElement('th');
            th.className = 'text-center';
            th.dataset.inning = inning;
            th.textContent = inning;
            insertBeforeTotals(headerRow, th);
            bodyRows.forEach(row => {
                const td = document.createElement('td');
                td.className = 'text-center';
                td.dataset.side = row.id === 'fila-visita' ? 'V' : 'C';
                td.dataset.inning = inning;
                const div = document.createElement('div');
                div.className = 'r fw-semibold';
                div.textContent = '0';
                td.appendChild(div);
                insertBeforeTotals(row, td);
            });
        }
    }

    function updateInningCell(side, inning, value) {
        const cell = document.querySelector(`td[data-side="${side}"][data-inning="${inning}"]`);
        if (!cell) return;
        const r = cell.querySelector('.r');
        if (r) r.textContent = value ?? 0;
    }

    function renderMarcadorPayload(payload) {
        if (!payload) return false;
        try {
            updateUI(payload);
            animateChanges();
            return true;
        } catch (err) {
            console.error('Error renderizando marcador', err);
            return false;
        }
    }

    async function refrescarMarcador() {
        try {
            const id = (window.marcadorConfig && window.marcadorConfig.partidoId) ? window.marcadorConfig.partidoId : null;
            if (!id) return;
            const json = await fetchMarcador(id);
            renderMarcadorPayload(json);
        } catch (e) { console.error('Error refrescando marcador', e); }
    }

    function updateOutIndicators(count, prefix = '') {
        const outsNumber = Number(count) || 0;
        let updated = false;
        for (let i = 1; i <= 3; i++) {
            const dot = document.getElementById(`${prefix}out${i}`);
            if (!dot) continue;
            updated = true;
            if (outsNumber >= i) {
                dot.classList.add('filled');
                dot.classList.add('active');
            } else {
                dot.classList.remove('filled');
                dot.classList.remove('active');
            }
        }

        if (updated) return;

        const indicatorIds = prefix
            ? [`${prefix}outs-indicator`, `${prefix}outs-indicadores`]
            : ['outs-indicator', 'outs-indicadores'];
        const indicator = indicatorIds
            .map(id => document.getElementById(id))
            .find(el => !!el);
        if (!indicator) return;
        const dots = indicator.querySelectorAll('[data-out-index]');
        dots.forEach(dot => {
            const idx = Number(dot.dataset.outIndex || dot.getAttribute('data-out-index') || 0);
            if (!idx) return;
            if (outsNumber >= idx) {
                dot.classList.add('filled');
                dot.classList.add('active');
            } else {
                dot.classList.remove('filled');
                dot.classList.remove('active');
            }
        });
    }

    function updateUI(json) {
        const pick = (camel, pascal) => valueFrom(json, camel, pascal);
        const setText = (id, value) => { const el = document.getElementById(id); if (el && value !== undefined && value !== null) el.textContent = value; };
        const carrerasCasa = pick('carrerasCasa', 'CarrerasCasa') ?? 0;
        const carrerasVisita = pick('carrerasVisita', 'CarrerasVisita') ?? 0;
        setText('totCarrerasCasa', carrerasCasa);
        setText('totHitsCasa', pick('hitsCasa', 'HitsCasa') ?? 0);
        setText('totErroresCasa', pick('erroresCasa', 'ErroresCasa') ?? 0);
        setText('totCarrerasVisita', carrerasVisita);
        setText('totHitsVisita', pick('hitsVisita', 'HitsVisita') ?? 0);
        setText('totErroresVisita', pick('erroresVisita', 'ErroresVisita') ?? 0);
        setText('score-casa', carrerasCasa);
        setText('score-visita', carrerasVisita);
        setText('score-casa-publico', carrerasCasa);
        setText('score-visita-publico', carrerasVisita);
        setText('entradaActual', pick('entradaActual', 'EntradaActual') ?? 0);
        const mitadEl = document.getElementById('mitadActual'); if (mitadEl) mitadEl.textContent = `(${pick('mitad', 'Mitad') ?? 'Alta'})`;
        // Outs: soporta ids alternos
        const outsValue = pick('outs', 'Outs') ?? 0;
        ['outs-count', 'outsActuales', 'outs-count-publico', 'outs-valor'].forEach(id => setText(id, outsValue));
        updateOutIndicators(outsValue);
        updateOutIndicators(outsValue, 'publico-');
        // Bases: soporta ids alternos y clases distintas
        const setClass = (id, on) => {
            const el = document.getElementById(id);
            if (!el) return;
            if (on) { el.classList.add('base-on'); el.classList.add('activa'); }
            else { el.classList.remove('base-on'); el.classList.remove('activa'); }
        };
        // Nuevos ids en vista: base1/base2/base3 además de base-1/base-2/base-3
        const b1 = pick('b1', 'B1') ?? false;
        const b2 = pick('b2', 'B2') ?? false;
        const b3 = pick('b3', 'B3') ?? false;
        setClass('base-1', b1); setClass('base1', b1);
        setClass('base-2', b2); setClass('base2', b2);
        setClass('base-3', b3); setClass('base3', b3);
        updateRunnerEventOptions(b1, b2, b3);

        const entradas = pick('entradas', 'Entradas') || [];
        const visitaPorInning = pick('carrerasVisitaPorInning', 'CarrerasVisitaPorInning') || [];
        const casaPorInning = pick('carrerasCasaPorInning', 'CarrerasCasaPorInning') || [];
        const maxEntrada = entradas.length
            ? entradas.reduce((max, e) => {
                const inning = e.numeroInning ?? e.NumeroInning ?? 0;
                return inning > max ? inning : max;
            }, 0)
            : 0;
        const maxReportado = Math.max(maxEntrada, visitaPorInning.length, casaPorInning.length, 9);
        ensureInningColumns(maxReportado);

        if (entradas.length) {
            entradas.forEach(function (e) {
                const inning = e.numeroInning ?? e.NumeroInning;
                updateInningCell('V', inning, e.carrerasVisita ?? e.CarrerasVisita ?? 0);
                updateInningCell('C', inning, e.carrerasCasa ?? e.CarrerasCasa ?? 0);
            });
        } else {
            visitaPorInning.forEach((value, idx) => updateInningCell('V', idx + 1, value));
            casaPorInning.forEach((value, idx) => updateInningCell('C', idx + 1, value));
        }

        applyTurnoState(json);
    }

    function animateChanges() {
        // Efecto visual si existe la tabla principal
        const table = document.getElementById('scoreboard-table') || document.getElementById('tabla-entradas');
        if (table) {
            table.classList.add('flash');
            setTimeout(() => table.classList.remove('flash'), 700);
        }
    }

    // ==== SignalR conexión global ====
    let connection = null;
    let ultimoEquipoBateando = null;
    const partidoId = (window.marcadorConfig && window.marcadorConfig.partidoId) ? window.marcadorConfig.partidoId : null;
    const groupName = partidoId ? `partido-${partidoId}` : null;

    function buildConnection() {
        return new signalR.HubConnectionBuilder()
            .withUrl('/hubs/marcador')
            .withAutomaticReconnect()
            .build();
    }

    function setRealtimeBanner(message, type = 'info') {
        const banner = document.getElementById('alertaMarcador');
        if (!banner) return;
        if (!message) {
            banner.classList.add('d-none');
            banner.textContent = '';
            banner.className = 'mt-3 d-none';
            return;
        }
        banner.className = `mt-3 alert alert-${type}`;
        banner.textContent = message;
    }

    async function startConnection() {
        if (!window.signalR) {
            console.warn('SignalR client no disponible');
            return;
        }
        if (!connection) {
            connection = buildConnection();
            // Exponer conexión global para inspección/uso desde consola
            window.__srConnection = connection;
            // Eventos de ciclo de vida
            connection.onreconnecting(err => {
                console.warn('Reconectando al hub...', err);
                setRealtimeBanner('Reconectando al servicio en tiempo real…', 'warning');
            });
            connection.onreconnected(id => {
                console.info('Reconectado al hub. ConnectionId:', id);
                setRealtimeBanner(null);
            });
            connection.onclose(err => {
                console.warn('Conexión cerrada hub marcador', err);
                setRealtimeBanner('Conexión en tiempo real finalizada. Intentando reconectar…', 'danger');
            });
            // Evento de actualización
            connection.on('ActualizarMarcador', async payload => {
                try {
                    const payloadId = valueFrom(payload, 'PartidoId', 'partidoId');
                    if (payloadId !== undefined && partidoId && payloadId !== partidoId) return;
                    if (!renderMarcadorPayload(payload)) {
                        await refrescarMarcador();
                    }
                    clearAlert();
                } catch (e) { console.error('Error manejando ActualizarMarcador', e); }
            });
        }
        try {
            await connection.start();
            console.info('Conectado al Hub de marcador');
            setRealtimeBanner(null);
            if (groupName) {
                try { await connection.invoke('JoinPartido', groupName); }
                catch { try { await connection.invoke('JoinGroup', partidoId); } catch { /* ignore */ } }
            }
        } catch (e) {
            console.error('Fallo conectando al Hub, reintento en 3s', e);
            setRealtimeBanner('No se pudo conectar al hub. Reintentando…', 'danger');
            setTimeout(startConnection, 3000);
        }
    }

    // Arranca la conexión tras cargar la página (no depende de DOM completo)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startConnection);
    } else {
        startConnection();
    }

    // Limpieza al cerrar pestaña
    window.addEventListener('beforeunload', async () => {
        if (!connection) return;
        try {
            if (groupName) {
                try { await connection.invoke('LeavePartido', groupName); }
                catch { try { await connection.invoke('LeaveGroup', partidoId); } catch { } }
            }
            await connection.stop();
        } catch { }
    });

    // ==== Verificación mínima del hub (ping explícito) ====
    // Uso: en consola ejecutar: await window.__srPing();
    window.__srPing = async () => {
        if (!connection) throw new Error('Conexión SignalR no inicializada');
        return await connection.invoke('Ping');
    };

    function setFormBusy(form, busy) {
        const fieldset = form.querySelector('fieldset');
        if (fieldset) {
            if (busy) {
                fieldset.dataset.prevDisabled = fieldset.disabled ? '1' : '0';
                fieldset.disabled = true;
            } else if (fieldset.dataset.prevDisabled === '0') {
                fieldset.disabled = false;
            }
        }
        const btn = form.querySelector('#btn-registrar-turno') || form.querySelector('[type="submit"]');
        if (btn) {
            if (busy) {
                btn.dataset.originalText = btn.dataset.originalText || btn.textContent;
                btn.textContent = 'Registrando…';
                btn.disabled = true;
            } else {
                const original = btn.dataset.originalText;
                if (original) btn.textContent = original;
                if (fieldset) btn.disabled = fieldset.disabled;
            }
        }
    }

    function updateBateadorDisplays(bateadorPayload) {
        const proximoDiv = document.getElementById('proximo-bateador');
        const lineupDiv = document.getElementById('lineup-proximo-bateador');
        const jugadorInput = document.getElementById('turnoJugadorId');
        const expectedInput = document.getElementById('turnoBateadorEsperadoId');
        const hasBatter = !!bateadorPayload;
        const nombre = valueFrom(bateadorPayload, 'nombre', 'Nombre') || '';
        const apellido = valueFrom(bateadorPayload, 'apellido', 'Apellido') || '';
        const numero = valueFrom(bateadorPayload, 'numeroUniforme', 'NumeroUniforme');
        const fullName = [nombre, apellido].filter(Boolean).join(' ').trim();
        let label = 'Sin bateador asignado.';
        if (hasBatter) {
            label = fullName || 'Bateador sin nombre';
            if (numero !== undefined && numero !== null && numero !== '') {
                label += ` (${numero})`;
            }
        }

        const nextId = valueFrom(bateadorPayload, 'id', 'Id');
        const nextIdValue = hasBatter && nextId !== undefined && nextId !== null ? String(nextId) : '';

        if (jugadorInput) {
            jugadorInput.dataset.expectedId = nextIdValue;
            if (jugadorInput.tagName === 'SELECT') {
                const option = Array.from(jugadorInput.options).find(o => o.value === nextIdValue);
                if (option) jugadorInput.value = nextIdValue;
            } else {
                jugadorInput.value = nextIdValue;
            }
        }

        if (expectedInput) {
            expectedInput.value = nextIdValue;
        }

        if (proximoDiv) {
            proximoDiv.textContent = label;
            proximoDiv.classList.toggle('text-muted', !hasBatter);
        }
        if (lineupDiv) {
            lineupDiv.textContent = label;
            lineupDiv.classList.toggle('text-muted', !hasBatter);
        }
    }

    function rebuildBatterSelect(lineupPayload) {
        const jugadorInput = document.getElementById('turnoJugadorId');
        if (!jugadorInput || jugadorInput.tagName !== 'SELECT' || !Array.isArray(lineupPayload)) return;

        const currentOptions = Array.from(jugadorInput.options)
            .map(option => option.value)
            .join('|');
        const nextOptions = lineupPayload
            .map(jugador => String(valueFrom(jugador, 'id', 'Id') ?? ''))
            .filter(Boolean)
            .join('|');
        if (currentOptions === nextOptions) return;

        jugadorInput.innerHTML = '';
        lineupPayload.forEach(jugador => {
            const id = valueFrom(jugador, 'id', 'Id');
            if (id === undefined || id === null) return;
            const nombre = valueFrom(jugador, 'nombre', 'Nombre') || '';
            const apellido = valueFrom(jugador, 'apellido', 'Apellido') || '';
            const numero = valueFrom(jugador, 'numeroUniforme', 'NumeroUniforme');
            const labelBase = [nombre, apellido].filter(Boolean).join(' ').trim() || 'Jugador';
            const option = document.createElement('option');
            option.value = String(id);
            option.textContent = numero === undefined || numero === null || numero === ''
                ? labelBase
                : `${labelBase} (${numero})`;
            jugadorInput.appendChild(option);
        });
    }

    function applyTurnoState(payload) {
        const lineup = valueFrom(payload, 'lineupBateando', 'LineupBateando');
        rebuildBatterSelect(lineup);

        const equipoBateando = valueFrom(payload, 'equipoBateando', 'EquipoBateando');
        const estado = valueFrom(payload, 'estado', 'Estado') || {};
        const entradaActual = valueFrom(payload, 'entradaActual', 'EntradaActual') ?? valueFrom(estado, 'entradaActual', 'EntradaActual');
        const mitad = valueFrom(payload, 'mitad', 'Mitad') ?? valueFrom(estado, 'mitad', 'Mitad');
        const equipoBateandoEl = document.getElementById('equipo-bateando');
        const turnoEquipoEl = document.getElementById('turno-equipo-bateando');
        const estadoMitadEl = document.getElementById('estado-mitad');

        if (equipoBateando) {
            if (equipoBateandoEl) equipoBateandoEl.textContent = equipoBateando;
            if (turnoEquipoEl) turnoEquipoEl.textContent = equipoBateando;
            if (ultimoEquipoBateando && ultimoEquipoBateando !== equipoBateando) {
                showAlert(`Cambio de turno: ahora batea ${equipoBateando}.`, 'info');
            }
            ultimoEquipoBateando = equipoBateando;
        }
        if (estadoMitadEl && mitad && entradaActual !== undefined && entradaActual !== null) {
            estadoMitadEl.textContent = `${mitad} del ${entradaActual} ini`;
        }

        const bateador = valueFrom(payload, 'bateador', 'Bateador', 'BateadorEsperado', 'ProximoBateador');
        updateBateadorDisplays(bateador);

        const form = document.querySelector('form[data-live="registrar"]');
        if (!form) return;
        const fieldset = form.querySelector('fieldset');
        const puedeRegistrar = !!valueFrom(payload, 'puedeRegistrar', 'PuedeRegistrar');
        if (fieldset) {
            fieldset.disabled = !puedeRegistrar;
            fieldset.dataset.prevDisabled = puedeRegistrar ? '0' : '1';
        }
        const btn = form.querySelector('#btn-registrar-turno') || form.querySelector('[type="submit"]');
        if (btn) {
            btn.disabled = !puedeRegistrar;
        }
        const motivo = valueFrom(payload, 'motivoBloqueo', 'MotivoBloqueo');
        const motivoDiv = document.getElementById('motivo-bloqueo-turno');
        if (motivoDiv) {
            if (motivo) {
                motivoDiv.textContent = motivo;
                motivoDiv.className = 'alert alert-warning mb-2';
            } else {
                motivoDiv.textContent = '';
                motivoDiv.className = 'd-none';
            }
        }
    }

    function prepararConfirmacionFueraTurno(form, fd) {
        if (!form || form.dataset.live !== 'registrar') return true;
        const jugadorInput = document.getElementById('turnoJugadorId');
        const expectedInput = document.getElementById('turnoBateadorEsperadoId');
        const confirmInput = document.getElementById('confirmarFueraTurno');
        const jugadorId = jugadorInput ? String(jugadorInput.value || '') : '';
        const esperadoId = expectedInput ? String(expectedInput.value || jugadorInput?.dataset.expectedId || '') : '';

        if (!jugadorId || !esperadoId || jugadorId === esperadoId) {
            if (confirmInput) confirmInput.value = 'false';
            fd.set('ConfirmarFueraTurno', 'false');
            return true;
        }

        const accepted = window.confirm('Este jugador no es el bateador que sigue en el orden. ¿Seguro que deseas anotar esta jugada para él?');
        if (!accepted) {
            if (jugadorInput) jugadorInput.value = esperadoId;
            return false;
        }

        if (confirmInput) confirmInput.value = 'true';
        fd.set('ConfirmarFueraTurno', 'true');
        return true;
    }

    // Intercept only the Registrar Turno form to update marcador without full reload
    function bindLiveForms() {
        const forms = document.querySelectorAll('form[data-live]');
        if (!forms || forms.length === 0) return;
        forms.forEach(function (form) {
            const fieldset = form.querySelector('fieldset');
            if (fieldset) fieldset.dataset.prevDisabled = fieldset.disabled ? '1' : '0';
            form.addEventListener('submit', async function (ev) {
                ev.preventDefault();
                if (form.dataset.submitting === '1') return;
                clearAlert();
                const fd = new FormData(form);
                if (!prepararConfirmacionFueraTurno(form, fd)) {
                    showAlert('Se mantuvo el bateador que sigue en el lineup.', 'warning');
                    return;
                }
                form.dataset.submitting = '1';
                setFormBusy(form, true);
                try {
                    const payload = await postForm(form, fd);
                    if (!payload || payload.ok === false) {
                        if (payload?.requiereConfirmacion) {
                            const accepted = window.confirm(payload.message || 'Este jugador no es el bateador que sigue en el orden. ¿Seguro que deseas anotar esta jugada para él?');
                            if (accepted) {
                                fd.set('ConfirmarFueraTurno', 'true');
                                const confirmInput = document.getElementById('confirmarFueraTurno');
                                if (confirmInput) confirmInput.value = 'true';
                                const retryPayload = await postForm(form, fd);
                                await refrescarMarcador();
                                applyTurnoState(retryPayload);
                                showAlert(retryPayload.message || 'Turno registrado como corrección manual.', 'success');
                                return;
                            }
                        }
                        const msg = payload?.message || 'Acción rechazada.';
                        showAlert(msg, 'danger');
                    } else {
                        await refrescarMarcador();
                        const kind = form.dataset.live;
                        if (kind === 'registrar') {
                            applyTurnoState(payload);
                        } else if (kind === 'evento') {
                            const eventSelect = document.getElementById('eventoCorredor');
                            const baseSelect = document.getElementById('baseEvento');
                            if (eventSelect) eventSelect.value = '';
                            if (baseSelect) baseSelect.value = '';
                        }
                        const defaultMsg = kind === 'evento'
                            ? 'Evento registrado correctamente.'
                            : 'Turno registrado correctamente.';
                        showAlert(payload.message || defaultMsg, 'success');
                    }
                } catch (err) {
                    console.error(err);
                    showAlert(err.message || 'Error en la comunicación.', 'danger');
                } finally {
                    const confirmInput = document.getElementById('confirmarFueraTurno');
                    if (confirmInput) confirmInput.value = 'false';
                    form.dataset.submitting = '0';
                    setFormBusy(form, false);
                }
            });
        });
    }

    function bindQuickResultButtons() {
        const resultado = document.getElementById('resultadoSelect');
        if (!resultado) return;

        document.querySelectorAll('[data-result-text]').forEach(button => {
            button.addEventListener('click', () => {
                const text = button.getAttribute('data-result-text');
                const option = Array.from(resultado.options).find(o => o.text === text || o.value === text);
                if (option) {
                    resultado.value = option.value;
                    resultado.dispatchEvent(new Event('change', { bubbles: true }));
                }
            });
        });
    }

    // Init on DOM ready
    function initFormsAndData() {
        bindQuickResultButtons();
        bindLiveForms();
        refrescarMarcador();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initFormsAndData); else initFormsAndData();

})();
