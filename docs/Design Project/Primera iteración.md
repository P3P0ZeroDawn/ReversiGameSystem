# Primera iteración

**Nota de versión:** Las decisiones sobre el patrón Two-Tier y el motor de juego del cliente planteadas en esta iteración fueron refinadas y sustituidas en la **Iteración 5** a raíz de los acuerdos con STK-06. Consultar la Iteración 5 para la arquitectura vigente.

Objetivo: establecer la estructura general del sistema

Paso 2: Drivers para establecer el objetivo de la iteración

- CNR-ARQ-01 — Estructura global
- RES-TEC-02 — Necesidad de comunicación remota.
- RES-TEC-04 — La persistencia relacional del sistema deberá utilizar Microsoft SQL Server.
- ESC-05 Procesamiento y propagación de una jugada
- RES-TEC-03 — El cliente de escritorio deberá implementarse utilizando Windows Presentation Foundation (WPF).
- ASM-01 — Se asume que el sistema deberá permitir partidas en modo offline/local.
- **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity

Paso 3: Elementos a refinar

Como nos encontramos en la primera iteración de un sistema greenfield, el elemento que debemos refinar es el sistema completo.

Paso 4: Conceptos de diseño que satisfacen los drivers

Arquitectura de referencia:

Nos basamos en una combinación de dos arquitecturas de referencia:

Rich Client Application:

Por estar orientada a aplicaciones instaladas ejecutadas en el dispositivo del usuario. Responde a RES-TEC-03.

Thick client por contener lógica de negocio.

Implementación en Windows Presentation Foundation (RES-TEC-03)

Fueron consideradas las alternativas Web Application y Mobile Application pero fueron descartadas ya que no satisfacen RES-TEC-03.

Costo aceptado: el cliente requiere instalación y ejecución local en el dispositivo del jugador.

Service Application

Por estar orientada a aplicaciones que ofrecen servicios consumidos por otras aplicaciones, sin disponer de una interfaz de usuario. Responde a RES-TEC-02.

Fue considera la alternativa en que el propio cliente sea host, pero fue descartada para garantizar **CON-01.**

Costo aceptado: comunicación remota, mantener la infraestructura del servidor.

Estilo arquitectónico:

Cliente - Servidor:

Por ser un estilo para sistemas distribuidos separados mediante una red. Se elige para dar cumplimiento a RES-TEC-02, **CON-01** y ESC-05.

Se consideró la alternativa de pares (peer to peer) pero fue descartada para cumplir con **CON-01.**

Costo aceptado: comunicación remota, mantener la infraestructura del servidor.

Patrón de despliegue:

Two-Tier:

Por ser un sistema que requiere un cliente ejecutándose localmente en el dispositivo del cliente y una aplicación de servicio funcionando de manera remota para dar cumplimiento a RES-TEC-02.

Se consideró la alternativa Three-Tier pero fue descartada porque los drivers actuales no justifican introducir un nivel físico independiente para la persistencia de los datos.

Costo aceptado: comunicación remota, mantener la infraestructura del servidor, incluyendo la persistencia de los datos.

Mecanismo para comunicación remota:

Windows Communication Foundation (WCF) - Seleccionado:

Se selecciona por ser un mecanismo nativo del ecosistema .NET, lo cual lo convierte en una opción obvia para cumplir con la necesidad de comunicación remota por su alta compatibilidad.

Costo aceptado: se considera una tecnología legacy

gRPC - Alternativa considerada:

Se consideró como alternativa por ser un mecanismo de comunicación remota más actualizado que WCF, por lo que puede ofrecer mejores condiciones de seguridad, rendimiento y compatibilidad con sistemas más nuevos, sin embargo, se descartó por  ser una tecnología externa al ecosistema .NET, por lo que muchas de sus capacidades no podrían utilizarse al máximo.

Paso 5: Instanciar elementos arquitectónicos

Rich Client Application → cliente

Por tener características de procesamiento local, lo que nos permite tener una aplicación que funcione del lado del cliente, manteniendo el servidor únicamente como aplicación de servicio.

Capa de presentación para mostrar y controla la interfaz de usuario

Capa de negocio para contener el motor de reglas de Reversi

Capa de datos adaptada para comunicación en red con el servidor

Service Application → servidor

Para exponer funcionalidades que puedan ser consumidas por los Rich Client Application de manera remota y mantener un control centralizado del sistema.

Capa de servicios para exponer los servicios a consumir por los Rich Client Application e intercambiar mensajes con ellos

Capa de negocio contener el motor de reglas de Reversi, lógica del sistema que soporta la funcionalidad primaria: partidas, puntuaciones, propagación de movimientos, etc.

Capa de datos para ofrecer una interfaz uniforme a la capa de negocio para el acceso a la base de datos.

Cliente - Servidor → estilo arquitectónico distribuido

Cliente para ejecutar las instancias del cliente de Reversi en los dispositivos de los usuarios.

Servidor para proporcionar de manera centralizada el manejo del servicio de juego, chat, autenticación, manejo de amistades.

Two - Tier → patrón de despliegue

Tier cliente para la ejecución de la Rich Client Application

Tier servidor para la ejecución de la Service Application y el servidor de base de datos

Paso 6: Bocetar vistas y registrar decisiones de diseño

![Reversi Módulos.png](Primera%20iteraci%C3%B3n/Reversi_Mdulos.png)

![Reversi despliegue.png](Primera%20iteraci%C3%B3n/Reversi_despliegue.png)

Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente Abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| CNR-ARQ-01 — Estructura global |  |  | X | Se definió la estructura global del sistema usando el estilo arquitectónico cliente - servidor y el patrón de despliegue Two Tier. |
| • RES-TEC-02 — Necesidad de comunicación remota |  |  | X | Se definió WCF como mecanismo de comunicación remota |
| RES-TEC-04 — La persistencia relacional del sistema deberá utilizar Microsoft SQL Server. |  |  | X | Se asignó la base de datos con SQL Server al nodo del servidor. |
| ESC-05 Procesamiento y propagación de una jugada |  | X |  | Se definió el estilo arquitectónico cliente - servidor, donde se localiza la responsabilidad del servidor de comunicar la propagación de las jugadas. |
| RES-TEC-03 — El cliente de escritorio deberá implementarse utilizando Windows Presentation Foundation (WPF). |  |  | X | Se definió el uso de una Rich Client  Application usando la tecnología de WPF. |
| ASM-01 — Se asume que el sistema deberá permitir partidas en modo offline/local. |  | X |  | Se definió una capa de negocio en el cliente para contener el motor de reglas de Reversi. |
| **CON-01** — Garantizar la integridad y consistencia del estado de las partidas en línea, evitando que un cliente pueda modificar unilateralmente el estado válido del juego. → Security / Integrity |  | X |  | Se definió un estilo arquitectónico cliente - servidor para permitir una autoridad centralizada en las partidas independiente de los clientes. |
| CU-01 Crear cuenta | X |  |  | - |
| CU-11 Crear partida | X |  |  | - |
| CU-09 Modificar idioma de interfaz | X |  |  | - |
| CU-15 Unirse a partida | X |  |  | - |
| CU-16 Realizar movimiento | X |  |  | - |
| CU-21 Enviar solicitud de amistad | X |  |  | - |
| CU-23 Responder solicitud de amistad | X |  |  | - |
| CU-26 Enviar mensaje | X |  |  | - |
| CU-28 Entrar como invitado | X |  |  | - |
| • ESC-04 Recuperación ante desconexión temporal | X |  |  | - |
| ESC-09 Protección de operaciones administrativas | X |  |  | - |
| ESC-03 Intento de realizar una jugada inválida | X |  |  | - |
| ESC-14 Añadiendo nuevo soporte de idioma | X |  |  | - |
| ESC-15 Modificación de reglas de Reversi | X |  |  | - |
| ESC-10 Prueba aislada y controlable de las reglas | X |  |  | - |
| ESC-13 Empaquetado de nueva versión | X |  |  | - |
| RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework. | X |  |  | - |
| • **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability | X |  |  | - |
| **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability | X |  |  | - |
| **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance | X |  |  | Se identificó el nodo externo en el despliegue, pero las decisiones para mitigar fallos y garantizar disponibilidad quedan para una iteración posterior |
| **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability | X |  |  | - |
| ASM-02 — Se asume un alcance operativo de 20 jugadores activos concurrentes | X |  |  | - |