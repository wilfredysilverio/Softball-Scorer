# Guia para usuarios

Esta guia explica como usar el sistema sin hablar de codigo.

## Que es este sistema

Softball Scorer es un anotador de softbol. Permite llevar el control de un partido:

- equipos,
- jugadores,
- alineacion,
- entrada actual,
- alta o baja,
- outs,
- bases ocupadas,
- carreras,
- historial de jugadas,
- estadisticas.

## Iniciar sesion

Abre:

```text
http://localhost:5118/Account/Login
```

Credenciales locales:

```text
Usuario: el correo configurado en Auth:AdminUser
Clave: la clave configurada en Auth:AdminPass
```

## Flujo normal para anotar un juego

### 1. Crear equipos

Entra a `Equipos`.

Crea los equipos que van a jugar. Por ejemplo:

- Tigres de Villa Duarte.
- Los Industriales de Santo Domingo.

### 2. Crear jugadores

Entra a `Jugadores`.

Registra cada jugador con:

- nombre,
- apellido,
- numero,
- posicion,
- equipo.

### 3. Crear partido

Entra a `Partidos`.

Crea un partido indicando:

- equipo visitante,
- equipo de casa,
- fecha.

El equipo visitante batea primero en la alta del primer ini.

### 4. Definir lineup

Despues de crear el partido, revisa el lineup.

El sistema selecciona los jugadores del roster. Si alguien no fue al juego, lo desmarcas.

Ordena o deja el orden de bateo segun corresponda.

### 5. Iniciar partido

En la pantalla del partido, presiona `Iniciar`.

El partido empieza con:

- ini 1,
- alta,
- 0 outs,
- bases limpias.

### 6. Registrar jugadas

En la seccion `Turno` selecciona el resultado:

- Hit: el bateador llega a 1B.
- Doble: el bateador llega a 2B.
- Triple: el bateador llega a 3B.
- Jonron: el bateador anota y se limpian las bases.
- Ponche: suma 1 out.
- Out: suma 1 out.

Presiona `Registrar`.

El sistema actualiza:

- bases,
- outs,
- carreras,
- bateador siguiente,
- historial,
- estadisticas.

## Que debe verse en la pantalla del partido

Debes ver:

- marcador visitante vs casa,
- carreras por entrada,
- ini actual,
- alta o baja,
- outs,
- diamante con bases ocupadas,
- bateador que sigue,
- botones de jugada,
- historial reciente.

## Bateador fuera de turno

El sistema muestra quien sigue bateando.

Si eliges otro jugador, aparece una advertencia:

```text
Este jugador no es el bateador que sigue en el orden.
¿Seguro que deseas anotar esta jugada para él?
```

Si cancelas, se mantiene el bateador correcto.

Si aceptas, la jugada se guarda como correccion manual.

## Cuando hay 3 outs

Al llegar a 3 outs:

- si estaba en alta, pasa a baja;
- si estaba en baja, avanza al siguiente ini;
- las bases quedan limpias;
- los outs vuelven a 0.

## Reportes

En la pantalla del partido puedes descargar:

- Box score.
- Jugada por jugada.

Estos archivos sirven para revisar que paso en el juego.

## Problemas comunes

### No puedo entrar

Revisa usuario y clave:

```text
Auth:AdminUser
Auth:AdminPass
```

### El partido no deja anotar

Puede ser por una de estas razones:

- el partido no esta iniciado,
- falta lineup,
- no hay bateador actual,
- la sesion expiro.

### El marcador no cambia

Refresca la pagina. Si sigue igual, revisa que MySQL este prendido y que el proyecto este corriendo.

### No veo jugadores en el lineup

Primero crea jugadores y asignales equipo. Luego vuelve a definir lineup.
