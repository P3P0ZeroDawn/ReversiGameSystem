# Iteración 5 -Refinamiento

Objetivo: Refinar el patrón de despliegue y la asignación de responsabilidades de las reglas del juego tras los acuerdos de alcance con la Autoridad Organizacional (STK-06), priorizando la arquitectura en línea y la usabilidad del cliente.

Paso 2: Drivers para establecer el objetivo de la iteración

- **STK-06 (Autoridad organizacional):** Priorización estricta del modo en línea evaluable; necesidad de desacoplamiento en despliegue y separación de esquemas de datos.
- **ASM-02 :** Se asume un alcance operativo base de 8 jugadores activos concurrentes (4 partidas simultáneas de 2 jugadores).
- **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity
- **ESC-05:** Procesamiento y propagación de una jugada
- **ESC-07:** Identificación y prevención de jugadas inválidas
- TEN-07 — Desacoplamiento de binarios y rendimiento de red vs. localidad de cambio.

Paso 3: Elementos a refinar

1. El patrón de despliegue global ****(relación entre el cliente, el servidor de aplicaciones y el servidor de datos).
2. La distribución de responsabilidades entre el nodo Cliente y el nodo Servidor respecto a las reglas de Reversi.

Paso 4: Conceptos de diseño que satisfacen los drivers

1. Patrón de despliegue Three-Tier:
    - **Decisión:** Evolucionar del patrón Two-Tier a una estructura Three-Tier lógica. El cliente, la aplicación de servicio y los datos conservan responsabilidades separadas. En el despliegue físico actual, la aplicación de servicio y la base de datos se ejecutan como procesos independientes dentro del mismo dispositivo del servidor.
    - **Justificación:** La separación lógica evita mezclar las responsabilidades de aplicación y persistencia aunque ambos procesos compartan un dispositivo físico.
    - **Costo aceptado:** La aplicación de servicio y la base de datos comparten los recursos y el dominio físico de fallos del dispositivo del servidor. Una falla del equipo afecta a ambos procesos y el nivel de datos no puede escalarse físicamente de forma independiente mientras permanezca en ese dispositivo.
    - **Alternativa descartada:** Two-Tier monolítico con la aplicación y la persistencia dentro del mismo proceso. Se descarta porque mezcla sus responsabilidades y ciclos de ejecución. También se descarta representar la base de datos en un tercer dispositivo, porque no corresponde al despliegue real del proyecto.
2. Distribución de reglas: Servidor autoritativo con pre-validación en cliente:
    - **Decisión:**
        - **Servidor autoritativo:** El servidor concentra la autoridad absoluta de la partida, el cálculo de volteo de fichas, el cambio de turno y la victoria/derrota.
        - **Pre-validación en cliente:** El cliente incorpora una validación ligera para identificar casillas válidas directamente en su interfaz.
        - **Eliminación del motor compartido:** Se descarta distribuir una biblioteca compartida de reglas entre ambos nodos.
    - **Justificación:**
        - **A favor:** La interfaz responde de forma instantánea al cursor del usuario (ESC-07) y previene enviar jugadas inválidas hacia el servidor (ESC-05). Se elimina el acoplamiento de compilación y empaquetado entre cliente y servidor.
        - ***Costo aceptado:*** Se asume duplicación de la lógica básica de detección de movimientos legales en ambos extremos.
    - **Alternativa descartada:** Motor de juego compartido en una única biblioteca física (descartada por acoplamiento binario estrecho) y cliente sin validación local (descartada por latencia entre cliente y servidor ante cada movimiento).

Paso 5: Instanciar elementos arquitectónicos

- **Nivel lógico de cliente:**
    - Módulo de presentación: Presenta la interfaz y proporciona retroalimentación visual al usuario.
    - Módulo validador de interfaz: Evalúa si una casilla es un movimiento válido para guiar la interacción local antes de emitir solicitudes.
    - Módulo de comunicación: Gestiona la conexión remota con el servidor.
- **Nivel lógico de aplicación:**
    - Módulo de servicios: Punto de entrada que recibe las solicitudes remotas de los clientes.
    - Módulo de juego: Gestiona las salas, los turnos y el temporizador.
    - Módulo motor de juego: Resuelve autoritativamente las reglas de Reversi, los volteos y el fin de partida.
    - Módulo de estado de juego: Mantiene en memoria el estado de las partidas activas.
    - Módulo de persistencia: Administra las operaciones sobre la base de datos.
- **Nivel lógico de datos:**
    - Base de datos relacional: Se ejecuta en un proceso independiente dentro del mismo dispositivo físico que el nivel de aplicación.

La vista física contiene dos dispositivos. La aplicación cliente se ejecuta en el dispositivo del jugador. La aplicación de servicio y la base de datos se ejecutan como procesos independientes dentro del dispositivo del servidor.

Paso 6: Bocetar vistas y registrar decisiones de diseño

- Despliegue

![](Iteración%205%20-Refinamiento/Reversi%20despliegue.png)

- Módulos

![](Iteración%205%20-Refinamiento/Reversi%20Módulos.png)

| **Elemento** | Nodo | **Qué hace** | **Qué no hace** | **De quién depende** |
| --- | --- | --- | --- | --- |
| **Módulo de presentación** | Cliente | Muestra el estado del juego, menús y captura la interacción del jugador. | No valida jugadas definitivas ni calcula el nuevo tablero. | Módulo validador de interfaz, Módulo de comunicación |
| **Módulo validador de interfaz** | Cliente | Determina casillas válidas sobre el tablero local para retroalimentación visual inmediata. | No modifica fichas ni calcula el estado oficial de la partida. | Ninguno |
| **Módulo de comunicación** | Cliente | Transmite peticiones y recibe eventos del servidor. | No procesa reglas ni almacena partidas. | Módulo de servicios |
| **Módulo de servicios** | Servidor | Expone las operaciones del sistema hacia los clientes remotos. | No contiene reglas del juego ni lógica de persistencia directa. | Módulo de juego, módulos de negocio, Módulo de registro de eventos |
| **Módulo de juego** | Servidor | Gestiona partidas en curso, turnos, inactividad y jugadores automatizados. | No define si una jugada es válida según el reglamento. | Módulo motor de juego, Módulo de estado de juego, Módulo de persistencia |
| **Módulo motor de juego** | Servidor | Valida con autoridad si una jugada es legal, resuelve el flanqueo de fichas y las condiciones de victoria. | No maneja conexiones de red, jugadores ni persistencia. | Ninguno |
| **Módulo de estado de juego** | Servidor | Mantiene en memoria el estado activo de las partidas en curso. | No ejecuta persistencia en disco ni aplica reglas. | Ninguno |
| Módulo de chat | Servidor | Valida que el mensaje no incumpla las normas de conducta y lo redirige al remitente. | No almacena los mensajes directamente en la base de datos ni gestiona la partida. | Módulo de juego, Módulo de datos de chat (`ChatDatos`) |
| Módulo de jugadores | Servidor | Conoce a los jugadores, realiza la autenticación y diferencia entre jugador con cuenta e invitado. | No gestiona las partidas de los jugadores. | Módulo despachador de correo, Módulo de datos de jugadores (`JugadoresDatos`) |
| Módulo de amistades | Servidor | Gestiona las amistades activas y las solicitudes de amistad. | No valida credenciales ni mensajes de las partidas. | Módulo de jugadores, Módulo de datos de amistades (`AmistadesDatos`) |
| Módulo despachador de correo | Servidor | Despacha los mensajes hacia el servicio de correo electrónico. | No decide las reglas de negocio de los mensajes enviados. | Servicio de correo electrónico |
| **Módulo de persistencia** | Servidor | Gestiona el almacenamiento y consulta de cuentas, resultados y auditoría. | No valida partidas en curso. | Base de datos relacional |
| Módulo de datos de partidas (`PartidaDatos`) | Servidor | Se encarga de las operaciones de persistencia de una partida. | No gestiona operaciones persistentes de otras áreas. | Base de datos relacional |
| Módulo de datos de chat (`ChatDatos`) | Servidor | Se encarga de las operaciones de persistencia del chat. | No gestiona operaciones persistentes de otras áreas. | Base de datos relacional |
| Módulo de datos de amistades (`AmistadesDatos`) | Servidor | Se encarga de las operaciones de persistencia de las amistades. | No gestiona operaciones persistentes de otras áreas. | Base de datos relacional |
| Módulo de datos de jugadores (`JugadoresDatos`) | Servidor | Se encarga de las operaciones de persistencia de los jugadores. | No gestiona operaciones persistentes de otras áreas. | Base de datos relacional |
| Módulo de registro de eventos | Servidor | Se encarga del registro de eventos del servidor en el disco local. | No intercepta excepciones automáticamente, no contiene lógica de negocio. | Almacenamiento local del servidor. |
| **Base de datos relacional** | Dispositivo del servidor | Almacena los datos relacionales en un proceso separado de la aplicación de servicio. | No ejecuta lógica de aplicación ni de partidas. | Ninguno |

Paso 7: Análisis del diseño actual

| **Driver** | **No abordado** | **Parcialmente abordado** | **Completamente abordado** | **Decisiones de diseño tomadas y justificación** |
| --- | --- | --- | --- | --- |
| **CNR-ARQ-01 — Estructura global** |  |  | **X** | Se refinó a una estructura Three-Tier lógica. El nivel de aplicación y el nivel de datos se ejecutan como procesos independientes dentro del mismo dispositivo del servidor. |
| **RES-TEC-02 — Comunicación remota** |  |  | **X** | Se mantiene comunicación remota cliente-servidor mediante WCF. |
| **RES-TEC-03 — Cliente de escritorio** |  |  | **X** | Rich Client en WPF con validación en interfaz para respuesta visual rápida. |
| **RES-TEC-04 — Base de datos relacional** |  |  | **X** | La base de datos opera en un proceso independiente dentro del dispositivo del servidor y conserva una responsabilidad lógica separada de la aplicación de servicio. |
| **ASM-01 — Modo offline/local** |  |  | **X** | Postergado para concentrar el esfuerzo en la arquitectura en línea evaluable. |
| **ASM-02 — Jugadores concurrentes** |  |  | **X** | Acotado a 8 concurrentes (4 partidas de 2 jugadores). |
| **CON-01 — Integridad de partidas** |  |  | **X** | El servidor es la única autoridad del estado oficial; clientes modificados no pueden alterar el juego. |
| **ESC-05 — Propagación y latencia** |  |  | **X** | La pre-validación en cliente previene el envío de clics inválidos al servidor, cuidando el canal de red. |
| **ESC-07 — Prevención de errores en UI** |  |  | **X** | El validador en interfaz destaca movimientos legales localmente sin depender de la red. |
| **ESC-10 — Aislamiento de pruebas** |  |  | **X** | El motor de juego del servidor se mantiene desacoplado de red y base de datos para pruebas unitarias. |
| ESC-15 — Modificación de reglas de Reversi |  |  |  | **[DESCARTADO]** Descartado tras sustituir el módulo de juego compartido por la duplicación deliberada entre el validador de interfaz y el motor de juego en el servidor |
