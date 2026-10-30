# Catálogo de contratos para la integración WCF

## 1. Propósito

Este documento define la frontera de comunicación entre el cliente de escritorio y el
servidor de Reversi. Especifica los servicios, operaciones, parámetros, valores devueltos,
callbacks y faults que forman o formarán la API WCF del sistema.

La interacción utiliza llamadas a procedimientos remotos (RPC) mediante WCF. Una operación
expresa una acción del servicio con sus parámetros, retorno y faults; no modela una ruta web.

El diseño se evalúa por operación: se utilizan parámetros directos para entradas sencillas
y objetos cuando agrupan datos relacionados y hacen más comprensible el contrato. Los
valores devueltos representan la información que necesita el cliente, sin imponer un objeto
de entrada ni un contenedor de resultado para todos los métodos.

El servidor es el propietario del código fuente de los contratos. El cliente no referencia
el ensamblado `ReversiGameSystem.Server.Contracts`; consume una referencia de servicio
generada a partir de los metadatos publicados por WCF. Por ello, los tipos generados en el
cliente son representaciones del contrato remoto y no un proyecto compartido.

El catálogo distingue el diseño objetivo del estado de implementación e integración. Una
firma propuesta no demuestra que existan su implementación, endpoint publicado, proxy
generado o consumo desde el cliente. Las referencias al código y al proxy actuales se
identifican como tales mientras se completa la migración.

## 2. Alcance y principios

- El servidor concentra las reglas de negocio, la administración de sesiones, la persistencia
  y el estado autoritativo de las partidas.
- El cliente conserva la presentación, la navegación, la localización, la captura de entradas
  y una validación preventiva mínima antes de invocar al servidor.
- El cliente puede calcular posiciones legales a partir del tablero recibido para orientar
  la interacción. Ese cálculo no acepta jugadas ni sustituye la validación autoritativa.
- Ninguna entidad generada por Entity Framework puede aparecer en una firma WCF.
- Los objetos de comunicación usan nombres relacionados con el dominio. No se agregan
  `Request`, `Response` o `Dto` solo para señalar transporte o envolver cada operación.
  Una palabra con significado de negocio es válida: `FriendRequest` es una solicitud
  de amistad, no un contenedor genérico de llamada remota.
- Las entradas sencillas se expresan mediante parámetros directos. Se utiliza un objeto de
  entrada cuando agrupa varios datos relacionados y mejora la claridad de la operación;
  no se exige un objeto por método ni se descomponen todos los objetos en parámetros.
- Las firmas objetivo tienen como máximo tres parámetros, incluidos tokens y objetos.
  Si se necesitan más datos, se agrupan los relacionados o se revisa la responsabilidad;
  no se crea una bolsa de parámetros sin significado para cumplir el límite.
- Cada operación devuelve el valor, objeto o colección que necesita el cliente, o `void`
  cuando basta con confirmar su finalización correcta. No se impone un contenedor de
  resultado ni un estado `Success` para todas las operaciones.
- Los rechazos que el cliente necesita distinguir se comunican mediante faults tipados
  declarados en la operación. Puede conservarse un resultado explícito cuando sus distintos
  desenlaces formen parte del funcionamiento normal y resulten más claros que un fault.
- Las colecciones vacías, la ausencia documentada de un dato opcional y los estados normales
  del dominio no se convierten automáticamente en excepciones.
- Las excepciones técnicas inesperadas se registran en el servidor y se convierten en un
  `ServiceFault` controlado. No se transmiten las excepciones internas ni sus detalles de
  implementación. El cliente selecciona los mensajes localizados según el resultado o fault.
- Los callbacks se reservan para sucesos originados por el servidor o por otro cliente.
- El servidor vuelve a validar toda entrada aunque el cliente ya la haya validado.
- Las direcciones, bindings y límites de WCF se configuran en los archivos `App.config` de
  los ejecutables y no se codifican directamente en C#.
- La capa de entrada del servidor se denomina `ReversiGameSystem.Server.ServiceApi`. El
  nombre expresa su responsabilidad y no la tecnología de transporte; WCF es la
  implementación actual y permanece como detalle del adaptador y del host.
- La capa de presentación del cliente se denomina
  `ReversiGameSystem.Client.Presentation`. WPF es su implementación actual y no forma parte
  del nombre de la capa.
- Las firmas de los contratos WCF no devuelven `Task` ni `Task<T>` y sus métodos no usan el
  sufijo `Async`. Expresan los parámetros y el resultado de la operación sin fijar el modelo
  de ejecución del cliente o del servidor.
- El cliente, el servidor y los repositorios pueden utilizar asincronía cuando existe espera
  real de red, base de datos u otro recurso con una API asíncrona. El proxy puede ofrecer
  llamadas asíncronas aunque el contrato se describa con firmas síncronas; esto no convierte
  automáticamente la implementación del servidor en asíncrona.
- Las operaciones en memoria y las APIs de E/S implementadas de forma síncrona no fabrican
  tareas con `Task.FromResult` o `Task.CompletedTask`.

## 3. Tipos de contrato

### 3.1 Service contract

Una interfaz marcada con `[ServiceContract]` define operaciones que el cliente puede invocar
en el servidor. Cada método remoto se marca con `[OperationContract]`.

Cada operación define sus entradas según los datos que necesita. Las entradas sencillas se
expresan como parámetros directos; un objeto de entrada se justifica cuando agrupa varios
datos relacionados y evita una firma extensa o difícil de comprender. Se admiten como máximo
tres parámetros en total, según la sección 5.8 del estándar, no tres por cada tipo de dato.
Con cuatro o más datos se revisa su cohesión antes de agruparlos; un objeto existente útil
no se descompone solo porque algunos de sus miembros sean primitivos.

Cada operación se describe mediante su propósito, entradas, retorno normal, autorización
y rechazos aplicables. Las tablas y apartados del servicio reúnen esos elementos sin repetir
una clase de solicitud o respuesta por método. Los nombres de parámetros expresan su papel;
se conservan distinciones útiles como `sessionToken`, `matchCode` y `expectedMatchStateVersion`.

Por ejemplo, `RequestRegistrationCode(string username, string emailAddress)` recibe dos
valores directos, mientras que `CompleteRegistration(NewAccount account)` agrupa los datos
necesarios para completar el registro. No se crea una clase de credenciales únicamente para
transportar el usuario y la contraseña.

La operación devuelve directamente un valor, un objeto, una colección o `void`, según lo
que necesite conocer el cliente. Un resultado compuesto se conserva cuando aporta datos o
desenlaces útiles, no solo para envolver otro objeto con un estado `Success`.

Un retorno `bool` se utiliza cuando ambos valores representan desenlaces normales distintos
que el cliente necesita conocer. No sustituye `void` por un `true` obligatorio ni oculta
un rechazo o una falla técnica en `false`. Los callbacks one-way siguen devolviendo `void`.

Las firmas contractuales no devuelven `Task` ni `Task<T>` y no usan el sufijo `Async`. Esta
decisión no elimina la asincronía de la implementación: en persistencia pueden utilizarse
`async`, `await` y `Task<T>` para consultas y escrituras asíncronas reales de Entity Framework.
Las capas que esperan esas operaciones pueden propagar su asincronía internamente; el
contrato WCF no adopta por ello las firmas del repositorio. La adaptación del servicio debe
resolver cómo ejecutar esas operaciones sin asumir que WCF vuelve asíncrono un método por
el solo hecho de publicarlo.

### 3.2 Callback contract

Una interfaz callback permite que el servidor notifique al cliente sin esperar una nueva
consulta. Los callbacks de este catálogo no devuelven valores y usan
`[OperationContract(IsOneWay = true)]`.

Los datos de cada callback se eligen con el mismo criterio que los de una operación del
servicio. Puede recibir parámetros directos para un suceso sencillo, un objeto existente
para comunicar un estado o un objeto de notificación cuando el suceso requiere varios datos
relacionados. No se exige una clase de notificación por callback.

Por ejemplo, `OnFriendChanged(long accountId, FriendChange change)` comunica dos
valores directos y `OnMatchStateChanged(MatchStateSnapshot state)` entrega el estado del
tablero sin envolverlo en otra clase. En ambos casos el retorno es `void`.

Se mantienen de uno a tres métodos por interfaz callback, buscando una sola notificación
cuando represente el mismo estado o suceso. Un enum puede distinguir variantes relacionadas;
no se añade un método por cada variante ni se mezclan mensajes y sanciones en una bolsa
genérica solo para llegar a un método. Cada familia justifica sus operaciones en su sección.

`IsOneWay = true` significa que no existe una respuesta contractual para la operación. No
garantiza por sí mismo la entrega, la ejecución inmediata ni la ausencia absoluta de bloqueos
de transporte.

Un callback one-way no devuelve un fault contractual a su emisor. Una excepción al procesar
la notificación se maneja en el cliente; una falla de comunicación al enviarla se maneja en
el servidor.

### 3.3 Data contract

Un data contract define información estructurada que cruza la frontera WCF. Las clases se
marcan con `[DataContract]` y sus miembros de comunicación con `[DataMember]`. Las
enumeraciones contractuales se marcan con `[DataContract]` y sus valores serializados con
`[EnumMember]`.

Los parámetros y retornos de tipos simples, como `string`, `int` o `DateTime`, no requieren
una clase propia que los envuelva. Tampoco se crea un contenedor para una colección cuando
no hay información adicional que entregar junto con sus elementos.

Se conservan objetos como `NewAccount`, `MatchConfiguration` o `PiecePlacement` cuando
organizan entradas relacionadas, y objetos como `AuthenticatedSession` o `MatchStateSnapshot`
cuando representan información que el cliente necesita recibir. Cada miembro debe tener un
propósito definido; no se agregan campos para posibles necesidades futuras sin concretar.

Los data contracts no son entidades de persistencia ni objetos de interfaz gráfica. La API
de servicio del servidor se encarga de mapearlos a los modelos de aplicación y dominio.

### 3.4 Fault contract

Un fault contract describe una condición de rechazo o falla que el cliente debe reconocer.
La operación lo declara mediante `[FaultContract(typeof(TFault))]`; el servicio lo comunica
mediante `FaultException<TFault>` y el proxy permite al cliente capturar ese fault tipado.

`TFault` es un data contract con la información necesaria para identificar y manejar la
condición. No es la excepción interna del servidor ni necesita derivar de `Exception`.
Las condiciones relacionadas pueden compartir un tipo de fault con razones diferenciadas
cuando el cliente las maneja de forma semejante; no se exige una clase por mensaje visible.

La agrupación requiere una responsabilidad común, significado estable de cada razón y
aplicabilidad explícita por operación. No se reúnen códigos solo por pertenecer al mismo
servicio. La justificación de los grupos vigentes se recoge en 16.3; no se agregan códigos
`Success` ni estados de transporte a los enums de rechazo.

Los faults de negocio se distinguen de `ServiceFault`, reservado para fallas técnicas
controladas. No incluyen stack traces, consultas SQL ni otros detalles internos. El cliente
elige el mensaje localizado correspondiente. Los resultados normales documentados, como una
colección vacía o la ausencia de una imagen, se mantienen como valores de retorno.

## 4. Espacios de nombres contractuales

Los contratos utilizan los siguientes identificadores de namespace WCF. Son identificadores
del protocolo, no direcciones de conexión ni rutas de proyectos.

| Categoría | Namespace WCF | Uso |
| --- | --- | --- |
| Servicios | `urn:reversi:services:v1` | Service contracts y callback contracts. |
| Datos | `urn:reversi:contracts:v1` | Objetos, enumeraciones y detalles de faults de negocio. |
| Diagnósticos | `urn:reversi:diagnostics:v1` | `ServiceFault` y contexto diagnóstico. |

Los faults de negocio se agrupan con los datos contractuales porque describen rechazos
reconocibles por el cliente. `ServiceFault` mantiene el namespace de diagnósticos ya
publicado. No se crea un namespace diferente para cada operación, objeto o motivo de rechazo.

Los tipos simples usados como parámetros o retornos no requieren un data contract propio
ni un namespace adicional del proyecto.

Los namespaces WCF forman parte de la compatibilidad del protocolo y no deben cambiarse solo
por una reorganización interna de namespaces de C#.

Conservar estos identificadores no garantiza por sí solo la compatibilidad con el proxy
existente. Los cambios de servicios, operaciones, parámetros, retornos y faults se evalúan
también conforme a las reglas de versionamiento de este catálogo antes de publicar los
contratos revisados y regenerar la referencia del cliente.

## 5. Estados de implementación

| Estado | Significado |
| --- | --- |
| `Implemented` | Existe una implementación del caso de uso en el servidor. |
| `Prototype` | Existe comportamiento prototipo o local que debe completarse o migrarse. |
| `Planned` | La operación es necesaria, pero aún no tiene implementación completa. |
| `Deferred` | Se reserva para una etapa posterior a la separación inicial. |

El estado de una operación describe el avance de su comportamiento, no la implementación
automática de la firma objetivo. `Implemented` no implica que ya exista un adaptador WCF,
un endpoint configurado, un proxy generado o una pantalla que lo consuma. Tampoco acredita
por sí solo una prueba de ejecución reciente.

El estado se asigna a la operación completa. Un flujo propuesto con verificación por código
no se considera implementado solo porque ya exista un registro o login sin ese paso. Las
firmas revisadas se distinguen de las que utiliza actualmente el código.

### 5.1 Estado actual de integración

| Flujo | Contrato | Adaptador WCF | Endpoint configurado | Proxy generado | Cliente integrado |
| --- | --- | --- | --- | --- | --- |
| Crear cuenta | Sí | Sí | Sí | Sí | No |
| Iniciar sesión | Sí | Sí | Sí | Sí | Sí |
| Consultar perfil | Sí | No | No | No | No |
| Consultar clasificación | Sí | No | No | No | No |

Esta matriz describe los artefactos y consumidores presentes en el código, anteriores a la
revisión contractual en curso. Un endpoint configurado no demuestra que el host esté en
ejecución ni que la comunicación se haya probado durante esta revisión documental.

Las operaciones actuales a las que corresponde la matriz son:

| Flujo | Contrato y operación actuales |
| --- | --- |
| Crear cuenta | `IAccountAccessService.RegisterAccountAsync` |
| Iniciar sesión | `IAccountAccessService.LoginAsync` |
| Consultar perfil | `IPlayerProfileService.GetPlayerProfileAsync` |
| Consultar clasificación | `ILeaderboardService.GetLeaderboardAsync` |

El login actual autentica con usuario o correo y contraseña y crea la sesión directamente.
La creación de cuenta del cliente todavía utiliza el runtime local. Los pasos de envío y
verificación de códigos descritos en el diseño objetivo no están integrados en estos flujos
WCF actuales.

### 5.2 Estado de la revisión contractual

La revisión documental de las 23 secciones incorpora la separación entre `ISessionService`
e `IAccountService`, las firmas sin `Task` ni sufijo `Async`, la selección de parámetros
directos u objetos por operación y los faults de negocio. Las referencias al diseño anterior
se conservan únicamente para identificar el código actual y las diferencias de migración.

La retroalimentación académica de la revisión 4 abre una nueva pasada sobre ese diseño.
Este bloque incorpora el límite de parámetros, la justificación de retornos y faults,
la simplificación de callbacks y unión de sala, y el nombre `PlayerSummary` para búsqueda.
La cohesión del servicio de partidas y las decisiones expresamente pendientes todavía
requieren resolución; no se declara cerrada la nueva revisión académica por estos cambios.

Actualizar este catálogo no modifica los contratos del código, las implementaciones del
servicio, la configuración del host ni la referencia generada del cliente. Cada operación
revisada requiere conciliar esos elementos antes de considerarse integrada con su nueva
firma. Las decisiones pendientes se indican en sus secciones; no constituyen excepciones
a los principios acordados ni se presentan como capacidades implementadas.

## 6. Resumen de service contracts

La siguiente distribución corresponde al diseño objetivo. Cada servicio reúne operaciones
con una responsabilidad definida; no se crea un servicio por operación ni por objeto de
comunicación. Los estados de implementación se consultan por separado en la sección 5.

| Contrato | Responsabilidad | Callback |
| --- | --- | --- |
| `ISessionService` | Acceso, verificación de login, sesiones de invitado y cierre. | No |
| `IAccountService` | Registro y su verificación, contraseñas y desactivación. | No |
| `IPlayerProfileService` | Perfil público, estadísticas, nombre y foto de perfil. | No |
| `IPlayerPreferencesService` | Idioma, zona horaria, estilo de piezas y fondo del tablero. | No |
| `ILeaderboardService` | Consulta de la clasificación global. | No |
| `IFriendshipService` | Búsqueda, invitaciones, amistades y presencia. | Sí |
| `IMatchLobbyService` | Creación, consulta, configuración y unión a salas. | Sí |
| `IGameplayMatchService` | Estado del tablero, jugadas, turnos, conclusión e historial. | Sí |
| `IMatchChatService` | Envío, consulta y moderación de mensajes de partida. | Sí |

La verificación para iniciar sesión pertenece a `ISessionService`; la verificación del
registro y la recuperación de contraseña pertenecen a `IAccountService`. Las preferencias
personales y el fondo del tablero pertenecen a `IPlayerPreferencesService`, mientras que el
nombre público y la foto de perfil permanecen en `IPlayerProfileService`.

La definición de una responsabilidad no adelanta la implementación de operaciones diferidas.
Por ejemplo, incluir recuperación de contraseña en el alcance de cuentas no cambia su
estado de avance.

Los cuatro servicios con callback utilizan estas interfaces:

| Servicio | Callback contract |
| --- | --- |
| `IFriendshipService` | `IFriendshipServiceCallback` |
| `IMatchLobbyService` | `IMatchLobbyServiceCallback` |
| `IGameplayMatchService` | `IGameplayMatchServiceCallback` |
| `IMatchChatService` | `IMatchChatServiceCallback` |

Los callbacks comunican sucesos como cambios de presencia, participantes, tablero o mensajes
recibidos. No se agregan para sustituir el retorno de cada método ni para confirmar todas las
operaciones. Los servicios sin callback entregan sus datos o faults mediante la operación
invocada por el cliente.

Esta distribución sustituye en el diseño objetivo la agrupación de registro y login en
`IAccountAccessService`. Ese contrato permanece identificado como implementación actual en
la sección 5 hasta que se migren el servicio, el host y el cliente.

## 7. `ISessionService`

### 7.1 Responsabilidad

Administra el acceso de usuarios registrados, la verificación del login por código, las
sesiones de invitado y el cierre de sesiones. No utiliza callback: cada operación entrega
su retorno o fault al cliente que la invoca.

En el diseño objetivo, validar usuario y contraseña permite solicitar el código; la sesión
autenticada se crea únicamente al completar su verificación. El login WCF actual todavía
crea la sesión directamente con las credenciales, como se distingue en la sección 5.

### 7.2 Operaciones

| Operación | Retorno | Estado del comportamiento objetivo |
| --- | --- | --- |
| `RequestLoginCode` | `DateTime` | `Planned` |
| `CompleteLogin` | `AuthenticatedSession` | `Planned` |
| `OpenGuestSession` | `GuestSession` | `Prototype` |
| `CloseSession` | `void` | `Planned` |

Firmas objetivo:

```csharp
DateTime RequestLoginCode(string loginIdentifier, string password);

AuthenticatedSession CompleteLogin(
    string loginIdentifier,
    string verificationCode);

GuestSession OpenGuestSession(string displayName);

void CloseSession(string sessionToken);
```

Las firmas reciben valores directos porque ninguna necesita una lista extensa de datos.
No requieren `AuthenticationCredentials`, `LoginVerification`, `GuestIdentity` ni un objeto
`SessionIdentity`. Las sesiones se devuelven directamente, sin un outcome que las envuelva.

El código actual contiene autenticación y creación de sesiones en el servidor, pero no el
flujo remoto de envío y verificación de códigos de login. El acceso de invitado existe como
prototipo local del cliente. El cierre remoto se mantiene planeado: el almacén actual del
servidor crea y consulta sesiones, pero no implementa su revocación explícita.

### 7.3 Parámetros y comportamiento

#### `RequestLoginCode`

- `loginIdentifier`: usuario o correo electrónico de la cuenta.
- `password`: contraseña que el servidor debe verificar.
- Retorno: fecha y hora UTC de caducidad del código cuyo envío se haya completado.

El servidor valida las credenciales y que la cuenta esté activa antes de emitir el código
al correo asociado a esa cuenta. La operación no crea una sesión ni devuelve el código.
El retorno no representa que el destinatario haya leído o recibido el correo; confirma que
el mecanismo de envío aceptó el mensaje.

El código queda asociado en el servidor a la cuenta y al intento de login autorizado por
las credenciales. Tiene caducidad y un solo uso. Un nuevo código para esa cuenta sustituye
al anterior pendiente. Los intentos de verificación se limitan según la política de seguridad
del servidor; el cliente no decide su vigencia ni su validez.

#### `CompleteLogin`

- `loginIdentifier`: usuario o correo de la cuenta cuyo código se verifica.
- `verificationCode`: código recibido por el usuario, conservado como `string`.
- Retorno: una `AuthenticatedSession` válida; los rechazos se comunican como faults.

El servidor resuelve la identidad de la cuenta y comprueba la asociación del código con el
intento de login, su valor, caducidad y que no haya sido consumido. Vuelve a comprobar que
la cuenta esté activa. La creación de la sesión y el consumo del código deben impedir que
dos verificaciones concurrentes del mismo código produzcan dos sesiones.

#### `OpenGuestSession`

- `displayName`: nombre solicitado por el invitado, sin el prefijo reservado.
- Retorno: la `GuestSession` creada por el servidor.

El servidor valida el nombre, aplica el prefijo `Guest-` y comprueba su disponibilidad entre
las sesiones de invitado activas. El nombre definitivo se devuelve en la sesión. El invitado
no recibe un `AccountId` ficticio ni los permisos de una cuenta registrada.

#### `CloseSession`

- `sessionToken`: token de la sesión autenticada o de invitado que se desea cerrar.
- Retorno: `void`, cuando el cierre se ha completado o la sesión ya no estaba activa.

El servidor revoca la sesión identificada por el token e impide su uso posterior. Para
invitados, también libera el nombre reservado. El cierre es idempotente: repetirlo con un
token ya revocado, caducado o inexistente no produce un rechazo de sesión inválida.
Un token vacío se considera una entrada inválida.

Cerrar el canal WCF o borrar el estado local del cliente no sustituye esta revocación.
Si el servidor no puede completar el cierre por una falla técnica, comunica `ServiceFault`;
el cliente no debe interpretar ese fallo como confirmación de revocación.
La operación conserva respuesta WCF aunque devuelva `void`; no se declara one-way, porque
el cliente debe poder recibir sus faults.

### 7.4 Datos devueltos

#### `AuthenticatedSession`


| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `SessionToken` | `string` | Token opaco emitido por el servidor. |
| `Account` | `AccountReference` | Identidad pública de la cuenta autenticada. |
| `EmailAddress` | `string` | Correo de la cuenta, entregado solo a su titular. |
| `PreferredLocaleCode` | `string` | Idioma preferido de la cuenta. |
| `ExpiresAtUtc` | `DateTime` | Caducidad de la sesión en UTC. |

Todos los miembros son obligatorios. La sesión se devuelve únicamente cuando el acceso se
completa correctamente; no contiene un estado `Success` ni una sesión interna opcional.
El servidor obtiene estos datos de la cuenta y del almacén de sesiones.

#### `GuestSession`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `SessionToken` | `string` | Token opaco de la sesión de invitado. |
| `DisplayName` | `string` | Nombre definitivo con el prefijo `Guest-`. |
| `ExpiresAtUtc` | `DateTime` | Caducidad de la sesión en UTC. |

Todos los miembros son obligatorios. La sesión no representa una cuenta persistente.

#### `AccountReference`


| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `AccountId` | `long` | Identificador estable de la cuenta. |
| `Username` | `string` | Nombre público de la cuenta. |

Ambos miembros son obligatorios. Este objeto se reutiliza donde se entregan conjuntamente
el identificador y el nombre de una cuenta; no obliga a recibirlo cuando un parámetro
`accountId` sea suficiente. No es una entidad de Entity Framework.

### 7.5 Faults declarados

Todas las operaciones declaran `ServiceFault` para fallas técnicas controladas. Los rechazos
de acceso o de entrada de este servicio utilizan un único `SessionFault`, con el miembro
obligatorio `Reason` de tipo `SessionFailure`. El detalle se serializa como data contract;
los valores del enum se declaran con `EnumMember`.

| Operación | Valores aplicables de `SessionFailure` |
| --- | --- |
| `RequestLoginCode` | `InvalidCredentials`, `InactiveAccount` |
| `CompleteLogin` | `InvalidVerificationCode`, `ExpiredVerificationCode`, `InactiveAccount` |
| `OpenGuestSession` | `InvalidGuestName`, `GuestNameUnavailable` |
| `CloseSession` | `MissingSessionToken` |

`InvalidCredentials` no distingue una cuenta inexistente de una contraseña incorrecta.
`InactiveAccount` se comunica después de comprobar las credenciales o el código válido.
`InvalidVerificationCode` incluye códigos incorrectos, consumidos o sin un intento pendiente
autorizado; `ExpiredVerificationCode` identifica un código asociado al intento que ya caducó.
`MissingSessionToken` se reserva para un token vacío o ausente, no para una sesión ya cerrada.

El enum incluye también `InvalidSession` para operaciones que reciben un token inexistente,
caducado o revocado. Una sesión de invitado produce ese rechazo solo cuando la operación
requiere una cuenta autenticada; no se rechaza por ese motivo en operaciones que admiten
invitados. Esas operaciones declaran `SessionFault` cuando necesitan este rechazo;
`CloseSession` no lo emite, porque su cierre permanece idempotente.

Las operaciones declaran `[FaultContract(typeof(SessionFault))]` y
`[FaultContract(typeof(ServiceFault))]` con los namespaces definidos en la sección 4.
El servidor comunica el rechazo mediante `FaultException<SessionFault>`; el cliente utiliza
`Reason` para seleccionar el recurso localizado. No se envían contraseñas, códigos, tokens
ni detalles internos en el fault.

La indisponibilidad del mecanismo de envío o de otro recurso del servidor es una falla
técnica y no se presenta como credenciales inválidas. Los errores de transporte observados
por el proxy se manejan en el cliente, separados de los faults que devuelve el servidor.

## 8. `IAccountService`

### 8.1 Responsabilidad

Administra el registro y su verificación, el cambio y recuperación de contraseña y la
desactivación de cuentas. La apertura y el cierre de sesiones pertenecen a `ISessionService`.

No utiliza callback. Las operaciones devuelven el dato producido o `void` al completarse
correctamente; los rechazos se comunican mediante los faults declarados.

### 8.2 Operaciones

| Operación | Retorno | Estado del comportamiento objetivo |
| --- | --- | --- |
| `RequestRegistrationCode` | `DateTime` | `Planned` |
| `CompleteRegistration` | `AccountReference` | `Planned` |
| `ChangePassword` | `void` | `Planned` |
| `ResetPassword` | `void` | `Deferred` |
| `DeactivateAccount` | `void` | `Deferred` |

Firmas objetivo:

```csharp
DateTime RequestRegistrationCode(string username, string emailAddress);

AccountReference CompleteRegistration(NewAccount account);

void ChangePassword(
    string sessionToken,
    string currentPassword,
    string newPassword);

void ResetPassword(
    string emailAddress,
    string verificationCode,
    string newPassword);

void DeactivateAccount(string sessionToken, string currentPassword);
```

El registro remoto actual utiliza `IAccountAccessService.RegisterAccountAsync` y no verifica
un código. Esa implementación no completa el flujo objetivo de esta sección. Las operaciones
de contraseña y desactivación todavía no tienen casos de uso implementados en el servidor;
las fronteras del cliente para cambio de contraseña y baja reportan indisponibilidad.

### 8.3 Parámetros y comportamiento

#### `RequestRegistrationCode`

- `username`: nombre público propuesto para la cuenta.
- `emailAddress`: correo que se asociará a la cuenta y recibirá el código.
- Retorno: fecha y hora UTC de caducidad del código cuyo envío haya sido aceptado.

El servidor valida formato y disponibilidad de ambos valores antes de emitir el código.
La operación no crea la cuenta, no reserva definitivamente el nombre ni devuelve el código.
Aceptar el envío no garantiza que el correo haya llegado a la bandeja del destinatario.

El código se vincula al nombre y correo validados y al propósito de registro. Tiene caducidad,
uso único y límites de verificación controlados por el servidor. Si cambian esos datos, debe
solicitarse un código correspondiente a los nuevos valores. Al reemplazar un código pendiente
para el mismo registro, el anterior deja de ser válido.

#### `CompleteRegistration`

- `account`: datos de la cuenta nueva definidos en la sección 8.4.
- Retorno: `AccountReference` con el identificador y nombre de la cuenta creada.

Se conserva un objeto de entrada porque reúne varios datos relacionados y evita una firma
extensa. No se añade un contenedor de resultado alrededor de la cuenta registrada.

El servidor valida el código, su asociación con el nombre y correo, su vigencia y uso único.
Valida también contraseña, idioma e imagen opcional y vuelve a comprobar la disponibilidad
del nombre y correo al guardar: la comprobación previa no garantiza que sigan disponibles.

La creación debe impedir altas duplicadas ante intentos concurrentes y consumir el código
cuando el registro se complete correctamente. La cuenta solo se confirma al cliente cuando
se hayan guardado los datos aceptados, incluida la imagen si se proporcionó. No se informa
éxito si la operación falla durante su almacenamiento.

Completar el registro no abre una sesión. El usuario inicia el flujo de acceso de
`ISessionService` por separado.

#### `ChangePassword`

- `sessionToken`: sesión autenticada que identifica a la cuenta titular.
- `currentPassword`: contraseña actual que el servidor debe comprobar.
- `newPassword`: contraseña nueva que debe cumplir la política vigente.
- Retorno: `void`, una vez guardado el cambio correctamente.

La cuenta objetivo se obtiene de la sesión; no se recibe un `AccountId` elegido por el
cliente. El servidor valida que la cuenta siga activa, verifica la contraseña actual y
guarda un hash seguro de la nueva. Una sesión de invitado no autoriza esta operación.

#### `ResetPassword`

- `emailAddress`: correo de la cuenta que se desea recuperar.
- `verificationCode`: código emitido para recuperación de contraseña.
- `newPassword`: contraseña nueva que debe cumplir la política vigente.
- Retorno: `void`, una vez guardado el cambio e invalidado el código utilizado.

El servidor exige una cuenta activa y un código asociado a esa cuenta y al propósito de
recuperación, vigente y no consumido. No acepta códigos de login o registro para recuperar
una contraseña. La actualización y el consumo del código impiden su reutilización concurrente.

Esta operación conserva su estado diferido. La operación de emisión del código de
recuperación todavía debe definirse antes de integrar el flujo completo; los métodos de
envío de códigos de login y registro no sustituyen esa responsabilidad.

#### `DeactivateAccount`

- `sessionToken`: sesión autenticada de la cuenta que se desea desactivar.
- `currentPassword`: contraseña actual para confirmar la titularidad.
- Retorno: `void`, una vez confirmada la desactivación y revocada la sesión autorizante.

El servidor rechaza la baja mientras el titular participe en una partida activa. Marca la
cuenta como inactiva sin eliminarla físicamente y conserva partidas, movimientos, mensajes
y resultados históricos. La comprobación de participación y la baja deben coordinarse para
impedir que una entrada concurrente a partida eluda esa regla.

Tras la baja, la cuenta no puede abrir nuevas sesiones ni utilizar otras sesiones existentes
para operaciones que requieran una cuenta activa. Esto se comprueba en el servidor.
El cliente muestra éxito y abandona el perfil únicamente después de la confirmación remota.

En cambios de contraseña y bajas, cancelar antes de invocar al servidor no modifica la
cuenta. Ante un timeout o una pérdida de conexión, el cliente no deduce que la operación
se haya deshecho: el servidor podría haberla completado antes de perderse la respuesta.

### 8.4 Datos de entrada y salida

#### `NewAccount`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `VerificationCode` | `string` | Sí |
| `Username` | `string` | Sí |
| `EmailAddress` | `string` | Sí |
| `Password` | `string` | Sí |
| `PreferredLocaleCode` | `string` | Sí |
| `ProfileImage` | `ImageContent?` | No |

El código se conserva como texto para no perder ceros iniciales. El idioma debe corresponder
a uno admitido por el servid[]()or. Omitir la imagen significa registrar la cuenta sin una foto
personalizada; no se envían rutas de archivos locales.

La confirmación de contraseña pertenece a la captura y validación de la interfaz. El
contrato recibe una contraseña; el servidor aplica su política sin confiar en que el cliente
ya la haya validado. Las contraseñas se transmiten mediante el canal cifrado y nunca se
almacenan en texto plano ni se incorporan a los logs.

#### `ImageContent`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `MediaType` | `string` | Sí |
| `Content` | `byte[]` | Sí |

Describe conjuntamente el formato y los bytes de una imagen. Se reutiliza para perfil y
fondo del tablero donde corresponda, sin crear un tipo idéntico para cada operación.
Los límites y formatos admitidos se validan según el uso de la imagen.

Para la foto de registro, el servidor admite PNG o JPEG, hasta 5 MiB y dimensiones de
1 a 4096 píxeles por eje. Comprueba el contenido real, no solo el MIME declarado.
La imagen aceptada se recodifica en el servidor antes de almacenarla.
El nombre de almacenamiento y la ruta interna son responsabilidad del servidor.

#### `AccountReference`

Se reutiliza el tipo definido en la sección 7.4, con `AccountId` y `Username`.
El resultado de registro contiene una referencia válida y no una cuenta opcional acompañada
de un estado `Success`.

No se requieren `CandidateAccount`, `NewPasswordCredentials`, `PasswordResetCredentials`
ni `DeactivationCredentials`. Tampoco se conservan los outcomes de despacho o registro
ni los enums de éxito y rechazo de cambios de contraseña y desactivación.

### 8.5 Faults declarados

Todas las operaciones declaran `ServiceFault` para fallas técnicas controladas.
Los rechazos propios de cuentas utilizan `AccountFault`, cuyo único miembro obligatorio
es `Reason`, de tipo `AccountFailure`. Sus valores serializados se declaran con `EnumMember`.

| Valor de `AccountFailure` | Significado |
| --- | --- |
| `MissingRegistrationData` | El objeto de registro está ausente. |
| `InvalidUsername` | El nombre no cumple el formato requerido. |
| `InvalidEmailAddress` | El correo no cumple el formato requerido. |
| `InvalidPassword` | La contraseña propuesta no cumple la política. |
| `InvalidLocale` | El idioma está ausente o no está admitido. |
| `InvalidImage` | La imagen no cumple los requisitos de formato, tamaño o contenido. |
| `UsernameUnavailable` | El nombre ya está ocupado. |
| `EmailUnavailable` | El correo ya está asociado a una cuenta. |
| `InvalidVerificationCode` | Código incorrecto, consumido o ajeno al proceso y sus datos. |
| `ExpiredVerificationCode` | El código asociado al proceso ha caducado. |
| `IncorrectCurrentPassword` | La contraseña actual no coincide con la almacenada. |
| `InactiveAccount` | La cuenta ya no está activa. |
| `ActiveMatchInProgress` | La cuenta participa en una partida activa. |

Los valores aplicables se seleccionan por operación:

- `RequestRegistrationCode`: formato y disponibilidad de nombre y correo.
- `CompleteRegistration`: presencia del objeto, validación de sus datos, disponibilidad de
  nombre y correo y verificación del código.
- `ChangePassword`: contraseña actual incorrecta, nueva contraseña inválida o cuenta inactiva.
- `ResetPassword`: correo inválido, código inválido o caducado, nueva contraseña inválida o
  cuenta inactiva. La inexistencia de un proceso de recuperación autorizado se comunica como
  `InvalidVerificationCode`; no se necesita un estado separado para enumerar cuentas.
- `DeactivateAccount`: contraseña incorrecta, cuenta inactiva o partida activa.

`ChangePassword` y `DeactivateAccount` declaran además `SessionFault` cuando el token no
autoriza una sesión autenticada de la cuenta titular, incluida una sesión caducada o de
invitado. Un token vacío o ausente produce `SessionFailure.MissingSessionToken`; los demás
rechazos de sesión utilizan `SessionFailure.InvalidSession`, definidos en la sección 7.5.

Las operaciones declaran los `FaultContract` aplicables con los namespaces de la sección 4.
El servidor comunica los rechazos mediante `FaultException<AccountFault>` o
`FaultException<SessionFault>`; los fallos técnicos se distinguen mediante
`FaultException<ServiceFault>`. Los métodos con retorno `void` conservan respuesta WCF y no
se declaran one-way, para que el cliente pueda recibir esos faults.

Los faults no contienen contraseñas, códigos, tokens, binarios ni mensajes internos.
El cliente selecciona los recursos localizados a partir del tipo de fault y su razón.
Los detalles de validación adicionales solo se incorporan si un mensaje o acción concreta
del cliente los requiere; no se agregan por anticipación.



## 9. `IPlayerProfileService`

### 9.1 Responsabilidad

Consulta el perfil público y las estadísticas de cuentas registradas y permite modificar
el nombre público y la foto de la cuenta autenticada. No utiliza callback.

Las preferencias personales, el estilo de piezas y el fondo del tablero pertenecen a
`IPlayerPreferencesService`, separado en la sección 10.

### 9.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `GetPlayerProfile` | `PlayerProfile` | `Implemented` |
| `GetProfileImage` | `ImageContent?` | `Planned` |
| `ChangeUsername` | `void` | `Prototype` |
| `UpdateProfileImage` | `void` | `Planned` |

Firmas objetivo:

```csharp
PlayerProfile GetPlayerProfile(
    string sessionToken,
    long? targetAccountId);

ImageContent? GetProfileImage(
    string sessionToken,
    long? targetAccountId);

void ChangeUsername(string sessionToken, string username);

void UpdateProfileImage(string sessionToken, ImageContent image);
```

El caso de uso de consulta de perfil y estadísticas existe en el servidor. El contrato de
código actual es `GetPlayerProfileAsync(PlayerProfileCriteria criteria)`, que devuelve un
outcome; todavía no tiene adaptador WCF, endpoint ni proxy integrados para esta consulta.
`Implemented` identifica el comportamiento del caso de uso, no la firma objetivo ni su
integración remota.

La comprobación explícita de que la cuenta solicitante siga activa es un requisito objetivo
pendiente de incorporar al caso de uso actual, que hoy valida el token de sesión.

El cambio de nombre actual modifica estado local de presentación sin comprobar ni guardar
la unicidad en el servidor. Por ello se identifica como prototipo. La modificación de imagen
tiene una frontera cliente que reporta indisponibilidad y sigue planeada.

### 9.3 Parámetros y comportamiento

Las cuatro operaciones requieren una sesión autenticada de una cuenta activa. El servidor
valida la sesión; no acepta una sesión de invitado para estas operaciones. Recibir un
identificador de otra cuenta en una consulta no autoriza a modificarla.

#### `GetPlayerProfile`

- `sessionToken`: sesión de la cuenta que realiza la consulta.
- `targetAccountId`: identificador positivo de la cuenta consultada, o `null` para la propia.
- Retorno: `PlayerProfile` con identidad pública y estadísticas.

El servidor resuelve la cuenta propia desde la sesión cuando no se especifica un destino.
Si el identificador es inválido o la cuenta no existe, devuelve el fault correspondiente;
no fabrica un perfil vacío ni sustituye la cuenta solicitada por la del solicitante.

La consulta entrega solo datos públicos del perfil. No incluye correo, preferencias privadas,
tokens de la cuenta consultada ni rutas de imágenes del servidor.

#### `GetProfileImage`

- `sessionToken`: sesión de la cuenta que realiza la consulta.
- `targetAccountId`: identificador positivo de la cuenta consultada, o `null` para la propia.
- Retorno: `ImageContent` o `null` si la cuenta existente no tiene foto personalizada.

El servidor resuelve internamente el contenido almacenado para esa cuenta. No recibe ni
devuelve una ruta de archivo. La ausencia normal de foto es diferente de una cuenta
inexistente o de un fallo al leer una imagen almacenada: estos casos se comunican mediante
faults, no como `null`.

`HasProfileImage` describe la disponibilidad al consultar el perfil. Si la imagen cambia
entre consultas, el cliente utiliza el resultado de esta operación para actualizar su vista.

#### `ChangeUsername`

- `sessionToken`: sesión de la cuenta cuyo nombre se modifica.
- `username`: nuevo nombre público propuesto.
- Retorno: `void`, después de guardar correctamente el cambio.

El servidor obtiene la cuenta desde la sesión, normaliza y valida el nombre según la política
vigente y comprueba su disponibilidad al guardar. La comprobación debe impedir duplicados
ante cambios concurrentes. Conservar el mismo nombre de la propia cuenta no se interpreta
como un nombre ocupado por otra cuenta.

El cliente confirma el cambio únicamente después de la respuesta correcta del servidor.
El identificador estable de la cuenta no cambia al modificar su nombre.

#### `UpdateProfileImage`

- `sessionToken`: sesión de la cuenta cuya foto se reemplaza.
- `image`: formato y contenido de la nueva imagen.
- Retorno: `void`, después de guardar correctamente la nueva foto.

Se reutiliza `ImageContent`, definido en la sección 8.4. No se agrega un objeto
`ProfileImageChange` para envolver la sesión y la imagen.

El servidor comprueba contenido real, formato, tamaño y dimensiones con los mismos límites
de foto de perfil definidos en la sección 8.4 y recodifica la imagen antes de almacenarla.
Un objeto o contenido ausente no significa eliminar la foto; es una entrada inválida.
Esta operación no incorpora una función de borrado.

La sustitución se confirma cuando el contenido y su asociación con la cuenta están
guardados. Una previsualización local no prueba que se haya guardado la foto. Ante un timeout
o pérdida de conexión, el cliente no deduce que el servidor haya deshecho la modificación;
puede consultar el perfil o la imagen para reconciliar su vista.

### 9.4 Datos devueltos

#### `PlayerProfile`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta. |
| `HasProfileImage` | `bool` | Indica si existe una foto personalizada. |
| `Statistics` | `PlayerStatistics` | Estadísticas de partidas terminadas. |

Todos los miembros son obligatorios. Se conserva un objeto de perfil porque reúne identidad,
disponibilidad de imagen y estadísticas. `PlayerStatistics` agrupa las cifras de un mismo
concepto; no representa un estado de éxito o fracaso ni otro contenedor de respuesta.

`AccountReference` se reutiliza de la sección 7.4. `ImageContent` se reutiliza de la sección
8.4. No se duplican esos tipos para cada consulta.

#### `PlayerStatistics`

| Miembro | Tipo | Significado |
| --- | --- | --- |
| `MatchesPlayed` | `int` | Total de partidas terminadas contabilizadas. |
| `Wins` | `int` | Partidas ganadas. |
| `Losses` | `int` | Partidas perdidas. |
| `Draws` | `int` | Partidas empatadas. |
| `WinRate` | `double` | Proporción de victorias entre 0 y 1. |

Todos los miembros son obligatorios. El servidor calcula las estadísticas de la cuenta
consultada. `MatchesPlayed = Wins + Losses + Draws`; `WinRate = Wins / MatchesPlayed`
usando división real. Cuando no hay partidas contabilizadas, las cifras y la proporción
son cero. El cliente formatea la proporción como porcentaje localizado.

Un fallo de consulta no se representa como estadísticas en cero. No se admiten valores
enviados por el cliente para modificar estas estadísticas mediante el servicio de perfil.

No se requieren `PlayerProfileCriteria`, `ProfileImageCriteria`, `UsernameChange` ni
`ProfileImageChange`. Tampoco se necesitan los outcomes que envuelven el perfil, la imagen
o el resultado de las modificaciones.

### 9.5 Faults declarados

Todas las operaciones declaran `ServiceFault` y `SessionFault`.
Un token vacío o ausente se comunica como `SessionFailure.MissingSessionToken`; un token
inexistente, caducado, revocado o de invitado, como `SessionFailure.InvalidSession`.

Los rechazos propios del perfil utilizan `ProfileFault`, con el miembro obligatorio
`Reason` de tipo `ProfileFailure`. Sus valores serializados se declaran con `EnumMember`.

| Valor de `ProfileFailure` | Significado |
| --- | --- |
| `InvalidAccountId` | El identificador de la cuenta destino no es positivo. |
| `AccountNotFound` | La cuenta consultada no existe. |
| `InvalidUsername` | El nombre propuesto no cumple la política. |
| `UsernameUnavailable` | El nombre corresponde a otra cuenta. |
| `InvalidImage` | Imagen ausente o con contenido, formato o dimensiones inválidos. |
| `InactiveAccount` | La cuenta solicitante ya no está activa. |

Los valores aplicables se seleccionan por operación:

- `GetPlayerProfile` y `GetProfileImage`: identificador inválido o cuenta inexistente.
- `ChangeUsername`: nombre inválido u ocupado por otra cuenta.
- `UpdateProfileImage`: imagen inválida.
- Las cuatro operaciones pueden rechazar una cuenta solicitante inactiva.

Las operaciones declaran los `FaultContract` aplicables con los namespaces de la sección 4.
El cliente distingue `FaultException<ProfileFault>`, `FaultException<SessionFault>` y
`FaultException<ServiceFault>` y selecciona el recurso localizado correspondiente.
Los métodos con retorno `void` conservan respuesta WCF; no se declaran one-way.

La ausencia de foto se devuelve como `null` y las estadísticas sin partidas como ceros.
Un error de almacenamiento o lectura se comunica como `ServiceFault`, sin exponer rutas,
mensajes de Entity Framework ni otros detalles internos.

## 10. `IPlayerPreferencesService`

### 10.1 Responsabilidad

Consulta y guarda las preferencias personales de una cuenta: idioma, zona horaria, estilo
de piezas y uso de fondo personalizado. También consulta y sustituye la imagen de fondo.

Las preferencias afectan a la vista del titular, no a la del oponente ni a las reglas de
la partida. El servidor conserva la configuración persistente; el cliente representa sus
efectos visuales. El servicio no utiliza callback.

### 10.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `GetPlayerPreferences` | `PlayerPreferences` | `Planned` |
| `UpdatePlayerPreferences` | `void` | `Prototype` |
| `GetBoardBackground` | `ImageContent?` | `Planned` |
| `ChangeBoardBackground` | `void` | `Planned` |

Firmas objetivo:

```csharp
PlayerPreferences GetPlayerPreferences(string sessionToken);

void UpdatePlayerPreferences(
    string sessionToken,
    PlayerPreferences preferences);

ImageContent? GetBoardBackground(string sessionToken);

void ChangeBoardBackground(string sessionToken, ImageContent image);
```

El cliente conserva actualmente el idioma y opciones visuales en estado local; sus cambios
no se guardan mediante este servicio. El estado `Prototype` identifica ese comportamiento
parcial, no una actualización remota implementada. La frontera cliente para guardar un fondo
reporta indisponibilidad.

No existen casos de uso, contratos WCF, adaptadores ni proxies de este servicio en el código
actual. El modelo de persistencia dispone de idioma, zona horaria, estilo de piezas y URI
del fondo, pero todavía no almacena la elección independiente de usarlo o desactivarlo.
Implementar el servicio requiere conciliar esa persistencia con el contrato objetivo.

### 10.3 Parámetros y comportamiento

Las cuatro operaciones requieren una sesión autenticada de una cuenta activa. La cuenta
objetivo se obtiene del token; no se recibe un `AccountId` que permita cambiar preferencias
ajenas. Una sesión de invitado no autoriza estas operaciones.

#### `GetPlayerPreferences`

- `sessionToken`: sesión de la cuenta cuyas preferencias se consultan.
- Retorno: `PlayerPreferences` con los valores efectivos de esa cuenta.

El servidor entrega el idioma guardado, la zona horaria elegida si existe, el estilo de
piezas y la elección de usar el fondo personalizado. No incluye bytes de imágenes ni rutas
de almacenamiento; el fondo se consulta mediante su operación específica.

Cuando no existen personalizaciones opcionales, los valores predeterminados son estilo
`STANDARD`, fondo personalizado desactivado y zona horaria `null`. El idioma corresponde al
de la cuenta. Un fallo de consulta no se oculta devolviendo esos valores como si fueran
las preferencias guardadas.

#### `UpdatePlayerPreferences`

- `sessionToken`: sesión de la cuenta que se modifica.
- `preferences`: conjunto de preferencias editables definido en la sección 10.4.
- Retorno: `void`, después de guardar correctamente el conjunto.

Se conserva un objeto de entrada porque sus valores describen una configuración personal
coherente y evitan una lista extensa de argumentos. La sesión permanece como parámetro
separado; no se crea un contenedor `PlayerPreferencesChange`.

La operación sustituye el conjunto de valores editables; no interpreta campos omitidos
como instrucciones de actualización parcial. El servidor valida el conjunto antes de
guardarlo y no confirma una actualización parcial como si estuviera completa.

Cambiar el idioma modifica la preferencia de la cuenta. La aplicación de recursos y
textos localizados sigue siendo responsabilidad del cliente. La elección de idioma antes
de iniciar sesión permanece local y no requiere llamar a este servicio sin autenticación.

La zona horaria es una preferencia de presentación. No cambia la hora UTC de las partidas,
la caducidad de sesiones o códigos ni los temporizadores autoritativos del servidor.

#### `GetBoardBackground`

- `sessionToken`: sesión de la cuenta cuyo fondo se consulta.
- Retorno: `ImageContent` o `null` cuando no hay un fondo personalizado guardado.

La operación puede devolver la imagen guardada aunque su uso esté desactivado. No devuelve
la imagen predeterminada de la aplicación, que es un recurso del cliente.
Una falla al leer el contenido almacenado se comunica mediante fault, no como ausencia.

#### `ChangeBoardBackground`

- `sessionToken`: sesión de la cuenta cuyo fondo se sustituye.
- `image`: formato y contenido del nuevo fondo.
- Retorno: `void`, después de guardar correctamente la imagen y su asociación con la cuenta.

Se reutiliza `ImageContent`, definido en la sección 8.4. El servidor verifica el contenido
real, admite PNG o JPEG, hasta 5 MiB y de 1 a 4096 píxeles por eje, y recodifica la imagen
antes de almacenarla. La selección de archivo y la previsualización son locales; no se
reciben rutas del equipo del cliente.

Guardar un fondo activa `UseCustomBoardBackground`. La imagen y esa elección deben quedar
coherentes antes de confirmar el éxito. Desactivar posteriormente su uso mediante
`UpdatePlayerPreferences` no borra la imagen ni obliga a subirla de nuevo para reactivarla.
Enviar una imagen ausente no significa borrar el fondo; es una entrada inválida.

Ante un timeout o pérdida de conexión, el cliente no deduce que el servidor haya deshecho
una actualización. Puede consultar las preferencias y el fondo para reconciliar su vista.

### 10.4 Datos de entrada y salida

#### `PlayerPreferences`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `PreferredLocaleCode` | `string` | Sí |
| `TimeZoneId` | `string?` | No |
| `PieceStyleCode` | `string` | Sí |
| `UseCustomBoardBackground` | `bool` | Sí |

El mismo objeto se usa para consultar y guardar valores editables. No contiene estados de
éxito o fracaso ni datos calculados que el cliente no deba modificar.

- `PreferredLocaleCode`: idioma admitido por el servidor; actualmente `es-MX` o `en-US`.
- `TimeZoneId`: identificador reconocido por el servidor para la zona elegida.
  `null` significa que no hay una zona personalizada y el cliente usa su zona local.
  Una cadena vacía o un identificador desconocido no se aceptan como zona válida.
- `PieceStyleCode`: `STANDARD` para piezas normales o `PROFILE_PICTURES` para usar fotos
  de perfil en su representación. `STANDARD` existe en el registro actual; el segundo código
  es la representación objetivo de la opción visual que hoy conserva el cliente.
- `UseCustomBoardBackground`: elección de mostrar el fondo guardado cuando esté disponible.
  Puede estar activada antes de guardar una imagen; mientras no exista, se utiliza el fondo
  predeterminado. No equivale a afirmar que el servidor ya almacena una imagen.

La ausencia de una foto utilizable al representar una pieza mantiene la pieza normal;
el estilo visual no impide participar en la partida.

No se conserva `HasCustomBoardBackground` como campo editable. La existencia de la imagen
se conoce por `GetBoardBackground`; su uso se controla con la preferencia independiente.
No se duplican `ImageContent` ni sus miembros en un tipo `BoardBackgroundContent`.

### 10.5 Faults declarados

Todas las operaciones declaran `ServiceFault` y `SessionFault`.
Un token vacío o ausente se comunica como `SessionFailure.MissingSessionToken`; un token
inexistente, caducado, revocado o de invitado, como `SessionFailure.InvalidSession`.

Los rechazos propios de preferencias utilizan `PreferencesFault`, cuyo miembro obligatorio
`Reason` es de tipo `PreferencesFailure`. Sus valores serializados usan `EnumMember`.

| Valor de `PreferencesFailure` | Significado |
| --- | --- |
| `MissingPreferences` | El objeto de preferencias está ausente. |
| `InvalidLocale` | El idioma está ausente o no está admitido. |
| `InvalidTimeZone` | La zona horaria indicada está vacía o no es reconocida. |
| `InvalidPieceStyle` | El estilo está ausente o no está admitido. |
| `InvalidImage` | Imagen ausente o con formato, tamaño o contenido inválidos. |
| `InactiveAccount` | La cuenta titular ya no está activa. |

Los valores aplicables se seleccionan por operación:

- `UpdatePlayerPreferences`: objeto ausente, idioma, zona horaria o estilo inválidos.
- `ChangeBoardBackground`: imagen inválida.
- Las cuatro operaciones pueden rechazar una cuenta titular inactiva.

Las operaciones declaran los `FaultContract` aplicables con los namespaces de la sección 4.
El cliente distingue `FaultException<PreferencesFault>`, `FaultException<SessionFault>` y
`FaultException<ServiceFault>` y selecciona el recurso localizado correspondiente.
Los métodos con retorno `void` conservan respuesta WCF; no se declaran one-way.

No tener un fondo personalizado se representa como `null`. Su uso desactivado se representa
como `false` en la preferencia, aunque exista una imagen guardada. Los fallos técnicos
de consulta o almacenamiento se comunican como `ServiceFault`, sin exponer rutas ni
detalles internos del servidor.

## 11. `ILeaderboardService`

### 11.1 Responsabilidad

Consulta la clasificación global calculada por el servidor. Es una operación de solo lectura
y no utiliza callback.

No recibe un token de sesión de la aplicación; conserva la consulta desde el menú de usuarios
registrados e invitados. Esto no elimina la seguridad de transporte configurada en WCF.

### 11.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `GetLeaderboard` | `LeaderboardEntry[]` | `Implemented` |

Firma objetivo:

```csharp
LeaderboardEntry[] GetLeaderboard(int maximumEntries);
```

El caso de uso y su consulta mediante Entity Framework existen en el servidor. El contrato
de código actual es `GetLeaderboardAsync(LeaderboardCriteria criteria)`, que devuelve
`LeaderboardStandings`. Todavía no tiene adaptador WCF, endpoint ni proxy integrados para
esta consulta. El cliente continúa consumiendo el runtime local, como se distingue en la
sección 5.

`Implemented` identifica ese comportamiento existente; no acredita la integración de la
firma objetivo ni convierte su revisión documental en una prueba de ejecución.

### 11.3 Parámetros y comportamiento

- `maximumEntries`: cantidad máxima de posiciones solicitadas, entre 1 y 100 inclusive.
- Retorno: array ordenado de `LeaderboardEntry`, nunca `null`.

El límite conserva la validación del caso de uso actual. Los valores fuera de rango se
rechazan; no se convierten silenciosamente a otro límite. La consulta puede devolver menos
elementos de los solicitados o un array vacío si no existen cuentas clasificables.
Un fallo de consulta no se representa como una clasificación vacía.

Se conservan las reglas de la implementación actual:

1. Participan cuentas registradas activas; los invitados no generan una posición persistente.
2. Se contabilizan resultados de partidas terminadas que cuentan para estadísticas.
3. El orden es por victorias descendentes, después proporción de victorias descendente y
   finalmente nombre de usuario ascendente, con comparación ordinal sin distinguir mayúsculas.
4. `Position` es el número consecutivo del elemento ordenado, empezando en 1; no representa
   una clasificación con puestos compartidos por empate.
5. Las cuentas activas sin partidas contabilizadas pueden aparecer con cifras en cero.

El servidor decide el orden y las cifras. El cliente presenta las posiciones recibidas y
formatea los números y porcentajes según su idioma; no recalcula la clasificación.

### 11.4 Datos devueltos

#### `LeaderboardEntry`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `Position` | `int` | Posición consecutiva dentro de la clasificación ordenada. |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta. |
| `HasProfileImage` | `bool` | Indica si existe una foto personalizada. |
| `MatchesPlayed` | `int` | Total de partidas terminadas contabilizadas. |
| `Wins` | `int` | Victorias contabilizadas. |
| `WinRate` | `double` | Proporción de victorias entre 0 y 1. |

Todos los miembros son obligatorios. Se reutiliza `AccountReference`, definido en la
sección 7.4. La proporción se calcula con división real entre victorias y partidas
contabilizadas; cuando no hay partidas, es cero.

Las posiciones no incluyen binarios de imágenes, correos, tokens ni rutas internas.
Un cliente con sesión autenticada puede consultar la foto mediante `IPlayerProfileService`
usando el identificador de la cuenta. Un invitado puede mostrar una imagen genérica;
la consulta de clasificación no amplía los permisos del servicio de perfil.

No se requieren `LeaderboardCriteria` para envolver el límite ni `LeaderboardStandings`
para envolver el array. Tampoco se conserva `GeneratedAtUtc` sin un uso definido para el
cliente ni un estado `Success` junto a una consulta correcta.

### 11.5 Faults declarados

`GetLeaderboard` declara `ServiceFault` para fallas técnicas controladas y
`LeaderboardFault` para el rechazo del límite solicitado.

`LeaderboardFault` es un data contract con el miembro obligatorio `Reason`, de tipo
`LeaderboardFailure`. Ese enum contiene únicamente `InvalidMaximumEntries`, serializado
con `EnumMember`. No incluye estados de éxito ni de indisponibilidad de persistencia.

Se conserva un detalle con `Reason` para que el cliente reconozca los rechazos con la misma
forma que en los demás servicios. Esta uniformidad corrige la excepción de representación
señalada en la revisión 4; no exige añadir datos o razones que todavía no se necesitan.

La operación declara `[FaultContract(typeof(LeaderboardFault))]` y
`[FaultContract(typeof(ServiceFault))]` con los namespaces de la sección 4.
El servidor comunica un límite inválido mediante `FaultException<LeaderboardFault>`;
el cliente distingue ese rechazo de `FaultException<ServiceFault>` y de los errores de
transporte observados por el proxy.

El fallo técnico de persistencia se comunica como `ServiceFault`, sin mensajes de
Entity Framework, consultas SQL ni otros detalles internos. El cliente conserva los recursos
localizados de indisponibilidad y no presenta datos vacíos como si la consulta hubiera
terminado correctamente.

## 12. `IFriendshipService`

### 12.1 Responsabilidad

Busca cuentas registradas, administra solicitudes y relaciones de amistad y comunica la
presencia de amigos. Las operaciones requieren una sesión autenticada de una cuenta activa;
los invitados no acceden a estas funciones.

Utiliza `IFriendshipServiceCallback` porque una solicitud, respuesta o cambio de presencia
puede originarse en otro cliente. El servidor es la autoridad sobre las relaciones y el
estado de las solicitudes.

### 12.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `OpenFriendshipChannel` | `void` | `Planned` |
| `FindPlayers` | `PlayerSummary[]` | `Prototype` |
| `SendFriendRequest` | `FriendRequest` | `Prototype` |
| `GetPendingFriendRequests` | `FriendRequest[]` | `Prototype` |
| `RespondToFriendRequest` | `void` | `Prototype` |
| `GetFriends` | `FriendSummary[]` | `Prototype` |
| `RemoveFriend` | `void` | `Prototype` |

Firmas objetivo:

```csharp
void OpenFriendshipChannel(string sessionToken);

PlayerSummary[] FindPlayers(string sessionToken, string searchText);

FriendRequest SendFriendRequest(
    string sessionToken,
    long recipientAccountId);

FriendRequest[] GetPendingFriendRequests(string sessionToken);

void RespondToFriendRequest(
    string sessionToken,
    string requestId,
    bool accept);

FriendSummary[] GetFriends(string sessionToken);

void RemoveFriend(string sessionToken, long friendAccountId);
```

El cliente actual utiliza un almacén prototipo con cuentas y relaciones de prueba en memoria,
identificadas principalmente por nombres. No existen casos de uso de amistad, contratos,
adaptadores ni endpoints WCF implementados para este servicio en el servidor.

El prototipo ya asigna un identificador textual a cada solicitud. El modelo EF actual
`Friendship` representa una relación por pareja de cuentas y no tiene ese identificador.
Antes de implementar el contrato debe resolverse su correspondencia con persistencia,
sin asumir que existe una columna `requestId`. El contrato no impone una tabla nueva,
pero exige distinguir una solicitud de cualquier solicitud posterior entre la misma pareja.

### 12.3 Parámetros y comportamiento

`sessionToken` identifica siempre a la cuenta que actúa. El servidor no acepta un remitente
o titular proporcionado por el cliente para actuar en nombre de otra cuenta.

#### `OpenFriendshipChannel`

Registra el canal callback de la llamada actual para la cuenta autenticada. Recibe únicamente
`sessionToken` y devuelve `void` cuando el registro queda completado. El cliente no envía una
dirección de callback ni recibe un identificador de canal adicional.

Registrar de nuevo el mismo canal para la misma sesión no duplica sus notificaciones.
El servidor deja de utilizarlo al cerrarse, fallar o perder autorización la sesión.
Abrirlo no devuelve los listados de amigos o solicitudes; se consultan mediante sus métodos.

#### `FindPlayers`

- `searchText`: texto de búsqueda por nombre.
- Retorno: array de `PlayerSummary`, nunca `null`.

Se eliminan espacios al inicio y final y se comparan nombres sin distinguir mayúsculas.
Un texto ausente se interpreta como búsqueda vacía.
Una búsqueda vacía lista las demás cuentas activas del prototipo que se migra; no se trata
como entrada inválida. El servidor excluye a la cuenta solicitante y ordena por nombre.
Sin coincidencias devuelve un array vacío, no un fault.

La búsqueda no crea solicitudes ni acredita que una cuenta siga disponible al enviarlas.

#### `SendFriendRequest`

- `recipientAccountId`: identificador positivo de la cuenta destinataria.
- Retorno: la `FriendRequest` creada, con identidad y fecha asignadas por el servidor.

El servidor rechaza solicitudes a la propia cuenta, a cuentas inexistentes o inactivas,
entre cuentas que ya son amigas o cuando exista una solicitud pendiente entre ambas
en cualquiera de los dos sentidos.

Las comprobaciones y la creación se coordinan de forma atómica para evitar duplicados,
incluidas solicitudes cruzadas concurrentes. Una solicitud repetida no se acepta como si
fuera otra nueva. No se envía un `AccountReference` completo cuando basta con su identificador.

#### `GetPendingFriendRequests`

Devuelve un array de `FriendRequest` con las solicitudes pendientes cuyo
destinatario es la cuenta autenticada. Se ordenan de más reciente a más antigua.
El cliente formatea las fechas UTC según su configuración de presentación.

El identificador permite seleccionar una solicitud concreta sin depender del nombre actual
del remitente ni de la posición de la fila. La ausencia de solicitudes es un array vacío.

#### `RespondToFriendRequest`

- `requestId`: identificador textual de la solicitud seleccionada.
- `accept`: `true` para aceptar o `false` para rechazar.
- Retorno: `void`, una vez guardada la decisión.

El servidor vuelve a comprobar el estado de la solicitud y que la cuenta autenticada sea
su destinataria. Una solicitud inexistente o ya resuelta produce `RequestUnavailable`;
una solicitud pendiente dirigida a otra cuenta se rechaza por falta de autorización.

Aceptar crea o activa una única amistad simétrica y cambia la solicitud a aceptada.
Rechazar solo cambia la solicitud a rechazada. Ambas decisiones la retiran del listado de
pendientes. El estado y la relación se modifican de forma atómica; dos respuestas
concurrentes no pueden resolver la misma solicitud con decisiones diferentes.

Antes de aceptar se comprueba que ambas cuentas sigan activas. Una decisión negativa es un
resultado normal de negocio, no una excepción. Cancelar el diálogo no invoca esta operación.

#### `GetFriends`

Devuelve un array de `FriendSummary` con cuentas activas relacionadas mediante una amistad
aceptada con la cuenta autenticada. La relación es simétrica y excluye al propio titular.
La lista se ordena por nombre; sin amistades devuelve un array vacío.

La presencia la determina el servidor a partir de las conexiones o sesiones que considere
activas. No equivale al estado activo o inactivo de la cuenta. Su integración remota está
planeada; el prototipo local no demuestra un seguimiento real de conexiones.

#### `RemoveFriend`

- `friendAccountId`: identificador positivo de la otra cuenta de la amistad.
- Retorno: `void`, cuando la relación se ha desactivado correctamente.

El servidor obtiene al titular de la sesión y solo modifica la relación que lo incluye.
Una relación inexistente o ya eliminada produce `FriendshipNotFound`, conservando la
distinción que utiliza la interfaz actual para refrescar el listado.

La baja se coordina de forma atómica y conserva cuentas, partidas, movimientos, resultados,
mensajes y solicitudes resueltas. No se borran datos históricos para eliminar una amistad.

En operaciones de escritura, un timeout o pérdida de conexión no demuestra que se haya
deshecho el cambio. El cliente puede consultar los listados para reconciliar su vista.

### 12.4 Datos devueltos

#### `PlayerSummary`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta encontrada. |
| `HasProfileImage` | `bool` | Indica si tiene una foto personalizada. |

Representa al jugador registrado encontrado por la búsqueda, no datos administrativos
de su cuenta. El nombre no introduce una identidad `PlayerId`: conserva `AccountReference`.

#### `FriendRequest`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `RequestId` | `string` | Identificador opaco, estable y no vacío de esa solicitud. |
| `Sender` | `AccountReference` | Cuenta remitente. |
| `Recipient` | `AccountReference` | Cuenta destinataria. |
| `CreatedAtUtc` | `DateTime` | Fecha de creación asignada por el servidor, en UTC. |

El objeto representa una solicitud pendiente: las operaciones que lo devuelven y el
callback que lo entrega ya establecen ese significado. No se añade un campo `State` ni
un enum contractual para repetirlo. El servidor conserva los estados internos de solicitud
pendiente, aceptada y rechazada para validar las decisiones, persistirlas y preservar historia.
El identificador distingue cada solicitud y no se reutiliza al crear otra entre las mismas
cuentas. Conocerlo no autoriza a responderla sin ser su destinatario.

#### `FriendSummary`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos del amigo. |
| `HasProfileImage` | `bool` | Indica si tiene una foto personalizada. |
| `IsOnline` | `bool` | Presencia conocida por el servidor al consultar. |

Todos los miembros de los tres tipos son obligatorios. Se reutiliza `AccountReference`
de la sección 7.4; las fotos se consultan mediante `IPlayerProfileService` usando el
identificador de la cuenta. Las solicitudes pueden obtener la foto de su remitente por esa
misma vía, sin añadir el binario a cada fila.

Las colecciones se devuelven como arrays directos. No se requieren `PlayerSearchCriteria`,
`PlayerSearchMatches`, `FriendRemoval`, objetos para envolver la decisión de una solicitud,
contenedores de colecciones ni outcomes que envuelvan esos datos.

### 12.5 `IFriendshipServiceCallback`

Los métodos tienen retorno `void` y se declaran con `OperationContract(IsOneWay = true)`.

```csharp
void OnFriendRequestReceived(FriendRequest request);

void OnFriendRequestResponded(string requestId, bool accepted);

void OnFriendChanged(long accountId, FriendChange change);
```

- `OnFriendRequestReceived` entrega al destinatario una nueva solicitud pendiente.
- `OnFriendRequestResponded` informa al remitente de la decisión sobre su solicitud.
  `accepted = false` significa rechazo voluntario, no un fallo técnico.
- `OnFriendChanged` identifica a la cuenta amiga cuyo estado cambió. `FriendChange` contiene
  `Connected`, `Disconnected` y `Removed`, serializados con `EnumMember`. Los dos primeros
  actualizan su presencia; el último retira la relación del listado. No equivale a eliminar
  una cuenta ni identifica una falla técnica.

Se conservan tres métodos porque la llegada de una solicitud, su decisión y el cambio de
una amistad tienen datos y acciones diferentes. Las variantes de presencia y eliminación
sí se agrupan: comparten la identidad de la cuenta y actualizan el mismo listado.
La solicitud recibida conserva su contenido y la respuesta conserva su identificador;
no se obliga a volver a consultar toda la lista por cada cambio de presencia.

El servidor solo envía información a canales autorizados de las cuentas afectadas.
No se crea una clase de notificación para envolver estos parámetros ni se añaden
`EventId`, `OccurredAtUtc` o `CorrelationId` por obligación a todos los callbacks.

Las notificaciones se emiten después de confirmar el cambio. Si falla un envío, el servidor
registra el problema y deja de utilizar el canal defectuoso; no deshace la amistad ni
presenta como fallida una escritura ya confirmada por ese motivo.

Los callbacks no garantizan que el cliente reciba todos los cambios mientras está
desconectado. Después de reconectar, el cliente vuelve a abrir su canal y consulta las
solicitudes y amistades actuales. Puede refrescar esos listados al recibir eventos; no debe
tratar la secuencia de callbacks como un historial completo ni como autoridad persistente.

Las consultas actuales recuperan solicitudes recibidas pendientes y amistades vigentes,
pero no un historial de decisiones sobre solicitudes enviadas. Tras una desconexión no
se promete reconstruir un rechazo perdido. Si ese aviso debe recuperarse, su consulta
debe definirse antes de ofrecer esa garantía, sin convertir el callback en una cola duradera.

### 12.6 Faults declarados

Todas las operaciones declaran `ServiceFault` y `SessionFault`.
Un token vacío o ausente produce `SessionFailure.MissingSessionToken`; un token inexistente,
caducado, revocado o de invitado produce `SessionFailure.InvalidSession`.

Los rechazos propios de amistad utilizan `FriendshipFault`, con el miembro obligatorio
`Reason` de tipo `FriendshipFailure`. Sus valores serializados usan `EnumMember`.

| Valor de `FriendshipFailure` | Significado |
| --- | --- |
| `InvalidAccountId` | El identificador de cuenta recibido no es positivo. |
| `CannotSendRequestToSelf` | La solicitud está dirigida a la cuenta remitente. |
| `AccountUnavailable` | La otra cuenta no existe o ya no está activa para la operación. |
| `FriendshipAlreadyExists` | Ya existe una relación de amistad aceptada. |
| `RequestAlreadyPending` | Existe una solicitud pendiente en alguno de los dos sentidos. |
| `MissingRequestId` | El identificador de solicitud está vacío o ausente. |
| `RequestUnavailable` | La solicitud no existe o ya no está pendiente. |
| `UnauthorizedRequest` | La cuenta no es destinataria de la solicitud pendiente. |
| `FriendshipNotFound` | No existe una amistad vigente entre las cuentas. |
| `InactiveAccount` | La cuenta solicitante ya no está activa. |

Los valores aplicables se seleccionan por operación:

- `SendFriendRequest`: identificador inválido, solicitud a sí mismo, cuenta no disponible,
  amistad existente o solicitud pendiente.
- `RespondToFriendRequest`: identificador ausente, solicitud ya procesada, destinatario no
  autorizado o cuenta no disponible al aceptar.
- `RemoveFriend`: identificador inválido o relación inexistente.
- Las siete operaciones pueden rechazar una cuenta solicitante inactiva.

Las operaciones declaran los `FaultContract` aplicables con los namespaces de la sección 4.
El cliente distingue `FaultException<FriendshipFault>`, `FaultException<SessionFault>` y
`FaultException<ServiceFault>` y selecciona los recursos localizados correspondientes.
Los métodos del servicio que devuelven `void` conservan respuesta WCF para recibir faults;
solo los métodos de la interfaz callback se declaran one-way.

Una búsqueda o lista vacía y el rechazo voluntario de una invitación no generan faults.
Los fallos de almacenamiento o consulta se comunican como `ServiceFault`, sin exponer
detalles internos. Una falla local de transporte se distingue de estos rechazos del servidor.

## 13. `IMatchLobbyService`

### 13.1 Responsabilidad

Crea y consulta salas de espera, permite editar su duración, une al segundo participante
e inicia la partida contra otro jugador o contra el sistema. Admite sesiones autenticadas
de cuentas activas y sesiones válidas de invitado.

Utiliza `IMatchLobbyServiceCallback` para informar al creador de cambios de configuración,
del inicio de la partida o del cierre de la sala. El servidor controla participantes,
códigos, autorización y transiciones de estado; el cliente no las confirma por su cuenta.

### 13.2 Operaciones

| Operación             | Retorno              | Estado del comportamiento |
| --------------------- | -------------------- | ------------------------- |
| `OpenLobbyChannel`    | `void`               | `Planned`                 |
| `CreateMatch`         | `LobbySnapshot`      | `Prototype`               |
| `InviteToMatch`       | `LobbySnapshot`      | `Planned`                 |
| `GetAvailableMatches` | `AvailableMatch[]`   | `Prototype`               |
| `JoinMatch`           | `LobbySnapshot`      | `Prototype`               |
| `ChangeTurnDuration` | `LobbySnapshot`      | `Prototype`               |
| `StartSystemMatch`    | `MatchStateSnapshot` | `Prototype`               |
| `CancelMatch`          | `void`               | `Prototype`               |

Firmas objetivo:

```csharp
void OpenLobbyChannel(string sessionToken);

LobbySnapshot CreateMatch(
    string sessionToken,
    MatchConfiguration configuration);

LobbySnapshot InviteToMatch(
    string sessionToken,
    long friendAccountId,
    MatchConfiguration configuration);

AvailableMatch[] GetAvailableMatches(string sessionToken);

LobbySnapshot JoinMatch(string sessionToken, string matchCode);

LobbySnapshot ChangeTurnDuration(
    string sessionToken,
    long matchId,
    int turnDurationSeconds);

MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId);

void CancelMatch(string sessionToken, long matchId);
```

La descripción de prototipos de esta sección corresponde a la etapa de definición del
catálogo. El avance posterior de creación, unión, inicio humano y chat remoto se registra
en [el inventario de migración](MigrationInventory.md#inicio-remoto-de-partida-humana-y-chat).
`InviteToMatch` es una ampliación acordada y todavía pendiente: no existe en la interfaz C#,
el proxy, el endpoint publicado ni la pantalla. Sus reglas se especifican en 13.8.
El identificador remoto es `long`, acorde con `GameMatch.matchId`; no depende
de los identificadores textuales del catálogo prototipo ni del código temporal de unión.

Se conserva `MatchConfiguration` para agrupar la configuración objetivo de creación.
La edición de una sala recibe solo la duración, que es el dato editable de CU-14.
`StartSystemMatch` recibe la sala existente: no crea una segunda partida a partir de otra
configuración. Las decisiones aún abiertas de la configuración se indican en la sección 13.7.

### 13.3 Parámetros y comportamiento

El servidor obtiene al actor desde `sessionToken`, incluido cuando sea invitado. Las cuentas
inactivas no pueden actuar. La autorización usa identidades estables y pertenencia a la sala,
no nombres visibles enviados por el cliente.

#### `OpenLobbyChannel`

Registra el canal callback de la llamada actual para la sesión. No recibe una dirección
remota ni devuelve un identificador de canal adicional. Repetir el registro del mismo canal
no duplica notificaciones.

Solo se envían estados de salas en las que la sesión esté autorizada a participar.
Un canal cerrado, fallido o sin autorización deja de utilizarse. El registro del canal no
sustituye las comprobaciones de autorización de las operaciones posteriores.

#### `CreateMatch`

Esta operación conserva la creación pública. La creación privada con destinatario y
envío de correo corresponde a la ampliación pendiente `InviteToMatch` de 13.8.

Recibe la configuración propuesta y devuelve el estado de la sala creada. La duración debe
estar entre 10 y 120 segundos inclusive; la interfaz utiliza 40 segundos como valor inicial.

El servidor crea la partida y registra al creador como participante del asiento 1 en una
misma operación, sin dejar una sala parcial si falla. El creador usa piezas negras y tiene
el primer turno cuando la partida comience. La sala inicia en `Waiting`.

Genera un código de seis caracteres con letras mayúsculas y dígitos, excluidos `I`, `O`,
`0` y `1`. El código es único entre los códigos activos del servidor. Deja de admitir
uniones cuando comienza o se cancela la partida. No sustituye al identificador estable
`matchId` ni autoriza acciones de creador.

#### `GetAvailableMatches`

Devuelve un array de resúmenes de salas que, al consultar, están en `Waiting`, tienen un solo
participante, admiten otro jugador y no pertenecen al solicitante. Excluye partidas contra
el sistema, partidas en curso, terminadas o canceladas.

Con la ampliación de 13.8 excluirá también las partidas privadas por invitación, incluso
al consultar desde la cuenta invitada. Estas se unen introduciendo el código recibido.

No mezcla salas disponibles con partidas observables en curso. El listado de observación
pertenece al servicio de partidas y conserva las restricciones de invitados.

Sin salas disponibles devuelve un array vacío, nunca `null`. La disponibilidad es temporal
y se vuelve a comprobar al unirse. Los fallos de consulta no se ocultan como un listado vacío.

#### `JoinMatch`

Recibe el código temporal de la sala. Seleccionar una fila del listado e introducir el
código manualmente son dos entradas de interfaz para la misma operación remota.
La fila entrega `MatchCode`; no se mantiene otro método de unión por identificador ni
un objeto con identificador y código opcionales.

El servidor normaliza y valida el código con la política descrita para creación. Distingue
un formato inválido de un código que ya no identifica una sala disponible. Después
comprueba que la sala siga esperando, tenga exactamente un participante, admita otro jugador
y no sea del propio solicitante.

Para una partida privada, la ampliación de 13.8 exige además que la cuenta autenticada sea
la destinataria reservada. Conocer el código no concede ese permiso. La comprobación de
destinatario, vigencia de la espera y ocupación del asiento debe ser atómica con la unión.

Una unión correcta registra exactamente un segundo participante en el asiento 2 con piezas
blancas, inicia la partida en `InProgress` e invalida el código para uniones posteriores.
El creador conserva las negras y el primer turno. La operación devuelve `LobbySnapshot`
con el estado ya iniciado; no introduce otra fase de espera o un botón de inicio adicional.

Quien se une obtiene el tablero mediante `IGameplayMatchService.GetMatchState`. El creador
puede recibirlo mediante `OnMatchStarted`. Recibir el callback no es condición para que la
llamada de unión termine correctamente.

Al entrar al tablero se carga `IGameplayMatchService.GetMatchInformation` para conocer
participantes, asiento propio y duración. El estado recibido por consulta o callback se
conserva mientras llegan esos datos y se concilia como indica 14.3; no se habilitan acciones
usando únicamente los participantes de la sala anterior al inicio.

La comprobación de disponibilidad, la ocupación del asiento y el inicio deben coordinarse
de forma atómica. Dos uniones concurrentes no crean un tercer participante ni dos tableros.
Un intento rechazado no cambia participantes, estado ni código.

#### `ChangeTurnDuration`

Recibe `matchId` y `turnDurationSeconds`; solo el creador puede modificar la duración
mientras la sala siga en `Waiting`. El nuevo valor debe estar entre 10 y 120 segundos.

La actualización no cambia participantes, código, oponente, elegibilidad estadística ni la
distribución inicial de piezas. Devuelve `LobbySnapshot` después de guardar el cambio.
La comprobación de estado y autoría y la escritura se coordinan con el inicio y cancelación
para que una actualización tardía no modifique una partida ya iniciada.

#### `StartSystemMatch`

Recibe el `matchId` de la sala en la que espera el creador. Solo él puede iniciar contra el
sistema y únicamente mientras la sala tenga un participante y siga en `Waiting`.

El servidor incorpora al oponente del sistema como asiento 2, cambia a `InProgress`,
invalida el código e inicia el tablero estándar y el reloj autoritativo. Devuelve el
`MatchStateSnapshot` dinámico inicial definido en el servicio de partidas. La información
base se carga mediante `GetMatchInformation`, incluida la incorporación del asiento del
sistema. No se acepta un nombre, cuenta, tablero ni resultado del sistema desde el cliente.

El inicio contra el sistema compite con la unión de otro jugador y con la cancelación:
solo una de esas transiciones puede completarse. Una llamada que pierde esa carrera no
revierte el inicio o cierre ya confirmado.

#### `CancelMatch`

Recibe el `matchId` de la sala que se desea cancelar y devuelve `void` al completar el cierre.
En el flujo de dos participantes, solo el creador espera en la sala; la llegada del segundo
ya inicia la partida.

El creador puede cancelar mientras siga en `Waiting`; la sala pasa a `Cancelled` y el
código deja de admitir uniones. Repetir el cierre de esa misma sala ya cancelada por el
creador no modifica nada ni produce otro cierre.

Una partida ya iniciada no se abandona mediante este método: corresponde a la operación
de abandono del servicio de partidas. Ningún cierre de sala elimina sus registros históricos.

Un timeout o pérdida de conexión no demuestra que una escritura se haya deshecho.
El cliente no confirma creación, unión, edición, inicio o cancelación solo por cambiar
su estado visual.

### 13.4 Datos de entrada y salida

#### `MatchConfiguration`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `TurnDurationSeconds` | `int` | Duración por turno, de 10 a 120 segundos. |
| `OpponentKind` | `OpponentKind` | Tipo de oponente propuesto o confirmado. |
| `CountsTowardStatistics` | `bool` | Elegibilidad estadística propuesta o confirmada. |

Todos los miembros son obligatorios. `OpponentKind` distingue `Human` y `System` mediante
valores serializados con `EnumMember`. La sesión se recibe por separado: no es un dato
de configuración. El servidor valida lo propuesto y devuelve la configuración aceptada.
La política de los dos últimos miembros todavía requiere las definiciones de la sección 13.7.

#### `AvailableMatch`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchCode` | `string` | Código temporal que la selección envía a `JoinMatch`. |
| `CreatorDisplayName` | `string` | Nombre visible del creador. |
| `TurnDurationSeconds` | `int` | Duración por turno que muestra la lista. |

Todos los miembros son obligatorios. Este resumen contiene los datos que necesita la fila
de selección; no envía tablero ni un estado que siempre sería `Waiting`. El código no
autoriza acciones de creador y el servidor revalida su disponibilidad al unirse.
La respuesta de unión entrega el `MatchId` estable en `LobbySnapshot` para las operaciones
posteriores; el código no reemplaza esa identidad en el juego o su persistencia.

#### `LobbySnapshot`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `MatchCode` | `string?` | Código de unión mientras esté disponible para los participantes. |
| `Participants` | `MatchParticipantSummary[]` | Participantes registrados en la sala o partida. |
| `Configuration` | `MatchConfiguration` | Configuración aceptada por el servidor. |
| `State` | `MatchState` | Estado actual confirmado por el servidor. |

Solo `MatchCode` es opcional: se devuelve `null` al iniciar la partida, cuando ya no sirve
para unirse. El creador corresponde al asiento 1; no se duplica su información en otro miembro.
`MatchState` distingue `Waiting`, `InProgress`, `Finished` y `Cancelled`; el estado de una
unión completada es `InProgress`.

El snapshot organiza información útil de la sala; no contiene un estado `Success`, un
outcome adicional ni el motor del juego. El tablero se entrega como `MatchStateSnapshot`.

#### `MatchParticipantSummary`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `SeatNumber` | `int` | Asiento 1 o 2 dentro de la partida. |
| `Kind` | `ParticipantKind` | Participante registrado, invitado o sistema. |
| `AccountId` | `long?` | Cuenta registrada, cuando corresponda. |
| `DisplayName` | `string` | Nombre visible confirmado por el servidor. |

`ParticipantKind` distingue `Registered`, `Guest` y `System`. Solo los registrados
tienen `AccountId`; los invitados y el sistema no reciben cuentas ficticias. Todos los
demás miembros son obligatorios. El asiento identifica al participante dentro de esa partida.

Este tipo se reutiliza al entregar participantes en `MatchInformation`. No incluye tokens
de sesión ni entidades EF. No se requieren `MatchJoinSelection`, `MatchSettingsChange`,
`SystemMatchConfiguration`, `LobbyDeparture` ni outcomes o contenedores de listados.

### 13.5 `IMatchLobbyServiceCallback`

Los métodos tienen retorno `void` y se declaran con `OperationContract(IsOneWay = true)`.

```csharp
void OnMatchStarted(MatchStateSnapshot state);

void OnLobbyClosed(long matchId);
```

El inicio entrega el estado del juego autorizado para el destinatario; el cierre identifica
la sala cancelada. Se conservan dos métodos porque uno entrega el tablero iniciado y el otro
comunica que una sala dejó de existir como espera; no necesitan un mensaje genérico opcional.

`OnMatchStarted` no repite los datos base de la partida. Al recibirlo, el cliente carga
`GetMatchInformation` y conserva el estado dinámico más reciente hasta completar la vista.

No se conserva un callback de cambio de ajustes: mientras la sala espera solo participa
el creador y `ChangeTurnDuration` ya le devuelve el estado confirmado. La sincronización
entre varias ventanas del mismo creador no es un comportamiento definido en este flujo.

No se conserva una notificación separada de incorporación: al entrar el segundo participante
comienza la partida y `OnMatchStarted` comunica ese cambio. Tampoco se requiere una
notificación separada de salida mientras solo espera el creador: su cancelación se comunica
mediante `OnLobbyClosed`. No se inventa una fase con dos jugadores esperando para justificar
esos callbacks.

Las notificaciones se envían después de confirmar la transición. Un fallo al enviarlas no
revierte una operación guardada ni se presenta como rechazo de esa operación por ese motivo.
El servidor registra el problema y deja de utilizar el canal defectuoso.

Los callbacks no son un historial completo ni garantizan la recuperación de eventos
perdidos al desconectarse. La recuperación del estado de una sala propia aún en espera debe
concretarse antes de integrar el flujo remoto, como se indica en la sección 13.7.

### 13.6 Faults declarados

Todas las operaciones declaran `ServiceFault` y `SessionFault`. La ampliación pendiente
`InviteToMatch` declarará además `MatchFault` y `FriendshipFault`, como precisa 13.8.
Un token vacío o ausente produce `SessionFailure.MissingSessionToken`; una sesión inexistente,
caducada o revocada produce `SessionFailure.InvalidSession`. La admisión objetivo de
invitados se conserva para los flujos generales; `InviteToMatch` requiere una cuenta
registrada activa y no está disponible para invitados.

Los rechazos propios de salas utilizan `MatchFault`, con el miembro obligatorio `Reason`
de tipo `MatchFailure`. Este fault puede reutilizarse en el servicio de partidas para
condiciones del mismo dominio, sin crear una clase por operación.

| Valor de `MatchFailure` | Significado en salas |
| --- | --- |
| `InvalidMatchId` | El identificador de partida no es positivo. |
| `InvalidConfiguration` | Configuración ausente o con opciones no admitidas. |
| `InvalidTurnDuration` | La duración no está entre 10 y 120 segundos. |
| `MatchNotFound` | La partida indicada no existe. |
| `CannotJoinOwnMatch` | El solicitante intenta unirse a su propia sala. |
| `InvalidMatchCode` | El código no cumple el formato. |
| `MatchCodeUnavailable` | El código no identifica una sala actualmente admisible. |
| `Unauthorized` | La sesión no tiene el rol requerido para la acción; en la ampliación privada, no es la cuenta destinataria o intenta invitarse a sí misma. |
| `MatchNotWaiting` | La partida ya no espera y no admite la acción de sala solicitada. |
| `InactiveAccount` | La cuenta registrada solicitante ya no está activa. |

Los valores se seleccionan según la operación:

- `CreateMatch`: configuración ausente o no admitida y duración inválida.
- `JoinMatch`: formato de código inválido, código no disponible o sala propia; con la
  ampliación privada, también cuenta distinta de la destinataria.
- `InviteToMatch` (pendiente): sesión, configuración y duración; identificador,
  disponibilidad y amistad del destinatario; autorización de cuenta registrada.
- `ChangeTurnDuration`: identificador, existencia, autoría, estado y duración.
- `StartSystemMatch`: identificador, existencia, autoría y estado de espera con un participante.
- `CancelMatch`: identificador, existencia, autoría y estado; una repetición del cierre ya
  confirmado por el creador no genera otro rechazo.
- Las operaciones pueden rechazar una cuenta registrada inactiva.

Los valores del enum usan `EnumMember`. Las operaciones declaran los `FaultContract`
aplicables con los namespaces de la sección 4. El cliente distingue
`FaultException<MatchFault>`, `FaultException<SessionFault>` y
`FaultException<ServiceFault>` y selecciona los recursos localizados correspondientes.
Los métodos del servicio con retorno `void` conservan respuesta WCF; no son one-way.

Los fallos de consulta o persistencia se comunican como `ServiceFault`, separados de una
lista vacía y de una sala que dejó de estar disponible. No se envían errores internos,
tableros parciales ni cambios de estado fabricados por el cliente.

### 13.7 Decisiones pendientes de concretar

La propuesta conserva `OpponentKind` y `CountsTowardStatistics` en la configuración, pero
la creación actual solo solicita el tiempo por turno. Antes de implementar esa entrada
deben precisarse la selección de oponente durante la creación y las reglas de elegibilidad
estadística. No se deduce que todas las combinaciones sean válidas ni se confía esa decisión
al cliente. El inicio contra el sistema desde una sala existente sí tiene el comportamiento
descrito en la sección 13.3.

También debe definirse cómo recuperar el estado confirmado de una sala propia que siga en
espera después de una desconexión. El listado de salas disponibles excluye al creador y no
resuelve esa consulta. La definición no debe depender de repetir una escritura ni de que
se haya recibido un callback anterior.

Estas decisiones forman parte del diseño pendiente; no autorizan a presentar la interfaz,
la persistencia ni la integración WCF como terminadas.

### 13.8 Ampliación pendiente: invitación privada por correo

**Estado:** diseño acordado, sin implementación. Esta ampliación no crea otro servicio,
callback, wrapper de solicitud ni objeto de resultado. Reutiliza `IMatchLobbyService`,
`MatchConfiguration`, `LobbySnapshot` y los faults existentes.

```csharp
LobbySnapshot InviteToMatch(
    string sessionToken,
    long friendAccountId,
    MatchConfiguration configuration);
```

#### Entrada y resultado

- `sessionToken` identifica al creador mediante una cuenta registrada activa.
- `friendAccountId` identifica al destinatario estable; no recibe nombre ni correo del cliente.
- `configuration` agrupa la configuración de la partida humana. Se valida en el servidor
  con las mismas reglas aplicables a `CreateMatch`; no añade privacidad ni destinatario.
- El resultado es la partida privada creada en `Waiting`, con su código y el creador en
  el asiento 1. La cuenta invitada no ocupa el asiento 2 hasta confirmar `JoinMatch`.
  El retorno no equivale a la entrega del mensaje en el buzón.

#### Autorización, privacidad y unión

La amistad debe estar aceptada y vigente al solicitar la invitación, ambas cuentas deben
estar activas y el destinatario debe ser distinto del creador. El amigo puede estar
conectado o desconectado: `IsOnline` es informativo, no un requisito ni una autorización.

El servidor crea la partida privada y reserva el segundo asiento para la cuenta invitada.
La privacidad y el destinatario se conservan internamente; no se agregan a
`MatchConfiguration`, `AvailableMatch` ni a un nuevo DTO de invitación. La partida no
aparece en `GetAvailableMatches` para ningún solicitante. Ocultarla no sustituye la
comprobación de identidad al entrar.

El destinatario inicia sesión y utiliza `JoinMatch(sessionToken, matchCode)`. El servidor
revalida cuentas activas, identidad reservada, código y estado disponible. La amistad es
el requisito para emitir la invitación; la autorización de entrada usa la reserva de la
cuenta, sin convertir la presencia en permiso. Una unión válida ocupa el asiento 2,
inicia la partida e invalida el código en una misma transición. No hay botón adicional de
aceptación ni otra sala de espera para ambos. Se reutilizan los callbacks de inicio y cierre.

#### Vigencia vinculada a la espera

El código es válido únicamente mientras el creador mantiene esa partida en espera. No
tiene una caducidad independiente de cinco minutos, una fecha fija en el correo ni una
duración adicional. Es el código de partida de seis caracteres de 13.3, no un código de
verificación de cuenta; no se incorpora como propósito de `VerificationCode`.

Cancelar o abandonar la espera invalida el código. Al cerrar la aplicación, perder su
canal o dejar de tener una sesión válida, el servidor cancela la espera cuando confirma
esa condición. La detección de una desconexión puede tardar según los tiempos de WCF;
el correo no promete detección instantánea. Otra instancia conectada del mismo creador
no prolonga por sí sola la espera mantenida por la sesión que creó la partida.

Al entrar el invitado también se invalida el código. Reiniciar el host no prolonga la
invitación: las partidas que quedaron esperando se cancelan conforme al ciclo actual de
sesiones. Ante carreras entre unión, cancelación y desconexión prevalece una única
transición confirmada por el servidor; un correo antiguo no reabre una partida.

#### Correo y secuencia de confirmación

El servidor obtiene el correo registrado del destinatario, prepara la partida privada,
envía el mensaje y devuelve `LobbySnapshot` solo si el proveedor acepta el envío y la
partida continúa esperando. Si el creador abandona durante el envío no se reactiva la
partida ni se informa que sigue disponible. La cancelación no puede retirar un mensaje
ya aceptado por el proveedor.

El mensaje identifica al creador por su nombre público, incluye el código, explica cómo
iniciar sesión y unirse, e indica: «Puedes unirte con este código mientras el creador siga
esperando». Aclara que la disponibilidad puede cambiar. Se reutilizan la configuración,
credenciales inyectadas por entorno y transporte del mailer, con contenido separado del
correo de verificación y nombres ajenos a la tecnología del proveedor. El correo del
destinatario y las credenciales no se devuelven al cliente.

El envío queda fuera de las transacciones SQL y de los bloqueos de estado. Se confirma
aceptación por el proveedor, nunca entrega garantizada al buzón. No se añaden cola,
reenvío automático ni operación de reenvío en esta ampliación.

#### Faults y fallos parciales

| Operación o condición | Fault y `Reason` |
| --- | --- |
| Token ausente; inválido, caducado o revocado | `SessionFault`: `MissingSessionToken` o `InvalidSession`, respectivamente. |
| Solicitante inactivo | `MatchFault.InactiveAccount`. |
| `InviteToMatch`: invitado o destinatario igual al creador | `MatchFault.Unauthorized`. |
| `InviteToMatch`: configuración ausente o incompatible; duración fuera de rango | `MatchFault.InvalidConfiguration` o `InvalidTurnDuration`, respectivamente. |
| `InviteToMatch`: identificador del destinatario no positivo | `FriendshipFault.InvalidAccountId`. |
| `InviteToMatch`: destinatario inexistente o inactivo | `FriendshipFault.AccountUnavailable`. |
| `InviteToMatch`: no existe amistad aceptada y vigente | `FriendshipFault.FriendshipNotFound`. |
| `JoinMatch`: cuenta distinta del destinatario reservado | `MatchFault.Unauthorized`; no ocupa asiento ni cambia la partida. |
| `JoinMatch`: código cancelado, usado o ya no disponible | `MatchFault.MatchCodeUnavailable`; el formato inválido conserva `InvalidMatchCode`. |
| El creador dejó de esperar antes de confirmar la invitación | `MatchFault.MatchNotWaiting`; no se devuelve una espera activa. |
| Fallo de persistencia, consulta o envío | `ServiceFault` con correlación; sin correo, código ni credenciales en el diagnóstico. |

Se reutilizan los miembros `Reason` de los enums existentes; no se usa
`CannotSendRequestToSelf`, que corresponde a una solicitud de amistad. La notación de la
tabla abrevia, por ejemplo, `FriendshipFault` con `Reason = FriendshipFailure.InvalidAccountId`.
El cliente distinguirá los cuatro `FaultException<T>` declarados. El código técnico de
`ServiceFault` se mantiene en la familia de operaciones de lobby; no se publica la causa
interna del proveedor ni se convierte el fallo de correo en un rechazo de amistad.

Si el envío falla, se cancela la partida preparada y se invalida el código. Un timeout
del proveedor puede ocurrir después de aceptar el mensaje; aun si llega ese correo, la
partida cancelada no admite unión. Si falla también la compensación en BD, no se afirma
que la cancelación se confirmó ni se devuelve éxito. Antes de implementar debe definirse
cómo impedir el uso de una preparación no confirmada y conciliar ese fallo parcial.
No se supone una transacción atómica entre SQL y el proveedor de correo.

Ante un timeout de WCF el cliente no repite automáticamente `InviteToMatch`: la partida
y el envío podrían haberse confirmado. Tampoco muestra «correo enviado» sin respuesta
confirmada. La recuperación de ese resultado incierto debe resolverse antes de ofrecer
reintentos que creen partidas o correos duplicados; no se incorpora ahora un wrapper o
una operación adicional para ocultar esa decisión pendiente.

#### Persistencia y compatibilidad pendientes

El modelo actual no conserva un destinatario de invitación asociado a `GameMatch`.
Se debe definir su representación durable y la distinción entre partidas públicas y
privadas, preparar la migración con claves y restricciones, y conciliar el EDMX mediante
su generación habitual. Una referencia opcional al destinatario es una alternativa
mínima por evaluar; no es un campo aprobado ni una migración ya preparada. El cambio
de cuenta o su baja nunca debe convertir una partida privada en pública.

La futura implementación agregará `InviteToMatch` y sus `FaultContract`, actualizará el
host y regenerará el proxy con las herramientas existentes. `JoinMatch` y
`GetAvailableMatches` conservan sus firmas y adquieren las comprobaciones descritas.
No se modifica hoy código, esquema, endpoint, proxy ni credenciales. Las tareas y
pruebas pendientes se registran en el inventario de migración.

## 14. `IGameplayMatchService`

### 14.1 Responsabilidad

Mantiene el tablero, jugadas, turnos, reloj, conclusión, observación e historial autoritativos
de las partidas. Utiliza `IGameplayMatchServiceCallback` para comunicar estados confirmados.

Los participantes registrados e invitados pueden jugar sus propias partidas. La observación,
el historial y las consultas de detalle o movimientos requieren una cuenta registrada activa
y la autorización correspondiente sobre la partida. Observar no concede asiento ni permite
jugar, enviar chat o abandonar en nombre de un participante.

El cliente representa el tablero recibido y usa su módulo local para calcular posiciones
legales y validar preventivamente una selección. El servidor no envía esa lista, porque se
deriva del tablero que el cliente ya conoce. El cálculo local no acepta ni persiste jugadas:
el servidor vuelve a comprobar su legalidad contra el estado vigente.

El cliente no contiene el motor autoritativo, no decide pases ni ejecuta movimientos del
sistema. Tampoco declara ganadores o expiraciones de turno. Una selección que parecía legal
localmente puede rechazarse si el estado cambió; se conserva `ExpectedMatchStateVersion`.

### 14.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `OpenMatchChannel` | `void` | `Planned` |
| `GetMatchInformation` | `MatchInformation` | `Planned` |
| `GetMatchState` | `MatchStateSnapshot` | `Prototype` |
| `PlacePiece` | `MatchStateSnapshot` | `Prototype` |
| `ForfeitMatch` | `MatchConclusion` | `Prototype` |
| `ObserveMatch` | `MatchStateSnapshot` | `Prototype` |
| `GetMatchHistory` | `MatchSummary[]` | `Prototype` |
| `GetMatchDetails` | `MatchDetails` | `Prototype` |
| `GetMoveHistory` | `MoveRecord[]` | `Prototype` |

Firmas objetivo:

```csharp
void OpenMatchChannel(string sessionToken, long matchId);

MatchInformation GetMatchInformation(string sessionToken, long matchId);

MatchStateSnapshot GetMatchState(string sessionToken, long matchId);

MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement);

MatchConclusion ForfeitMatch(string sessionToken, long matchId);

MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);

MatchSummary[] GetMatchHistory(string sessionToken);

MatchDetails GetMatchDetails(string sessionToken, long matchId);

MoveRecord[] GetMoveHistory(string sessionToken, long matchId);
```

El cliente actual ejecuta las reglas en `GameMoveService`, `ReversiBoard` y `MatchSession`,
y consulta datos prototipo mediante `MatchInformationService` y `MovementHistoryService`.
No existen casos de uso, contratos, adaptadores ni endpoints WCF implementados para este
servicio en el servidor. El pase existe como transición automática local, no como operación
remota disponible. La sincronización, reloj y persistencia autoritativos siguen pendientes.

### 14.3 Autorización y comportamiento

`sessionToken` identifica al actor. El servidor obtiene su asiento o autorización de lectura
desde las relaciones de esa sesión con la partida; no acepta nombres, colores o asientos
enviados por el cliente como prueba de participación.

Los identificadores de partida deben ser positivos. El acceso al historial propio exige
participación de la cuenta autenticada. Consultar detalles o movimientos exige participación
o una autorización de observación de esa partida que siga vigente; conocer el identificador
o haber abierto una pantalla no sustituye esa comprobación.

#### `OpenMatchChannel`, `GetMatchInformation` y `GetMatchState`

`OpenMatchChannel` registra el canal callback del participante y devuelve `void` al completar
el registro. No crea una partida, añade participantes ni concede un asiento. Registrar de
nuevo el mismo canal no duplica sus notificaciones. Conserva respuesta WCF para comunicar
los faults declarados; no es una operación one-way.

`GetMatchInformation` consulta los participantes, el asiento del destinatario y la duración
configurada. Son datos base de la vista que no se repiten por cada jugada. No registra un
canal, crea una partida ni concede permiso de observación. Es una consulta nueva del diseño
objetivo; no se presenta como ya implementada por existir información semejante en el prototipo.

`GetMatchState` consulta el estado dinámico confirmado, incluido el tablero completo, sin
registrar un canal ni modificar la partida. No vuelve a enviar participantes, asiento propio
o duración configurada. Ambas consultas requieren autorización de participante u observador,
incluido para recuperar el cierre de una partida que ya estaban autorizados a consultar.
Las salas aún en espera pertenecen al servicio de lobby.

Para cargar el tablero, el participante completa primero `OpenMatchChannel`, obtiene
`GetMatchInformation` y después consulta `GetMatchState`. Conserva los estados que lleguen
por callback durante la carga y aplica el de mayor `MatchStateVersion` para esa partida.
Una respuesta anterior no sobrescribe un estado más reciente ni se aplica dos veces una
misma versión. La vista necesita la información base antes de identificar participantes
o habilitar acciones; una falla al obtenerla no se oculta usando datos o asientos ficticios.

Abrir registra la suscripción; consultar información carga el contexto de la vista;
consultar estado obtiene el tablero inicial o recuperado. La información base se consulta
al entrar o reconectar, no después de cada movimiento.

Cada snapshot es coherente para una misma versión del estado. `MatchStateVersion` es un número
no negativo que aumenta con cada transición observable del estado autoritativo: jugada,
pase, nuevo turno o conclusión. No equivale al número de movimientos; una transición sin
colocar ficha también puede cambiar la versión del estado.

#### `PlacePiece`

Recibe `PiecePlacement`, definido en la sección 14.4, y devuelve el estado confirmado tras
la jugada y las transiciones automáticas que produzca. Ese `MatchStateSnapshot` contiene
solo el estado dinámico de 14.5, sin repetir la información base de `MatchInformation`.
No se cambia por un `long`, un booleano ni un contenedor de éxito.

El servidor comprueba participación, estado `InProgress`, revisión, turno y legalidad de la
posición en las ocho direcciones. La casilla debe estar vacía y flanquear al menos una ficha
rival. No recibe un tablero, piezas volteadas, origen del movimiento ni puntuaciones del
cliente.

Una posición inválida no cambia tablero, turno, registro de movimientos ni plazo vigente.
La jugada válida, su registro, capturas, próximo turno y posible conclusión se coordinan
como una transición consistente antes de comunicar éxito.

#### Pase automático

El servidor comprueba las jugadas legales al resolver el turno. Si el participante no puede
mover y el rival sí, cede el turno automáticamente; si ninguno puede mover, concluye la
partida por conteo de fichas. No se expone una operación remota para solicitar o forzar el pase.

La transición actualiza `MatchStateVersion` y el estado confirmado se comunica mediante
`OnMatchStateChanged`. `SkippedSeatNumber` identifica al participante cuyo turno se omitió
cuando corresponde. Un pase no crea un movimiento de colocación ficticio. El cliente
representa el resultado y puede recuperar el estado mediante `GetMatchState`.

#### Reloj y oponente del sistema

El servidor inicia cada turno con la duración configurada y controla su vencimiento.
El contador del cliente es visual y no envía una operación para declarar que terminó el
tiempo. La aceptación de una jugada se coordina con el reloj del servidor: una jugada y el
vencimiento concurrentes no pueden resolver el mismo turno de dos formas.

Se conservan las reglas del prototipo:

- El primer y segundo vencimiento consecutivos de un participante producen una jugada
  automática legal, con origen `Timeout`.
- El tercer vencimiento consecutivo termina por inactividad y otorga la victoria al rival,
  sin registrar otro movimiento.
- Una jugada manual o del sistema reinicia el contador de vencimientos consecutivos de
  ese participante; una jugada forzada por timeout lo mantiene acumulado.
- Si no hay jugadas legales, se resuelve el pase o la conclusión antes de intentar colocar
  una ficha automática.
- El oponente del sistema elige una jugada legal y la registra con origen `System`.
  La estrategia local actual es aleatoria; no se presenta como una IA competitiva terminada.

Si un turno no puede continuar o su resultado final no puede guardarse, el servidor no
admite nuevas jugadas ni anuncia una conclusión definitiva inventada. Comunica el fallo
controlado y debe recuperar un estado consistente antes de reanudar o confirmar el cierre.

#### `ForfeitMatch`

Solo un participante puede abandonar su propia partida en curso. El servidor registra al
participante que abandona, la victoria del rival, el motivo `Abandonment`, la hora UTC y el
estado `Finished` de forma consistente. Conserva el tablero y los movimientos existentes,
sin añadir una colocación artificial.

Si la partida ya terminó mientras se confirmaba el abandono, devuelve su conclusión
existente después de autorizar al participante. No cambia ganador, motivo, fecha o revisión
terminal. Cancelar el diálogo no invoca el servicio.

#### `ObserveMatch`

Una cuenta registrada autorizada puede observar una partida en curso. La operación registra
su canal de observación y devuelve el estado dinámico de solo lectura. Después consulta
`GetMatchInformation`, cuyo `AssignedSeatNumber` es `null` para ella.
Los estados recibidos mientras carga esos datos se concilian por `MatchStateVersion`.
No crea un tercer participante ni repite los datos base en cada callback.

El registro de observación no sustituye la autorización que el servidor exige en cada
operación de escritura. La recepción del cierre no incorpora la partida al historial propio
de quien solo la observó.

#### Historial, detalle y movimientos

`GetMatchHistory` devuelve únicamente partidas de la cuenta autenticada, de más reciente a
más antigua según `RecordedAtUtc`. Incluye partidas en curso y terminadas; el nombre de la
pantalla de partidas previas no implica filtrar solo las terminadas. Las salas pendientes o
canceladas no se incorporan por el mero hecho de haber sido creadas.

`GetMatchDetails` entrega los datos necesarios para inspeccionar o reproducir una partida
autorizada. El cliente avanza entre tableros confirmados recibidos; no recalcula las
capturas con una copia del motor de reglas.

`GetMoveHistory` devuelve las jugadas de esa partida ordenadas por `MoveNumber` ascendente.
Una lista sin movimientos es un array vacío; una partida inexistente o no autorizada no se
presenta como si tuviera un historial vacío.

Una pérdida de conexión no demuestra que una jugada o abandono se haya deshecho. El cliente
consulta el estado antes de decidir qué mostrar o volver a enviar. No reintenta una jugada
ignorando su revisión original.

### 14.4 Datos de entrada

#### `PiecePlacement`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Partida en la que se solicita colocar la ficha. |
| `Row` | `int` | Fila de 0 a 7. |
| `Column` | `int` | Columna de 0 a 7. |
| `ExpectedMatchStateVersion` | `long` | Versión no negativa del estado visto por el cliente. |

Todos los miembros son obligatorios. Se agrupan porque describen una misma colocación
sobre un estado concreto y evitan una firma extensa. El token se recibe por separado.

No se requieren objetos `MatchIdentity`, `MatchForfeit`, `MatchObservation` o
`MatchConnection` para envolver token e identificador. El pase automático no tiene solicitud
contractual propia.

### 14.5 Datos devueltos

#### `MatchInformation`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `Participants` | `MatchParticipantSummary[]` | Participantes y asientos definidos en 13.4. |
| `AssignedSeatNumber` | `int?` | Asiento asignado al destinatario; `null` para un observador. |
| `TurnDurationSeconds` | `int` | Duración configurada por turno. |

Solo `AssignedSeatNumber` es opcional; el array nunca es `null`. El asiento 1 corresponde a
negras y el 2 a blancas. Un invitado participante tiene asiento aunque no tenga `AccountId`.
El servidor calcula el asiento para la sesión autorizada: esta información no se reutiliza
entre sesiones como si todos los destinatarios tuvieran el mismo rol.

Los participantes, sus asientos y la duración quedan fijados al comenzar la partida.
Los nombres son etiquetas de presentación confirmadas al consultar la información; no se
promete sincronización instantánea de cambios posteriores del perfil. La vista puede
refrescarlos al recargar esta consulta sin convertirlos en datos de cada jugada.

Se separa esta información porque el cliente la necesita al entrar o recuperar la vista,
pero no en cada actualización del tablero. No contiene el tablero ni envuelve un estado.
Las dos consultas se relacionan por `MatchId`; no se promete una lectura atómica conjunta.
La versión del estado ordena los datos dinámicos, no las etiquetas de presentación.

#### `MatchStateSnapshot`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `MatchStateVersion` | `long` | Versión confirmada del estado de la partida. |
| `State` | `MatchState` | Estado de la partida, definido en la sección 13.4. |
| `Board` | `BoardCellState[]` | Las 64 casillas, ordenadas por filas. |
| `BlackPieceCount` | `int` | Conteo de fichas negras confirmado por el servidor. |
| `WhitePieceCount` | `int` | Conteo de fichas blancas confirmado por el servidor. |
| `CurrentTurnSeatNumber` | `int?` | Asiento que tiene el turno, si hay uno activo. |
| `TurnStartedAtUtc` | `DateTime?` | Comienzo UTC del turno activo. |
| `LastMove` | `MoveRecord?` | Colocación confirmada en la transición de esta revisión. |
| `SkippedSeatNumber` | `int?` | Asiento cuyo turno se pasó en esa transición. |
| `Conclusion` | `MatchConclusion?` | Conclusión confirmada cuando la partida termina. |

Solo los tipos nullable son opcionales; los arrays nunca son `null`. Este objeto contiene
el estado dinámico completo de una versión, no diferencias que el cliente deba acumular.
Conserva `MatchId` para identificar la partida y `MatchStateVersion` para ordenar las
actualizaciones. Participantes, asiento propio y duración se obtienen de `MatchInformation`.
La misma representación dinámica se usa en consultas de estado, retorno de jugada y callbacks.

`BoardCellState` contiene `Empty`, `Black` y `White`, serializados con `EnumMember`.
El índice de una casilla es `row * 8 + column`. No se necesita `BoardSnapshot` para envolver
solo el array ni repetir fila y columna en cada una de sus 64 entradas.
El servicio valida longitudes, índices y consistencia antes de entregar tableros o cuadros
de reproducción; los atributos de serialización no imponen por sí solos esas restricciones.

Las posiciones legales se calculan localmente a partir de `Board`. No se transmite una
colección adicional por movimiento ni se agrega una consulta remota para cada casilla.
El cliente habilita la interacción según su asiento, el turno y el estado de la partida;
el cálculo local no concede permisos a un observador ni permite actuar fuera de turno.
El servidor valida de nuevo coordenadas, turno, versión y capturas de toda colocación.
Una posición no legal conserva el rechazo `InvalidPosition` de 14.7.

`CurrentTurnSeatNumber` y `TurnStartedAtUtc` son `null` al terminar. La conclusión no se
deduce del contador visual ni de que el cálculo local no encuentre posiciones legales.

`LastMove` y `SkippedSeatNumber` permiten mostrar jugadas automáticas y pases confirmados
por el servidor. Describen la transición de esa versión; no son instrucciones para
volver a aplicarla. Una revisión sin colocación tiene `LastMove = null`, sin borrar por ello
movimientos anteriores del historial. Un tercer timeout no fabrica una nueva colocación.

#### `MoveRecord`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MoveNumber` | `int` | Número consecutivo del movimiento, empezando en 1. |
| `SeatNumber` | `int` | Asiento del participante que colocó la ficha. |
| `Row` | `int` | Fila de la colocación. |
| `Column` | `int` | Columna de la colocación. |
| `PlayedAtUtc` | `DateTime` | Hora UTC confirmada por el servidor. |
| `FlippedPieceCount` | `int` | Fichas rivales volteadas, sin contar la colocada. |
| `Origin` | `MoveOrigin` | `Manual`, `Timeout` o `System`. |

Todos los miembros son obligatorios. El asiento se resuelve contra los participantes de la
partida; no se duplica su nombre en cada movimiento. El cliente deriva la etiqueta automática
del origen y formatea la coordenada y fecha, sin recibir un `WasAutomatic` redundante.

#### `MatchConclusion`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Partida concluida. |
| `MatchStateVersion` | `long` | Versión del estado final confirmado de la partida. |
| `Reason` | `MatchEndReason` | Motivo de finalización. |
| `WinnerSeatNumber` | `int?` | Asiento ganador; `null` en empate. |
| `AbandoningSeatNumber` | `int?` | Asiento que abandonó, cuando ese sea el motivo. |
| `BlackPieceCount` | `int` | Conteo final de negras. |
| `WhitePieceCount` | `int` | Conteo final de blancas. |
| `EndedAtUtc` | `DateTime` | Fecha y hora UTC de conclusión. |

Solo los asientos nullable son opcionales. `MatchEndReason` contiene `NoLegalMoves`,
`Inactivity` y `Abandonment`. `NoLegalMoves` indica que ninguno puede jugar, tanto con el
tablero lleno como con casillas vacías. En abandono o inactividad el ganador no se decide
por mayoría de fichas.

#### `MatchSummary`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Identificador para consultar el detalle. |
| `Participants` | `MatchParticipantSummary[]` | Participantes de la partida. |
| `AssignedSeatNumber` | `int` | Asiento asignado a la cuenta titular del historial. |
| `State` | `MatchState` | Partida en curso o terminada. |
| `RecordedAtUtc` | `DateTime` | Fecha de registro de la partida para ordenar y mostrar. |
| `BlackPieceCount` | `int` | Conteo actual o final de negras. |
| `WhitePieceCount` | `int` | Conteo actual o final de blancas. |
| `Conclusion` | `MatchConclusion?` | Resultado confirmado, solo cuando terminó. |

El cliente presenta el conteo propio primero según el asiento y etiqueta victoria, derrota
o empate a partir de la conclusión del servidor. Una partida en curso no contiene un
resultado definitivo ni un motivo de finalización.

#### `MatchDetails`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Partida consultada. |
| `MatchStateVersion` | `long` | Versión del estado al que corresponden los datos. |
| `Participants` | `MatchParticipantSummary[]` | Participantes y asientos. |
| `TurnDurationSeconds` | `int` | Tiempo configurado por turno. |
| `State` | `MatchState` | Estado confirmado de la partida. |
| `RecordedAtUtc` | `DateTime` | Fecha de registro de la partida. |
| `Conclusion` | `MatchConclusion?` | Conclusión confirmada, si existe. |
| `Moves` | `MoveRecord[]` | Colocaciones confirmadas en orden. |
| `BoardFrames` | `BoardCellState[][]` | Tableros confirmados para reproducción visual. |

Solo la conclusión es opcional; los arrays nunca son `null`. Cada tablero tiene 64 casillas.
El cuadro 0 es el tablero inicial estándar y el cuadro `n` corresponde al movimiento `n`.
Se incluyen hasta 60 colocaciones y hasta 61 cuadros, sin cuadros artificiales por pase,
abandono o timeout sin colocación. Los conteos se presentan desde cada cuadro; el resultado
final se obtiene de la conclusión, no de una decisión del cliente.

Los datos corresponden a una misma revisión, incluso si la partida sigue en curso.
Un espectador utiliza el estado vivo para actualizar la vista; los controles de reproducción
no convierten una partida en curso en un resultado histórico definitivo.

Los objetos de detalle y resumen agrupan datos que necesita cada vista. No se añaden
`MatchHistory` o `MovementHistory` para envolver únicamente sus arrays ni outcomes de
consulta alrededor de estos objetos. Ninguna salida expone entidades EF o el motor del juego.

### 14.6 `IGameplayMatchServiceCallback`

Los métodos tienen retorno `void` y se declaran con `OperationContract(IsOneWay = true)`.

```csharp
void OnMatchStateChanged(MatchStateSnapshot state);

void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected);
```

`OnMatchStateChanged` comunica el estado confirmado tras una jugada, pase, vencimiento
o finalización. No se crean métodos distintos para variantes que ya expresa el snapshot:
`MoveRecord.Origin` identifica una colocación por timeout y `Conclusion` identifica
el cierre, incluida la derrota por inactividad sin fabricar otra colocación.

El servidor distribuye el estado solo a destinatarios autorizados. El rol y asiento del
destinatario se expresan en `MatchInformation`; calcular posiciones locales no cambia ese rol.
El timeout comunica el estado ya resuelto, incluida la jugada automática o conclusión que
corresponda. Al finalizar se envía también el tablero completo;
el cliente no necesita otra consulta solo porque antes se notificaba una conclusión aislada.

La revisión permite descartar estados antiguos y evitar aplicar dos veces el mismo estado
recibido por retorno y callback. La conclusión incluye la revisión terminal para que un
snapshot anterior no reabra visualmente una partida terminada. El tablero final se obtiene
del snapshot o de la consulta autorizada de recuperación; no se reconstruye a partir del
motivo de finalización.

Se conservan dos callbacks: estado de juego y conexión de participantes. La conexión no es
una jugada ni altera por sí misma la revisión del tablero; unificar ambos exigiría ampliar
el snapshot o agregar campos alternativos sin una necesidad definida.

Los cambios se notifican después de confirmarse. Una falla de callback no revierte una
jugada guardada ni obliga al cliente a repetirla. Tras reconectar, el participante vuelve
a registrar su canal con `OpenMatchChannel` y consulta `GetMatchState`, conciliando la
respuesta con los callbacks mediante `MatchStateVersion`. En esa recuperación también vuelve
a cargar `GetMatchInformation` para la misma sesión y partida. El observador renueva su
registro si sigue autorizado y recupera la información base antes de completar la vista.
Los callbacks no sustituyen las consultas de recuperación ni un historial completo.

La conexión informada corresponde a un participante humano. El sistema no se presenta
como un jugador remoto desconectado. Este evento no declara por sí mismo abandono,
vencimiento o derrota; la decisión sigue perteneciendo al servidor.

Al abrir o renovar un canal de juego u observación, el servidor emite el estado de conexión
actual de cada asiento humano mediante `OnParticipantConnectionChanged`. Coordina esos
envíos iniciales con los cambios posteriores del mismo canal para no enviar primero un
cambio nuevo y después una presencia inicial obsoleta. Hasta recibir la notificación, el
cliente muestra conexión desconocida, no supone que el participante esté desconectado.

### 14.7 Faults declarados

Todas las operaciones declaran `ServiceFault` y `SessionFault`. Se reutilizan los motivos de
sesión de la sección 7.5. Las operaciones de juego propio admiten invitados; las de
observación, historial, detalle y movimientos requieren cuenta autenticada.

Se reutilizan `MatchFault` y `MatchFailure`, definidos para salas en la sección 13.6.
Los valores compartidos `InvalidMatchId`, `MatchNotFound`, `Unauthorized` e
`InactiveAccount` conservan su significado. Se añaden estos motivos:

| Valor de `MatchFailure` | Significado en partidas |
| --- | --- |
| `MissingPlacement` | El objeto de colocación está ausente. |
| `InvalidMatchStateVersion` | La versión del estado enviada es negativa. |
| `StaleMatchStateVersion` | La versión enviada difiere de la confirmada. |
| `MatchNotInProgress` | El estado no admite la acción de juego solicitada. |
| `NotPlayersTurn` | El participante no tiene el turno. |
| `InvalidPosition` | Casilla fuera del tablero, ocupada o sin captura legal. |
| `SpectatingUnavailable` | No se puede iniciar la observación en el estado solicitado. |

Los valores se aplican según la operación:

- `OpenMatchChannel`: identificador, existencia y pertenencia a la partida.
- `GetMatchInformation`: identificador, existencia y autorización de participante u observador.
- `GetMatchState`: identificador, existencia y autorización de participante u observador.
- `PlacePiece`: objeto, identificador, revisión, participación, estado, turno y posición.
- `ForfeitMatch`: identificador, existencia y participación. Una partida ya terminada
  devuelve su conclusión existente; una sala en espera no se abandona por esta operación.
- `ObserveMatch`: identificador, existencia, autorización y estado admisible de observación.
- `GetMatchHistory`: sesión de cuenta registrada y participación en los registros devueltos.
- `GetMatchDetails` y `GetMoveHistory`: identificador, existencia y autorización de lectura.
- Las operaciones pueden rechazar una cuenta registrada solicitante inactiva.

Las operaciones declaran los `FaultContract` aplicables con los namespaces de la sección 4.
El cliente distingue `FaultException<MatchFault>`, `FaultException<SessionFault>` y
`FaultException<ServiceFault>`. No se necesita una clase de fault distinta por cada jugada.

Ante `StaleMatchStateVersion`, el cliente consulta el estado actual. Una colección vacía de
historial es un retorno normal; una lectura fallida, una jugada no guardada o un cierre
sin confirmar se comunican como `ServiceFault`, sin datos internos. El cliente no fabrica
estadísticas, movimientos o resultados para sustituir ese fallo.

### 14.8 Decisiones pendientes de concretar

El prototipo dispone de `GetSpectatableMatches` para descubrir partidas en curso, pero este
catálogo todavía debe concretar su operación remota. `GetAvailableMatches` del lobby solo
lista salas admisibles y no sustituye esa consulta. No se presenta el descubrimiento remoto
como integrado por el hecho de definir `ObserveMatch`.

Antes de implementar también deben precisarse la vigencia de autorizaciones de observación
tras el cierre o reconexión y los límites de consulta del historial acumulado. No se recorta
un resultado silenciosamente ni se agregan cursores o contenedores sin una necesidad definida.
Los límites de mensajes WCF deben permitir el detalle de reproducción descrito en esta sección.

La revisión 4 también requiere evaluar la cohesión de esta interfaz: control de una partida
viva y consulta de historial o reproducción son responsabilidades distinguibles. Antes de
implementar debe resolverse si las tres consultas históricas permanecen aquí o se separan
en un servicio de lectura. No se reduce la cantidad mediante una operación genérica con
selectores ni se presenta esa separación como ya acordada o implementada.

## 15. `IMatchChatService`

### 15.1 Responsabilidad

Envía, consulta y distribuye mensajes de una partida y aplica la moderación de sus emisores.
Utiliza `IMatchChatServiceCallback` para entregar mensajes guardados y comunicar un
silenciamiento a la cuenta afectada.

Las tres operaciones requieren una cuenta registrada activa que participe en una partida
en `InProgress`. Los invitados, observadores y el oponente del sistema no reciben permisos
de chat por poder consultar o jugar una partida. Un participante silenciado puede seguir
leyendo el chat autorizado; la sanción impide enviar mensajes.

### 15.2 Operaciones

| Operación | Retorno | Estado del comportamiento |
| --- | --- | --- |
| `OpenChatChannel` | `void` | `Planned` |
| `SendChatMessage` | `MatchChatEntry` | `Prototype` |
| `GetChatHistory` | `MatchChatEntry[]` | `Prototype` |

Firmas objetivo:

```csharp
void OpenChatChannel(string sessionToken, long matchId);

MatchChatEntry SendChatMessage(
    string sessionToken,
    long matchId,
    string content);

MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
```

El cliente actual utiliza `ChatService` y `ChatStore` en memoria, con mensajes identificados
por texto y conversaciones ligadas al código temporal de partida. No existen casos de uso,
contratos, adaptadores ni endpoints WCF implementados para este servicio en el servidor.

El diseño remoto utiliza el identificador estable de partida. El modelo EF de mensajes
ya dispone de `matchId`, `messageNumber` y `senderSeatNumber`; no se necesita otro
identificador global o de evento para distinguir cada mensaje dentro de su conversación.

### 15.3 Parámetros y comportamiento

El servidor resuelve la cuenta y el asiento desde `sessionToken` y la participación real
en `matchId`. No acepta un remitente, nombre, asiento o fecha elegidos por el cliente.
La validación de acceso se repite en consultas, envíos y distribución de callbacks.

#### `OpenChatChannel`

Registra el canal callback de la llamada actual para el chat de la partida autorizada.
Devuelve `void` al completar el registro; no crea otra conversación ni devuelve un objeto
con un identificador de canal.

Registrar de nuevo el mismo canal para la misma sesión y partida no duplica suscripciones.
El servidor deja de utilizarlo al cerrar el canal, perder autorización o terminar la partida.

Si la cuenta tiene un silenciamiento vigente, comunica su intervalo mediante
`OnPlayerMuted`, sin rechazar por ese motivo el acceso de lectura. La comprobación se realiza
en el servidor; no se necesita un objeto adicional de estado para abrir el canal.

#### `SendChatMessage`

- `content`: texto que se desea enviar; no se recibe un `ChatMessageSubmission`.
- Retorno: el `MatchChatEntry` que el servidor haya guardado correctamente.

Se eliminan espacios al principio y final. El texto resultante debe tener entre 1 y 200
unidades de código UTF-16, conservando el límite usado por el prototipo. Un valor ausente
o vacío produce `EmptyMessage`; exceder el límite produce `MessageTooLong`.
Ninguno de esos rechazos crea un mensaje o una infracción.

Después de autorizar al participante, el servidor comprueba el silenciamiento, aplica
moderación y guarda el resultado correspondiente. Un mensaje aceptado conserva el texto
recortado; la normalización usada para buscar expresiones prohibidas no sustituye el texto
que se publica.

Al guardar un mensaje, el servidor asigna su número dentro de la partida, asiento emisor y
hora UTC. La asignación del número y el almacenamiento se coordinan para que dos envíos
concurrentes no produzcan la misma identidad.

El retorno y el callback representan el mismo mensaje confirmado. El cliente limpia el
compositor únicamente después de confirmar el envío; en los rechazos conserva el texto.
Un timeout no autoriza a reenviar automáticamente: el mensaje podría haberse guardado
antes de perderse la respuesta.

#### Moderación y silenciamiento

El servidor compara palabras o expresiones completas sin distinguir mayúsculas ni
diacríticos y considera la puntuación como separador. No bloquea una coincidencia que solo
sea un fragmento dentro de otra palabra. Las reglas de detección se mantienen en el servidor;
el contrato no publica la expresión que coincidió ni la lista interna de reglas.

Un mensaje prohibido no se almacena en la conversación ni se distribuye. Se registra la
infracción con cuenta, partida, regla y hora UTC. El cómputo usa la cuenta estable, no su
nombre visible ni solo la conversación actual.

Al acumular al menos tres infracciones dentro de una ventana móvil de 24 horas, se aplica un
silenciamiento de 10 minutos. La consulta de sanciones, el recuento y su creación se coordinan
por cuenta, incluso si los intentos ocurren en distintas partidas.

Un intento durante un silenciamiento vigente no crea mensajes, infracciones adicionales
ni una nueva sanción; comunica el vencimiento existente. El servidor controla ese plazo.
El contador visual del cliente no puede acortarlo ni confirmar por sí solo que terminó.

Las dos primeras detecciones se comunican como `MessageBlocked`; la detección que crea el
silenciamiento se comunica como `PlayerMuted`, con su vencimiento. No se informa una sanción
como confirmada si su persistencia falla.

El almacenamiento de un mensaje aceptado o del resultado de moderación debe completarse
de forma consistente antes de responder y notificar. Una falla técnica no se transforma
en credenciales inválidas ni se oculta como un mensaje enviado correctamente.

#### `GetChatHistory`

Devuelve un array de los mensajes guardados de la partida autorizada, ordenado por
`MessageNumber` ascendente. La fecha permite mostrar la hora, pero no sustituye la identidad
ni el orden estable del mensaje. El prototipo ordena por fecha y usa un identificador
textual; el contrato objetivo utiliza la numeración disponible en persistencia.

Sin mensajes devuelve un array vacío, nunca `null`. Un fallo de consulta o autorización
no se presenta como una conversación sin mensajes.

La consulta conserva la restricción de partida en curso del flujo actual. Su nombre no
habilita por sí solo una consulta histórica de chat después de finalizar la partida.

### 15.4 Datos devueltos y de moderación

#### `MatchChatEntry`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Partida a la que pertenece el mensaje. |
| `MessageNumber` | `int` | Número positivo y estable del mensaje dentro de la partida. |
| `SenderSeatNumber` | `int` | Asiento 1 o 2 del emisor registrado. |
| `Content` | `string` | Texto recortado y aceptado por el servidor. |
| `SentAtUtc` | `DateTime` | Hora UTC de almacenamiento confirmada por el servidor. |

Todos los miembros son obligatorios. La identidad del mensaje es la pareja
`(MatchId, MessageNumber)`, que no se reutiliza para otro mensaje. No se agrega un
`EventId` distinto para reconocer la misma entrada recibida por retorno, consulta o callback.

El nombre y la referencia de cuenta del emisor se resuelven mediante los participantes de
`MatchInformation`, que reutiliza `MatchParticipantSummary` de 13.4. No se duplican en cada
mensaje ni se aceptan desde el texto enviado por el cliente. El asiento no autoriza por sí
solo a enviar mensajes.

El mismo tipo se reutiliza para el envío confirmado, el historial y la notificación.
No se necesitan `ChatSubmissionOutcome`, `ChatTranscript`, `ChatHistoryCriteria` ni
`ChatMessageNotification` para envolver estos datos.

#### `ChatMuteNotification`

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `MatchId` | `long` | Contexto de chat al que se entrega la notificación. |
| `ReasonCode` | `string` | Razón estable de la sanción: `REPEATED_VIOLATIONS`. |
| `StartsAtUtc` | `DateTime` | Comienzo UTC del silenciamiento confirmado. |
| `EndsAtUtc` | `DateTime` | Fin UTC del silenciamiento confirmado. |

Todos los miembros son obligatorios. Se conserva un objeto porque el suceso reúne varios
datos que el cliente necesita interpretar conjuntamente. No contiene el mensaje prohibido,
la expresión detectada, el identificador interno de regla ni el historial de infracciones.

`MatchId` identifica el canal que recibe la información, no limita la sanción a esa partida.
El silenciamiento pertenece a la cuenta; se comunica a sus canales de chat autorizados.
No se distribuye a otras cuentas como si fuera un mensaje público de la conversación.

### 15.5 `IMatchChatServiceCallback`

Los métodos tienen retorno `void` y se declaran con `OperationContract(IsOneWay = true)`.

```csharp
void OnChatMessageReceived(MatchChatEntry message);

void OnPlayerMuted(ChatMuteNotification mute);
```

Se conservan dos métodos porque el mensaje pertenece a la conversación y se distribuye
a sus participantes autorizados, mientras la sanción pertenece a una cuenta y solo se
comunica a ella. Unirlos exigiría campos alternativos y no eliminaría una redundancia real.

El servidor distribuye únicamente mensajes ya guardados y solo a participantes actualmente
autorizados de esa conversación. Una falla de callback no deshace un mensaje o sanción
persistidos ni convierte una escritura confirmada en un rechazo por ese motivo.

Para cargar o recuperar la conversación, el cliente registra primero el canal y consulta
después el historial. Conserva las notificaciones que lleguen durante la consulta y combina
ambas fuentes por `(MatchId, MessageNumber)`; no reemplaza ciegamente una lista que ya
recibió mensajes más recientes.

La misma clave evita duplicar un mensaje que llegue como retorno de envío y como callback.
El cliente ordena por número, verifica la partida y la autorización actual y representa
los cambios en el hilo de interfaz. Una notificación ajena o recibida después de perder
acceso no habilita la lectura de mensajes.

Al salir de la página se retira la suscripción cliente y se deja de utilizar el canal de chat
que corresponda. Tras una reconexión se vuelve a registrar y consultar; no se supone que
los callbacks sean un historial completo o una cola de entrega persistente.

El fault de un envío silenciado incluye el vencimiento aunque no se haya recibido
`OnPlayerMuted`. El callback no sustituye la validación de sanciones de cada envío.

### 15.6 Faults declarados

Las tres operaciones declaran `ServiceFault`, `SessionFault` y `MatchFault`.

Se reutilizan `SessionFailure.MissingSessionToken` para un token ausente o vacío y
`SessionFailure.InvalidSession` para una sesión inexistente, caducada, revocada o de invitado.
De `MatchFailure` se utilizan `InvalidMatchId`, `MatchNotFound`, `Unauthorized`,
`MatchNotInProgress` e `InactiveAccount` según la comprobación que falle. Observar una
partida no autoriza a leer o enviar su chat.

`SendChatMessage` declara además `ChatFault`, con estos miembros:

| Miembro | Tipo | Propósito |
| --- | --- | --- |
| `Reason` | `ChatFailure` | Motivo de rechazo del mensaje. |
| `MuteEndsAtUtc` | `DateTime?` | Vencimiento requerido cuando el motivo es `PlayerMuted`. |

Los valores de `ChatFailure` se serializan con `EnumMember` y tienen estos significados:

| Valor de `ChatFailure` | Significado |
| --- | --- |
| `EmptyMessage` | El contenido está ausente o queda vacío después de recortar espacios. |
| `MessageTooLong` | El texto recortado excede las 200 unidades de código UTF-16 permitidas. |
| `MessageBlocked` | La moderación rechaza el contenido sin aplicar un nuevo silenciamiento. |
| `PlayerMuted` | Hay un silenciamiento vigente, incluido el recién aplicado por infracciones. |

El vencimiento `MuteEndsAtUtc` es obligatorio para `PlayerMuted` y está ausente en los
demás casos. No se devuelve un `ChatFault` con un estado `Success`.

Los faults se declaran con los namespaces de la sección 4. El servidor comunica el rechazo
de contenido o sanción mediante `FaultException<ChatFault>`; acceso y estado utilizan
`FaultException<SessionFault>` o `FaultException<MatchFault>`. El cliente selecciona sus
recursos localizados sin recibir detalles internos o expresiones de moderación.

Los fallos técnicos de registro del canal, envío, moderación y consulta se comunican mediante
`FaultException<ServiceFault>`, conforme al tratamiento común de 17.3. El servidor conserva
la causa original en el diagnóstico y el cliente distingue estas fallas de los rechazos de
negocio. Una falla de consulta no se representa como un historial vacío ni una falla de envío
como un mensaje confirmado.

Los errores de transporte observados por el proxy se distinguen de los faults del servidor.
`OpenChatChannel` conserva respuesta WCF aunque devuelva `void`; no es one-way.

### 15.7 Límites de la integración pendiente

La moderación actual se conserva en memoria y usa nombres; su traslado al servidor requiere
recontar por cuenta estable y persistir mensajes, infracciones y sanciones de forma consistente.
El catálogo no acredita que el modelo o el servicio actual ya implementen esas transacciones.

Antes de integrar deben concretarse los límites de volumen del historial de una conversación
y la configuración de mensajes WCF. La consulta no se recorta silenciosamente ni se agregan
cursores, colas o contenedores sin definir primero la necesidad que resolverían.

## 16. Contratos comunes

### 16.1 Identidades y referencias

Las operaciones utilizan los identificadores directos definidos en las secciones 7 a 15.
No se crea una clase para envolver cada identificador.

| Dato o tipo | Representación | Uso |
| --- | --- | --- |
| Token de sesión | `string sessionToken` | Permite al servidor resolver y validar la sesión. |
| Identificador de cuenta | `long accountId` | Selecciona una cuenta; no acredita quién llama. |
| Identificador de partida | `long matchId` | Referencia estable, distinta del código de acceso. |
| Asiento | `int seatNumber` | Identifica al participante 1 o 2 dentro de una partida. |
| `AccountReference` | `AccountId`, `Username` | Referencia pública de una cuenta registrada. |
| `MatchParticipantSummary` | Definido en 13.4. | Participante de una partida. |

El servidor obtiene la identidad del emisor desde la sesión y comprueba su autorización.
Recibir un identificador de cuenta, partida o asiento no sustituye esa comprobación.
El token es un dato privado de acceso; no se copia a referencias públicas ni a callbacks.

`AccountReference`, definido en 7.4, se reutiliza en perfiles, amistades y clasificación.
El nombre puede cambiar; la identidad estable de la cuenta es `AccountId`. El modelo actual
no define un `PlayerId` independiente. Los invitados y el sistema no reciben una cuenta
ficticia: `MatchParticipantSummary` expresa su clase de participante y su asiento.

No se requieren `SessionIdentity`, `MatchReference` ni `ParticipantReference` adicionales
para las firmas revisadas. Un código de partida sirve para entrar en la sala según 13.3;
no reemplaza a `MatchId` para identificar mensajes, movimientos o partidas persistidas.

### 16.2 Reutilización de datos

Un tipo se comparte cuando conserva el mismo significado y los mismos datos necesarios.
No se crea una clase base, una jerarquía de mensajes ni un contenedor genérico por el solo
hecho de que varias operaciones reciban o devuelvan objetos.

Por ejemplo, `ImageContent`, definido en 8.4, reúne tipo de medio y contenido binario para
las imágenes de perfil y de fondo. `NewAccount`, `MatchConfiguration` y `PiecePlacement`
mantienen los grupos de entrada justificados en sus respectivas operaciones; no se
generalizan como una solicitud común.

Los tipos aquí mencionados se definen una sola vez en el catálogo. Reutilizarlos no significa
exponer entidades EF, colecciones de navegación ni objetos propios de WPF. Tampoco exige
utilizar el mismo objeto cuando una consulta necesita menos datos, como `AvailableMatch`
frente a `LobbySnapshot`.

Las listas de clasificación, búsqueda y amistades no incorporan binarios de imagen por fila.
Durante la carga de una vista el cliente evita descargar repetidamente la misma foto y
descarta la copia reutilizada tras un reemplazo confirmado. La reutilización respeta la
autorización de la consulta; no comparte imágenes entre sesiones por suposición.
Miniaturas, versiones de imagen y reutilización entre vistas requieren definir primero
su necesidad y medición. No se agregan endpoints o campos de caché por anticipación ni se
afirma que el rendimiento de transferencia ya esté verificado.

### 16.3 Retornos y faults

Cada operación devuelve directamente sus datos o `void`, como definen las secciones 7 a 15.
No existe un `Outcome` obligatorio, un miembro `Status` universal ni un estado `Success`
que deba acompañar toda respuesta.

Los retornos `void` del diseño tienen una confirmación concreta:

- Apertura de canales: el servidor completó el registro; no hay otro dato de canal que devolver.
- Cambios de contraseña, cuenta, perfil o preferencias: la modificación quedó confirmada;
  sus rechazos y fallas se comunican por faults, no mediante un `false` ambiguo.
- `RespondToFriendRequest`: se aplicó la decisión indicada; devolver el mismo `accepted`
  repetiría la entrada y confundiría el rechazo voluntario con un fallo de procesamiento.
- `RemoveFriend`: terminó la relación; una relación inexistente mantiene su fault documentado.
- `CloseSession` y `CancelMatch`: se completó el cierre definido, incluida su repetición
  idempotente. No se agrega un booleano para distinguir estados que el flujo no utiliza.

Estos métodos conservan respuesta WCF. El `void` de callbacks one-way tiene otro significado:
no confirma al servidor que el cliente haya procesado la notificación.

Una colección vacía, una imagen opcional ausente o un empate son resultados normales y se
representan mediante los valores documentados. Un rechazo que el cliente deba distinguir,
como credenciales inválidas o una revisión de partida obsoleta, utiliza el fault tipado
declarado por la operación. Una falla técnica utiliza `ServiceFault`; no se oculta como
una colección vacía ni como ausencia de imagen.

Los faults se reutilizan por significado: por ejemplo, `SessionFault` para las condiciones
de sesión y `MatchFault` para las de acceso o estado de partida que ya lo emplean.
Cada operación declara únicamente los faults que correspondan a su comportamiento.
No se exige un fault por método, una clase de excepción por mensaje visible ni un enum
genérico que mezcle todos los rechazos del sistema. Las razones y los datos adicionales
se conservan en la definición específica de cada fault.

La agrupación vigente se justifica por estas responsabilidades, además de los motivos
aplicables por operación que define cada sección:

| Grupo | Cohesión y frontera |
| --- | --- |
| `SessionFault` | Acceso, identidad de sesión y su vigencia; no describe persistencia. |
| `AccountFault` | Alta, credenciales y disponibilidad de la cuenta titular. |
| `ProfileFault` | Lectura y edición del perfil público; no administra sesiones. |
| `PreferencesFault` | Valores y recursos de configuración personal. |
| `LeaderboardFault` | Validación de la consulta de clasificación. |
| `FriendshipFault` | Solicitudes y relaciones entre cuentas autorizadas. |
| `MatchFault` | Acceso y transiciones de la misma partida en sala, juego y chat. |
| `ChatFault` | Rechazo de contenido y sanción aplicable al envío de mensajes. |
| `ServiceFault` | Falla técnica controlada; se separa de los grupos de negocio. |

Compartir un detalle con `Reason` no habilita todos sus motivos en cada método. Por ejemplo,
`InvalidMatchCode` corresponde a la unión, `StaleMatchStateVersion` al control de juego y
`PlayerMuted` al envío de chat. Si una condición requiere datos o un tratamiento que no encajan
en el grupo, se revisa su contrato en lugar de agregar códigos ajenos o miembros opcionales.

### 16.4 Colecciones y valores opcionales

Las firmas revisadas devuelven arrays de tipos contractuales concretos. Una consulta
correcta sin elementos devuelve un array vacío, nunca `null`; un fallo de consulta conserva
su fault. El orden, la autorización y los límites se definen en cada operación.

No se exponen consultas diferidas, `IQueryable`, contextos EF ni colecciones de navegación.
No se agregan contenedores de colección, contadores, cursores o fechas de generación
sin una necesidad definida.

Un miembro o retorno solo admite ausencia cuando su definición lo indica expresamente.
Por ejemplo, una imagen no configurada puede ser `null`, mientras que una lista de
movimientos sin entradas es un array vacío. Los datos ausentes no se sustituyen por
identificadores ficticios, fechas arbitrarias u objetos vacíos que aparenten existir.

Las fechas de instantes se expresan en UTC según los miembros documentados; las duraciones,
índices, revisiones y porcentajes conservan las unidades y rangos definidos por cada tipo.
Compartir una representación no elimina la validación del servidor ni convierte un valor
de un dominio en otro: un número de mensaje no es una revisión de tablero.

## 17. Contexto diagnóstico y logging

### 17.1 Regla principal

`ILogger<T>` es una dependencia local de cada proceso y nunca forma parte de un contrato WCF.
No se serializa, no se entrega al proxy y no aparece como parámetro de una operación.

- El cliente registra fallas que puede observar en el proxy, los callbacks y la presentación.
- El servidor registra fallas de servicios, casos de uso, persistencia y recursos propios.
- Los proveedores se configuran únicamente en los composition roots ejecutables.

El diagnóstico ayuda a investigar una falla; no sustituye el dato de retorno ni determina
la autorización de una operación. Esta sección describe el diseño objetivo y distingue los
elementos que ya existen en el acceso remoto actual.

### 17.2 Correlation ID

Se conserva un identificador de correlación para relacionar el diagnóstico de una invocación
entre cliente y servidor. Se transporta mediante un encabezado SOAP, sin agregar parámetros
a las firmas ni miembros a todos los objetos enviados.

| Elemento | Valor |
| --- | --- |
| Header | `CorrelationId` |
| Namespace | `urn:reversi:diagnostics:v1` |
| Tipo | `string` con un GUID en formato `D`. |

El cliente genera el identificador al iniciar la invocación. Si está ausente o no es válido,
el servidor genera uno nuevo; si es válido, lo normaliza. El servidor lo asocia al contexto
local de logging y lo conserva en `ServiceFault` si la operación falla técnicamente.

No identifica una cuenta, sesión, transacción ni evento de dominio. Tampoco evita ejecutar
dos veces una escritura: repetir una llamada con el mismo `CorrelationId` no la hace
idempotente. No se utiliza como credencial ni se muestra como mensaje de error al jugador.

El acceso actual implementa el encabezado en `RemoteAccountAccessService` y
`CorrelationContext`; el servidor también lo agrega a los encabezados de salida.
Extender ese manejo a los demás servicios forma parte de su integración pendiente.
No se introduce otro protocolo diagnóstico ni un encabezado obligatorio para cada callback.

El fallback descrito es una regla objetivo: el lector actual busca el encabezado antes de su
bloque de manejo de errores y no cubre de esa forma un encabezado duplicado. Ese caso debe
resolverse al integrar la validación del contexto diagnóstico.

### 17.3 `ServiceFault`

Es el contrato común para comunicar una falla técnica controlada del servidor. Se mantiene
como objeto porque el código de falla y la referencia de diagnóstico cumplen funciones
distintas que el cliente necesita relacionar.

| Miembro objetivo | Tipo | Propósito |
| --- | --- | --- |
| `FailureCode` | `string` | Código técnico estable y no localizado. |
| `CorrelationId` | `string` | Relaciona el fault con el diagnóstico de esa invocación. |

Ambos miembros son obligatorios y utilizan el namespace diagnóstico de la sección 4.
Las operaciones con respuesta declaran `[FaultContract(typeof(ServiceFault))]`, incluidas
las que devuelven `void`. El servidor comunica la falla mediante
`FaultException<ServiceFault>`. Los rechazos de negocio conservan sus propios faults;
no se convierten en `ServiceFault` solo por viajar como excepciones WCF.

El código permite seleccionar un tratamiento o recurso localizado. No contiene el mensaje
de una excepción, su nombre de clase ni texto SQL. Los códigos existentes en
`ServiceFaultFactory` son `ACCOUNT_REGISTRATION_FAILURE` y `ACCOUNT_LOGIN_FAILURE`.
Los códigos de los demás adaptadores se concretarán cuando exista una diferencia de
tratamiento que los justifique, sin publicar por defecto cada etapa interna de persistencia.

El contrato implementado y su proxy todavía contienen `IsTransient`; la fábrica actual lo
establece siempre en `false` y el consumidor de login no lo consulta. El diseño objetivo
lo elimina: un indicador genérico no define si repetir una operación concreta es seguro.
El miembro es obligatorio en el esquema actual; retirarlo rompe la compatibilidad con el
proxy que lo exige. La simplificación requiere una nueva versión del contrato según 22 y
la actualización coordinada del consumidor. El esquema publicado conserva sus tres miembros
mientras lo utilice ese proxy. La identidad de la nueva versión se concretará antes de
publicarla; editar este catálogo no modifica el contrato actual ni su referencia generada.

Un timeout o una pérdida de respuesta pueden ocurrir después de guardar los datos. El cliente
no deduce de `FailureCode`, del encabezado o de una desconexión que puede repetir una escritura.
Los reintentos y la recuperación se resuelven según el comportamiento de cada operación.

### 17.4 Excepciones relacionadas con los contratos

Una clase de fault describe datos de comunicación; no hereda de `Exception`. WCF transporta
esos datos mediante `FaultException<TFault>`, como define 3.4. Las excepciones .NET internas
del servidor, incluidas las personalizadas, no se serializan directamente hacia el cliente.

Cuando una condición conocida exige rechazar la operación, la frontera la traduce al fault
de negocio declarado. Esto no exige lanzar excepciones entre todas las capas internas ni
crear una excepción personalizada por validación. Los resultados normales de 16.3 siguen
siendo valores de retorno.

| Servicio | Fault de negocio propio o compartido | Definición |
| --- | --- | --- |
| `ISessionService` | `SessionFault` | 7.5 |
| `IAccountService` | `AccountFault` | 8.5 |
| `IPlayerProfileService` | `ProfileFault` | 9.5 |
| `IPlayerPreferencesService` | `PreferencesFault` | 10.5 |
| `ILeaderboardService` | `LeaderboardFault` | 11.5 |
| `IFriendshipService` | `FriendshipFault` | 12.6 |
| `IMatchLobbyService` | `MatchFault` | 13.6 |
| `IGameplayMatchService` | `MatchFault` | 14.7 |
| `IMatchChatService` | `MatchFault`, `ChatFault` | 15.6 |

Las operaciones protegidas declaran también `SessionFault` cuando así lo indica su sección.
La tabla no obliga a todos los métodos de un servicio a declarar todos los faults listados:
por ejemplo, `ChatFault` corresponde al envío. `LeaderboardFault` mantiene la misma forma
de detalle con `Reason` que los demás faults de negocio.

Los faults de negocio ya seleccionados deben conservar su tipo y razón. Un manejador general
de fallas inesperadas no debe capturarlos de nuevo para convertirlos en `ServiceFault`.
Los errores de EF, SQL Server, archivos o criptografía no se convierten por su sola clase
en un rechazo de negocio. Solo se realiza esa traducción cuando la condición se ha reconocido
de forma segura; las fallas técnicas restantes se registran y se comunican con `ServiceFault`.

El cliente distingue estos casos:

- `FaultException<TFault>` de negocio declarado: selecciona el mensaje y la acción por su
  tipo y razón; no lo presenta como desconexión ni lo transforma en un retorno vacío.
- `FaultException<ServiceFault>`: utiliza `FailureCode` y `CorrelationId` para el diagnóstico
  local y presenta la falla técnica en el contexto de la operación.
- Un fault desconocido o no compatible: registra el problema contractual y presenta una falla
  controlada; no supone éxito ni clasifica automáticamente el rechazo como credenciales inválidas.
- `CommunicationException` o `TimeoutException`: registra lo observado en el proxy y aplica
  la recuperación de comunicación. La falta de respuesta no prueba que el servidor no ejecutó
  la operación.
- Un error de serialización o configuración observado localmente, como
  `SerializationException` o `InvalidOperationException`: registra la incompatibilidad o
  configuración afectada sin datos sensibles. El proxy puede envolver algunos fallos en una
  excepción de comunicación; no se exige que todos lleguen con esos tipos concretos.

Los faults reconocidos se manejan antes de una captura general de `CommunicationException`.
El cliente cierra los canales utilizables y aborta los que están en `Faulted` o no pueden
cerrarse correctamente. Una falla de limpieza no debe ocultar un resultado ya confirmado
ni reemplazar la excepción original. No se reenvían automáticamente operaciones mutantes
para recuperarse de un timeout.

La implementación actual de `IAccountAccessService` conserva outcomes y firmas con `Task`.
Su manejador general y el consumidor de login aún no implementan esta separación de faults
de negocio. Es una diferencia pendiente de migración, no una excepción al diseño de 7 a 16.

#### 17.4.1 Mensajes visibles conservados durante la migración

El servidor entrega datos o faults; el cliente selecciona recursos localizados.
No se incorporan títulos, mensajes visibles ni recursos de WPF a `ServiceFault` o a los
faults de negocio. Los textos actuales se mantienen en los archivos de recursos.

Las siguientes correspondencias utilizan los nombres objetivo. Salvo la excepción indicada,
cada raíz de recurso citada tiene las claves con sufijos `Title` y `Message`:

- Campos de login vacíos: validación preventiva y recursos
  `LoginScreen_MissingCredentials`; el servidor vuelve a validar la entrada.
- Credenciales rechazadas: `SessionFailure.InvalidCredentials` y
  `LoginScreen_InvalidCredentials`.
- Cuenta inactiva durante el acceso autorizado: `SessionFailure.InactiveAccount` y
  `LoginScreen_AccountInactive`.
- Login con falla técnica o de comunicación: familia `LoginScreen_ServiceUnavailable`,
  distinguiendo la causa en el diagnóstico local.
- Registro completado: retorno de `AccountReference` y `AccountCreation_Success`.
  No se necesita `Success` ni se inicia sesión automáticamente.
- Nombre ocupado: `AccountFailure.UsernameUnavailable` y
  `AccountCreation_UsernameAlreadyTaken`.
- Correo ocupado: `AccountFailure.EmailUnavailable` y `AccountCreation_EmailAlreadyTaken`.
- Nombre inválido: `AccountFailure.InvalidUsername` y
  `AccountCreation_InvalidUsernameFormat`.
- Correo inválido: `AccountFailure.InvalidEmailAddress` y
  `AccountCreation_InvalidEmailFormat`, junto con los recursos de detalle existentes.
- Contraseña inválida: `AccountFailure.InvalidPassword` y el título
  `AccountCreation_InvalidPasswordFormatTitle`; los mensajes son los recursos específicos
  de longitud, mayúscula, minúscula, dígito, símbolo o caracteres no permitidos.
- Imagen inválida: `AccountFailure.InvalidImage` y `AccountCreation_ImageNotSupported`.
- Registro con problema de comunicación: familia `AccountCreation_ServerConnectionFailed`.
  Una falla técnica del servidor se distingue de ese problema y conserva la familia
  `AccountCreation_PersistenceError` cuando corresponda a una creación que no pudo confirmarse.

La validación preventiva puede explicar un requisito específico de correo o contraseña.
El fault genérico correspondiente no proporciona por sí solo cuál requisito falló.
Si la interfaz necesita ese detalle desde el servidor, debe justificarse y definirse
antes de ampliar el fault; no se inventa a partir de la razón genérica ni se envía texto interno.

`InvalidLocale` no tiene un recurso dedicado en el registro actual. Los rechazos nuevos,
como códigos de verificación incorrectos o caducados, también requieren completar su
correspondencia con mensajes localizados antes de integrar el flujo remoto. Los diálogos
de captura existentes no acreditan que ya estén cubiertos todos esos rechazos.

Una respuesta desconocida, un timeout o una incompatibilidad no demuestran que no se haya
creado la cuenta. El texto actual de `AccountCreation_PersistenceError`, que afirma que
no se guardó, requiere una alternativa localizada cuando el resultado sea incierto.
No se cambia la condición real para poder reutilizar un mensaje que describe otra causa.

Los demás servicios conservan las familias de recursos aplicables a cada operación:

- Perfil: `PlayerStatistics_ServerConnectionFailed`, `PlayerStatistics_RetrievalError`,
  `ProfileImageModification_ServerConnectionFailed` y
  `ProfileImageModification_PersistenceError`.
- Preferencias: recursos de idioma, zona horaria, estilo de piezas y fondo del tablero
  de las pantallas actuales; su correspondencia se concreta al integrar la sección 10.
- Clasificación: `MainMenu_TopWinnersUnavailable`.
- Amistades: `FriendRequest_*`, `FriendRequests_*`, `FriendRequestResponse_*`,
  `Friends_*` y `FriendRemoval_*`.
- Salas: `MatchCreation_*`, `AvailableMatches_*`, `MatchSettings_*` y `JoinMatch_*`.
- Juego: `Game_*`, `AbandonMatch_*`, `MatchHistory_*` y `MatchInformation_*`.
- Chat: `Chat_*` y `ChatRead_*` para envío, consulta y rechazos de moderación. Las fallas
  técnicas siguen el tratamiento común de `ServiceFault` descrito en 15.6 y 17.3.

Esta lista conserva referencias de presentación; no sustituye la declaración de faults ni
afirma que cada flujo objetivo ya tenga todos sus mensajes implementados.

#### 17.4.2 Encadenamiento y conservación de la causa

La traducción a un fault remoto no equivale a encadenar excepciones internas. En el acceso
actual, `AccountAccessService` registra la excepción original y `ServiceFaultFactory` crea
un `FaultException<ServiceFault>` nuevo sin enlazar aquella causa mediante `InnerException`.
La relación diagnóstica se conserva en el log y el `CorrelationId`, no en una excepción
interna enviada al cliente.

Si una capa interna necesita envolver una causa en una excepción propia, conserva esa causa
como `InnerException` cuando corresponda, según la sección 6.2 del estándar. Si relanza la
misma excepción utiliza `throw;`, según 6.1. Esto no exige envolver toda excepción ni crear
una cadena artificial por capa.

La causa técnica no se oculta: queda disponible en el diagnóstico permitido del servidor.
El cliente recibe el rechazo o la falla que puede manejar, sin stack traces, nombres internos
o datos sensibles. Un fault de negocio ya reconocido conserva su tipo al atravesar la frontera.

### 17.5 Responsabilidad de logging

| Situación | Responsable |
| --- | --- |
| El proxy no conecta, excede el tiempo o detecta incompatibilidad. | Cliente |
| Se pierde el canal observado por el cliente. | Cliente |
| Falla un caso de uso, EF, SQL Server u otro recurso del servidor. | Servidor |
| Se traduce una falla técnica a `ServiceFault`. | Servidor |
| Falla el envío de un callback. | Servidor |
| Falla el procesamiento local de un callback recibido. | Cliente |

Un rechazo previsto de validación o negocio no se registra automáticamente como `Error`.
La severidad refleja el efecto técnico y sigue la sección 6 del estándar. Los eventos
de moderación y las sanciones conservan su persistencia de negocio; un log no la sustituye.

La misma falla no se registra repetidamente al pasar por capas del mismo proceso.
Se registra en la frontera que puede describir el resultado con contexto suficiente.
El servidor conserva el diagnóstico de la causa técnica; el cliente registra el fault
o la falla de transporte que realmente observa, sin inventar una causa interna.

### 17.6 Datos prohibidos en logs

- Contraseñas, hashes y salts.
- Tokens de sesión.
- Códigos de verificación.
- Cadenas de conexión y claves.
- Contenido binario de imágenes.
- Objetos contractuales completos sin revisión de sus campos.
- Correos electrónicos sin una necesidad revisada y minimización apropiada.
- Contenido completo del chat.
- Expresiones internas detectadas por el moderador cuando revelen el mensaje original.

El contexto diagnóstico identifica la operación y la falla sin volcar sus argumentos.
La precaución también se aplica a los mensajes y excepciones internas que pudieran contener
datos sensibles; no basta con omitirlos del fault si se copian sin revisión a los logs.

### 17.7 Diagnóstico de callbacks

Se utilizan los datos que el callback ya necesita: la revisión del estado de partida,
la pareja `(MatchId, MessageNumber)` del chat o el identificador de invitación, según
corresponda. No se agregan `EventId`, `OccurredAtUtc` ni `CorrelationId` por obligación
a todos los payloads. Las fechas con significado de negocio conservan su propósito.

Cuando el servidor conoce la invocación que originó una notificación puede relacionarla
en su diagnóstico local. Los sucesos originados por el servidor, como un vencimiento de
turno, no necesitan fingir una llamada de cliente ni introducir otra clase de mensaje.

Los callbacks son one-way y no devuelven faults contractuales al servidor. Una falla de
procesamiento se maneja en el cliente; el servidor solo registra las fallas de envío que
puede observar. La ausencia de excepción al enviar no confirma que el cliente procesó
la notificación.

Si falla un callback, el servidor identifica localmente el canal o suscripción afectada,
deja de utilizar los canales defectuosos y permite notificar a los demás destinatarios.
No deshace una escritura confirmada por ese fallo ni repite indefinidamente sobre un canal
en `Faulted`. La recuperación utiliza las consultas y reglas de cada servicio; no se
introduce una cola persistente ni una confirmación por evento sin una necesidad definida.

## 18. Responsabilidades exclusivas del cliente

Las siguientes acciones se resuelven localmente y no necesitan una operación WCF propia:

- Navegar entre páginas, abrir ventanas y mostrar diálogos.
- Seleccionar mensajes localizados y cargar recursos e instrucciones empacados.
- Seleccionar archivos locales y generar previsualizaciones.
- Renderizar y animar el tablero a partir del estado recibido.
- Formatear fechas, porcentajes y textos para la cultura y zona horaria seleccionadas.
- Mostrar una cuenta regresiva con el inicio recibido en `MatchStateSnapshot` y la duración
  configurada recibida en `MatchInformation`.
- Comprobar preventivamente campos vacíos, límites de archivos y coordenadas del tablero.
- Calcular posiciones legales con el módulo local a partir del tablero recibido, para
  orientar la interacción y validar preventivamente la selección del participante.
- Deshabilitar una acción mientras su llamada está pendiente, sin bloquear el hilo de interfaz.
- Procesar callbacks en el hilo de interfaz, combinar mensajes y descartar estados obsoletos
  según las reglas de cada servicio.
- Retirar las suscripciones locales y cerrar o abortar los canales cuando corresponda.

Estas comprobaciones mejoran la interacción; el servidor vuelve a validar la entrada,
la sesión, la autorización y el estado real. La previsualización no confirma que una imagen
haya sido aceptada ni guardada.

El cliente no decide la validez definitiva de una jugada, las capturas, los pases, el
vencimiento de un turno, el resultado de la partida, la aceptación de imágenes o las sanciones.
Su reloj visual no autoriza un movimiento automático ni un final de partida. El prototipo
todavía ejecuta parte de esas reglas localmente; trasladarlas al servidor sigue pendiente.

Los permisos de invitado, participante y observador provienen de las reglas de 7 a 15.
Ocultar un botón o un panel no sustituye la autorización remota. Un observador no adquiere
permiso para jugar o leer el chat por recibir el estado del tablero.

Cerrar una ventana es una acción local; cerrar una sesión de aplicación requiere
`CloseSession`. Cerrar un canal tampoco equivale a abandonar una partida: se utilizan
`CancelMatch` o `ForfeitMatch` cuando corresponde a la intención del usuario.
El cliente no convierte una desconexión en una confirmación de ninguna de esas acciones.

Reconectar el transporte no crea otra sesión, asiento ni partida y no reenvía automáticamente
la última escritura. El cliente valida su sesión mediante las operaciones autorizadas,
registra de nuevo el canal y recupera el estado por las consultas del servicio. Un token
caducado conserva su fault de sesión; no se reemplaza silenciosamente por una sesión nueva.
La recuperación de sala propia y la vigencia del permiso de observación mantienen sus
decisiones pendientes. No se promete restauración tras reinicio del servidor por definir
la reconexión del canal.

## 19. Nombres reservados por persistencia

Los siguientes nombres corresponden actualmente a entidades generadas por Entity Framework.
En este catálogo no se reutilizan como nombres exactos de data contracts para evitar
confundir modelos de comunicación y persistencia:

- `Account`
- `AccountSettings`
- `ChatMessage`
- `ChatMute`
- `ChatViolation`
- `Friendship`
- `GameMatch`
- `GameMove`
- `MatchParticipant`
- `ModerationPattern`
- `ModerationRule`
- `SupportedLocale`
- `VerificationCode`

Los ejemplos contractuales vigentes incluyen `AccountReference`, `PlayerPreferences`,
`MatchChatEntry`, `ChatMuteNotification`, `MatchParticipantSummary`,
`MatchStateSnapshot`, `PiecePlacement` y `FriendRequest`.
Sus nombres expresan la información que representan, sin sufijos de transporte obligatorios.

La separación exige más que nombres distintos: una entidad EF no puede cruzar la frontera
WCF aunque se renombre. Se seleccionan y mapean únicamente los datos definidos por el contrato,
sin contexto, seguimiento de cambios, navegación ni comportamiento de persistencia.
El contexto `ReversiGameSystemEntities` tampoco es un dato contractual.

No se crea una clase adicional solo para evitar una colisión cuando basta con un parámetro
simple, como `long matchId`. Los nombres no justifican por sí solos nuevos envoltorios.

## 20. Trazabilidad con la interfaz actual

Este mapa utiliza las operaciones objetivo de las secciones 7 a 15. Relacionarlas con una
pantalla no acredita que ya exista el contrato implementado, el endpoint, el proxy o su
consumidor; el estado actual se mantiene separado en la sección 5.

### 20.1 Acceso y cuentas

- Inicio de sesión: `ISessionService.RequestLoginCode` y `CompleteLogin`.
  La captura y presentación del código son locales; su envío y validación corresponden
  al servidor. El login remoto actual aún utiliza el flujo anterior sin ese paso.
- Acceso de invitado y cierre: `OpenGuestSession` y `CloseSession` de `ISessionService`.
  No se confunde cerrar sesión con cerrar la ventana o el canal.
- Creación de cuenta: `IAccountService.RequestRegistrationCode` y `CompleteRegistration`.
  La selección y previsualización de la foto siguen siendo locales.
- Modificación de contraseña: `IAccountService.ChangePassword`.
- Recuperación y desactivación: `ResetPassword` y `DeactivateAccount` de `IAccountService`,
  ambas diferidas. La emisión del código de recuperación sigue sin operación definida;
  no se sustituye por un código generado o aceptado exclusivamente en el cliente.

### 20.2 Perfil, preferencias y menú

- Perfil propio o ajeno: `IPlayerProfileService.GetPlayerProfile` y `GetProfileImage`.
- Cambio de nombre y foto: `ChangeUsername` y `UpdateProfileImage` del servicio de perfil.
- Configuración de idioma, zona horaria y estilo de piezas:
  `IPlayerPreferencesService.GetPlayerPreferences` y `UpdatePlayerPreferences`.
- Fondo del tablero: `GetBoardBackground` y `ChangeBoardBackground` del servicio de preferencias.
  La opción de utilizarlo y la existencia de una imagen conservan la distinción de 10.3.
- Clasificación del menú: `ILeaderboardService.GetLeaderboard`, también disponible para
  invitados según 11.3. No concede acceso a consultas de perfil protegidas.

### 20.3 Amistades

El menú, la búsqueda de jugadores, las solicitudes y la lista de amigos utilizan
`IFriendshipService`: `OpenFriendshipChannel`, `FindPlayers`, `SendFriendRequest`,
`GetPendingFriendRequests`, `RespondToFriendRequest`, `GetFriends` y `RemoveFriend`.
Los callbacks actualizan solicitudes y presencia; las consultas recuperan el estado
según 12.5. Este flujo requiere cuenta registrada activa.

### 20.4 Salas y partida

La creación, búsqueda, unión y espera utilizan `IMatchLobbyService`:
`OpenLobbyChannel`, `CreateMatch`, `GetAvailableMatches`, `JoinMatch`,
`ChangeTurnDuration`, `StartSystemMatch` y `CancelMatch`.
La unión del segundo participante inicia la partida; no se agrega otro botón remoto
de inicio humano que el flujo no necesita.

El tablero utiliza `IGameplayMatchService.OpenMatchChannel`, `GetMatchInformation`,
`GetMatchState`, `PlacePiece` y `ForfeitMatch`, junto con los callbacks de 14.6.
`MatchInformation` carga los datos base al entrar o reconectar; los retornos de jugada y
callbacks envían el estado dinámico completo, sin repetir participantes, asiento o duración.
La jugada del sistema, los vencimientos y el cierre por reglas son decisiones del servidor,
sin operaciones cliente para imponer su resultado.

La observación utiliza `ObserveMatch`, sin ocupar un asiento. `GetAvailableMatches` enumera
salas admisibles y no resuelve el descubrimiento de partidas observables en curso.
Ese descubrimiento y la recuperación de una sala propia tras desconexión conservan
las decisiones pendientes de 13.7 y 14.8; no se inventa una operación para cubrirlas.

El historial y la reproducción visual utilizan `GetMatchHistory`, `GetMatchDetails`
y `GetMoveHistory`. La representación de los fotogramas no vuelve autoritativo al
motor local ni habilita nuevas jugadas. Los invitados pueden jugar su partida según
13 y 14; observación e historial mantienen las restricciones de cuenta registrada de 14.3.

### 20.5 Chat

El panel utiliza `IMatchChatService.OpenChatChannel`, `SendChatMessage` y `GetChatHistory`,
junto con los callbacks de 15.5. Solo una cuenta registrada participante de la partida
en curso accede al chat. Invitados y observadores no adquieren ese permiso.
Un participante silenciado conserva lectura, pero no envío, durante la sanción.

### 20.6 Alcance de la integración

La captura, la localización y el estado visual de espera permanecen en el cliente.
Los casos de uso, los cambios persistidos y las decisiones de juego se trasladan al servidor.

Las firmas objetivo no usan `Task` ni sufijo `Async`. El cliente puede consumirlas mediante
operaciones asíncronas del proxy y esperar sin bloquear la interfaz, como permite 3.1.
Las pantallas no llaman directamente a EF para completar un flujo presentado como remoto.

## 21. Generación del proxy

### 21.1 Referencia existente

El cliente contiene una referencia generada para `IAccountAccessService` en
`Connected Services/AccountAccess/Reference.cs`, con namespace
`ReversiGameSystem.ServerProxy`. El consumidor remoto de login utiliza esa referencia;
la creación de cuenta todavía utiliza el runtime local.

La referencia ofrece métodos síncronos y métodos con `Task` y sufijo `Async` para las
operaciones de acceso. Las acciones SOAP actuales terminan en `RegisterAccount` y `Login`.
Por tanto, el nombre de un método C# del proxy no basta para identificar el protocolo
publicado ni demuestra que una firma nueva sea compatible.

| Elemento | Configuración actual |
| --- | --- |
| Endpoint del cliente | `AccountAccessEndpoint` |
| Dirección local | `net.tcp://localhost:8090/ReversiGameSystem/AccountAccess` |
| Binding | `netTcpBinding` |
| Seguridad de transporte | Credenciales de Windows con `EncryptAndSign`. |
| Configuración del cliente | `ReversiGameSystem.Client.Presentation/App.config` |
| Configuración del host | `ReversiGameSystem.Server.Host/App.config` |

El host configura el servicio de acceso y su endpoint de metadatos; esto no demuestra que
esté ejecutándose ni que los nueve servicios objetivo estén publicados.
La dirección `localhost` es de desarrollo. Un servidor compartido se selecciona por
configuración, con su dirección y condiciones de acceso de transporte.
Las credenciales de Windows del canal no sustituyen el token ni los permisos de aplicación.

### 21.2 Regeneración e integración objetivo

La referencia se genera desde metadatos del contrato que realmente publique el host.
Antes de regenerarla se concilian interfaz, adaptador, host, faults y reglas de versión.
No se genera contra el catálogo Markdown ni se adapta el archivo generado a mano.

- Mantener desactivada la reutilización de tipos de ensamblados referenciados, para evitar
  compartir accidentalmente modelos del servidor. Es un requisito de regeneración;
  no se presenta como una opción histórica comprobada de la herramienta.
- Conservar un namespace generado que identifique al proxy.
- Permitir métodos asíncronos de consumo cuando la herramienta los genere. Esa opción no
  obliga a publicar firmas con `Task` ni a bloquear el hilo de interfaz.
- Registrar las opciones necesarias para repetir la generación y revisar sus diferencias.
  La presencia actual de `Reference.cs` no acredita un procedimiento reproducible completo.
- Mantener dirección, binding, seguridad y límites en los archivos de configuración de los
  ejecutables, sin introducirlos en los objetos contractuales.
- Identificar el código generado para los analizadores y evitar cambios manuales en él.
- Actualizar consumidores y tratamiento de faults junto con la referencia; un cambio de
  nombre generado no sustituye la migración del comportamiento.

Una regeneración correcta solo acredita que se obtuvo una referencia. La integración exige
invocar el servicio hospedado desde el consumidor previsto, comprobar datos y faults y,
en servicios dúplex, verificar callbacks, autorización y recuperación del canal.
Los límites de imágenes, tableros, historial y mensajes deben ser coherentes en ambos extremos;
aumentar cuotas no sustituye la validación de contenido.

## 22. Reglas de versionamiento

Los nombres y namespaces contractuales, acciones SOAP, formas de mensajes, operaciones,
parámetros, retornos, faults y miembros serializados forman parte del protocolo.
Mover una clase entre proyectos o quitar `Async` de un identificador C# no basta para decidir
si cambió ese protocolo: se comparan los metadatos publicados y el consumidor real.

- Mantener estables los nombres, tipos y órdenes de `DataMember` publicados.
  No reordenar miembros ni reutilizar sus órdenes para otro significado.
- Agregar miembros opcionales al final con un orden nuevo solo tras comprobar que su ausencia
  conserva el significado para el consumidor existente.
- Tratar cambios de forma o tipo de parámetros, retornos y faults como cambios que requieren
  evaluación explícita de compatibilidad; no asumir que todos son renombrados internos.
- No retirar un miembro obligatorio de un contrato publicado sin una nueva versión.
  En particular, el `ServiceFault` objetivo de 17.3 no puede reemplazar silenciosamente
  al esquema que exige `IsTransient` en el proxy actual.
- Evaluar también cambios de valores `EnumMember`. Un cliente anterior puede rechazar un
  valor nuevo durante la deserialización, antes de alcanzar su lógica de selección.
  No basta con agregar un caso por defecto que suponga un valor ya deserializado.
- Distinguir la ausencia opcional documentada de una falla o un formato incompatible.
  No recuperar compatibilidad convirtiendo faults desconocidos en éxito, arrays vacíos
  o datos ficticios.
- Coordinar la publicación de contratos y adaptadores con la regeneración del proxy,
  la configuración y la actualización de sus consumidores.
- Conservar el esquema anterior mientras tenga consumidores admitidos que lo necesitan,
  o realizar un cambio coordinado que retire ese consumo. No se exige mantener dos versiones
  activas si el despliegue del proyecto puede actualizar ambos extremos conjuntamente.

Las versiones y sus nombres contractuales se concretan antes de publicar un cambio incompatible.
No se inventa ahora un namespace distinto por operación o por objeto. La sección 4 identifica
los namespaces vigentes; conservarlos no acredita compatibilidad con las formas nuevas.
El catálogo no afirma que ya exista una segunda versión hospedada.

Al implementar se verifican serialización, ausencia de miembros opcionales, incompatibilidades
relevantes y faults declarados, además de la comunicación WCF real. Los casos de uso críticos
incluyen escrituras con respuesta incierta y callbacks de los servicios que los utilicen.
Estas son verificaciones requeridas para la migración, no pruebas ejecutadas por editar
este documento.

## 23. Secuencia de implementación

### 23.1 Punto de partida comprobable

Existen el proyecto de contratos, el adaptador y el host iniciales de acceso a cuentas,
la referencia generada y el consumidor remoto de login. También existen contratos y
casos de uso de consulta de perfil y clasificación sin completar su integración WCF,
como distingue la matriz de 5.1.

Los flujos nuevos de verificación por código, la separación de acceso y cuentas, los
faults de negocio revisados y el resto de las firmas objetivo no se consideran integrados
por disponer de esos artefactos anteriores. La creación de cuenta del cliente y otros
flujos todavía tienen consumidores locales o prototipos.

Las pruebas existentes incluyen contratos y outcomes del diseño anterior y pruebas del
adaptador de acceso. Su presencia no verifica las firmas nuevas, el proxy `net.tcp`
configurado ni los callbacks objetivo. No se marcan etapas nuevas como completadas
a partir de pruebas que todavía no cubren esa frontera.

### 23.2 Trabajo pendiente por grupos funcionales

Cada grupo se completa con contrato, caso de uso, adaptador, configuración del host,
proxy y consumidor. La selección de datos y faults de este catálogo guía el trabajo;
no se migran primero todas las pantallas suponiendo que el servidor ya conserva su semántica.

1. Concretar las decisiones pendientes necesarias para el grupo que se vaya a integrar.
   Incluyen identidad de la versión incompatible, políticas de configuración de partida,
   identidad estable de invitaciones, recuperación y descubrimiento de partidas, vigencia
   del permiso de observación y límites de los historiales. Resolver un grupo no exige
   crear abstracciones para todos los demás.
2. Implementar las firmas y datos revisados del grupo en `Server.Contracts`, con faults
   declarados y mapeo explícito. Verificar su serialización y compatibilidad antes de
   publicar metadatos. Mantener la asincronía real de persistencia fuera de esas firmas.
3. Integrar `ISessionService` e `IAccountService`: solicitud y validación de códigos,
   apertura y cierre de sesiones, registro y cambio de contraseña. Conectar sus flujos
   del cliente y sus mensajes localizados; no conservar la autenticación directa anterior
   como si ya cumpliera `CompleteLogin`. Recuperación de contraseña y desactivación
   mantienen su estado `Deferred`.
4. Integrar `IPlayerProfileService`, `IPlayerPreferencesService` e `ILeaderboardService`,
   reutilizando el comportamiento de servidor válido y completando autorización,
   imágenes, preferencias y presentación de ausencias normales.
5. Integrar `IFriendshipService`, incluida la identidad de invitación, sus cambios
   persistidos, presencia, callbacks y recuperación mediante consultas.
6. Integrar `IMatchLobbyService` e `IGameplayMatchService`, con autoridad de reglas,
   reloj, revisiones, concurrencia, cierre y persistencia en el servidor. Cubrir jugador
   registrado, invitado y observador según sus permisos, sin trasladar decisiones al cliente
   para completar temporalmente una operación remota.
7. Integrar `IMatchChatService`, con identidad estable de mensajes, autorización,
   moderación por cuenta, persistencia de sanciones y recuperación de la conversación.
8. Retirar cada consumidor local cuando su reemplazo remoto esté integrado. Eliminar
   `ReversiGameSystem.LocalRuntime` y los prototipos que ya no tengan consumidores;
   no retirarlos por haber generado contratos o un proxy.
9. Verificar cada grupo desde el consumidor y el host previstos antes de dar por terminada
   su migración. Registrar resultados reales y actualizar el estado correspondiente con
   autorización del equipo; no convertir una revisión documental en evidencia de ejecución.

La verificación de cada grupo acompaña su implementación; el último punto no pospone
todas las pruebas hasta el final. Para los servicios dúplex incluye pérdidas de canal,
consultas de recuperación, fallas de callback y conservación de escrituras confirmadas.
Para llamadas mutantes incluye impedir reenvíos automáticos ante una respuesta incierta.

### 23.3 Estado de este catálogo

La revisión documental abarca las 23 secciones y mantiene los criterios acordados de
parámetros proporcionales, retornos directos y faults tipados. Las decisiones de integración
expresamente pendientes siguen visibles en su sección; no se rellenan con mecanismos
genéricos sin una necesidad definida.

La revisión académica 4 inicia una nueva pasada, con las primeras correcciones indicadas
en 5.2. La evaluación de responsabilidades de gameplay, recuperación pendiente y transferencia
de imágenes continúa abierta. Las observaciones sobre capas, repositorios, BD y modificaciones
del estándar no se consideran implementadas por editar este catálogo.

Este documento no acredita compilación, pruebas de ejecución, metadatos regenerados ni
migración de pantallas. Los cambios del diseño objetivo deberán reflejarse de forma
coordinada en el código y su verificación antes de cambiar los estados de implementación.

