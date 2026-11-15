# Tercera iteración

Objetivo:

Diseñar el manejo de desconexiones temporales y de fallos del servicio de correo para que una partida activa conserve su estado válido y no sea afectada por la indisponibilidad de una dependencia externa.

Paso 2: Drivers para establecer el objetivo de la iteración

- **ASR-02 / QAS-Disponibilidad-1:** El servidor debe conservar durante 30 segundos la sesión y el estado necesarios para reincorporar a un jugador desconectado sin perder ni duplicar movimientos confirmados.
- **ASR-04 / QAS-Disponibilidad-2:** La comunicación con el proveedor de correo debe estar aislada. Si el proveedor no responde, la operación debe terminar en un máximo de 5 segundos sin cancelar, pausar ni modificar partidas activas.
- **STK-01:** El jugador requiere recuperar una partida interrumpida temporalmente y recibir una respuesta controlada cuando una función de correo no puede completarse.
- **RES-TEC-01 / RES-TEC-04:** La persistencia utiliza Entity Framework y Microsoft SQL Server, pero el estado vigente de una partida activa no debe depender de una consulta a la base de datos para cada operación.

Escenarios afectados:

- **ESC-04:** Recuperación ante desconexión temporal.
- **ESC-16:** Tolerancia a indisponibilidad de correo.
- **CU-01:** Crear cuenta, cuando se requiere enviar un código por correo.

Paso 3: Seleccionar uno o más elementos del sistema a refinar

- El Módulo de juego, que coordina las partidas activas.
- El Módulo de persistencia, que concentra las responsabilidades de almacenamiento.
- El Módulo de jugadores, que coordina las funciones de cuenta.
- El Módulo despachador de correo y su relación con el proveedor externo de correo.

Paso 4: Conceptos de diseño que satisfacen los drivers

### Conservación temporal de la sesión y del estado activo

Como concepto candidato se considera conservar temporalmente en el servidor la información necesaria para recuperar una interacción interrumpida. Este concepto permite establecer un periodo de 30 segundos en el que la sesión y la asociación con la partida continúan disponibles.

Alternativas consideradas:

- **Retirar al jugador inmediatamente:** Se descarta porque una interrupción breve terminaría la asociación con la partida e impediría cumplir la recuperación dentro de 30 segundos.
- **Conservar la sesión y la asociación sin límite:** Se descarta porque mantendría información de sesiones y partidas abandonadas sin un criterio para liberar los recursos ocupados.
- **Reconstruir la partida únicamente desde persistencia:** Se descarta porque la recuperación dependería de que todo el estado activo ya se encontrara almacenado. También incorporaría acceso a datos al flujo inmediato de reconexión.

Costo del concepto: El servidor deberá controlar el periodo de conservación y liberar la información temporal cuando expire.

### Estado activo separado de la representación persistente

Como concepto candidato se considera separar el estado activo de una partida de la información que debe conservarse de forma persistente. El estado activo puede mantenerse en memoria para atender el flujo de juego y la recuperación temporal, mientras la persistencia mantiene los datos que deben sobrevivir al proceso.

Alternativas consideradas:

- **Consultar y actualizar la base de datos en cada operación:** Se descarta porque la continuidad de la partida y la reconexión temporal quedarían condicionadas por la disponibilidad y el tiempo de respuesta de SQL Server.
- **Mantener toda la información únicamente en memoria:** Se descarta porque las cuentas, resultados y demás datos que deben sobrevivir al proceso no quedarían almacenados.
- **Introducir una base de datos en memoria adicional:** Se descarta porque agregaría otra tecnología y responsabilidades operativas que no se justifican para la carga base de 8 jugadores concurrentes.

Costo del concepto: Una interrupción completa del proceso del servidor puede provocar la pérdida de cambios que todavía no se hayan persistido. Este concepto no resuelve la recuperación ante una caída total del servidor.

### Tiempo límite para el proveedor de correo

Como concepto candidato se considera limitar temporalmente la interacción con una dependencia externa y convertir su fallo en una respuesta controlada. El límite debe permitir que la operación termine en un máximo de 5 segundos y que las partidas activas continúen sin cambios.

Alternativas consideradas:

- **Esperar sin un límite definido:** Se descarta porque puede mantener bloqueada la operación y no cumple el máximo de 5 segundos.
- **Realizar varios reintentos dentro de la misma solicitud:** Se descarta porque reduce el tiempo disponible para cada intento y puede superar el límite establecido antes de entregar una respuesta.
- **Usar una cola persistente de correos:** Se descarta porque requiere administrar mensajes pendientes, reintentos y estados de entrega que no forman parte del alcance actual.
- **Ignorar el fallo del proveedor:** Se descarta porque el usuario no recibiría una respuesta que indique que la operación requerida no pudo completarse.

Costo del concepto: El usuario deberá solicitar nuevamente la operación cuando el proveedor se recupere.

Paso 5: Instanciar elementos arquitectónicos

Se decide refinar el Módulo de juego mediante la creación de un Módulo de estado de juego. El nuevo módulo mantendrá en memoria el estado activo y la asociación temporal de los jugadores. La persistencia conservará una responsabilidad separada y la comunicación con el proveedor de correo quedará aislada mediante un límite de 5 segundos.

- **Módulo de juego, refinado:** Conserva la coordinación de las partidas activas. Solicita el estado vigente, valida la continuidad de la sesión y coordina la reincorporación del jugador.
- **Módulo de estado de juego, nuevo:** Mantiene en memoria el tablero, el turno, los participantes y la asociación temporal necesaria para recuperar una partida durante 30 segundos.
- **Módulo de persistencia, refinado:** Almacena cuentas, resultados y demás información persistente. No decide qué estado debe recuperar un jugador durante una desconexión temporal.
- **Módulo de jugadores, refinado:** Coordina las funciones de cuenta que requieren correo y recibe el resultado controlado del despacho.
- **Módulo despachador de correo, refinado:** Ejecuta la comunicación con un límite de 5 segundos y devuelve éxito o fallo al módulo solicitante.
- **Servicio de correo electrónico:** Sistema externo que puede no responder y no forma parte del Módulo de persistencia.

Paso 6: Bocetar vistas y registrar decisiones de diseño

| Elemento | Nodo | Qué hace | Qué no hace | De quién depende |
| --- | --- | --- | --- | --- |
| Módulo de servicios | Servidor | Recibe las solicitudes remotas y las delega al módulo correspondiente. | No conserva el estado de las partidas ni accede directamente a la base de datos o al proveedor de correo. | Módulo de juego, Módulo de jugadores |
| Módulo de juego | Servidor | Coordina las partidas activas y reincorpora al jugador cuando su sesión continúa vigente. | No obtiene del cliente el estado oficial ni envía correos. | Módulo de estado de juego |
| Módulo de estado de juego | Servidor | Conserva el estado vigente y la asociación temporal de los participantes durante el periodo definido. | No aplica reglas, no envía correos y no ejecuta operaciones de persistencia. | Ninguno |
| Módulo de jugadores | Servidor | Coordina las funciones de cuenta y solicita los correos requeridos. | No se comunica directamente con el proveedor externo. | Módulo de persistencia, Módulo despachador de correo |
| Módulo despachador de correo | Servidor | Limita a 5 segundos la comunicación con el proveedor y devuelve un resultado controlado. | No forma parte de la persistencia ni modifica partidas activas. | Servicio de correo electrónico |
| Módulo de persistencia | Servidor | Agrupa el acceso a los datos que deben almacenarse. | No determina el estado vigente de una partida ni administra el tiempo de reconexión. | Base de datos |
| Módulo de comunicación | Cliente | Restablece la comunicación y solicita recuperar la sesión del jugador. | No reconstruye ni decide el estado oficial de la partida. | Módulo de servicios |

Flujo de reconexión temporal:

1. Se interrumpe la comunicación del jugador durante una partida activa.
2. El servidor conserva durante 30 segundos la sesión, la asociación con la partida y el último estado válido.
3. El cliente restablece la comunicación y solicita recuperar su sesión.
4. El servidor valida que la sesión continúa dentro del periodo permitido.
5. El Módulo de juego reincorpora al jugador y entrega el estado vigente sin volver a aplicar movimientos confirmados.

Flujo de error del servicio de correo:

![CU-01 Crear cuenta-ErrorEmail.png](Tercera%20iteraci%C3%B3n/CU-01_Crear_cuenta-ErrorEmail.png)

La ruta de excepción o timeout mostrada en la vista debe terminar en un máximo de 5 segundos. El Módulo despachador de correo se mantiene separado del Módulo de persistencia.

Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **ASR-01:** Autoridad del servidor sobre el estado legal de la partida |  |  | X | El estado vigente se conserva en el servidor y no se reconstruye a partir del estado enviado por el cliente. La validación completa de movimientos corresponde a las decisiones de las iteraciones anteriores. |
| **ASR-02:** Recuperación durante 30 segundos |  |  | X | El servidor conserva temporalmente la sesión, la asociación con la partida y el último estado válido para reincorporar al jugador sin duplicar movimientos. |
| **ASR-03:** Procesamiento y propagación en menos de 1 segundo |  | X |  | Mantener el estado activo en memoria evita una lectura obligatoria de la base de datos, pero esta iteración no analiza la latencia ni la carga de 8 jugadores. |
| **ASR-04:** Aislamiento del correo y timeout de 5 segundos |  |  | X | El Módulo despachador de correo aísla al proveedor externo, limita la operación a 5 segundos y devuelve un fallo controlado sin afectar partidas activas. |
| **ASR-05:** Cliente WPF y servidor WCF separados |  |  | X | El cliente accede mediante la frontera remota y no consulta directamente el estado ni la persistencia del servidor. |
| **ASR-06:** Prevención de jugadas inválidas en el cliente | X |  |  | Esta iteración no diseña la validación ligera ni la retroalimentación del cliente. |
| **ASR-07:** Suite aislada de reglas en menos de 5 segundos |  | X |  | El estado y la persistencia permanecen separados del Módulo motor de juego, pero todavía no se define la estructura completa de pruebas ni se comprueba el tiempo de la suite. |
| **QAS-Rendimiento-2:** Gestión de inactividad durante un turno | X |  |  | La automatización por inactividad se abordará en una iteración posterior. |
| **QAS-Usabilidad-2:** Partida contra el sistema disponible desde el inicio | X |  |  | El acceso a la partida contra el sistema no forma parte de esta iteración. |
| **QAS-Modificabilidad-1:** Incorporación de un nuevo idioma | X |  |  | La localización del cliente no forma parte de esta iteración. |
| **QAS-Desplegabilidad-1:** Empaquetado con un comando | X |  |  | Se conserva como escenario planeado para la cuarta iteración y no forma parte de los ASR prioritarios. |
