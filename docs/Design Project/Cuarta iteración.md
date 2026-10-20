# Cuarta iteración

Objetivo:

Diseñar las tácticas de facilidad de despliegue para garantizar que el sistema puede desplegarse en un tiempo y esfuerzo predecibles y aceptables.

Paso 2: Drivers para establecer el objetivo de la iteración

- ESC-13 Empaquetado de nueva versión
- CON-05 — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability

Paso 3: Seleccionar uno o más elementos del sistema a refinar

El proceso de construcción y empaquetado del cliente y del servidor y su relación con las salidas de compilación.

Paso 4: Conceptos de diseño que satisfacen los drivers

- **Comandos de despliegue mediante script:** una secuencia repetible, guardada junto al código, que falle si cualquiera de sus etapas falla.
- **Empaquetado de dependencias:** definición explícita de los binarios, bibliotecas, recursos e información de versión que debe incluir el instalador.
    
    **Herramienta de empaquetado seleccionada: Inno Setup.** Se utilizarán dos definiciones de instalación, una para el cliente WPF y otra para el servidor. El script de versión compilará los proyectos, preparará sus archivos y ejecutará el compilador de Inno Setup para generar ambos instaladores mediante un único comando. Se selecciona esta herramienta porque permite expresar el contenido de cada instalador en archivos mantenidos junto al código y automatizar su generación desde la línea de comandos.
    
    **Alternativas consideradas:** WiX, por su integración con MSBuild y la generación de MSI, y ClickOnce, por sus capacidades de publicación y actualización del cliente Windows. Se pospone WiX porque el proyecto no requiere un MSI en esta iteración. Se descarta ClickOnce para este proceso conjunto porque la decisión arquitectónica actual contempla generar también un instalador independiente del servidor.
    
    **Costo aceptado:** El equipo deberá mantener el script y las dos definiciones de Inno Setup cuando cambien los archivos publicados, las dependencias, los requisitos de instalación o la forma de ejecutar el servidor. También deberá comprobar que ambos instaladores corresponden a la misma versión y que funcionan en una instalación de prueba.
    
- **Separación de configuración por entorno:** el instalador no debe incorporar una cadena de conexión personal o secretos.

Paso 5: Instanciar elementos arquitectónicos

| Elemento propuesto | Responsabilidad |
| --- | --- |
| Script de versión | Recibir la versión, ejecutar las etapas en orden y detenerse ante errores. |
| Definiciones de los instaladores | Declarar por separado los archivos del cliente y del servidor, así como la ubicación de cada artefacto |
| Artefactos de instalación | Contener la versión compilada y los recursos necesarios para instalarla. |
| Configuración externa | Aportar valores propios de cada entorno sin fijarlos en el instalador. |

Paso 6: Bocetar vistas y registrar decisiones de diseño

![Despliegue de instaladores.png](Cuarta%20iteraci%C3%B3n/Despliegue_de_instaladores.png)

El proceso comienza cuando el equipo ejecuta un único comando, que inicia el script encargado de compilar la solución y reunir las dependencias necesarias. Esta secuencia automatizada evita que cada integrante tenga que repetir manualmente las tareas de preparación y reduce las diferencias entre versiones generadas.

Después, el proceso se divide para producir por separado el instalador del cliente y el del servidor, de acuerdo con la arquitectura cliente-servidor definida en la primera iteración. Cada instalador incorpora los archivos que necesita su respectivo nodo. Ambas ramas deben concluir correctamente antes de versionar los artefactos; si falla la compilación o la generación de cualquiera de los instaladores, el proceso se detiene y no presenta la versión como completa.

El script recibe al inicio el identificador de versión y lo aplica a ambos instaladores. Al finalizar, comprueba que los dos artefactos se generaron correctamente con esa versión. Así, la vista muestra cómo se atiende **ESC-13**: un solo comando desencadena la compilación y la generación automática de los archivos de instalación, sin pasos manuales adicionales. La distribución e instalación de esos archivos en los equipos queda pendiente para abordar completamente **CON-05**.

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
| ESC-13 Empaquetado de nueva versión |  |  | X | Abordado mediante la decisión de automatizar la creación de instaladores para nuevas versiones del sistema. |
| RES-TEC-01 — La persistencia deberá implementarse utilizando Entity Framework. |  |  | X | Se encapsuló el acceso a datos en el Componente de persistencia, donde Entity Framework gestionará el mapeo y almacenamiento de las entidades derivadas del modelo de dominio. |
| • **CON-02** — Asegurar la persistencia íntegra e inmutable del progreso de los jugadores y de los registros de auditoría ante fallos operativos. → Reliability / Auditability |  | X |  | Debido a que, ante una interrupción súbita o caída del proceso del servidor, los movimientos no registrados a la BD se pierden, priorizando la disponibilidad del juego en memoria sobre la consistencia. |
| **CON-03** — Facilitar la adaptación a nuevos idiomas y perfiles de accesibilidad con un impacto mínimo en la arquitectura y esfuerzo de desarrollo. → Modifiability / Usability | X |  |  | - |
| **CON-04** — Una falla o indisponibilidad del servicio externo de correo no deberá degradar ni interrumpir el flujo principal de las partidas en línea. → Availability / Fault Tolerance |  |  | X | Se formalizo el flujo de respuesta del Despachador de correo ante una indisponibilidad del servicio de correo. |
| **CON-05** — Facilitar la distribución de actualizaciones con mínima intervención manual. → Deployability/Maintainability |  | X |  | Se define una forma para obtener las actualizaciones de manera empaquetada, falta definir la distribución. |
| ASM-02 — Se asume un alcance operativo de 20 jugadores activos concurrentes | X |  |  | - |