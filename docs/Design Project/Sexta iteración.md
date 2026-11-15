# Sexta iteración

Objetivo: Refinar la estructura interna de la aplicación de servicio del servidor para lograr facilidad de prueba aislada y facilidad de modificación (`QAS-FacilidadPrueba-1`, `ASR-07`), manteniendo una separación de capas y fronteras comprensible para el trabajo paralelo del equipo (`ORG-01`, `ORG-02`, `STK-02`, `TEN-04`).

### Paso 2: Drivers para establecer el objetivo de la iteración

- **QAS-FacilidadPrueba-1 (ASR-07):** Ejecutar el 100 % de la suite automatizada de reglas en menos de 5 segundos, controlando los estados de prueba y sin iniciar WPF, WCF, SQL Server, Entity Framework ni el proveedor externo de correo.
- **STK-02 (Equipo de desarrollo):** Contar con módulos desacoplados que puedan probarse de forma independiente y desarrollarse en paralelo sin colisiones.
- **ORG-01 / ORG-02 (Equipo y calendario):** Equipo de dos integrantes trabajando dentro del calendario escolar; requiere una estructura clara sin sobrecarga ceremonial.
- **ORG-03 (Roles integrales):** El mismo equipo asume diseño, desarrollo, pruebas y despliegue sin áreas externas de soporte.
- **TEN-04 (Modifiability/Testability vs. simplicidad):** Mejorar la facilidad de prueba y cambio sin introducir abstracciones excesivas que frenen al equipo.
- **QAS-Disponibilidad-2 (ASR-04):** Aislar la comunicación con el proveedor externo de correo para que su falla termine en un máximo de 5 segundos sin afectar partidas activas.
- **RES-TEC-01 / RES-TEC-04 (Entity Framework y SQL Server):** Aislar el acceso relacional para que los detalles del ORM y del motor de base de datos no invadan la lógica de negocio.
- **RES-TEC-02 (WCF):** Aislar los contratos de comunicación remota respecto a la lógica de negocio del servidor.

### Paso 3: Elementos a refinar

1. **Capas del sistema:** Explicitar las capas del cliente y del servidor, junto con los módulos que pertenecen a cada una. En el servidor se delimita la relación entre las capas de arranque, servicios, negocio, persistencia y transversal. El `Módulo despachador de correo` permanece en la capa de servicios y separado de la capa de persistencia.
2. **Fronteras de dependencia y punto de ensamblado:** Definir dónde se invierten las dependencias y cómo se conectan los módulos en tiempo de arranque.
3. **Subdivisión del `Módulo de servicios`:** Descomponer la frontera de entrada del servidor por cohesión funcional para reducir acoplamiento y facilitar modificaciones y pruebas aisladas.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

- **Inversión de dependencias selectiva en fronteras de salida (`QAS-FacilidadPrueba-1`, `ASR-07`, `STK-02`, `ORG-01`, `TEN-04`):**
  - La capa de negocio define contratos abstractos para las operaciones de almacenamiento y envío de correo, mientras que la capa de persistencia y el `Módulo despachador de correo` implementan dichos contratos. Esta inversión se aplica únicamente en las fronteras que conectan con recursos externos lentos o no deterministas (base de datos y correo), permitiendo sustituirlos por dobles de prueba en memoria para ejecutar la suite de reglas en menos de 5 segundos sin levantar ORM, base de datos ni red externa (`ASR-07`), y habilitando el trabajo paralelo entre capas (`STK-02`, `ORG-01`).
	  - **Costo aceptado:** La capa de servicio permanece acoplada a la firma de los módulos de negocio al no existir contratos de entrada simétricos, y el equipo debe mantener sincronizados los contratos de salida con sus implementaciones.
  - **Alternativas consideradas y descartadas:**
    - *Arquitectura Hexagonal completa (puertos y adaptadores simétricos en todas las fronteras):* Aunque maximiza el aislamiento tanto en entradas como en salidas, obliga a mantener puertos de entrada por cada caso de uso, adaptadores primarios y secundarios y objetos intermedios en cada frontera. Para dos desarrolladores sin áreas de soporte (`ORG-01`, `ORG-03`) y bajo calendario escolar (`ORG-02`), introduce una sobrecarga estructural innecesaria que inclina la tensión `TEN-04` en contra de la productividad.
    - *Arquitectura en capas tradicional con dependencia directa hacia abajo:* Evita crear interfaces entre capas, pero obliga a que cualquier prueba sobre el negocio cargue Entity Framework, SQL Server y la configuración de correo. Esto vuelve las pruebas lentas y dependientes de infraestructura activa, incumpliendo `QAS-FacilidadPrueba-1` (`ASR-07`).

- **Raíz de composición centralizada (`QAS-FacilidadPrueba-1`, `STK-02`, `RES-TEC-01`, `RES-TEC-02`):**
  - El ensamblado de todas las implementaciones concretas se concentra en un único punto de arranque del servidor, el cual construye e inyecta las dependencias hacia los módulos de negocio y la capa de servicio. Con esto se evita que los módulos de negocio referencien implementaciones de persistencia o correo, facilitando intercambiar configuraciones de producción y de prueba (`QAS-FacilidadPrueba-1`).
	  - **Costo aceptado:** El punto de arranque conoce todas las implementaciones concretas del servidor y crece conforme se agregan nuevos módulos.
  - **Alternativas consideradas y descartadas:**
    - *Instanciación directa dentro de cada módulo:* Reduce código de arranque, pero vuelve a acoplar en compilación y ejecución a los módulos de negocio con las implementaciones concretas de datos y correo, anulando el aislamiento para pruebas (`ASR-07`).
    - *Patrón Localizador de Servicios (Service Locator):* Permite resolver dependencias bajo demanda sin pasarlas por construcción, pero oculta los colaboradores reales de cada módulo y traslada errores de ensamblado hasta el tiempo de ejecución, aumentando el riesgo operativo para un equipo pequeño sin QA dedicado (`ORG-03`).

- **Separación de modelos en las fronteras del sistema (`QAS-FacilidadPrueba-1`, `STK-02`, `RES-TEC-01`, `RES-TEC-02`):**
  - La capa de negocio opera exclusivamente con modelos propios de dominio, mientras que las traducciones hacia estructuras de red o hacia entidades relacionales ocurren únicamente en la frontera de servicio y en la capa de persistencia respectivamente. Esto protege al `Módulo motor de juego` y a los casos de uso frente a atributos de serialización remota o restricciones del ORM (`RES-TEC-01`, `RES-TEC-02`), permitiendo instanciar y probar el dominio directamente en memoria (`ASR-07`).
	  - **Costo aceptado:** Requiere mantener estructuras de datos separadas y realizar traducciones explícitas en los bordes del servidor.
  - **Alternativas consideradas y descartadas:**
    - *Modelo único compartido entre red, negocio y persistencia:* Evita duplicar estructuras y traducciones, pero propaga restricciones de Entity Framework y WCF hacia el núcleo de negocio, acoplando los cambios de esquema relacional con los contratos remotos y bloqueando el trabajo paralelo (`STK-02`).

- **Descomposición de la frontera de servicios por cohesión funcional (`STK-02`, `ORG-01`, `TEN-04`):**
  - El `Módulo de servicios` se divide en submódulos especializados por área funcional (cuentas, sesiones, perfil, clasificación, amistades, sala de espera, partida en curso y chat). Cada submódulo expone únicamente las operaciones de su área y depende solo de los módulos de negocio que necesita, disminuyendo el acoplamiento, facilitando modificaciones localizadas sin colisiones entre desarrolladores (`ORG-01`) y permitiendo probar cada frontera de servicio sin inicializar las demás (`STK-02`).
	  - **Costo aceptado:** Incrementa el número de puntos de entrada remotos que el cliente y la raíz de composición deben configurar y coordinar.
  - **Alternativas consideradas y descartadas:**
    - *Fachada única monolítica de servicios:* Concentra todas las operaciones remotas en un solo módulo. Se descarta porque acumula dependencias hacia todos los módulos de negocio, mezcla operaciones simples con canales persistentes de tiempo real, genera conflictos frecuentes al editar en paralelo (`ORG-01`) y obliga a inicializar todo el servidor para probar una sola operación.

### Paso 5: Decisión y elementos resultantes

Se adopta una **arquitectura en capas con raíz de composición centralizada, inversión de dependencias selectiva en salidas, separación de modelos en fronteras y descomposición funcional de la capa de servicio**.

La división en capas del sistema queda definida de la siguiente manera:

| Ámbito | Capa | Responsabilidad | Módulos principales |
| --- | --- | --- | --- |
| Cliente | **Capa de presentación** | Presentar la interfaz, mostrar el estado recibido y capturar las acciones del jugador. | `Módulo de presentación` |
| Cliente | **Capa de negocio** | Proporcionar la validación ligera necesaria para la retroalimentación inmediata de la interfaz. | `Módulo validador de interfaz` |
| Cliente | **Capa de datos** | Encapsular la comunicación del cliente con los servicios remotos del servidor. | `Módulo de comunicación` |
| Servidor | **Capa de arranque** | Construir el servidor y conectar las implementaciones concretas con los contratos que consumen los módulos. | `Raíz de composición del servidor` |
| Servidor | **Capa de servicios** | Recibir las solicitudes remotas, traducir los datos de frontera y dirigir cada operación hacia el módulo de negocio correspondiente. También contiene la integración con el proveedor externo de correo. | `Módulo de servicios`, sus submódulos funcionales y `Módulo despachador de correo` |
| Servidor | **Capa de negocio** | Ejecutar los casos de uso, conservar la autoridad sobre las reglas y administrar el estado activo mediante modelos propios del negocio. También define los contratos de salida que necesita. | `Módulo de juego`, `Módulo motor de juego`, `Módulo de estado de juego`, `Módulo de jugadores`, `Módulo de amistades` y `Módulo de chat` |
| Servidor | **Capa de persistencia** | Implementar los contratos de almacenamiento y traducir los modelos del negocio hacia las estructuras relacionales. | `Módulo de persistencia` y módulos de datos por área funcional |
| Servidor | **Capa transversal** | Registrar los eventos y fallos producidos por los servicios del servidor. | `Módulo de registro de eventos` |

La base de datos relacional se ejecuta en un proceso independiente y es utilizada por la capa de persistencia. El proveedor de correo es una dependencia externa utilizada por el `Módulo despachador de correo`. Ninguno de los dos se considera una capa adicional del sistema.

Elementos nuevos o refinados resultantes:
- **Raíz de composición del servidor (nuevo):** Punto único de arranque de la aplicación de servicio encargado de construir las implementaciones de persistencia, correo, negocio y servicio, e inyectar sus dependencias.
- **Contratos de frontera de salida (nuevos, definidos en la capa de negocio):**
  - Contrato de persistencia de partidas (consumido por `Módulo de juego`, implementado por `PartidaDatos`).
  - Contrato de persistencia de jugadores (consumido por `Módulo de jugadores`, implementado por `JugadoresDatos`).
  - Contrato de persistencia de amistades (consumido por `Módulo de amistades`, implementado por `AmistadesDatos`).
  - Contrato de persistencia de chat (consumido por `Módulo de chat`, implementado por `ChatDatos`).
  - Contrato de envío de correo (consumido por `Módulo de jugadores` y `Módulo de juego`, implementado por `Módulo despachador de correo` de forma separada a la capa de persistencia).
- **Submódulos del `Módulo de servicios` (refinamiento de la capa de servicio):**
  - `Servicio de registro de cuentas`: Expone el alta de cuentas de jugador.
  - `Servicio de sesiones`: Expone autenticación, solicitud de código de acceso, sesiones de invitado y cierre de sesión.
  - `Servicio de perfil de jugador`: Expone consulta y actualización de perfil e imagen del jugador.
  - `Servicio de clasificación`: Expone la consulta de la tabla global de posiciones.
  - `Servicio de amistades`: Expone la gestión de amistades y notificaciones de presencia en línea.
  - `Servicio de sala de partida`: Expone creación, listado, invitación, unión por código, configuración de turno e inicio de partida contra el sistema.
  - `Servicio de partida en curso`: Expone ejecución de jugadas, consulta de estado, observación, abandono e historial de partidas.
  - `Servicio de chat de partida`: Expone mensajería en tiempo real e historial de chat dentro de una partida.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

![[Reversi Módulos.png]]

| Elemento | Capa / Nodo | Qué hace | Qué no hace | De quién depende |
| --- | --- | --- | --- | --- |
| **Módulo de presentación** | Cliente | Muestra el estado del juego, menús y captura la interacción del jugador. | No valida jugadas definitivas ni calcula el nuevo tablero. | Módulo validador de interfaz, Módulo de comunicación |
| **Módulo validador de interfaz** | Cliente | Determina casillas válidas sobre el tablero local para retroalimentación visual inmediata. | No modifica fichas ni calcula el estado oficial de la partida. | Ninguno |
| **Módulo de comunicación** | Cliente | Transmite peticiones y recibe eventos del servidor. | No procesa reglas ni almacena partidas. | Módulo de servicios |
| **Raíz de composición del servidor** | Servidor (Capa de arranque) | Construye las implementaciones concretas y las inyecta en los módulos de negocio y de servicio. | No contiene lógica de juego ni atiende peticiones remotas. | Módulo de servicios, módulos de negocio, Módulo de persistencia, Módulo despachador de correo, Módulo de registro de eventos |
| **Módulo de servicios** | Servidor (Capa de servicios) | Agrupa los submódulos de entrada remota del servidor y registra eventos de servicio. | No contiene reglas del juego ni lógica de persistencia directa. | Submódulos de servicio, Módulo de registro de eventos |
| **Servicio de registro de cuentas** | Servidor (Capa de servicios) | Expone el registro de cuentas y traduce datos hacia el negocio. | No valida reglas de partida ni accede a la base de datos. | Módulo de jugadores, Módulo de registro de eventos |
| **Servicio de sesiones** | Servidor (Capa de servicios) | Expone autenticación, sesiones de invitado y cierre de sesión. | No gestiona tableros ni accede directamente a la base de datos. | Módulo de jugadores, Módulo de registro de eventos |
| **Servicio de perfil de jugador** | Servidor (Capa de servicios) | Expone consulta y actualización de perfil e imagen. | No gestiona salas ni ejecuta consultas SQL directas. | Módulo de jugadores, Módulo de registro de eventos |
| **Servicio de clasificación** | Servidor (Capa de servicios) | Expone la consulta de la tabla global de posiciones. | No modifica estadísticas ni accede directamente a la base de datos. | Módulo de jugadores, Módulo de registro de eventos |
| **Servicio de amistades** | Servidor (Capa de servicios) | Expone la gestión de amistades y notificaciones de presencia en línea. | No gestiona partidas ni accede directamente a la base de datos. | Módulo de amistades, Módulo de registro de eventos |
| **Servicio de sala de partida** | Servidor (Capa de servicios) | Expone creación, listado, invitación, unión y configuración previa de salas. | No ejecuta jugadas del tablero ni accede directamente a la base de datos. | Módulo de juego, Módulo de registro de eventos |
| **Servicio de partida en curso** | Servidor (Capa de servicios) | Expone jugadas, estado en curso, observación, abandono e historial de partidas. | No calcula flanqueo por sí mismo ni accede directamente a la base de datos. | Módulo de juego, Módulo de registro de eventos |
| **Servicio de chat de partida** | Servidor (Capa de servicios) | Expone mensajería en tiempo real e historial de chat de partida. | No controla turnos ni accede directamente a la base de datos. | Módulo de chat, Módulo de registro de eventos |
| **Módulo de juego** | Servidor (Capa de negocio) | Gestiona partidas en curso, turnos, inactividad y jugadores automatizados usando modelos de negocio. | No define si una jugada es válida ni conoce el motor de base de datos o transporte remoto. | Módulo motor de juego, Módulo de estado de juego, Contrato de persistencia de partidas, Contrato de envío de correo |
| **Módulo motor de juego** | Servidor (Capa de negocio) | Valida con autoridad si una jugada es legal, resuelve el flanqueo de fichas y las condiciones de victoria. | No maneja conexiones de red, estado en memoria ni persistencia. | Ninguno |
| **Módulo de estado de juego** | Servidor (Capa de negocio) | Mantiene en memoria el estado activo de las partidas en curso. | No ejecuta persistencia en disco ni aplica reglas. | Ninguno |
| **Módulo de chat** | Servidor (Capa de negocio) | Valida que el mensaje no incumpla las normas de conducta y lo distribuye entre participantes. | No almacena los mensajes directamente en la base de datos ni gestiona la partida. | Módulo de juego, Contrato de persistencia de chat |
| **Módulo de jugadores** | Servidor (Capa de negocio) | Conoce a los jugadores, realiza la autenticación y diferencia entre jugador con cuenta e invitado. | No gestiona las partidas de los jugadores ni ejecuta consultas SQL o envíos SMTP directos. | Contrato de persistencia de jugadores, Contrato de envío de correo |
| **Módulo de amistades** | Servidor (Capa de negocio) | Gestiona las amistades activas y las solicitudes de amistad. | No valida credenciales ni mensajes de las partidas. | Módulo de jugadores, Contrato de persistencia de amistades |
| **Módulo despachador de correo** | Servidor (Capa de servicios) | Implementa el contrato de envío de correo y despacha mensajes hacia el proveedor externo con límite de 5 segundos. | No forma parte de la capa de persistencia ni decide reglas de negocio. | Contrato de envío de correo (implementa), Servicio externo de correo |
| **Módulo de persistencia** | Servidor (Capa de persistencia) | Agrupa el almacenamiento y consulta relacional por área funcional. | No valida partidas en curso ni contiene reglas de negocio. | Base de datos relacional |
| **Módulo de datos de partidas (`PartidaDatos`)** | Servidor (Capa de persistencia) | Implementa el contrato de persistencia de partidas y traduce hacia la base de datos relacional. | No gestiona operaciones de otras áreas. | Contrato de persistencia de partidas (implementa), Base de datos relacional |
| **Módulo de datos de chat (`ChatDatos`)** | Servidor (Capa de persistencia) | Implementa el contrato de persistencia de chat y traduce hacia la base de datos relacional. | No gestiona operaciones de otras áreas. | Contrato de persistencia de chat (implementa), Base de datos relacional |
| **Módulo de datos de amistades (`AmistadesDatos`)** | Servidor (Capa de persistencia) | Implementa el contrato de persistencia de amistades y traduce hacia la base de datos relacional. | No gestiona operaciones de otras áreas. | Contrato de persistencia de amistades (implementa), Base de datos relacional |
| **Módulo de datos de jugadores (`JugadoresDatos`)** | Servidor (Capa de persistencia) | Implementa el contrato de persistencia de jugadores y traduce hacia la base de datos relacional. | No gestiona operaciones de otras áreas. | Contrato de persistencia de jugadores (implementa), Base de datos relacional |
| **Módulo de registro de eventos** | Servidor (Capa transversal) | Registra eventos y fallos del servidor en el disco local. | No intercepta excepciones automáticamente ni contiene lógica de negocio. | Almacenamiento local del servidor |
| **Base de datos relacional** | Dispositivo del servidor | Almacena los datos relacionales en un proceso separado de la aplicación de servicio. | No ejecuta lógica de aplicación ni de partidas. | Ninguno |

#### Contratos de frontera (negocio hacia salidas)

| Contrato                                  | Definido por    | Consumido por                            | Implementado por               | Propósito                                                                                                |
| ----------------------------------------- | --------------- | ---------------------------------------- | ------------------------------ | -------------------------------------------------------------------------------------------------------- |
| **Contrato de persistencia de partidas**  | Capa de negocio | `Módulo de juego`                        | `PartidaDatos`                 | Desacoplar el almacenamiento y consulta de partidas, participantes y resultados del núcleo de negocio.   |
| **Contrato de persistencia de jugadores** | Capa de negocio | `Módulo de jugadores`                    | `JugadoresDatos`               | Desacoplar la gestión de cuentas, sesiones, perfiles y códigos de verificación respecto al ORM.          |
| **Contrato de persistencia de amistades** | Capa de negocio | `Módulo de amistades`                    | `AmistadesDatos`               | Desacoplar la administración de solicitudes y relaciones sociales manteniendo el aislamiento por área.   |
| **Contrato de persistencia de chat**      | Capa de negocio | `Módulo de chat`                         | `ChatDatos`                    | Desacoplar el registro histórico de mensajes sin afectar el flujo de juego.                              |
| **Contrato de envío de correo**           | Capa de negocio | `Módulo de jugadores`, `Módulo de juego` | `Módulo despachador de correo` | Aislar el despacho de notificaciones temporales y su límite de 5 segundos respecto al proveedor externo. |

### Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **QAS-FacilidadPrueba-1 (ASR-07)** | | | **X** | La inversión de dependencias en salidas, la raíz de composición y los modelos propios de dominio permiten ejecutar la suite de reglas en menos de 5 segundos sin infraestructura externa. |
| **STK-02 (Equipo de desarrollo)** | | | **X** | La separación de fronteras de salida y la subdivisión funcional del `Módulo de servicios` permiten desarrollar y probar cada área de forma independiente. |
| **ORG-01 / ORG-02 (Equipo y calendario)** | | | **X** | Se evita la sobrecarga de Arquitectura Hexagonal completa y se habilita trabajo paralelo sin colisiones entre ambos integrantes. |
| **ORG-03 (Roles integrales)** | | | **X** | La estructura en capas con raíz de composición centralizada reduce la complejidad operativa para un equipo sin áreas externas de soporte. |
| **TEN-04 (Modifiability/Testability vs. simplicidad)** | | | **X** | Se aplica inversión de dependencias solo donde aporta aislamiento real para pruebas, evitando puertos de entrada simétricos innecesarios. |
| **QAS-Disponibilidad-2 (ASR-04)** | | | **X** | `Módulo despachador de correo` queda separado de la capa de persistencia e implementa su propio contrato de salida con límite de 5 segundos. |
| **RES-TEC-01 / RES-TEC-04 (EF y SQL Server)** | | | **X** | La capa de persistencia queda aislada detrás de los contratos de salida definidos por el negocio, sin contaminar el dominio. |
| **RES-TEC-02 (WCF)** | | | **X** | Los submódulos del `Módulo de servicios` encapsulan los contratos remotos y las traducciones de red en la frontera de entrada. |
