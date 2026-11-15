# Novena iteración

Objetivo: Refinar la presentación del tablero y de la sala de partida para que el jugador pueda identificar sus opciones desde el momento en que las necesita.

### Paso 2: Drivers para establecer el objetivo de la iteración

- **QAS-Usabilidad-1 (ASR-06):** STK-01 (Jugador durante su turno) recibe la indicación de posiciones válidas en un máximo de 1 segundo. El cliente bloquea el 100 % de las posiciones que identifica como inválidas.
- **QAS-Usabilidad-2:** STK-01 (Jugador que crea una partida) puede seleccionar la opción de jugar contra el sistema desde la primera presentación de la sala de espera, con 0 segundos de espera obligatoria.

### Paso 3: Elementos a refinar

Se refinan Partida en curso y Sala de partida, ambos submódulos de presentación definidos en IT7. Se conserva el Módulo validador de interfaz para las comprobaciones locales y el Cliente de partidas para las solicitudes remotas. La autoridad del servidor y la comprobación de la versión se mantienen según IT2 e IT8.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

- **Calcular las posiciones válidas en el cliente a partir del tablero confirmado.** Permite mostrar las opciones durante el turno sin introducir una consulta remota adicional. El costo es mantener alineada la comprobación local con las reglas del servidor. Se consideraron las siguientes alternativas:
	- **Consultar al servidor las posiciones válidas.** Centraliza el cálculo, pero añade una solicitud remota cada vez que la interfaz necesita obtener las opciones de un nuevo estado. La indicación queda sujeta al tiempo de respuesta de esa consulta.
- **Ofrecer jugar contra el sistema desde que aparece la sala.** Deja al anfitrión elegir sin espera obligatoria. La sala debe distinguir esta acción de seguir esperando a otro jugador y reflejar el resultado de la solicitud de inicio. Se consideraron las siguientes alternativas:
	- **Habilitar la opción tras un tiempo de espera.** Da prioridad a encontrar otro jugador, pero añade una demora que incumple QAS-Usabilidad-2.

### Paso 5: Decisión y elementos resultantes

El ViewModel de Partida en curso usa el estado confirmado más reciente. Cuando la partida está activa y el estado indica que es el turno del jugador, consulta al Módulo validador de interfaz y expone las posiciones habilitadas a la vista. La vista marca esas posiciones y bloquea las identificadas como inválidas. Al llegar un nuevo estado confirmado se actualizan las opciones. El máximo de 1 segundo se mide desde que el cliente dispone del estado confirmado que le da el turno hasta que la indicación aparece en pantalla.

Cuando el jugador selecciona una posición habilitada, el Cliente de partidas envía la solicitud con la versión correspondiente al estado consultado. El servidor decide si acepta la jugada según IT8.

El ViewModel de Sala de partida presenta habilitada la opción de jugar contra el sistema desde la primera presentación de la sala de espera al anfitrión. Al elegirla, solicita el inicio mediante el Cliente de partidas y espera la confirmación del servidor para mostrar la partida en curso. Los 0 segundos corresponden a la espera obligatoria antes de poder elegir la opción.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

![Indicar posiciones válidas](Novena%20iteración/Indicar%20posiciones%20válidas.png)

![Iniciar una partida contra el sistema](Novena%20iteración/Iniciar%20una%20partida%20contra%20el%20sistema.png)



| Elemento | Capa | Responsabilidad en esta iteración | Dependencias |
| --- | --- | --- | --- |
| Partida en curso | Presentación | Actualiza el tablero y las posiciones habilitadas a partir del estado confirmado. | Módulo validador de interfaz y Cliente de partidas. |
| Sala de partida | Presentación | Ofrece jugar contra el sistema desde la primera presentación de la sala y muestra el resultado de la solicitud de inicio. | Cliente de partidas. |
| Módulo validador de interfaz | Negocio | Calcula las posiciones permitidas sobre el tablero local para el jugador en turno. | Ninguna hacia otras capas del cliente. |
| Cliente de partidas | Datos | Entrega el estado confirmado y tramita las solicitudes de jugada e inicio contra el sistema. | Contrato remoto de partida en curso y Contrato remoto de sala de partida para estos recorridos. |

### Paso 7: Análisis del diseño actual

La tabla registra la cobertura de diseño de los escenarios.

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **QAS-Usabilidad-1 (ASR-06)** | | | **X** | El cálculo local permite indicar las posiciones válidas a partir del estado confirmado. La vista solo habilita las posiciones que identifica como válidas. El máximo de 1 segundo es el criterio de verificación. |
| **QAS-Usabilidad-2** | | | **X** | La opción de jugar contra el sistema se habilita desde la primera presentación de la sala al anfitrión, con 0 segundos de espera obligatoria. |
