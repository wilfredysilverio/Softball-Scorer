// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Helpers: confirmación modal para eliminar/acciones y activar validación Bootstrap
(function () {
	// Confirmación global: elementos con data-confirm="mensaje" y data-target-form="#formId" o data-form-action
	document.addEventListener('click', function (e) {
		var btn = e.target.closest('[data-confirm]');
		if (!btn) return;
		e.preventDefault();
		var msg = btn.getAttribute('data-confirm') || '¿Deseas continuar?';
		var targetForm = btn.getAttribute('data-target-form');
		var formAction = btn.getAttribute('data-form-action');

		var modalEl = document.getElementById('confirmModal');
		if (!modalEl) return;
		var modal = new bootstrap.Modal(modalEl);
		modalEl.querySelector('#confirmModalMessage').textContent = msg;

		var okBtn = modalEl.querySelector('#confirmModalOk');
		okBtn.addEventListener('click', function () {
			modal.hide();
			if (targetForm) {
				var f = document.querySelector(targetForm);
				if (f) f.submit();
				return;
			}
			if (formAction) {
				var f = document.createElement('form');
				f.method = 'post';
				f.action = formAction;
				var token = document.querySelector('input[name="__RequestVerificationToken"]');
				if (token) {
					var i = document.createElement('input'); i.type = 'hidden'; i.name = '__RequestVerificationToken'; i.value = token.value; f.appendChild(i);
				}
				document.body.appendChild(f);
				f.submit();
				return;
			}
			var href = btn.getAttribute('href');
			if (href) location.href = href;
		}, { once: true });

		modal.show();
	});

	// Activar estilos de validación Bootstrap en formularios con .needs-validation
	window.addEventListener('load', function () {
		var forms = document.getElementsByClassName('needs-validation');
		Array.prototype.filter.call(forms, function (form) {
			form.addEventListener('submit', function (event) {
				if (!form.checkValidity()) {
					event.preventDefault();
					event.stopPropagation();
				}
				form.classList.add('was-validated');
			}, false);
		});
	});

	// Filas expandibles genéricas (jugadores, equipos, partidos): click y teclado (Enter/Space)
	document.addEventListener('DOMContentLoaded', function () {
		var selector = '.fila-jugador, .fila-equipo, .fila-partido';
		var filas = document.querySelectorAll(selector);
		filas.forEach(function (fila) {
			var toggleRow = function () {
				var id = fila.getAttribute('data-id');
				if (!id) return;

				var tipo = null;
				if (fila.classList.contains('fila-jugador')) tipo = 'jugador';
				else if (fila.classList.contains('fila-equipo')) tipo = 'equipo';
				else if (fila.classList.contains('fila-partido')) tipo = 'partido';
				if (!tipo) return;

				var target = document.getElementById('acciones-' + id) || document.getElementById('acciones-' + tipo + '-' + id);
				if (!target) return;

				// Cerrar otras del mismo tipo
				var openSelector = '.acciones-' + (tipo === 'jugador' ? 'jugador' : tipo) + '.show';
				document.querySelectorAll(openSelector).forEach(function (openEl) {
					if (openEl !== target) {
						var bsOther = bootstrap.Collapse.getInstance(openEl) || new bootstrap.Collapse(openEl, { toggle: false });
						bsOther.hide();
						var otherId = openEl.id.split('-').pop();
						var maybeRow = document.querySelector('[data-id="' + otherId + '"]');
						if (maybeRow) {
							maybeRow.classList.remove('table-active');
							maybeRow.setAttribute('aria-expanded', 'false');
						}
					}
				});

				var instance = bootstrap.Collapse.getInstance(target) || new bootstrap.Collapse(target, { toggle: false });
				if (target.classList.contains('show')) {
					instance.hide();
					fila.classList.remove('table-active');
					fila.setAttribute('aria-expanded', 'false');
				} else {
					instance.show();
					document.querySelectorAll(selector + '.table-active').forEach(function (f) { f.classList.remove('table-active'); });
					fila.classList.add('table-active');
					fila.setAttribute('aria-expanded', 'true');
					var btn = target.querySelector('a, button');
					if (btn) btn.focus();
				}
			};

			fila.addEventListener('click', function () { toggleRow(); });
			fila.addEventListener('keydown', function (e) {
				if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
					e.preventDefault();
					toggleRow();
				}
			});
		});
	});
})();
