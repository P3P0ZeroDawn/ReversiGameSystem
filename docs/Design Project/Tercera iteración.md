# Tercera iteración

Objetivo: 

Diseñar **las tácticas** de disponibilidad y tolerancia a fallos para mitigar el riesgo de indisponibilidad de la base de datos **y del servicio de correo electrónico**.

Paso 2: Drivers para establecer el objetivo de la iteración

- **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability
- **CON-04 —** Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea → (Availability / Fault Tolerance)
- **ASM-02 —** Se asume un alcance operativo de 20 jugadores activos concurrentes.

Casos de uso afectados:

- **CU-16 —** Realizar movimiento
- **CU-01 —** Crear cuenta

Paso 3: Seleccionar uno o más elementos del sistema a refinar

- La capa de lógica de negocio del Servidor: Específicamente la descomposición de la gestión de partidas para desacoplar el estado activo de la persistencia.
- La Capa de Datos del Servidor: Refinamiento del componente de persistencia.

Paso 4: Conceptos de diseño que satisfacen los drivers

- Degradación Elegante (*Graceful Degradation*) y Aislamiento de Fallas:
    - Se prioriza la continuidad del juego activo en memoria RAM sobre la persistencia inmediata en disco. Si la base de datos se vuelve inaccesible o responde con lentitud, las partidas activas no se cancelan ni se congelan; el fallo de persistencia se aísla y se reporta en segundo plano.
        - Costo aceptado: Si el servidor experimenta un apagado abrupto o pérdida de energía antes de persistir, los movimientos de las partidas en curso que permanecían únicamente en memoria no podrán recuperarse en el historial final.
    - Alternativas consideradas y descartadas:
        - Colas de mensajería persistente distribuida (ej. RabbitMQ / Kafka): Descartada por sobreingeniería, complejidad operativa y consumo innecesario de recursos para la escala de 20 jugadores concurrentes (ASM-02).
        - Bloqueo síncrono transaccional en cada movimiento: Descartada porque cualquier lentitud o desconexión de SQL Server congelaría la interfaz de los jugadores, violando la disponibilidad requerida.
- Separación de Responsabilidades y Motor Sin Estado:
    - El cálculo y validación de reglas de Reversi se mantiene puro en el Motor de juego, mientras que el Módulo de estado de juego retiene en memoria la verdad absoluta de la partida activa.
    - **Costo aceptado:** El componente orquestador debe consultar explícitamente el estado en memoria antes de invocar las reglas.
    - Alternativas consideradas y descartadas:
        - Motor acoplado a persistencia: Descartada porque dificultaba las pruebas unitarias y creaba dependencias cíclicas con el acceso a datos.
- Servicios de persistencia especializados y aislados (Bulkhead y fire-and-forget):
    - Aplica el principio de mamparas (*Bulkhead*) para aislar los accesos por área funcional, además de añadir invocación asíncrona en operaciones que no requieren una retroalimentación. Esto evita que la carga pesada o la lentitud en áreas no críticas (chat, amistades) sature el *pool* de conexiones de Entity Framework o acapare hilos de ejecución, protegiendo el flujo de las partidas (CON-02).
        - Costo aceptado**:** Si el proceso del servidor se interrumpe de forma abrupta antes de vaciar las escrituras a disco, los movimientos que residían solo en la memoria RAM no se consolidarán en la base de datos.
    - Alternativas consideradas y descartadas:
        - Componente monolítico único:  Descartado por centralizar todas las entidades en una sola clase, facilitando que se compartan contextos de larga duración o se mezclen bloqueos, generando un punto único de falla para el servidor.
        - Patrón Repositorio Genérico abstracto: Descartada por añadir capas de abstracción e interfaces innecesarias para un equipo y proyecto pequeño.
- Fallo Rápido (*Fail-Fast*) para Servicios Externos de Autenticación (CON-04):
    - El módulo Despachador correo ejecuta el envío de códigos de forma síncrona pero acotada por un tiempo límite estricto (*timeout*). Si el proveedor no responde, la petición se cancela de inmediato y se notifica al usuario.
        - Costo aceptado: El usuario debe solicitar manualmente el reenvío del correo si el servicio externo falla temporalmente.
    - Alternativas consideradas y descartadas:
        - Reintentos en segundo plano: Descartada porque retendría hilos en el servidor innecesariamente ante caídas prolongadas del proveedor de correo.

Paso 5: Instanciar elementos arquitectónicos

- **Módulo estado de juego:** Mantendrá en memoria RAM la información de los tableros y turnos de las partidas activas, siendo consultado y actualizado directamente por el Componente de juego.
- **PartidasDatos:** Submódulo del Componente de persistencia de uso exclusivo del Componente de juego para almacenar movimientos y resultados finales de partidas.
- **ChatDatos:** Submódulo del Componente de persistencia de uso exclusivo del Módulo de chat para registrar los mensajes enviados.
- **AmistadesDatos:** Submódulo del Componente de persistencia de uso exclusivo del Módulo de amistades para gestionar el estado de las relaciones.
- **JugadoresDatos:** Submódulo del Componente de persistencia utilizado por el Módulo de jugadores para el registro y autenticación.
- **Módulo registro de eventos:** Servicio transversal utilizado por los distintos componentes para registrar incidencias y errores en disco local (como fallos o tiempos de espera al interactuar con la base de datos).

Paso 6: Bocetar vistas y registrar decisiones de diseño

![Reversi MódulosI3.png](Tercera%20iteraci%C3%B3n/Reversi_MdulosI3.png)

| Elemento | Nodo | Que hace | Que no hace | De quien depende |
| --- | --- | --- | --- | --- |
| API de servicio | Servidor | Recibe solicitudes remotas de los clientes y las delega al modulo correspondiente. | No valida reglas del juego ni interactúa directo con la base de datos. | Módulos de la capa de negocio |
| Componente de juego | Servidor | Coordina las partidas activas, los turnos de los jugadores. | No define si una jugada es válida, ni ejecuta consultas a la base de datos. | Motor de juego, Módulo de estado de juego, PartidaDatos |
| Motor de juego | Servidor | Conoce las reglas del juego y decide si una jugada es válida. | No sabe de red, ni jugadores. | - |
| Módulo estado de juego | Servidor | Conoce el estado e información de las partidas activas. | No sabe de validaciones de jugadas. |  |
| Módulo de chat | Servidor | Se encarga de validar que el mensaje no incumpla las normas de conducta, y lo redirige al remitente. | No almacena los mensajes directamente en la base de datos, ni gestiona la partida | Componente de juego, ChatDatos |
| Módulo de jugadores | Servidor | Conoce los jugadores, realiza autenticación, diferencia entre jugador con cuenta e invitado | No gestiona la partida de los jugadores | Despachador correo, JugadoresDatos |
| Módulo de amistades | Servidor | Gestiona las amistades activas, las solicitudes de amistad | No valida credenciales de los jugadores, ni los mensajes de las partidas | Módulo de jugadores, AmistadesDatos |
| Despachador correo | Servidor | Despacha los mensajes hacia el servidor de correo electrónico | No decide las reglas de negocio de los mensajes enviados | Servicio de correo electrónico |
| Componente de persistencia | Servidor | Agrupa lógicamente el acceso a la base de datos a través de sus submódulos especializados | No realiza validaciones de las partidas. No registra las fallas directamente en los archivos | Submódulos especializados. |
| PartidaDatos | Servidor | Se encarga de las operaciones de persistencia de una partida. | No gestiona operaciones persistentes de otras áreas. | Base de datos, Módulo registro de eventos |
| ChatDatos | Servidor | Se encarga de las operaciones de persistencia del chat. | No gestiona operaciones persistentes de otras áreas. | Base de datos, Módulo registro de eventos |
| AmistadesDatos | Servidor | Se encarga de las operaciones de persistencia de las amistades. | No gestiona operaciones persistentes de otras áreas. | Base de datos, Módulo registro de eventos |
| JugadoresDatos | Servidor | Se encarga de las operaciones de persistencia de los jugadores. | No gestiona operaciones persistentes de otras áreas. | Base de datos, Módulo registro de eventos |
| Módulo registro de eventos | Servidor | Se encarga del registro de eventos del servidor en el disco local. | No intercepta excepciones automáticamente, no contiene lógica de negocio. | Almacenamiento local del servidor. |
| Componente de comunicación | Cliente | Se comunica con el servidor. | No se comunica con la persistencia. | Servidor |
| Componente de UI | Cliente | Implementa la interfaz del cliente | No se comunica directamente con el servidor | Componente de comunicación. (Operación normal)
Componente de juego y Motor de juego locales (Modo offline) |
| Componente de juego | Cliente | Coordina las partidas activas, los turnos de los jugadores para partidas offline. | No define si una jugada es válida. | Motor de juego (Cliente) |
| Motor de juego | Cliente | Conoce las reglas del juego y decide si una jugada es válida para partidas offline. | No sabe de red, ni jugadores. | - |

CU-16 Realizar movimiento

![CU-16 Realizar movimientoI3.png](Tercera%20iteraci%C3%B3n/CU-16_Realizar_movimientoI3.png)

CU-01 Crear cuenta-ErrorEmail

![CU-01 Crear cuenta-ErrorEmail.png](Tercera%20iteraci%C3%B3n/CU-01_Crear_cuenta-ErrorEmail.png)

Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente Abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| CNR-ARQ-01 — Estructura global |  |  | X | Se definió la estructura global del sistema usando el estilo arquitectónico cliente - servidor y el patrón de despliegue Two Tier. |
| RES-TEC-02 — Necesidad de comunicación remota |  |  | X | Se definió WCF como mecanismo de comunicación remota |
| RES-TEC-04 — La persistencia relacional del sistema deberá utilizar Microsoft SQL Server. |  |  | X | Se asignó la base de datos con SQL Server al nodo del servidor. |
| ESC-05 Procesamiento y propagación de una jugada |  |  | X | Se definió el componente de juego, encargado de la gestión de las partidas, y la propagación de las jugadas |
| RES-TEC-03 — El cliente de escritorio deberá implementarse utilizando Windows Presentation Foundation (WPF). |  |  | X | Se definió el uso de una Rich Client  Application usando la tecnología de WPF. |
| ASM-01 — Se asume que el sistema deberá permitir partidas en modo offline/local. |  |  | X | Se incluyó un componente de juego y un motor de juego en el cliente para permitir el juego offline. |
| **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity |  |  | X | Se aisló el motor de juego dentro de la capa de negocio del servidor, asegurando que la validación de movimientos y el cálculo del nuevo tablero se realicen exclusivamente del lado del servidor antes de actualizar la persistencia y notificar a los clientes. |
| CU-01 Crear cuenta |  |  | X | Se definió el componente de jugadores, encargado de la gestión de jugadores y del registro. |
| CU-11 Crear partida |  |  | X | Se definió el componente de juego, encargado de la gestión de la partida |
| CU-09 Modificar idioma de interfaz | X |  |  | - |
| CU-15 Unirse a partida |  |  | X | Se definió el componente de juego, encargado de la gestión de la partida |
| CU-16 Realizar movimiento |  |  | X | Se definió el componente de juego, encargado de la gestión de la partida, y el motor de juego encargado de validar movimientos. |
| CU-21 Enviar solicitud de amistad |  |  | X | Se definió el modulo de amistades, encargado de la gestión de amistades entre jugadores |
| CU-23 Responder solicitud de amistad |  |  | X | Se definió el modulo de amistades, encargado de la gestión de amistades entre jugadores |
| CU-26 Enviar mensaje |  |  | X | Se definió el modulo de chat, encargado de la gestión de los mensajes. |
| CU-28 Entrar como invitado |  |  | X | Se definió el modulo de jugadores, encargado de diferencia jugadores con cuenta e invitados. |
| ESC-04 Recuperación ante desconexión temporal | X |  |  | - |
| ESC-09 Protección de operaciones administrativas | X |  |  | - |
| ESC-03 Intento de realizar una jugada inválida |  | X |  | El motor de juego se convirtió en el mecanismo para validar jugadas, evitando jugadas inválidas. |
| ESC-14 Añadiendo nuevo soporte de idioma | X |  |  | - |
| ESC-15 Modificación de reglas de Reversi |  | X |  | Se definió el motor de juego encargado de contener las reglas de Reversi. |
| ESC-10 Prueba aislada y controlable de las reglas | X |  |  | - |
| ESC-13 Empaquetado de nueva versión | X |  |  | - |
| RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework. |  |  | X | Se encapsuló el acceso a datos en el Componente de persistencia, donde Entity Framework gestionará el mapeo y almacenamiento de las entidades derivadas del modelo de dominio. |
| • **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability |  | X |  | Debido a que, ante una interrupción súbita o caída del proceso del servidor, los movimientos no registrados a la BD se pierden, priorizando la disponibilidad del juego en memoria sobre la consistencia. |
| **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability | X |  |  | - |
| **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance |  |  | X | Se formalizo el flujo de respuesta del Despachador de correo ante una indisponibilidad del servicio de correo. |
| **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability | X |  |  | - |
| ASM-02 — Se asume un alcance operativo de 20 jugadores activos concurrentes | X |  |  | - |