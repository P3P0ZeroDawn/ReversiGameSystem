# Séptima iteración

Objetivo: Refinar la estructura interna del cliente para lograr modificabilidad y facilidad de prueba, conservando la separación entre presentación, negocio del cliente y comunicación remota.

### Paso 2: Drivers para establecer el objetivo de la iteración

- **STK-02 (Equipo de desarrollo):** Separar el comportamiento de la interfaz de los controles WPF para poder modificar y probar la interacción con dependencias controladas.
- **ASR-05:** Mantener la comunicación entre el cliente WPF y el servidor WCF mediante contratos remotos, sin acceso directo del cliente a la lógica o persistencia del servidor.
- **QAS-Usabilidad-1 (ASR-06):** STK-01 (Jugador durante su turno) debe ver las posiciones válidas en un máximo de 1 segundo. El cliente bloquea el 100 % de las posiciones que identifica como inválidas.
- **TEN-04:** Mantener una estructura que facilite las pruebas y los cambios sin introducir abstracciones sin una responsabilidad concreta.

### Paso 3: Elementos a refinar

Se refinan los módulos de presentación y comunicación del cliente. Se revisa cómo se coordina la interacción con la capa de negocio del cliente y cómo se utiliza el Módulo de comunicación de la capa de datos. Se conserva la autoridad del servidor sobre las reglas, el turno y el estado vigente.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

- **MVVM:** Permite enlazar las vistas WPF al estado y a las acciones que expone cada ViewModel. Ayuda a probar el comportamiento de presentación sin crear los controles de la pantalla. Su costo es mantener los enlaces y la sincronización del estado de presentación. Se consideraron las siguientes alternativas:
	  - **Código de interacción en cada vista:** Es sencillo para una pantalla pequeña. Se descarta como estructura general porque mezclar la coordinación de operaciones con los eventos de los controles dificulta probar esa lógica sin cargar WPF.
	  - **MVP:** Un presentador puede coordinar la interacción a través de una interfaz de vista. Se descarta porque exige definir y mantener esa interfaz y sincronizar la pantalla desde el presentador cuando WPF ya dispone de enlaces de datos.
- **Proxy remoto en el Módulo de comunicación:** Encapsula el acceso a los servicios del servidor detrás de clientes que puede consumir la presentación. Su costo es mantener la correspondencia entre esos clientes y los contratos remotos, así como gestionar los fallos de comunicación en un punto común.
	  - **Gestionar la comunicación desde cada ViewModel:** Evita una interfaz intermedia para cada servicio, pero dispersa la gestión de canales y errores entre pantallas y dificulta sustituir el acceso remoto en las pruebas.
	  - **Invocar directamente los proxies WCF generados desde presentación:** Reutiliza los contratos generados, pero expone a la capa de presentación a los tipos y al ciclo de vida de WCF.

### Paso 5: Decisión y elementos resultantes

Se adopta MVVM para la capa de presentación. Las vistas WPF muestran el estado expuesto por sus ViewModels y les entregan las acciones del usuario. El ViewModel coordina la interacción, llama a las validaciones locales que corresponden y solicita operaciones remotas mediante el Módulo de comunicación. El código de la vista atiende comportamientos propios de los controles.

El Módulo de comunicación aplica Proxy remoto. Gestiona las llamadas y las notificaciones del servidor y devuelve a presentación resultados o fallos que esta puede mostrar. Las cargas útiles de WCF y la gestión del canal quedan en comunicación.

En una partida, el ViewModel consulta la validación local sobre el tablero recibido para indicar posiciones válidas. Al seleccionar una posición admitida, solicita la jugada al servidor. El cliente actualiza lo que muestra a partir del estado confirmado por el servidor.

El Módulo de presentación se subdivide por flujo de usuario. Cada submódulo agrupa las vistas y la lógica de presentación de su área. Diálogos, localización y estado de sesión se agrupan como apoyo compartido dentro de la misma capa.

El Módulo de comunicación se subdivide según los clientes remotos existentes. El Cliente de partidas reúne el acceso remoto a la sala de partida, la partida en curso y el chat de partida. Cada responsabilidad conserva su propio contrato con el servidor. Se agrupan porque forman parte del mismo flujo de juego y pueden compartirse entre las pantallas que participan en él.

**Costo aceptado.** El Cliente de partidas acumula operaciones y dependencias de las tres áreas. Un cambio en una de ellas puede requerir revisar las demás y ampliar las pruebas del submódulo.

**Posible refactorización.** Si las áreas empiezan a cambiar de forma independiente o se vuelve difícil probarlas por separado, el Cliente de partidas puede dividirse en un Cliente de sala de partida, un Cliente de partida en curso y un Cliente de chat de partida. Cada uno consumiría su contrato remoto actual. La división se haría dentro del Módulo de comunicación.

El Módulo validador de interfaz conserva sus responsabilidades. Para el diagrama se distinguen las comprobaciones de juego y las políticas de entrada dentro del mismo módulo.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

![Reversi Módulos](Séptima%20iteración/Reversi%20Módulos.png)

| Capa | Módulo | Responsabilidad | Dependencias |
| --- | --- | --- | --- |
| Presentación | Módulo de presentación | Organiza las vistas y los ViewModels por flujo de usuario. | Módulo validador de interfaz y Módulo de comunicación. |
| Negocio | Módulo validador de interfaz | Agrupa las comprobaciones locales de juego y las políticas de entrada. | Ninguna hacia otras capas del cliente. |
| Datos | Módulo de comunicación | Encapsula las llamadas y notificaciones remotas. | Contratos del Módulo de servicios del servidor. |

#### Subdivisión del Módulo de presentación

| Submódulo              | Qué hace                                                                                                        | De quién depende                                                                                                                  |
| ---------------------- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Acceso y registro      | Presenta el inicio de sesión, el acceso como invitado y la creación de cuentas.                                 | Cliente de sesiones, Cliente de cuentas, políticas de entrada y apoyo de presentación.                                            |
| Menú principal         | Muestra la clasificación y la lista de amigos. Permite acceder a partidas y cerrar sesión.                      | Cliente de clasificación, Cliente de amistades, Cliente de sesiones y apoyo de presentación.                                      |
| Perfil y configuración | Presenta el perfil y las opciones de interfaz. Desde el perfil también se pueden enviar solicitudes de amistad. | Cliente de perfil, Cliente de cuentas y apoyo de presentación.                                                                    |
| Amistades              | Presenta la búsqueda de jugadores y las solicitudes de amistad.                                                 | Cliente de amistades y apoyo de presentación.                                                                                     |
| Sala de partida        | Presenta la creación, la búsqueda y la espera de partidas.                                                      | Cliente de partidas, comprobaciones de juego y apoyo de presentación.                                                             |
| Partida en curso       | Muestra el tablero, recibe las acciones de juego y presenta el chat de la partida.                              | Cliente de partidas, comprobaciones de juego y apoyo de presentación.                                                             |
| Historial de partidas  | Presenta el historial y permite abrir la información de una partida.                                            | Cliente de partidas                                                                                                               |
| Apoyo de presentación  | Proporciona diálogos, localización, estado local de sesión y acceso a los clientes remotos compartidos.         | Cliente de sesiones, Cliente de cuentas, Cliente de perfil, Cliente de clasificación, Cliente de amistades y Cliente de partidas. |

Los nombres de los clientes en la columna de dependencias corresponden a los submódulos del Módulo de comunicación que se enumeran abajo.

#### Subdivisión del Módulo de comunicación

| Submódulo | Qué hace | De quién depende |
| --- | --- | --- |
| Cliente de sesiones | Solicita el inicio y cierre de sesión y el acceso como invitado. | Contrato remoto de sesiones. |
| Cliente de cuentas | Envía operaciones de registro y administración de la cuenta. | Contrato remoto de cuentas. |
| Cliente de perfil | Consulta y actualiza el perfil y su imagen. | Contrato remoto de perfil. |
| Cliente de clasificación | Consulta la clasificación de jugadores. | Contrato remoto de clasificación. |
| Cliente de amistades | Envía operaciones de amistad y recibe sus notificaciones. | Contrato remoto de amistades. |
| Cliente de partidas | Agrupa las llamadas remotas de sala, partida en curso y chat. | Contrato remoto de sala de partida, Contrato remoto de partida en curso y Contrato remoto de chat de partida. |

#### Contratos remotos consumidos por el cliente

| Contrato | Provisto por el servidor | Consumido por el cliente | Propósito |
| --- | --- | --- | --- |
| Contrato remoto de sesiones | Servicio de sesiones | Cliente de sesiones | Solicitar y completar el inicio de sesión, abrir una sesión de invitado y cerrar sesión. |
| Contrato remoto de cuentas | Servicio de registro de cuentas | Cliente de cuentas | Registrar cuentas, gestionar cambios y restablecimientos de contraseña y desactivar cuentas. |
| Contrato remoto de perfil | Servicio de perfil de jugador | Cliente de perfil | Consultar el perfil y actualizar el nombre de usuario o la imagen. |
| Contrato remoto de clasificación | Servicio de clasificación | Cliente de clasificación | Consultar la tabla de posiciones. |
| Contrato remoto de amistades | Servicio de amistades | Cliente de amistades | Buscar jugadores, gestionar amistades y recibir notificaciones. |
| Contrato remoto de sala de partida | Servicio de sala de partida | Cliente de partidas | Crear, listar, configurar y unirse a salas, y solicitar el inicio de una partida contra el sistema. |
| Contrato remoto de partida en curso | Servicio de partida en curso | Cliente de partidas | Enviar jugadas, consultar y recibir el estado de la partida, y solicitar el abandono. |
| Contrato remoto de chat de partida | Servicio de chat de partida | Cliente de partidas | Enviar y recibir mensajes de la partida. |

### Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **STK-02** | | | **X** | La lógica de presentación se concentra en ViewModels y el acceso remoto se consume por una frontera sustituible. |
| **ASR-05** | | | **X** | El Módulo de comunicación mantiene la frontera remota. El cliente accede al servidor mediante sus contratos de servicio. |
| **QAS-Usabilidad-1 (ASR-06)** | | **X** | | La validación local tiene una ruta definida hasta la interfaz. La iteración de usabilidad detallará el comportamiento visual y el criterio de un segundo. |
| **TEN-04** | | | **X** | MVVM se aplica dentro de presentación y reutiliza las fronteras de validación y comunicación del cliente. |
