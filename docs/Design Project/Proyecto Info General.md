# Proyecto

César Daniel Ortega Castillejos

Pedro Enrique Sánchez Rodríguez

# Reversi

### Frontera del sistema:

Dentro del sistema bajo diseño:

- Gestionar el registro de jugadores.
- Autenticar a los jugadores.
- Gestionar la información de perfil de los jugadores.
- Gestionar relaciones de amistad entre jugadores.
- Crear y administrar partidas de Reversi.
- Validar y mantener el estado de las partidas en línea.
- Permitir partidas contra un oponente controlado por el sistema.
- Gestionar la comunicación mediante chat entre jugadores.
- Persistir el historial de chat de acuerdo con la política de retención que se defina
- Persistir información de las partidas.
- Persistir las estadísticas e historial de los jugadores.
- Persistir la información necesaria para auditoría.
- Permitir la consulta de partidas anteriores.
- Calcular y presentar estadísticas de partidas anteriores.

Fuera de la frontera:

- DEP-01 Servicio externo de correo electrónico

### Stakeholders:

STK-__  _______ tiene relación con el sistema porque ______________

Le importa _____________ y eso podría hacernos razonar sobre ____________

- **STK-01 Jugador** tiene relación con el sistema porque es su usuario principal y utiliza sus servicios de partida, comunicación y consulta de información. Su información, partidas y progreso serán administrados por el sistema. Le importa poder jugar de manera sencilla, justa y confiable, que su progreso se conserve correctamente, que las partidas respondan con rapidez y que una interrupción temporal no provoque innecesariamente la pérdida de una partida. Esto podría hacernos razonar sobre **usability, performance, availability, security, integridad y persistencia de información**.
- **STK-02 Equipo de desarrollo** tiene relación con el sistema porque será responsable de diseñarlo, implementarlo, probarlo, desplegarlo y mantenerlo. Le importa que los cambios puedan localizarse, que los módulos puedan probarse de manera independiente y que nuevas funcionalidades puedan incorporarse sin provocar modificaciones innecesarias en otras partes del sistema. Esto podría hacernos razonar sobre **modifiability, testability, deployability y control de dependencias**.
- **STK-03 Administrador del sistema** tiene relación con el sistema porque será responsable de atender incidencias operativas y dar soporte a los jugadores. Le importa poder identificar problemas, consultar información relevante de cuentas y partidas y realizar las acciones administrativas permitidas sin comprometer la integridad de la información. Esto podría hacernos razonar sobre **observability, auditability, security, availability y herramientas administrativas**.
- **STK-04 Auditor** tiene relación con el sistema porque debe revisar posteriormente las acciones realizadas sobre información relevante del sistema y verificar la trazabilidad de los eventos registrados. Le importa poder determinar quién realizó una acción, qué acción realizó, cuándo ocurrió, sobre qué recurso se realizó y cuál fue su resultado. Esto podría hacernos razonar sobre **auditabilidad, trazabilidad, integridad de los registros, seguridad y costo de almacenamiento y procesamiento de la auditoría**.
- STK-05 **Adversario** tiene relación con el sistema porque puede intentar manipular el cliente, enviar jugadas ilegales, alterar mensajes, acceder a recursos sin autorización o modificar información de manera indebida. Esto podría hacernos razonar sobre **security**.
- STK-06 **Autoridad organizacional** tiene relación con el sistema porque establece restricciones obligatorias de alcance, tecnología, proceso y criterios de evaluación para el proyecto. Le importa que la solución cumpla las restricciones técnicas y normativas establecidas y que las decisiones arquitectónicas estén justificadas y documentadas. Esto podría hacernos razonar sobre **restricciones arquitectónicas, tecnologías obligatorias, documentación, trazabilidad de decisiones y criterios de aceptación no negociables**.
- **STK-07** Responsable del producto tiene relación con el sistema porque establece las capacidades que debe ofrecer y prioriza su valor para los usuarios. Le importa que el producto alcance sus objetivos funcionales y de calidad dentro del tiempo disponible. Esto lleva a razonar sobre alcance, prioridades, experiencia de usuario, costo y calendario.

### Concerns:

- **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity
- **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability
- **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability
- **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance
- **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability

### Requisitos funcionales

JUG → Jugador y cuenta

PAR → Partida y reglas de juego

SOC → Social y Moderación

HIS → Historial

- **RF-JUG-01** —  El sistema debe autenticar las credenciales de acceso de un jugador.
- **RF-JUG-02** — El sistema debe permitir la edición de los datos de perfil del jugador.
- **RF-JUG-03** — El sistema debe permitir el registro de jugadores
- **RF-JUG-04** — El sistema debe permitir jugar sin necesidad de una cuenta de jugador
- **RF-JUG-05** — El sistema debe permitirle al jugador consultar su información de perfil.
- **RF-JUG-06** — El sistema debe permitir consultar la información de perfil publica de otro jugador.
- RF-JUG-07 — El sistema debe enviar un código de verificación temporal por correo electrónico para confirmar el registro de una cuenta nueva.
- RF-JUG-08 — El sistema debe enviar un código de verificación temporal por correo electrónico para autorizar el restablecimiento de la contraseña.
- RF-JUG-09 — El sistema debe enviar un código de verificación temporal por correo electrónico para validar el inicio de sesión mediante autenticación de dos factores.

- **RF-PAR-01** — El sistema debe controlar el tiempo límite asignado a cada turno de juego.
- **RF-PAR-02** — El sistema debe ejecutar una jugada automática válida cuando se agote el tiempo del turno.
- **RF-PAR-03** — El sistema debe declarar la derrota por inactividad tras acumular tres turnos consecutivos sin jugada directa.
- **RF-PAR-04** — El sistema debe validar jugadas conforme a las reglas de Reversi
- **RF-PAR-05** — El sistema debe calcular el resultado de una jugada
- **RF-PAR-06** — El sistema debe compartir el nuevo estado del tablero al realizarse una nueva jugada
- **RF-PAR-07** — El sistema debe permitir la creación de partidas.
- **RF-PAR-08** — El sistema debe permitir a un jugador unirse a una partida cuando la partida aun no inicia, y hay un cupo disponible.
- **RF-PAR-09** — El sistema debe permitir configurar el tiempo por turno de una partida antes de que inicie la partida.
- **RF-PAR-10** — El sistema debe permitir una alternativa de juego contra el sistema ante la ausencia de oponente.
- **RF-PAR-11** — El sistema debe permitir la consulta de las reglas de Reversi durante una partida en curso.

- **RF-SOC-01** — El sistema debe transmitir mensajes de texto entre los jugadores de una partida activa.
- **RF-SOC-02** — El sistema debe registrar las violaciones al código de conducta detectadas en el chat.
- **RF-SOC-03** — El sistema debe aplicar las sanciones de suspensión o silenciamiento determinadas para una cuenta.
- **RF-SOC-04** — El sistema debe permitir relaciones de amistad entre los jugadores.
- **RF-SOC-05** — El sistema debe permitirle al jugador consultar si cuenta con un muteo activo.

- **RF-HIS-01** — El sistema debe registrar el resultado de una partida.
- **RF-HIS-02** — El sistema debe permitir la consulta de partidas pasadas.
- **RF-HIS-03** — El sistema debe permitir la consulta de estadísticas acumuladas del jugador.

### Dependencias

- DEP-01 Servicio externo de correo electrónico

### Preguntas abiertas:

- Q-01 — ¿Las reglas de Reversi podrán modificarse durante la evolución del sistema y quién autorizará esos cambios?
- Q-02 — ¿Una desconexión superior a 30 segundos será considerada abandono y producirá automáticamente la derrota?
- Q-03 — ¿Cómo se detectará una violación del código de conducta y quién podrá imponer muteo o suspensión?
- Q-04 — ¿Qué criterios determinan muteo, suspensión temporal o definitiva y cuánto dura una suspensión temporal?
- Q-05 — ¿Durante cuánto tiempo se conservarán las partidas finalizadas?
- Q-06 — ¿Cuántos jugadores simultáneos y cuántas partidas concurrentes deberá soportar el servidor?
- Q-07 — ¿Para qué funciones será obligatorio el servicio de correo electrónico?
- Q-08 — ¿Qué comportamiento tendrá el sistema si el servicio de correo no está disponible?
- Q-09 — ¿Durante cuánto tiempo se conservará el historial de chat?
- Q-10 — ¿El temporizador continúa mientras el jugador consulta las instrucciones?
- Q-11 — ¿Cuál es el rango máximo configurable para el tiempo por turno?
- Q-12 — ¿Las imágenes personalizadas del tablero se almacenan únicamente localmente o también en el servidor?
- **Q-13 —** ¿Se reinicia el contador de inactividades consecutivas cuando el jugador realiza normalmente una jugada?

---

## Segunda capa

### Objetivos de negocio:

- OBJ-01 — Facilitar la adopción del juego reduciendo las barreras de acceso y aprendizaje para nuevos jugadores.
- OBJ-02 — Mantener una experiencia de juego que no dependa de publicidad invasiva ni de mecanismos de monetización agresivos.
- OBJ-03 — Favorecer la retención y fidelidad de los jugadores mediante una experiencia de juego accesible, confiable y consistente.
    - **Rationale:** una comunidad activa y satisfecha puede favorecer la incorporación orgánica de nuevos jugadores y reducir la dependencia de promoción pagada.

### Restricciones tecnológicas

- RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework.
- RES-TEC-02 — Necesidad de comunicación remota
- RES-TEC-03 — El cliente de escritorio deberá implementarse utilizando Windows Presentation Foundation (WPF).
- RES-TEC-04 — La persistencia relacional del sistema deberá utilizar Microsoft SQL Server.

### Supuestos

- ASM-01 — Se asume que el sistema deberá permitir partidas en modo offline/local. [Postergado]
    - **POSTERGADO:** **El modo offline se excluye del entregable principal para priorizar la arquitectura cliente-servidor en línea acordada con STK-06. Queda clasificado como extensión opcional de mantenimiento
- ASM-02 — Se asume un alcance operativo de 20 jugadores activos concurrentes [Actualizado]
    - **ACTUALIZADO:** Se asume un alcance operativo base de 8 jugadores activos concurrentes (4 partidas simultáneas de 2 jugadores).

### Condiciones organizacionales

- ORG-01 — El equipo de desarrollo estará compuesto por dos personas.
- ORG-02 — El sistema deberá alcanzar el alcance acordado dentro del periodo escolar asignado al proyecto.
- ORG-03 — Los integrantes del equipo deberán diseñar, implementar, probar, documentar y desplegar el sistema sin equipos especializados independientes para infraestructura, QA o seguridad.

### Tensiones identificadas

- ~~TEN-01~~ — Integridad online vs. operación offline: Centralizar la autoridad de las partidas online mejora el control sobre la integridad del juego, mientras que permitir partidas offline requiere que la lógica necesaria para jugar pueda ejecutarse localmente. [Descartado]
- TEN-02 — Auditabilidad vs. rendimiento y almacenamiento: Registrar más información facilita la reconstrucción de eventos, pero aumenta el número de operaciones de persistencia y el espacio utilizado.
- TEN-03 — Availability vs. recursos: Mantener durante 30 segundos las sesiones de jugadores desconectados facilita la recuperación, pero conserva recursos asociados a partidas que temporalmente no pueden avanzar.
- TEN-04 — Modifiability/Testability vs. simplicidad: Separar responsabilidades y dependencias mejora la capacidad de cambio y prueba, pero aumenta el número de abstracciones que un equipo de dos desarrolladores debe implementar y comprender.
- TEN-05 — Seguridad vs. latencia: Validar cada movimiento en el servidor mejora la integridad de las partidas online, pero introduce comunicación de red antes de confirmar una jugada.
- TEN-06 — Persistencia de chat vs. almacenamiento/privacidad: Conservar más historial facilita moderación y auditoría, pero incrementa la cantidad de información almacenada y su periodo de exposición.
- TEN-07 — Desacoplamiento de binarios y rendimiento de red vs. localidad de cambio (Duplicación): Implementar un validador ligero desacopla el ciclo de vida del cliente respecto al servidor y evita el envío de jugadas ilegales por la red, pero sacrifica la localidad de cambio al obligar a actualizar la lógica de validación básica en dos módulos independientes si las reglas cambian.

### Escenarios

**ESC-01: Reconstrucción de acciones sobre partidas**

**Narrativa**

Durante la operación normal del sistema, un auditor solicita revisar las acciones realizadas sobre la información de las partidas durante el último mes con el propósito de determinar quién realizó cada acción relevante y qué efecto tuvo, el sistema proporciona los registros del periodo solicitado y permite determinar, para cada acción auditable, el actor que la realizó, la acción ejecutada, el recurso afectado, el momento en que ocurrió y el resultado de la operación.

**Escenario**

Fuente del estímulo:

STK-04 Auditor.

Estímulo:

Solicita reconstruir las acciones relevantes realizadas sobre la información de las partidas durante el último mes.

Ambiente:

El sistema se encuentra en operación normal y conserva los registros correspondientes al periodo solicitado.

Artefacto:

Los registros de auditoría y la información persistida asociada con las partidas.

Respuesta:

El sistema proporciona los registros del periodo solicitado y permite determinar, para cada acción auditable, el actor que la realizó, la acción ejecutada, el recurso afectado, el momento en que ocurrió y el resultado de la operación.

Medida de la respuesta:

Para el 100 % de las acciones clasificadas como auditables y registradas durante el periodo solicitado se puede determinar:

- actor
- acción realizada
- recurso afectado
- fecha y hora
- resultado de la operación

Además, los datos registrados no presentan contradicciones con la información persistida de las partidas que pueda ser utilizada para verificar dichas acciones.

**ESC-02: Disponibilidad de la partida contra el sistema**

**Narrativa**

Durante la operación normal del sistema, un jugador crea una partida y entra a la pantalla de espera. Desde que se muestra esta pantalla, el sistema presenta la opción para iniciar una partida contra un oponente controlado por el sistema.

**Escenario**

Fuente del estímulo:

STK-01 Jugador.

Estímulo:

Entra a la pantalla de espera después de crear una partida.

Ambiente:

El sistema se encuentra en operación normal y los servicios necesarios para jugar están disponibles.

Artefacto:

El flujo de creación y espera de la partida.

Respuesta:

El sistema muestra la opción para iniciar una partida contra un oponente controlado por el sistema desde el inicio de la espera.

Medida de la respuesta:

La opción está disponible desde la primera presentación de la pantalla de espera, con 0 segundos de espera obligatoria.

**ESC-03: Intento de realizar una jugada inválida**

Narrativa

Durante una partida en línea activa, un adversario utiliza un cliente modificado para solicitar una jugada sobre una casilla que no produce ningún flanqueo válido de fichas enemigas, el sistema identifica que la solicitud no constituye una jugada válida respecto de las reglas, el turno y el estado actual, la rechaza y conserva el último estado válido de la partida.

Escenario

**Fuente del estímulo:**

STK-05 Adversario mediante un cliente modificado.

**Estímulo:**

Solicita realizar una jugada que viola las reglas de la partida, por ejemplo colocando una ficha en una posición que no produce un flanqueo válido, actuando fuera de su turno o utilizando un estado desactualizado.

**Ambiente:**

Operación normal durante una partida en línea activa.

**Artefacto:**

El estado de la partida y los mecanismos responsables de procesar y validar jugadas.

**Respuesta:**

El sistema identifica que la solicitud no constituye una jugada válida respecto de las reglas, el turno y el estado actual, la rechaza y conserva el último estado válido de la partida.

**Medida de la respuesta:**

El **100 % de las solicitudes identificadas como inválidas** son rechazadas y no producen:

- incorporación de una ficha;
- volteo de fichas;
- cambio de turno;
- modificación del contador de fichas;
- modificación del estado persistido de la partida.

Además, ambos jugadores continúan utilizando el último estado válido reconocido por el sistema.

**ESC-04: Recuperación ante desconexión temporal**

Narrativa

Durante una partida en línea activa, el jugador pierde la conexión de red de manera inesperada, el sistema identifica la perdida de la comunicación del cliente y mantiene la partida activa durante 30 segundos, antes de que terminen los 30 segundos el jugador recupera la conexión, el sistema le permite reincorporarse a la partida comunicándole el estado valido actual de la partida.

**Fuente del estímulo:**

STK-01 Jugador que participa en una partida en línea.

**Estímulo:**

Se interrumpe inesperadamente la comunicación entre el cliente del jugador y el sistema durante una partida activa.

**Ambiente:**

Operación normal durante una partida en línea activa.

**Artefacto:**

La sesión y el estado de la partida en línea.

**Respuesta:**

El sistema conserva la partida durante el periodo permitido de reconexión y, si el jugador restablece la conexión dentro de dicho periodo, le permite reincorporarse a la misma partida recuperando el estado válido correspondiente.

**Medida de la respuesta:**

Si el jugador restablece su conexión dentro de los **30 segundos** posteriores a la desconexión:

- vuelve a la misma partida
- recupera el último estado válido de la partida
- no se pierden movimientos previamente confirmados
- no se duplican movimientos
- la desconexión no es tratada como abandono definitivo

**ESC-05: Procesamiento y propagación de una jugada**

Narrativa

Durante una partida en línea en condiciones normales de operación, un jugador realiza una jugada válida y el sistema procesa la acción y comunica el nuevo estado de la partida a ambos jugadores.

Escenario

**Fuente del estímulo:**

STK-01 Jugador en turno en una partida en línea.

**Estímulo:**

El jugador solicita realizar una jugada válida durante su turno.

**Ambiente:**

Operación normal con una carga de hasta 8 jugadores concurrentes (4 partidas activas simultáneas).

**Artefacto:**

El procesamiento de jugadas y la sincronización del estado de la partida en línea.

**Respuesta:**

El sistema recibe la jugada, la procesa, determina el nuevo estado válido y comunica dicho estado a los dos participantes de la partida.

**Medida de la respuesta:**

El tiempo transcurrido desde que el sistema recibe la solicitud de jugada hasta que el nuevo estado queda disponible para ser comunicado a ambos jugadores no deberá superar 1 segundo de latencia en el 95% de las jugadas.

**ESC-06 — Gestión de inactividad durante un turno**

Narrativa

Durante una partida en línea configurada con el tiempo predeterminado de 40 segundos por turno, un jugador que dispone de movimientos válidos no realiza ninguna jugada antes de que se agote su tiempo y el sistema determina las posiciones válidas disponibles, selecciona una de ellas aleatoriamente, realiza una única jugada válida en nombre del jugador, actualiza la partida y contabiliza el turno como agotado por inactividad.

Escenario

**Fuente del estímulo:**

STK-01 Jugador cuyo turno se encuentra activo.

**Estímulo:**

Se alcanza el límite de tiempo configurado para el turno sin que el jugador haya realizado una jugada válida.

**Ambiente:**

Operación normal durante una partida en línea activa en la que el jugador en turno dispone de al menos una posición válida.

**Artefacto:**

El control temporal, las reglas y el estado de la partida.

**Respuesta:**

El sistema determina las posiciones válidas disponibles, selecciona una de ellas aleatoriamente, realiza una única jugada válida en nombre del jugador, actualiza la partida y contabiliza el turno como agotado por inactividad.

**Medida de la respuesta:**

- Se realiza **exactamente una** jugada automática.
- La posición seleccionada pertenece al conjunto de posiciones válidas del estado actual.
- Ninguna posición inválida puede ser utilizada para la jugada automática.
- Ambos participantes continúan con el mismo estado válido de la partida.
- Después de **tres turnos consecutivos agotados por inactividad**, la partida finaliza con derrota del jugador inactivo.

**ESC-07 — Identificación y prevención de jugadas inválidas**

Narrativa

Durante su turno, un jugador desplaza el cursor por el tablero para identificar dónde puede jugar e intenta seleccionar una casilla que no corresponde a una posición válida, la interfaz distingue visualmente las posiciones válidas al posicionar el cursor sobre ellas e impide que una posición inválida pueda generar una jugada.

Escenario

**Fuente del estímulo:**

STK-01 Jugador durante su turno.

**Estímulo:**

Explora las posiciones del tablero y selecciona una posición inválida durante su turno.

**Ambiente:**

Operación normal durante una partida, mientras el jugador posee el turno.

**Artefacto:**

La interfaz del tablero y la información correspondiente a los movimientos válidos.

**Respuesta:**

La interfaz distingue visualmente las posiciones válidas al posicionar el cursor sobre ellas e impide que una posición inválida pueda generar una jugada.

**Medida de la respuesta:**

- El indicador de posiciones válidas aparece en un máximo de 1 segundo.
- El 100 % de las posiciones inválidas identificadas por la interfaz quedan bloqueadas.
- Intentar seleccionar una posición inválida no modifica el tablero ni provoca un cambio de turno.

**ESC-08 — Consulta de instrucciones durante una partida**

Narrativa

Durante una partida, un jugador que tiene dudas sobre las reglas utiliza la opción de instrucciones disponible en la interfaz, el sistema presenta las instrucciones del juego y permite al jugador regresar posteriormente a la vista de la misma partida.

Escenario

**Fuente del estímulo:**

STK-01 Jugador.

**Estímulo:**

Solicita consultar las instrucciones de Reversi mientras se encuentra dentro de una partida.

**Ambiente:**

Operación normal durante una partida.

**Artefacto:**

La interfaz de partida y el contenido de ayuda.

**Respuesta:**

El sistema presenta las instrucciones del juego y permite al jugador regresar posteriormente a la vista de la misma partida.

**Medida de la respuesta:**

- Las instrucciones son accesibles directamente desde la partida mediante el botón definido.
- Consultar las instrucciones no modifica el tablero, el jugador en turno, el resultado ni la configuración de la partida.
- Al cerrar las instrucciones, el jugador regresa a la misma partida.

**~~ESC-09~~ — Protección de operaciones administrativas [Descartado]**

- **DESCARTADO:** no existe un caso de uso administrativo dentro del alcance acordado.

Narrativa

Durante la operación normal del sistema, un jugador autenticado sin privilegios administrativos intenta ejecutar directamente una operación reservada a la administración, como suspender la cuenta de otro jugador, el sistema identifica que el jugador no posee autorización suficiente y rechaza la operación, la cuenta objetivo no modifica su estado, la operación administrativa no es ejecutada y el intento queda disponible para auditoría si dicha operación está clasificada como auditable.

**Fuente del estímulo:**

STK-01 Jugador autenticado sin privilegios administrativos.

**Estímulo:**

Intenta ejecutar directamente una operación reservada a la administración, como suspender la cuenta de otro jugador.

**Ambiente:**

Operación normal.

**Artefacto:**

Los mecanismos de autorización y las funciones administrativas.

**Respuesta:**

El sistema identifica que el jugador no posee autorización suficiente y rechaza la operación.

**Medida de la respuesta:**

- La cuenta objetivo no modifica su estado.
- La operación administrativa no es ejecutada.
- El intento queda disponible para auditoría si dicha operación está clasificada como auditable.

**ESC-10 — Prueba aislada y controlable de las reglas**

Narrativa

Durante el desarrollo del proyecto, el equipo necesita comprobar que las reglas del juego funcionen exactamente como deben. Para ello, configuran situaciones de juego específicas y simulan jugadas y límites de tiempo de forma automática. El sistema evalúa y resuelve cada regla (como voltear fichas, pasar turno o determinar al ganador) de manera confiable e independiente, sin necesidad de encender la pantalla del juego, la conexión de red ni la base de datos.

**Fuente del estímulo:**

STK-02 Equipo de desarrollo.

**Estímulo:**

Ejecuta pruebas automatizadas sobre las reglas de una partida utilizando estados conocidos del tablero.

**Ambiente:**

Entorno de desarrollo y pruebas.

**Artefacto:**

La lógica responsable de las reglas de Reversi.

**Respuesta:**

Las pruebas pueden establecer estados concretos del tablero, ejecutar movimientos y eventos temporales y observar de forma determinista el estado resultante.

**Medida de la respuesta:**

El 100 % de la suite de reglas se ejecuta en menos de 5 segundos.

La suite se ejecuta sin iniciar:

- interfaz WPF;
- comunicación WCF;
- SQL Server;
- Entity Framework;
- servicio externo de correo.
Las pruebas permiten verificar de manera aislada al menos:

| Caso | Resultado observable |
| --- | --- |
| Movimiento válido | ficha colocada y flanqueos aplicados |
| Flanqueos en varias direcciones | todas las fichas correspondientes cambian |
| Sin movimientos disponibles | turno omitido automáticamente |
| Expiración del turno | se selecciona una posición válida |
| Tres expiraciones consecutivas | derrota por inactividad |
| Ninguno puede jugar | fin de partida |
| Tablero lleno | fin de partida |
| Fin normal | ganador determinado por número de fichas |

**ESC-11 — Modificación de una política de inactividad**

Narrativa

El responsable del producto solicita cambiar la regla sobre cuántos turnos puede perder un jugador por inactividad antes de ser declarado derrotado. El equipo de desarrollo aplica este ajuste directamente sobre la lógica de los turnos de juego, logrando actualizar y validar la nueva regla sin tener que tocar ni poner en riesgo otras funciones del sistema, como el chat, la lista de amigos, el inicio de sesión o el envío de correos.  

**Fuente del estímulo:**

STK-07 Responsable del producto.

**Estímulo:**

Solicita modificar la política de derrota por inactividad, por ejemplo cambiar de tres turnos consecutivos agotados a otro límite aprobado.

**Ambiente:**

Desarrollo y pruebas.

**Artefacto:**

La implementación de las reglas relacionadas con turnos e inactividad.

**Respuesta:**

El equipo modifica y prueba la regla sin producir cambios innecesarios en funcionalidades no relacionadas.

**Medida de la respuesta:**

El cambio no requiere modificar:

- chat;
- amistades;
- autenticación;
- gestión de perfiles;
- integración de correo.

Y las pruebas de las reglas afectadas pueden actualizarse independientemente.

**ESC-12 — Personalización visual local**

Narrativa

Durante una partida en curso, un jugador decide personalizar su experiencia visual cambiando la apariencia de las fichas o seleccionando una imagen personalizada para el fondo del tablero. El sistema aplica los cambios cosméticos únicamente en la vista del cliente local, garantizando que la perspectiva del oponente, las posiciones del tablero, los turnos y el estado general de la partida permanezcan inalterados.

**Fuente del estímulo:**

STK-01 Jugador.

**Estímulo:**

Cambia la representación de las fichas o selecciona una imagen personalizada como fondo del tablero.

**Ambiente:**

Durante una partida.

**Artefacto:**

La presentación visual de la partida y el estado compartido del juego.

**Respuesta:**

El sistema aplica la personalización únicamente a la vista local del jugador.

**Medida de la respuesta:**

- La vista del oponente no cambia.
- Las posiciones de las fichas no cambian.
- Los movimientos válidos no cambian.
- El turno no cambia.
- El resultado de la partida no cambia.

**ESC-13 — Empaquetado de nueva versión**

Narrativa

Al finalizar una versión del videojuego, el equipo de desarrollo decide generar el archivo de instalación para la nueva versión ejecutando el comando para la generación del archivo, el sistema de manera automática compila, empaqueta y genera el archivo de instalación sin necesidad de pasos manuales del equipo de desarrollo

**Fuente del estímulo:**

STK-02 Equipo de desarrollo.

**Estímulo:**

Ejecuta el comando para generar el archivo de instalación.

**Ambiente:**

Al finalizar una versión del videojuego.

**Artefacto:**

El modulo de empaquetado y compilación de la solución

**Respuesta:**

Compila, empaqueta y genera el archivo de instalación de forma automática.

**Medida de la respuesta:**

Se completa mediante un único comando y sin requerir pasos manuales adicionales.

**ESC-14 — Añadiendo nuevo soporte de idioma**

Narrativa

Durante la fase de mantenimiento, el equipo de desarrollo decide incorporar un nuevo idioma a la aplicación; el sistema permite cargar los nuevos textos agregando únicamente el archivo de recursos correspondiente, sin modificar la lógica del juego ni los servicios de red, completando la integración en menos de 4 horas

**Fuente del estímulo:**

STK-02 Equipo de desarrollo.

**Estímulo:**

Se solicita incorporar un nuevo idioma a la interfaz.

**Ambiente:**

Tiempo de desarrollo o mantenimiento del videojuego.

**Artefacto:**

Capa de presentación y archivos de idioma.

**Respuesta:**

Se agregan los nuevos recursos textuales y visuales sin necesidad de modificar el código fuente de la lógica del juego ni los contratos de red.

**Medida de la respuesta:**

100% de los cambios se concentran en archivos de recursos, requiriendo 0 modificaciones en las capas de lógica o comunicación y un tiempo de integración menor a 4 horas de desarrollo.

**~~ESC-15~~ — Modificación de reglas de Reversi [Descartado]**

- **DESCARTADO:** tras descartar el modulo de juego compartido y el modo offline.
    
    Narrativa
    
    El equipo de desarrollo implementa una modificación en las reglas de Reversi, el cambio se aplica en un único punto de la solución y queda disponible tanto para las partidas locales como para las partidas en línea, sin necesidad de reescribir la lógica ni modificar la interfaz de usuario.
    
    **Fuente del estímulo:**
    
    STK-07 Responsable del producto.
    
    **Estímulo:**
    
    Se modifican las reglas de Reversi.
    
    **Ambiente:**
    
    Tiempo de desarrollo o mantenimiento del videojuego.
    
    **Artefacto:**
    
    Módulo del motor de reglas del juego
    
    **Respuesta:**
    
    El sistema incorpora la nueva regla y queda disponible tanto para las partidas locales como para las partidas en línea.
    
    **Medida de la respuesta:**
    
    El cambio se aplica en un único punto de la solución, sin necesidad de reescribir la lógica ni modificar la interfaz de usuario

**ESC-16 — Tolerancia a indisponibilidad de correo**

Narrativa

Durante una función que requiere enviar un correo, el proveedor externo no responde dentro del tiempo permitido. El sistema termina el intento, informa el fallo al jugador y permite que las operaciones no relacionadas con el correo continúen.

**Fuente del estímulo:**

STK-01 Jugador que solicita una función que requiere el envío de correo.

**Estímulo:**

El proveedor externo de correo no responde o supera el tiempo permitido para completar el envío.

**Ambiente:**

Operación normal durante una función que requiere enviar un correo.

**Artefacto:**

La comunicación con el proveedor externo de correo.

**Respuesta:**

El sistema termina la operación de envío, informa el fallo al jugador y permite que las operaciones no relacionadas con el correo continúen.

**Medida de la respuesta:**

La operación de envío termina en un máximo de 5 segundos. Ninguna partida activa se cancela, se pausa o modifica su estado como consecuencia del fallo del proveedor.
    

### **Decisiones arquitectónicas candidatas**

¿Servidor autoritativo para partidas online?

¿Estado de partida persistido en cada jugada o periódicamente?

¿Historial basado en secuencia de movimientos?

¿Motor de reglas compartido entre cliente y servidor?

¿Mecanismo de detección de desconexiones?

¿Política de reintentos para correo?

¿Arquitectura modular concreta?

#### Convención de nomenclatura arquitectónica

- Un **módulo** representa una unidad estructural del código y sus responsabilidades.
- Un **componente** representa una unidad que existe durante la ejecución.
- Las tablas estructurales de las iteraciones usarán la denominación de módulo. Los diagramas de ejecución podrán usar componentes cuando representen instancias o unidades desplegadas en funcionamiento.

#### Decisión incorporada en la tercera iteración

El Módulo de juego proviene de las iteraciones anteriores y se refina en la tercera iteración. Como resultado se incorpora el nuevo Módulo de estado de juego, responsable de mantener en memoria el estado activo y la asociación temporal necesaria para la reconexión durante 30 segundos.

El Módulo de persistencia conserva las responsabilidades de almacenamiento. El Módulo despachador de correo permanece separado de persistencia y limita a 5 segundos la comunicación con el proveedor externo.

#### Evolución estructural de las primeras iteraciones

- La primera iteración define las capas de presentación, negocio, datos y servicios. Todavía no descompone esas capas en módulos.
- La segunda iteración incorpora los módulos de presentación, comunicación, servicios, juego, motor de juego, jugadores, amistades, chat, despacho de correo y persistencia.
- La tercera iteración refina el Módulo de juego e incorpora el nuevo Módulo de estado de juego.
- La quinta iteración conserva tres niveles lógicos: cliente, aplicación y datos. En la vista física existen dos dispositivos; la aplicación de servicio y la base de datos se ejecutan como procesos independientes dentro del mismo dispositivo del servidor.
- En la quinta iteración, los módulos de datos dependen de la base de datos relacional. El Módulo de servicios utiliza el Módulo de registro de eventos para registrar los eventos del servidor.
- La sexta iteración explicita la división del cliente en las capas de presentación, negocio y datos, y la división del servidor en las capas de arranque, servicios, negocio, persistencia y transversal. También refina la estructura interna del servidor para priorizar la facilidad de prueba: incorpora la Raíz de composición del servidor, invierte las dependencias hacia la capa de persistencia y el Módulo despachador de correo mediante contratos definidos en la capa de negocio, separa los modelos de dominio en las fronteras y subdivide el Módulo de servicios por área funcional.

- La séptima iteración define MVVM para la capa de presentación del cliente. Conserva la capa de negocio del cliente en el proyecto de validación y la capa de datos en el proyecto de comunicación. Los ViewModels coordinan la interacción con ambas capas y el Módulo de comunicación encapsula el acceso remoto mediante Proxy.
- En la séptima iteración, presentación se subdivide por flujo de usuario y comunicación por cliente remoto. Sala, partida en curso y chat comparten el cliente de partidas.

### Árbol de utilidad

Utilidad

| Categoría (Atributo de calidad) | Refinamiento (Atributo) | Escenario (Hoja, ASR) | Par |
| --- | --- | --- | --- |
| Availability | Recoverability | ESC-04 Recuperación ante desconexión temporal | (A, A) |
| Security | Auditability | ESC-01 Reconstrucción de acciones sobre partidas | (M, M) |
| Security | Integrity | ESC-03 Intento de realizar una jugada inválida | (A, M) |
| Availability | Fault Tolerance | ESC-16 Tolerancia a indisponibilidad de correo | (A, M) |
| Performance | Latency / Response Time | ESC-05 Procesamiento y propagación de una jugada | (A, M) |
| Performance | Deadline Compliance / Temporal Behavior | ESC-06 Gestión de inactividad durante un turno | (M, M) |
| Usability | User Error Prevention | ESC-07 Identificación y prevención de jugadas inválidas | (M, B) |
| Usability | Learnability | ESC-08 Consulta de instrucciones durante la partida | (M, B) |
| Usability | Operational Efficiency | ESC-02 Disponibilidad de la partida contra el sistema | (M, B) |
| Testability | Controllability / Test Isolation | ESC-10 Prueba aislada y controlable de las reglas | (A, M) |
| Modifiability | Change Locality | ESC-11 Modificación de una política de inactividad | (M, B) |
| Deployability | Automation | ESC-13 Empaquetado de nueva versión | (M,M) |
| Modifiability | Change Locality / Internationalization | ESC-14 Añadiendo nuevo soporte de idioma | (A,M) |
| Modifiability | Change Locality | ~~ESC-15~~ Modificación de reglas de Reversi | (A,M) |

### Historias de usuario

Como rol, quiero capacidad, para valor

HU-01

Tarjeta

- Identificador: HU-01
- Título: Creación de partida
- Narrativa:
    
    Como jugador anfitrión, quiero crear una partida, para poder iniciar el juego una vez que haya un contrincante disponible.
    

Conversación y Notas de Alcance 

Dependencias y reglas:

- La partida se inicializa con el tiempo de turno predeterminado del sistema.
- La opción para iniciar una partida contra el sistema está disponible desde que se muestra la pantalla de espera, según lo definido en ESC-02.
- Si la partida deriva en un juego contra el bot, se considera una partida de práctica local y no alterará las estadísticas competitivas del jugador.

Criterios de aceptación

Escenario 1: Creación de sala y espera de contrincante (Camino principal - Parte 1)

Dado que el jugador anfitrión se encuentra autenticado en el menú principal,
Cuando selecciona la opción de crear partida,
Entonces el sistema muestra la pantalla de espera con el indicador visual *"Esperando contrincante",*
Y la sala queda registrada y disponible para recibir conexiones de otros jugadores. 

Escenario 2: Conexión de un contrincante humano e inicio de partida (Camino principal - Parte 2)

Dado que el jugador anfitrión se encuentra en la pantalla de espera *"Esperando contrincante"*,
Cuando un segundo jugador se une a la partida,
Entonces el sistema transiciona a la pantalla de juego del tablero,
Y el tablero presenta el estado de inicio,
Y se indica en la interfaz a qué jugador corresponde el primer turno,
Y se inicia la cuenta regresiva del tiempo asignado para dicho turno

Escenario 3: Inicio de partida contra el sistema (Camino alterno)

Dado que el jugador anfitrión se encuentra en la pantalla de espera,
Cuando selecciona la opción para jugar contra el sistema,
Entonces el sistema muestra la pantalla de juego del tablero,
Y el tablero presenta el estado de inicio,
Y se indica el jugador que tiene el turno inicial,
Y comienza la cuenta regresiva del tiempo del turno,
Y la sala deja de aceptar conexiones de otros jugadores.

HU-02

Tarjeta

- Identificador: HU-02
- Título: Realización de una jugada
- Narrativa:
    
    Como jugador, quiero identificar y seleccionar una posición válida durante mi turno, para realizar avanzar la partida.
    

Conversación y Notas de Alcance

Dependencias y reglas:

- La historia aplica durante una partida activa.
- El sistema debe determinar las posiciones válidas a partir del estado actual del tablero y las reglas de Reversi.
- La interfaz debe permitir al jugador reconocer visualmente las posiciones válidas.
- Una posición inválida no debe producir ningún cambio sobre la partida.
- En una partida en línea, el servidor conserva la autoridad sobre la validez de la jugada; la validación realizada por la interfaz no sustituye la validación del servidor.
- Una jugada válida debe colocar una ficha y voltear todas las fichas correspondientes conforme a las reglas de Reversi.
- Después de una jugada válida, el estado actualizado debe ser consistente para ambos participantes.
- El movimiento debe conservarse como parte del historial de la partida.
- La gestión del agotamiento del tiempo se desarrolla específicamente en HU-03.

Criterios de aceptación

**Escenario 1: Identificación de posiciones válidas (Camino principal - Parte 1)**

Dado que el jugador se encuentra en una partida activa y posee el turno,

Cuando desplaza el cursor sobre las posiciones del tablero,

Entonces el sistema distingue visualmente las posiciones en las que puede realizar una jugada válida,

Y las posiciones indicadas corresponden al estado actual del tablero.

**Escenario 2: Realización de una jugada válida (Camino principal - Parte 2)**

Dado que el jugador posee el turno y existe al menos una posición válida,

Cuando selecciona una posición válida,

Entonces el sistema coloca la ficha en la posición seleccionada,

Y voltea todas las fichas correspondientes conforme a las reglas de Reversi,

Y registra el movimiento realizado,

Y actualiza el conteo de fichas,

Y determina el turno que corresponde a continuación,

Y ambos participantes reciben el mismo estado válido de la partida.

**Escenario 3: Selección de una posición inválida (Camino alterno)**

Dado que el jugador posee el turno,

Cuando intenta seleccionar una posición que no constituye una jugada válida,

Entonces el sistema rechaza la selección,

Y no incorpora ninguna ficha,

Y no voltea ninguna ficha,

Y no modifica el conteo de fichas,

Y no cambia el turno,

Y conserva el último estado válido de la partida.

**Escenario 4: Solicitud inválida enviada mediante un cliente modificado (Camino alterno)**

Dado que existe una partida en línea activa,

Cuando se recibe una solicitud de movimiento que viola las reglas, corresponde a otro turno o utiliza un estado desactualizado,

Entonces el servidor rechaza la solicitud,

Y conserva el último estado válido reconocido para la partida,

Y la solicitud no genera un movimiento persistido.

**Escenario 5: El oponente no dispone de movimientos válidos**

Dado que el jugador realiza correctamente una jugada,

Cuando el sistema determina que el oponente no dispone de ninguna posición válida pero el jugador actual sí dispone de al menos una,

Entonces el sistema omite el turno del oponente,

Y asigna nuevamente el turno al jugador que puede realizar una jugada,

Y la partida continúa.

**Escenario 6: Ninguno de los jugadores dispone de movimientos válidos**

Dado que se ha actualizado el tablero después de una jugada,

Cuando el sistema determina que ninguno de los dos jugadores dispone de movimientos válidos,

Entonces el sistema finaliza la partida,

Y determina el resultado utilizando la cantidad de fichas de cada jugador,

Y registra el resultado definitivo,

Y no permite realizar nuevos movimientos.

HU-03

Tarjeta

- Identificador: HU-03
- Título: Gestión de inactividad durante el turno
- Narrativa:
    
    Como jugador, quiero que la partida continúe cuando un participante no realiza una jugada dentro del tiempo disponible, para evitar que la inactividad bloquee indefinidamente el desarrollo del juego.
    

Conversación y Notas de Alcance

Dependencias y reglas:

- Cada turno utiliza el límite temporal configurado para la partida.
- El tiempo predeterminado es de 40 segundos.
- Esta historia aplica cuando el jugador en turno dispone de al menos un movimiento válido.
- Si el jugador no realiza una jugada antes de finalizar el tiempo, el sistema selecciona aleatoriamente una de las posiciones válidas disponibles.
- El sistema debe realizar exactamente una jugada automática.
- Una jugada automática por agotamiento de tiempo se registra diferenciándola de una jugada realizada directamente por el jugador.
- Cada agotamiento de tiempo se contabiliza como una inactividad consecutiva.
- Si el jugador realiza normalmente una jugada dentro del tiempo disponible, su contador de inactividades consecutivas se reinicia.
- Tres turnos consecutivos agotados por inactividad provocan la derrota del jugador.
- La ausencia de movimientos válidos no debe contabilizarse como inactividad del jugador.

Criterios de aceptación

**Escenario 1: Jugada realizada antes de agotarse el tiempo (Camino principal)**

Dado que el jugador posee el turno y dispone de movimientos válidos,

Cuando realiza una jugada válida antes de alcanzar el límite temporal configurado,

Entonces el sistema registra la jugada realizada por el jugador,

Y no registra el turno como agotado por inactividad,

Y reinicia el contador de inactividades consecutivas del jugador,

Y la partida continúa normalmente.

**Escenario 2: Primer agotamiento consecutivo del tiempo (Camino alterno)**

Dado que el jugador posee el turno, dispone de movimientos válidos y no ha agotado el tiempo en el turno inmediatamente anterior,

Cuando se alcanza el límite temporal sin que realice una jugada válida,

Entonces el sistema determina las posiciones válidas disponibles,

Y selecciona aleatoriamente una de ellas,

Y realiza exactamente una jugada válida en nombre del jugador,

Y registra que el movimiento fue realizado automáticamente por agotamiento del tiempo,

Y contabiliza una inactividad consecutiva,

Y la partida continúa.

**Escenario 3: Segundo agotamiento consecutivo del tiempo (Camino alterno)**

Dado que el jugador posee una inactividad consecutiva registrada,

Cuando vuelve a agotarse el tiempo de su turno sin realizar una jugada válida,

Entonces el sistema realiza exactamente una jugada automática sobre una posición válida,

Y contabiliza dos inactividades consecutivas,

Y la partida continúa.

**Escenario 4: Tercer agotamiento consecutivo del tiempo (Camino alterno)**

Dado que el jugador ha agotado sus dos turnos anteriores de forma consecutiva,

Cuando vuelve a alcanzar el límite temporal sin realizar una jugada válida,

Entonces el sistema registra la tercera inactividad consecutiva,

Y finaliza la partida,

Y registra la derrota del jugador por inactividad,

Y registra al oponente como ganador,

Y no permite realizar nuevos movimientos.

**Escenario 5: El jugador no dispone de movimientos válidos**

Dado que comienza el turno de un jugador,

Cuando el sistema determina que no dispone de ninguna posición válida,

Entonces el sistema omite su turno conforme a las reglas de Reversi,

Y no contabiliza el turno omitido como agotado por inactividad,

Y determina si el otro jugador puede continuar jugando.

### Definition of Done

### Acuerdos con STK-06

1. **¿Por qué se descarta el modo offline?**
    - *Justificación:* Optimización de esfuerzo. Con un equipo de dos desarrolladores (ORG-01) dentro del periodo escolar (ORG-02), implementar juego local diluye el foco del criterio de evaluación principal, que es la gestión de concurrencia y comunicación remota.
2. **¿Por qué se reduce el supuesto de 20 a 8 jugadores concurrentes?**
    - Ajuste a un escenario de prueba verificable y realista para el alcance académico (4 salas de 2 jugadores), complementado con la obligación de ejecutar pruebas empíricas de saturación para identificar el limite del servidor.
3. **¿Por qué se acepta la duplicidad de código y se sacrifica la localidad del cambio en las reglas del juego?**
    - Evitar el acoplamiento binario estrecho en despliegues independientes cliente-servidor. El cliente solo requiere conocer si una casilla es seleccionable para dar retroalimentación inmediata al cursor (UX/Performance sin latencia de red); el servidor retiene la autoridad legal completa del cálculo del flanqueo y estado.

## Proceso de Diseño

### Revisar entradas

#### Catálogo de QAS aprobados

| QAS | Escenario | Fuente | Medida principal |
| --- | --- | --- | --- |
| **QAS-Seguridad-1** | Intento de realizar una jugada inválida | STK-05 Adversario mediante un cliente modificado | El 100 % de las solicitudes identificadas como inválidas se rechazan sin colocar fichas, cambiar el turno, modificar el conteo ni persistir un movimiento. |
| **QAS-Disponibilidad-1** | Recuperación ante desconexión temporal | STK-01 Jugador que participa en una partida en línea | Si la conexión se recupera en un máximo de 30 segundos, el jugador vuelve a la misma partida y obtiene el último estado válido sin perder ni duplicar movimientos confirmados. |
| **QAS-Disponibilidad-2** | Tolerancia a indisponibilidad de correo | STK-01 Jugador que solicita una función que requiere correo | El envío termina en un máximo de 5 segundos y el fallo no cancela, pausa ni modifica partidas activas. |
| **QAS-Rendimiento-1** | Procesamiento y propagación de una jugada | STK-01 Jugador en turno | El estado actualizado queda disponible para ambos jugadores en menos de 1 segundo en el 95 % de las jugadas, con hasta 8 jugadores en 4 partidas. |
| **QAS-Rendimiento-2** | Gestión de inactividad durante un turno | STK-01 Jugador cuyo turno está activo | Se realiza exactamente 1 jugada automática válida y, al tercer turno consecutivo agotado, la partida termina con derrota del jugador inactivo. |
| **QAS-Usabilidad-1** | Identificación y prevención de jugadas inválidas | STK-01 Jugador durante su turno | Las posiciones válidas se indican en un máximo de 1 segundo y el 100 % de las posiciones identificadas como inválidas quedan bloqueadas. |
| **QAS-Usabilidad-2** | Disponibilidad de la partida contra el sistema | STK-01 Jugador que crea una partida | La opción está disponible desde la primera presentación de la pantalla de espera, con 0 segundos de espera obligatoria. |
| **QAS-FacilidadPrueba-1** | Prueba aislada y determinista de las reglas | STK-02 Equipo de desarrollo | El 100 % de la suite de reglas se ejecuta en menos de 5 segundos sin iniciar WPF, WCF, SQL Server, Entity Framework ni el proveedor de correo. |
| **QAS-Modificabilidad-1** | Incorporación de un nuevo idioma | STK-02 Equipo de desarrollo | Los cambios se concentran en recursos, requieren 0 cambios en reglas o contratos remotos y se completan en menos de 4 horas. |
| **QAS-Desplegabilidad-1** | Empaquetado de una nueva versión | STK-02 Equipo de desarrollo | La generación se inicia con 1 comando y requiere 0 pasos manuales adicionales durante la compilación y el empaquetado. |

#### Catálogo de ASR prioritarios

| ASR | Qué debe cumplir la arquitectura | Viene de |
| --- | --- | --- |
| **ASR-01** | Mantener en el servidor la autoridad sobre las reglas, el turno y el estado vigente. Las solicitudes inválidas deben rechazarse sin alterar ni persistir un movimiento. | CU-16 / HU-02 y QAS-Seguridad-1 |
| **ASR-02** | Conservar durante 30 segundos la sesión y el estado necesarios para reincorporar al jugador a la misma partida sin perder ni duplicar movimientos confirmados. | QAS-Disponibilidad-1 |
| **ASR-03** | Procesar una jugada válida y poner el estado actualizado a disposición de ambos jugadores en menos de 1 segundo en el 95 % de los casos, con hasta 8 jugadores en 4 partidas. | QAS-Rendimiento-1 y ASM-02 |
| **ASR-04** | Aislar al proveedor externo de correo y terminar la operación en un máximo de 5 segundos sin afectar partidas activas. | QAS-Disponibilidad-2 |
| **ASR-05** | Mantener la aplicación distribuida entre un cliente WPF y un servidor WCF mediante contratos remotos, sin acceso directo del cliente a la lógica o persistencia del servidor. | RES-TEC-02 y RES-TEC-03 |
| **ASR-06** | Permitir que el cliente indique posiciones válidas en un máximo de 1 segundo y bloquee las posiciones que reconoce como inválidas antes de enviar una solicitud. | CU-16 / HU-02 y QAS-Usabilidad-1 |
| **ASR-07** | Ejecutar toda la suite de reglas en menos de 5 segundos con estados controlados y sin iniciar WPF, WCF, SQL Server, Entity Framework ni el proveedor de correo. | STK-02 y QAS-FacilidadPrueba-1 |

QAS-Desplegabilidad-1 se conserva como escenario planeado para la cuarta iteración y no forma parte de los ASR prioritarios.

Drivers con mayor peso arquitectónico y que encapsulan la funcionalidad primaria

- CU-01 Crear cuenta
- CU-09 Modificar idioma de interfaz
- CU-11 Crear partida
- CU-15 Unirse a partida
- CU-16 Realizar movimiento
- CU-21 Enviar solicitud de amistad
- CU-23 Responder solicitud de amistad
- CU-26 Enviar mensaje
- CU-28 Entrar como invitado

---

- ESC-03 Intento de realizar una jugada inválida
- ESC-04 Recuperación ante desconexión temporal
- ESC-16 Tolerancia a indisponibilidad de correo
- ESC-05 Procesamiento y propagación de una jugada
- ESC-06 Gestión de inactividad durante un turno
- ESC-07 Identificación y prevención de jugadas inválidas
- ESC-02 Disponibilidad de la partida contra el sistema
- ESC-10 Prueba aislada y controlable de las reglas
- ESC-14 Añadiendo nuevo soporte de idioma
- ESC-13 Empaquetado de nueva versión

---

- RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework.
- RES-TEC-02 — Necesidad de comunicación remota.
- RES-TEC-03 — El cliente de escritorio deberá implementarse utilizando Windows Presentation Foundation (WPF).
- RES-TEC-04 — La persistencia relacional del sistema deberá utilizar Microsoft SQL Server.

---

- **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity
- **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability
- **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability
- **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance
- **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability

---

- El modo offline o local queda fuera del entregable principal para priorizar las partidas en línea.
- ASM-02 — Se asume una carga base de 8 jugadores activos concurrentes, distribuidos en 4 partidas simultáneas de 2 jugadores.

---

- ORG-01 — El equipo de desarrollo estará compuesto por dos personas.
- ORG-02 — El sistema deberá alcanzar el alcance acordado dentro del periodo escolar asignado al proyecto.
- ORG-03 — Los integrantes del equipo deberán diseñar, implementar, probar, documentar y desplegar el sistema sin equipos especializados independientes para infraestructura, QA o seguridad.

[Primera iteración](Primera%20iteraci%C3%B3n.md)

[Segunda iteración](Segunda%20iteraci%C3%B3n.md)

[Tercera iteración](Tercera%20iteraci%C3%B3n.md)

[Cuarta iteración](Cuarta%20iteraci%C3%B3n.md)

[Iteración 5 -Refinamiento](Iteraci%C3%B3n%205%20-Refinamiento.md)

[Documento-Entrega](Documento-Entrega.md)
