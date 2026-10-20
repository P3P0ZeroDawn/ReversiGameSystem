# Segunda iteración

**Nota de versión:** Las decisiones sobre el motor de juego del cliente planteadas en esta iteración fueron refinadas y sustituidas en la **Iteración 5** a raíz de los acuerdos con STK-06. Consultar la Iteración 5 para la arquitectura vigente.

Objetivo: identificar estructuras que soporten la funcionalidad primaria.

Paso 2: Drivers para establecer el objetivo de la iteración

Funcionalidad principal:

- CU-01 Crear cuenta
- CU-11 Crear partida
- CU-15 Unirse a partida
- CU-16 Realizar movimiento
- CU-21 Enviar solicitud de amistad
- CU-23 Responder solicitud de amistad
- CU-26 Enviar mensaje
- CU-28 Entrar como invitado

Drivers acoplados:

- **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity
- RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework.

Paso 3: Seleccionar uno o más elementos del sistema a refinar

Los elementos seleccionados para refinar en este caso son las capas definidas tanto en el cliente como en el servidor en IT1. Estas capas serán descompuestas en componentes que nos permitan soportar las funcionalidades más importantes identificadas.

Paso 4: Conceptos de diseño que satisfacen los drivers

Modelo de dominio

En el caso del juego, por la complejidad y evolución de las reglas conviene crearlo porque nos permite identificar las entidades principales el dominio junto con sus relaciones, facilitando considerar todos los requisitos de valor.
Costo aceptado: más elementos y mapeo hacia la base de datos.

Fueron consideradas las alternativas Transaction Script y Table Module, pero fueron descartadas. En el caso de Transaction Script por el riesgo de presentar duplicación ante un crecimiento de las reglas. En el caso de Table Module para no atar el diseño al esquema de la base de datos.

Vamos a identificar Objetos del Dominio que se traduzcan a los requerimientos funcionales, en este caso los casos de uso que tenemos como drivers y a los componentes de la descomposición.

Se usará Entity Framework para la integración de la capa de persistencia para dar cumplimiento con RES-TEC-01.

Paso 5: Instanciar elementos arquitectónicos

Se creará un Modelo de Dominio inicial con las entidades necesarias para soportar los casos de uso que consideramos como drivers para esta iteración.

Los Objetos de Dominio del Modelo de Dominio creado serán asociados a un componente de la descomposición realizada de las capas tanto del servidor como del cliente.

Entity Framework será encapsulado en el modulo de persistencia del servidor.

Paso 6: Bocetar vistas y registrar decisiones de diseño

![Dominio.png](Segunda%20iteraci%C3%B3n/Dominio.png)

![Objetos de dominio.png](Segunda%20iteraci%C3%B3n/Objetos_de_dominio.png)

![Reversi Módulos.png](Segunda%20iteraci%C3%B3n/Reversi_Mdulos.png)

| Elemento | Nodo | Que hace | Que no hace | De quien depende |
| --- | --- | --- | --- | --- |
| API de servicio | Servidor | Recibe solicitudes remotas de los clientes y las delega al modulo correspondiente. | No valida reglas del juego ni interactúa directo con la base de datos. | Módulos de la capa de negocio |
| Componente de juego | Servidor | Coordina las partidas activas, los turnos de los jugadores. | No define si una jugada es válida, ni ejecuta consultas a la base de datos. | Motor de juego |
| Motor de juego | Servidor | Conoce las reglas del juego y decide si una jugada es válida. | No sabe de red, ni jugadores. | - |
| Modulo de chat | Servidor | Se encarga de validar que el mensaje no incumpla las normas de conducta, y lo redirige al remitente. | No almacena los mensajes directamente en la base de datos, ni gestiona la partida | Componente de juego, componente de persistencia |
| Modulo de jugadores | Servidor | Conoce los jugadores, realiza autenticación, diferencia entre jugador con cuenta e invitado | No gestiona la partida de los jugadores | Despachador correo, componente de persistencia |
| Modulo de amistades | Servidor | Gestiona las amistades activas, las solicitudes de amistad | No valida credenciales de los jugadores, ni los mensajes de las partidas | Modulo de jugadores, componente de persistencia |
| Despachador correo | Servidor | Despacha los mensajes hacia el servidor de correo electrónico | No decide las reglas de negocio de los mensajes enviados | Servicio de correo electrónico |
| Componente de persistencia | Servidor | Se comunica con la base de datos. | No realiza validaciones de las partidas. | Base de datos |
| Componente de comunicación | Cliente | Se comunica con el servidor. | No se comunica con la persistencia. | Servidor |
| Componente de UI | Cliente | Implementa la interfaz del cliente | No se comunica directamente con el servidor | Componente de comunicación. (Operación normal)
Componente de juego y Motor de juego locales (Modo offline) |
| Componente de juego | Cliente | Coordina las partidas activas, los turnos de los jugadores para partidas offline. | No define si una jugada es válida. | Motor de juego (Cliente) |
| Motor de juego | Cliente | Conoce las reglas del juego y decide si una jugada es válida para partidas offline. | No sabe de red, ni jugadores. | - |

Diagramas de algunos CU relevantes para ilustrar la comunicación entre componentes de diferentes capas y nodos:

CU-16 Realizar movimiento

![CU-16 Realizar movimiento.png](Segunda%20iteraci%C3%B3n/CU-16_Realizar_movimiento.png)

CU-01 Crear cuenta

![CU-01 Crear cuenta.png](Segunda%20iteraci%C3%B3n/CU-01_Crear_cuenta.png)

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
| CU-01 Crear cuenta |  |  | X | Se definió el componente de jugadores, encargado de la gestión de jugadores y del registro.  |
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
| • **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability | X |  |  | - |
| **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability | X |  |  | - |
| **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance |  | X |  | Se desacopló el envío de correos en el módulo Despachador correo (capa de servicios) para no mezclarlo con la lógica de negocio de los jugadores ni de las partidas |
| **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability | X |  |  | - |
| ASM-02 — Se asume un alcance operativo de 20 jugadores activos concurrentes | X |  |  | - |