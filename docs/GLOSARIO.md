# Glosario

Este documento explica palabras usadas en el proyecto.

## Softbol

### Anotador

Persona que registra lo que pasa en el partido.

### Equipo visitante

Equipo que batea primero. Batea en la alta del ini.

### Equipo casa

Equipo local. Batea en la baja del ini.

### Ini

Entrada o inning.

### Alta

Primera mitad de una entrada. Batea el visitante.

### Baja

Segunda mitad de una entrada. Batea el equipo de casa.

### Out o aut

Cuando la defensa elimina a un bateador o corredor.

### Ponche

El bateador falla el turno por strikes. Suma 1 out.

### Hit o sencillo

El bateador llega a primera base.

### Doble

El bateador llega a segunda base.

### Triple

El bateador llega a tercera base.

### Jonron

El bateador saca la pelota o completa la vuelta al home. Anota carrera el bateador y tambien los corredores en base.

### Base por bolas

El bateador recibe boleto y llega a primera.

### Lineup

Orden de bateo del equipo.

### Bateador actual

Jugador que le toca batear segun el lineup.

### Bateador fuera de turno

Jugador que fue seleccionado aunque no era el siguiente en el orden.

### Correccion manual

Jugada registrada por decision del anotador aunque no coincide con el flujo normal.

## Estadisticas

### AB

Turnos oficiales al bate.

### PA

Apariciones al plato.

### H

Hits.

### HR

Jonrones.

### RBI

Carreras impulsadas.

### R

Carreras anotadas por el jugador.

### BB

Bases por bolas.

### SO

Ponches recibidos.

### HBP

Golpeado por lanzamiento.

### SF

Sacrificio fly.

### SH

Sacrificio toque.

### OBP

Porcentaje de embasarse.

### SLG

Slugging.

### OPS

OBP + SLG.

## Programacion

### Controller

Clase que recibe solicitudes del navegador. Ejemplo: `PartidosController`.

### View

Pantalla que ve el usuario. Ejemplo: `VerPartido.cshtml`.

### Model

Clase que representa datos. Ejemplo: `Partido`.

### Service

Clase con logica importante. Ejemplo: `MarcadorService`.

### DbContext

Clase que conecta C# con la base de datos. En este proyecto es `ContextoMarcador`.

### Migration

Archivo que describe un cambio en la base de datos.

### Seed

Datos iniciales que se crean automaticamente.

### SignalR

Tecnologia para avisar cambios en vivo al navegador.

### DTO

Objeto simple para enviar datos de un lado a otro.

### Snapshot

Copia del estado del partido antes o despues de una jugada.
