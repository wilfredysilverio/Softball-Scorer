// JS de marcador en vivo (externalizado desde la vista VerPartido)
// Requiere que la vista defina `window.marcadorConfig = { partidoId, marcadorJsonUrl }` y que los formularios incluyan __RequestVerificationToken
(function () {
    'use strict';

    // Helpers
    async function postForm(form) {
        const fd = new FormData(form);
        const body = new URLSearchParams();
        for (const pair of fd.entries()) {
            body.append(pair[0], pair[1]);
        }
        const resp = await fetch(form.action, {
            method: 'POST',
            headers: { 'Accept': 'text/html' },
            body: body
        });
        return resp;
    }

    async function fetchMarcador(id) {
        const base = window.marcadorConfig && window.marcadorConfig.marcadorJsonUrl ? window.marcadorConfig.marcadorJsonUrl : '/Partidos/MarcadorJson';
        const resp = await fetch(base + '/' + id);
        if (!resp.ok) throw new Error('Error fetching marcador');
        return resp.json();
    }

    function showAlert(message, type = 'info') {
        let alertDiv = document.getElementById('live-alert');
        if (!alertDiv) {
            const container = document.querySelector('.container');
            alertDiv = document.createElement('div');
            alertDiv.id = 'live-alert';
            container.insertBefore(alertDiv, container.firstChild);
        }
        alertDiv.innerHTML = `<div class="alert alert-${type} alert-dismissible" role="alert">${message}<button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\"></button></div>`;
    }

    function clearAlert() {
        const alertDiv = document.getElementById('live-alert'); if (alertDiv) alertDiv.innerHTML = '';
    }

    function updateUI(json) {
        const setText = (id, value) => { const el = document.getElementById(id); if (el) el.textContent = value ?? 0; };
        setText('totCarrerasCasa', json.CarrerasCasa);
        setText('totHitsCasa', json.HitsCasa);
        setText('totErroresCasa', json.ErroresCasa);
        setText('totCarrerasVisita', json.CarrerasVisita);
        setText('totHitsVisita', json.HitsVisita);
        setText('totErroresVisita', json.ErroresVisita);
        const outsEl = document.getElementById('outs-count'); if (outsEl) outsEl.textContent = json.Outs;
        const setClass = (id, on) => { const el = document.getElementById(id); if (!el) return; if (on) el.classList.add('base-on'); else el.classList.remove('base-on'); };
        setClass('base-1', json.B1);
        setClass('base-2', json.B2);
        setClass('base-3', json.B3);

        if (json.entradas && json.entradas.length) {
            json.entradas.forEach(function (e, idx) {
                const nameCasa = `Entradas[${idx}].CarrerasCasa`;
                const nameVis = `Entradas[${idx}].CarrerasVisita`;
                const inputCasa = document.querySelector(`[name="${nameCasa}"]`);
                const inputVis = document.querySelector(`[name="${nameVis}"]`);
                if (inputCasa) inputCasa.value = e.CarrerasCasa || 0;
                if (inputVis) inputVis.value = e.CarrerasVisita || 0;
            });
        }
    }

    function animateChanges() {
        const table = document.getElementById('scoreboard-table');
        if (table) {
            table.classList.add('flash');
            setTimeout(() => table.classList.remove('flash'), 700);
        }
    }

    // SignalR
    async function initSignalR() {
        try {
            if (typeof signalR === 'undefined') return;
            const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/marcador').withAutomaticReconnect().build();
            connection.on('ActualizarMarcador', function (data) {
                try { updateUI(data); animateChanges(); clearAlert(); } catch (e) { console.error(e); }
            });
            await connection.start();
            const id = (window.marcadorConfig && window.marcadorConfig.partidoId) ? window.marcadorConfig.partidoId : null;
            if (id) await connection.invoke('JoinGroup', id);
        } catch (err) {
            console.warn('SignalR no disponible', err);
        }
    }

    (function loadSignalR() {
        // Try local copy first (/lib/signalr/signalr.min.js). If it fails, fall back to CDN.
        var local = document.createElement('script');
        local.src = '/lib/signalr/signalr.min.js';
        local.onload = initSignalR;
        local.onerror = function () {
            console.debug('Local SignalR client not found, loading CDN...');
            var s = document.createElement('script');
            s.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@7.0.5/dist/browser/signalr.min.js';
            s.onload = initSignalR;
            s.onerror = function () { console.warn('Failed loading SignalR client from CDN'); };
            document.head.appendChild(s);
        };
        document.head.appendChild(local);
    })();

    // Intercept forms inside live actions card
    function bindLiveForms() {
        const liveCard = document.querySelector('.card .card-body');
        if (!liveCard) return;
        const forms = liveCard.querySelectorAll('form');
        forms.forEach(function (form) {
            form.addEventListener('submit', async function (ev) {
                ev.preventDefault();
                clearAlert();
                try {
                    const r = await postForm(form);
                    if (!r.ok) {
                        const text = await r.text();
                        showAlert('Error del servidor: ' + (text || r.statusText), 'danger');
                        return;
                    }
                    const partidoId = (document.querySelector('input[name="id"]') || document.querySelector('input[name="partidoId"]'))?.value || (window.marcadorConfig && window.marcadorConfig.partidoId);
                    const json = await fetchMarcador(partidoId);
                    updateUI(json);
                    showAlert('Acción ejecutada correctamente', 'success');
                } catch (err) {
                    console.error(err);
                    showAlert('Error en la comunicación: ' + err.message, 'danger');
                }
            });
        });
    }

    // Init on DOM ready
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', bindLiveForms); else bindLiveForms();

})();
