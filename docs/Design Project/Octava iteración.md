# Octava iteración

Objetivo: Refinar el Módulo de juego para rechazar solicitudes de jugada basadas en una versión anterior del estado de la partida.

### Paso 2: Drivers para establecer el objetivo de la iteración

- **QAS-Seguridad-1 (ASR-01):** Cuando STK-05 (Adversario mediante un cliente modificado) envía una jugada inválida, el servidor rechaza el 100 % de las solicitudes identificadas como inválidas sin colocar fichas, cambiar el turno, modificar el conteo ni persistir un movimiento.
- **ASR-05:** La solicitud se recibe mediante el contrato remoto de partida en curso. El cliente accede al estado de la partida a través del servicio.

### Paso 3: Elementos a refinar

La segunda iteración ya asignó al Módulo de juego la coordinación de la partida y al Módulo motor de juego la validación de las reglas en el servidor. El refinamiento se concentra en el Módulo de juego, el estado activo que consulta y la versión que recibe a través del Servicio de partida en curso.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

- **Validar la jugada solo contra el estado vigente.** La solicitud puede llevar únicamente la posición. Una jugada preparada sobre un tablero anterior podría seguir siendo legal en el tablero vigente y aplicarse a una situación distinta de la que vio el jugador.
- **Exigir la versión esperada y compararla bajo acceso exclusivo por partida.** La solicitud identifica el estado a partir del cual se eligió la jugada. Si ese estado ya cambió, el servidor rechaza la solicitud. Mantener el acceso exclusivo hasta aplicar la jugada evita que el estado cambie entre la comprobación y la aplicación.

### Paso 5: Decisión y elementos resultantes

Se exige que la solicitud incluya la versión del estado que el cliente usó para elegir la jugada. El Servicio de partida en curso entrega la solicitud al Módulo de juego. Después de comprobar la sesión e identificar la partida, el Módulo de juego verifica la participación, el turno y la versión esperada con acceso exclusivo al estado de esa partida. Si la versión coincide y se cumplen las demás condiciones, el motor valida la jugada sobre el tablero vigente. Cuando se acepta, el Módulo de juego registra el movimiento y avanza la versión dentro del mismo acceso exclusivo.

Una solicitud con una versión anterior se rechaza antes de llegar al motor. El rechazo conserva el tablero, el turno y el historial. El servicio responde al solicitante y esa solicitud no genera un movimiento para persistir ni una actualización para los demás clientes.

**Costo aceptado.** El cliente conserva la versión recibida y la incluye en la solicitud. Cuando recibe un rechazo por una versión anterior, necesita consultar el estado vigente antes de volver a intentar la jugada. Las operaciones que modifican una misma partida esperan mientras se comprueba y se aplica la jugada.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

![Validación de la versión al realizar una jugada válida](Octava%20iteración/Validación%20de%20la%20versión%20al%20realizar%20una%20jugada%20válida.png)

| Elemento | Capa | Qué hace | Qué no hace | De quién depende en este recorrido |
| --- | --- | --- | --- | --- |
| Servicio de partida en curso | Servicios | Recibe la solicitud y comunica el rechazo o publica el estado confirmado. | No decide si la jugada es legal. | Módulo de juego. |
| Módulo de juego | Negocio | Compara la versión esperada con la vigente y coordina la aplicación y el avance de la versión bajo acceso exclusivo por partida. | No acepta el tablero del cliente como estado oficial. | Módulo de estado de juego, Módulo motor de juego y Contrato de persistencia de partidas. |
| Módulo de estado de juego | Negocio | Conserva el estado activo de la partida, incluida su versión. | No aplica las reglas de juego. | Ninguno en este recorrido. |
| Módulo motor de juego | Negocio | Evalúa el flanqueo y modifica el tablero si la jugada es válida. | No autoriza al jugador ni persiste el movimiento. | Ninguno en este recorrido. |

**Recorrido de rechazo.** STK-05 (Adversario mediante un cliente modificado) envía una jugada con una versión anterior del estado. El Módulo de juego compara esa versión con la vigente mientras tiene acceso exclusivo a la partida. Devuelve el rechazo antes de invocar al motor. El Servicio de partida en curso responde al solicitante y el estado vigente se conserva.

### Paso 7: Análisis del diseño actual

La tabla registra la cobertura acumulada del diseño en el escenario de jugadas inválidas.

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **QAS-Seguridad-1 (ASR-01)** | | | **X** | La validación de reglas en el servidor definida en IT2 se complementa con la comprobación de la versión y el acceso exclusivo por partida. Una solicitud desactualizada se rechaza antes de modificar el estado. |
| **ASR-05** | | | **X** | El contrato remoto de partida en curso conserva la frontera entre el cliente y el negocio del servidor. |
