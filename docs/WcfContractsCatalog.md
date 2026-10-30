# Catálogo de contratos WCF

Equipo 12

Pedro Enrique Sánchez Rodríguez
César Daniel Ortega Castillejos

## 1. Identificación contractual

| Categoría                        | Namespace                    |
| -------------------------------- | ---------------------------- |
| Servicios y callbacks            | `urn:reversi:services:v1`    |
| Datos, enums y faults de negocio | `urn:reversi:contracts:v1`   |
| `ServiceFault` y correlación     | `urn:reversi:diagnostics:v1` |

- Servicios: `ServiceContract`; operaciones con respuesta mediante `OperationContract`.
- Callbacks: `OperationContract(IsOneWay = true)`, retorno `void` y sin faults de respuesta.
- Datos: `DataContract` y `DataMember`; valores de enums: `EnumMember`.
- Miembros obligatorios salvo tipos con `?`; arrays no nulos y vacíos cuando no hay elementos.
- Instantes en UTC; coordenadas del tablero de 0 a 7; asientos 1 y 2.
- Encabezado SOAP `CorrelationId`: `string` con GUID en formato `D`, namespace de diagnósticos.

## 2. Contratos de servicio

### 2.1. `ISessionService`

**Descripción:** Administra verificación de acceso, sesiones registradas y sesiones de invitado.

**Es callback:** No.
**Callback asociado:** No.

**Firmas:**

```csharp
DateTime RequestLoginCode(string loginIdentifier, string password);

AuthenticatedSession CompleteLogin(
    string loginIdentifier,
    string verificationCode);

GuestSession OpenGuestSession(string displayName);

void CloseSession(string sessionToken);
```

**Datos:** `AuthenticatedSession`, `GuestSession`, `AccountReference`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- `RequestLoginCode` devuelve la caducidad UTC del código; no devuelve el código.
- `CompleteLogin` crea la sesión tras validar el código.
- `CloseSession` es idempotente para sesiones inexistentes, caducadas o revocadas; el token ausente
  o vacío se rechaza.

### 2.2. `IAccountService`

**Descripción:** Registra cuentas y administra contraseñas y desactivación.

**Es callback:** No.
**Callback asociado:** No.

**Firmas:**

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

**Datos:** `NewAccount`, `AccountReference`, `ImageContent`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| `RequestRegistrationCode`, `CompleteRegistration`, `ResetPassword` | `AccountFault` |
| `ChangePassword`, `DeactivateAccount` | `AccountFault`, `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- `RequestRegistrationCode` devuelve la caducidad UTC del código; `CompleteRegistration` devuelve la
  cuenta creada.
- `ChangePassword` y `DeactivateAccount` requieren sesión de la cuenta titular.
- `ResetPassword` y `DeactivateAccount`: alcance diferido.

### 2.3. `IPlayerProfileService`

**Descripción:** Consulta el perfil público y modifica nombre y foto de perfil.

**Es callback:** No.
**Callback asociado:** No.

**Firmas:**

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

**Datos:** `PlayerProfile`, `PlayerStatistics`, `AccountReference`, `ImageContent`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `ProfileFault`, `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Acceso con cuenta registrada activa.
- `targetAccountId = null`: perfil propio; `GetProfileImage` devuelve `null` si no hay foto.

### 2.4. `IPlayerPreferencesService`

**Descripción:** Consulta y modifica preferencias visuales, idioma, zona horaria y fondo.

**Es callback:** No.
**Callback asociado:** No.

**Firmas:**

```csharp
PlayerPreferences GetPlayerPreferences(string sessionToken);

void UpdatePlayerPreferences(
    string sessionToken,
    PlayerPreferences preferences);

ImageContent? GetBoardBackground(string sessionToken);

void ChangeBoardBackground(string sessionToken, ImageContent image);
```

**Datos:** `PlayerPreferences`, `ImageContent`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `PreferencesFault`, `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Acceso con cuenta registrada activa; `GetBoardBackground` devuelve `null` si no existe imagen.
- `ChangeBoardBackground` guarda la imagen y activa su uso; desactivar su uso no elimina la imagen.

### 2.5. `ILeaderboardService`

**Descripción:** Consulta la clasificación de jugadores.

**Es callback:** No.
**Callback asociado:** No.

**Firmas:**

```csharp
LeaderboardEntry[] GetLeaderboard(int maximumEntries);
```

**Datos:** `LeaderboardEntry`, `AccountReference`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| `GetLeaderboard` | `LeaderboardFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Consulta sin token de sesión; `maximumEntries` entre 1 y 100 inclusive.
- Orden: victorias descendentes, proporción de victorias descendente y nombre ascendente sin
  distinguir mayúsculas.

### 2.6. `IFriendshipService`

**Descripción:** Busca jugadores y administra solicitudes, amistades y presencia.

**Es callback:** No.
**Callback asociado:** `IFriendshipServiceCallback`.

**Firmas:**

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

**Datos:** `PlayerSummary`, `FriendRequest`, `FriendSummary`, `AccountReference`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `FriendshipFault`, `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Acceso con cuenta registrada activa; `searchText` vacío permite listar jugadores según la búsqueda
  definida.
- `GetPendingFriendRequests` devuelve solicitudes recibidas pendientes.
- `RespondToFriendRequest`: `accept = true` acepta; `false` rechaza.
- `RemoveFriend` desactiva la relación sin borrar su historia.

#### `IFriendshipServiceCallback`

**Descripción:** Notifica solicitudes recibidas, respuestas y cambios de amistad.
**Es callback:** Sí; one-way, sin faults de respuesta.

```csharp
void OnFriendRequestReceived(FriendRequest request);

void OnFriendRequestResponded(string requestId, bool accepted);

void OnFriendChanged(long accountId, FriendChange change);
```

### 2.7. `IMatchLobbyService`

**Descripción:** Crea salas, permite la unión y administra el inicio o cancelación de partidas.

**Es callback:** No.
**Callback asociado:** `IMatchLobbyServiceCallback`.

**Firmas:**

```csharp
void OpenLobbyChannel(string sessionToken);

LobbySnapshot CreateMatch(
    string sessionToken,
    MatchConfiguration configuration);

// Ampliación pendiente de implementación.
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

**Datos:** `MatchConfiguration`, `AvailableMatch`, `LobbySnapshot`, `MatchParticipantSummary`,
`MatchStateSnapshot`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `MatchFault`, `SessionFault` |
| `InviteToMatch` (pendiente) | Además, `FriendshipFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Acceso objetivo con cuenta registrada activa o sesión de invitado; `InviteToMatch`
  requiere cuenta registrada activa.
- `JoinMatch` recibe el código de sala e inicia la partida con el segundo participante.
- `ChangeTurnDuration`, `StartSystemMatch` y `CancelMatch`: creador de una sala en `Waiting`.
- `CancelMatch` es idempotente para el cierre ya confirmado por el mismo creador.

**Ampliación pendiente: `InviteToMatch`.** Firma aún ausente del código y del proxy.

- Crea una partida humana privada en `Waiting`, con el creador en el asiento 1 y el
  asiento 2 reservado para el amigo indicado. Devuelve `LobbySnapshot` tras la aceptación
  del correo por el proveedor, si la partida sigue esperando; no garantiza entrega al buzón.
- Requiere amistad aceptada y vigente y cuentas activas y distintas. No requiere que el
  destinatario esté conectado. El servidor resuelve su correo registrado.
- Reutiliza `MatchConfiguration` y `LobbySnapshot`; no incorpora datos de privacidad ni
  correo a estos contratos. `CreateMatch` conserva la creación pública.
- Las partidas privadas quedan excluidas de `GetAvailableMatches`, también para el
  destinatario. `JoinMatch` conserva su firma y exige la cuenta reservada, además del código.
- Código válido solo mientras el creador mantiene esa espera; sin vencimiento temporal
  independiente. Cancelación, abandono, desconexión detectada, invalidez de la sesión
  creadora o reinicio del host cancelan la espera. Otra instancia del creador no la prolonga.
- La unión confirmada inicia la partida e invalida el código; reutiliza los callbacks existentes.
- Correo con creador, código e instrucciones: «Puedes unirte con este código mientras el
  creador siga esperando». No utiliza los códigos de verificación de cuenta.
- Rechazos de `InviteToMatch`: `MatchFault` con `Unauthorized` para invitado o destinatario
  propio, `InvalidConfiguration`, `InvalidTurnDuration`, `InactiveAccount` o `MatchNotWaiting`;
  `FriendshipFault` con `InvalidAccountId`, `AccountUnavailable` o `FriendshipNotFound`;
  `SessionFault` con `MissingSessionToken` o `InvalidSession`, según corresponda.
- Unión de un tercero: `MatchFault.Unauthorized`; código cancelado o usado:
  `MatchFault.MatchCodeUnavailable`. Un rechazo no ocupa asiento ni altera el estado.
- Fallo técnico o de correo: `ServiceFault`; se cancela la preparación y se invalida el
  código. La compensación fallida y el resultado incierto tras timeout requieren resolución
  antes de implementar. No hay reenvío automático ni confirmación visual anticipada.

Especificación interna completa y decisiones técnicas pendientes:
[catálogo de integración, sección 13.8](WcfIntegrationContracts.md#138-ampliación-pendiente-invitación-privada-por-correo).

#### `IMatchLobbyServiceCallback`

**Descripción:** Notifica inicio y cancelación de la sala.
**Es callback:** Sí; one-way, sin faults de respuesta.

```csharp
void OnMatchStarted(MatchStateSnapshot state);

void OnLobbyClosed(long matchId);
```

### 2.8. `IGameplayMatchService`

**Descripción:** Controla jugadas y consulta información, estado e historial de partidas.

**Es callback:** No.
**Callback asociado:** `IGameplayMatchServiceCallback`.

**Firmas:**

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

**Datos:** `PiecePlacement`, `MatchInformation`, `MatchStateSnapshot`, `MoveRecord`,
`MatchConclusion`, `MatchSummary`, `MatchDetails`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| Todas | `MatchFault`, `SessionFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Participantes registrados e invitados pueden actuar sobre su propia partida.
- Observación, historial, detalle y movimientos requieren cuenta registrada activa y autorización de
  lectura.
- `GetMatchInformation` y `GetMatchState` admiten participantes u observadores autorizados.
- Carga inicial: `OpenMatchChannel`, `GetMatchInformation`, `GetMatchState`.
- `ObserveMatch` registra la observación y devuelve estado; la información base se consulta aparte.
- `PlacePiece` valida `ExpectedMatchStateVersion`; pases, vencimientos y resultado los resuelve el
  servidor.
- Consultas y callbacks se concilian por `MatchId` y `MatchStateVersion`.

#### `IGameplayMatchServiceCallback`

**Descripción:** Notifica estado de partida y conexión de participantes.
**Es callback:** Sí; one-way, sin faults de respuesta.

```csharp
void OnMatchStateChanged(MatchStateSnapshot state);

void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected);
```

### 2.9. `IMatchChatService`

**Descripción:** Envía y consulta mensajes y comunica silenciamientos.

**Es callback:** No.
**Callback asociado:** `IMatchChatServiceCallback`.

**Firmas:**

```csharp
void OpenChatChannel(string sessionToken, long matchId);

MatchChatEntry SendChatMessage(
    string sessionToken,
    long matchId,
    string content);

MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
```

**Datos:** `MatchChatEntry`, `ChatMuteNotification`.

**Faults de negocio por operación:**

| Operación | Detalle del fault |
| --- | --- |
| `OpenChatChannel`, `GetChatHistory` | `SessionFault`, `MatchFault` |
| `SendChatMessage` | `ChatFault`, `SessionFault`, `MatchFault` |

**Falla técnica:** `FaultException<ServiceFault>` en todas las operaciones.

**Condiciones contractuales:**

- Acceso exclusivo de cuentas registradas activas participantes de una partida en `InProgress`.
- Texto recortado: de 1 a 200 unidades de código UTF-16.
- Tres o más infracciones en 24 horas aplican un silenciamiento de 10 minutos por cuenta.
- Una cuenta silenciada puede consultar mensajes; sus intentos de envío no amplían la sanción ni
  suman infracciones.
- Carga: registrar canal, consultar historial y combinar mensajes por `(MatchId, MessageNumber)`.

#### `IMatchChatServiceCallback`

**Descripción:** Notifica mensajes confirmados y silenciamientos de la cuenta.
**Es callback:** Sí; one-way, sin faults de respuesta.

```csharp
void OnChatMessageReceived(MatchChatEntry message);

void OnPlayerMuted(ChatMuteNotification mute);
```

## 3. Contratos de datos

### 3.1. `AccountReference`

Referencia pública de cuenta registrada.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `AccountId` | `long` | Identificador estable de la cuenta. |
| `Username` | `string` | Nombre público de la cuenta. |

### 3.2. `AuthenticatedSession`

Datos privados de la sesión de la cuenta titular.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `SessionToken` | `string` | Token opaco emitido por el servidor. |
| `Account` | `AccountReference` | Identidad pública de la cuenta autenticada. |
| `EmailAddress` | `string` | Correo de la cuenta, entregado solo a su titular. |
| `PreferredLocaleCode` | `string` | Idioma preferido de la cuenta. |
| `ExpiresAtUtc` | `DateTime` | Caducidad de la sesión en UTC. |

### 3.3. `GuestSession`

Sesión de invitado con nombre confirmado por el servidor.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `SessionToken` | `string` | Token opaco de la sesión de invitado. |
| `DisplayName` | `string` | Nombre definitivo con el prefijo `Guest-`. |
| `ExpiresAtUtc` | `DateTime` | Caducidad de la sesión en UTC. |

### 3.4. `NewAccount`

Datos para completar un registro verificado.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `VerificationCode` | `string` | Código de registro. |
| `Username` | `string` | Nombre público. |
| `EmailAddress` | `string` | Correo de registro. |
| `Password` | `string` | Contraseña inicial. |
| `PreferredLocaleCode` | `string` | Código de idioma preferido. |
| `ProfileImage` | `ImageContent?` | Foto opcional. |

### 3.5. `ImageContent`

PNG o JPEG; máximo 5 MiB y dimensiones de 1 a 4096 píxeles por eje.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MediaType` | `string` | `image/png` o `image/jpeg`. |
| `Content` | `byte[]` | Contenido binario. |

### 3.6. `PlayerProfile`

Identidad pública, disponibilidad de foto y estadísticas.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta. |
| `HasProfileImage` | `bool` | Indica si existe una foto personalizada. |
| `Statistics` | `PlayerStatistics` | Estadísticas de partidas terminadas. |

### 3.7. `PlayerStatistics`

Partidas terminadas contabilizadas; `WinRate` entre 0 y 1, cero si no hay partidas.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchesPlayed` | `int` | Total de partidas terminadas contabilizadas. |
| `Wins` | `int` | Partidas ganadas. |
| `Losses` | `int` | Partidas perdidas. |
| `Draws` | `int` | Partidas empatadas. |
| `WinRate` | `double` | Proporción de victorias entre 0 y 1. |

### 3.8. `PlayerPreferences`

`PreferredLocaleCode`: `es-MX` o `en-US`. `TimeZoneId = null`: zona local; cadena vacía no válida.
`PieceStyleCode`: `STANDARD` o `PROFILE_PICTURES`. El uso del fondo es independiente de la
existencia de su imagen.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `PreferredLocaleCode` | `string` | Código de idioma preferido. |
| `TimeZoneId` | `string?` | Zona personalizada opcional. |
| `PieceStyleCode` | `string` | Código de estilo de piezas. |
| `UseCustomBoardBackground` | `bool` | Elección de utilizar el fondo personalizado. |

### 3.9. `LeaderboardEntry`

Entrada de clasificación; `Position` comienza en 1 y `WinRate` está entre 0 y 1.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Position` | `int` | Posición consecutiva dentro de la clasificación ordenada. |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta. |
| `HasProfileImage` | `bool` | Indica si existe una foto personalizada. |
| `MatchesPlayed` | `int` | Total de partidas terminadas contabilizadas. |
| `Wins` | `int` | Victorias contabilizadas. |
| `WinRate` | `double` | Proporción de victorias entre 0 y 1. |

### 3.10. `PlayerSummary`

Jugador registrado encontrado en una búsqueda.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos de la cuenta encontrada. |
| `HasProfileImage` | `bool` | Indica si tiene una foto personalizada. |

### 3.11. `FriendRequest`

Solicitud pendiente con identidad estable no reutilizable.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `RequestId` | `string` | Identificador opaco, estable y no vacío de esa solicitud. |
| `Sender` | `AccountReference` | Cuenta remitente. |
| `Recipient` | `AccountReference` | Cuenta destinataria. |
| `CreatedAtUtc` | `DateTime` | Fecha de creación asignada por el servidor, en UTC. |

### 3.12. `FriendSummary`

Identidad y presencia de una cuenta amiga.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Account` | `AccountReference` | Identificador y nombre públicos del amigo. |
| `HasProfileImage` | `bool` | Indica si tiene una foto personalizada. |
| `IsOnline` | `bool` | Presencia conocida por el servidor al consultar. |

### 3.13. `MatchConfiguration`

Duración por turno entre 10 y 120 segundos inclusive.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `TurnDurationSeconds` | `int` | Duración por turno, de 10 a 120 segundos. |
| `OpponentKind` | `OpponentKind` | Tipo de oponente propuesto o confirmado. |
| `CountsTowardStatistics` | `bool` | Elegibilidad estadística propuesta o confirmada. |

### 3.14. `AvailableMatch`

Sala disponible; el código temporal permite solicitar la unión.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchCode` | `string` | Código temporal que la selección envía a `JoinMatch`. |
| `CreatorDisplayName` | `string` | Nombre visible del creador. |
| `TurnDurationSeconds` | `int` | Duración por turno que muestra la lista. |

### 3.15. `LobbySnapshot`

`MatchCode = null` al iniciar la partida. El asiento 1 corresponde al creador.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `MatchCode` | `string?` | Código de unión mientras esté disponible para los participantes. |
| `Participants` | `MatchParticipantSummary[]` | Participantes registrados en la sala o partida. |
| `Configuration` | `MatchConfiguration` | Configuración aceptada por el servidor. |
| `State` | `MatchState` | Estado actual confirmado por el servidor. |

### 3.16. `MatchParticipantSummary`

`AccountId` solo para `Registered`; asiento 1 negras y asiento 2 blancas.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `SeatNumber` | `int` | Asiento 1 o 2 dentro de la partida. |
| `Kind` | `ParticipantKind` | Participante registrado, invitado o sistema. |
| `AccountId` | `long?` | Cuenta registrada, cuando corresponda. |
| `DisplayName` | `string` | Nombre visible confirmado por el servidor. |

### 3.17. `PiecePlacement`

Fila y columna de 0 a 7; `ExpectedMatchStateVersion` no negativa.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Partida en la que se solicita colocar la ficha. |
| `Row` | `int` | Fila de 0 a 7. |
| `Column` | `int` | Columna de 0 a 7. |
| `ExpectedMatchStateVersion` | `long` | Versión no negativa del estado visto por el cliente. |

### 3.18. `MatchInformation`

Datos base por destinatario. `AssignedSeatNumber = null` para observadores; participantes, asientos
y duración fijos al comenzar.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `Participants` | `MatchParticipantSummary[]` | Participantes y asientos; sección 3.16. |
| `AssignedSeatNumber` | `int?` | Asiento asignado al destinatario; `null` para un observador. |
| `TurnDurationSeconds` | `int` | Duración configurada por turno. |

### 3.19. `MatchStateSnapshot`

Estado dinámico completo. `Board` contiene 64 casillas en orden por filas: `row * 8 + column`.
`MatchStateVersion` no negativa. Al terminar, turno e inicio son `null` y existe `Conclusion`. Sin
colocación en la transición, `LastMove = null`. Las posiciones legales se calculan en el cliente.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Identificador estable de la partida. |
| `MatchStateVersion` | `long` | Versión confirmada del estado de la partida. |
| `State` | `MatchState` | Estado de la partida; sección 4. |
| `Board` | `BoardCellState[]` | Las 64 casillas, ordenadas por filas. |
| `BlackPieceCount` | `int` | Conteo de fichas negras confirmado por el servidor. |
| `WhitePieceCount` | `int` | Conteo de fichas blancas confirmado por el servidor. |
| `CurrentTurnSeatNumber` | `int?` | Asiento que tiene el turno, si hay uno activo. |
| `TurnStartedAtUtc` | `DateTime?` | Comienzo UTC del turno activo. |
| `LastMove` | `MoveRecord?` | Colocación confirmada en la transición de esta revisión. |
| `SkippedSeatNumber` | `int?` | Asiento cuyo turno se pasó en esa transición. |
| `Conclusion` | `MatchConclusion?` | Conclusión confirmada cuando la partida termina. |

### 3.20. `MoveRecord`

Colocación confirmada; `MoveNumber` comienza en 1 y `FlippedPieceCount` excluye la ficha colocada.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MoveNumber` | `int` | Número consecutivo del movimiento, empezando en 1. |
| `SeatNumber` | `int` | Asiento del participante que colocó la ficha. |
| `Row` | `int` | Fila de la colocación. |
| `Column` | `int` | Columna de la colocación. |
| `PlayedAtUtc` | `DateTime` | Hora UTC confirmada por el servidor. |
| `FlippedPieceCount` | `int` | Fichas rivales volteadas, sin contar la colocada. |
| `Origin` | `MoveOrigin` | `Manual`, `Timeout` o `System`. |

### 3.21. `MatchConclusion`

`WinnerSeatNumber = null` en empate. `AbandoningSeatNumber` solo cuando corresponde a abandono.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Partida concluida. |
| `MatchStateVersion` | `long` | Versión del estado final confirmado de la partida. |
| `Reason` | `MatchEndReason` | Motivo de finalización. |
| `WinnerSeatNumber` | `int?` | Asiento ganador; `null` en empate. |
| `AbandoningSeatNumber` | `int?` | Asiento que abandonó, cuando ese sea el motivo. |
| `BlackPieceCount` | `int` | Conteo final de negras. |
| `WhitePieceCount` | `int` | Conteo final de blancas. |
| `EndedAtUtc` | `DateTime` | Fecha y hora UTC de conclusión. |

### 3.22. `MatchSummary`

Partida en curso o terminada del historial propio; `Conclusion` solo al terminar.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Identificador para consultar el detalle. |
| `Participants` | `MatchParticipantSummary[]` | Participantes de la partida. |
| `AssignedSeatNumber` | `int` | Asiento asignado a la cuenta titular del historial. |
| `State` | `MatchState` | Partida en curso o terminada. |
| `RecordedAtUtc` | `DateTime` | Fecha de registro de la partida para ordenar y mostrar. |
| `BlackPieceCount` | `int` | Conteo actual o final de negras. |
| `WhitePieceCount` | `int` | Conteo actual o final de blancas. |
| `Conclusion` | `MatchConclusion?` | Resultado confirmado, solo cuando terminó. |

### 3.23. `MatchDetails`

`BoardFrames[0]` es el tablero inicial; el cuadro `n` sigue a la colocación `n`. Hasta 60 jugadas y
61 cuadros de 64 casillas.

| Miembro | Tipo | Información |
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

### 3.24. `MatchChatEntry`

Identidad compuesta `(MatchId, MessageNumber)`; contenido recortado de 1 a 200 unidades UTF-16.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Partida a la que pertenece el mensaje. |
| `MessageNumber` | `int` | Número positivo y estable del mensaje dentro de la partida. |
| `SenderSeatNumber` | `int` | Asiento 1 o 2 del emisor registrado. |
| `Content` | `string` | Texto recortado y aceptado por el servidor. |
| `SentAtUtc` | `DateTime` | Hora UTC de almacenamiento confirmada por el servidor. |

### 3.25. `ChatMuteNotification`

Silenciamiento de cuenta. `ReasonCode = REPEATED_VIOLATIONS`; `MatchId` identifica el contexto de
entrega.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `MatchId` | `long` | Contexto de chat al que se entrega la notificación. |
| `ReasonCode` | `string` | Razón estable de la sanción: `REPEATED_VIOLATIONS`. |
| `StartsAtUtc` | `DateTime` | Comienzo UTC del silenciamiento confirmado. |
| `EndsAtUtc` | `DateTime` | Fin UTC del silenciamiento confirmado. |

## 4. Enumeraciones de dominio

**`OpponentKind`:** `Human`, `System`.

**`ParticipantKind`:** `Registered`, `Guest`, `System`.

**`MatchState`:** `Waiting`, `InProgress`, `Finished`, `Cancelled`.

**`BoardCellState`:** `Empty`, `Black`, `White`.

**`MoveOrigin`:** `Manual`, `Timeout`, `System`.

**`MatchEndReason`:** `NoLegalMoves`, `Inactivity`, `Abandonment`.

**`FriendChange`:** `Connected`, `Disconnected`, `Removed`.

`NoLegalMoves`: ninguno de los participantes puede jugar, incluso si quedan casillas vacías.

## 5. Fault contracts y excepciones

Cada detalle se declara mediante `FaultContract(typeof(TFault))` y se comunica como
`FaultException<TFault>`.

### 5.1. `SessionFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `SessionFailure` | Motivo de rechazo. |

**Valores de `SessionFailure`:**

| Valor | Significado |
| --- | --- |
| `InvalidCredentials` | Cuenta o contraseña incorrectas, sin distinguir la causa. |
| `InactiveAccount` | La cuenta no está activa. |
| `InvalidVerificationCode` | Código incorrecto, consumido o sin proceso autorizado. |
| `ExpiredVerificationCode` | El código del proceso ha caducado. |
| `InvalidGuestName` | Nombre de invitado no válido. |
| `GuestNameUnavailable` | Nombre de invitado no disponible. |
| `MissingSessionToken` | Token ausente o vacío. |
| `InvalidSession` | Sesión no válida o de invitado donde se exige cuenta registrada. |

### 5.2. `AccountFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `AccountFailure` | Motivo de rechazo. |

**Valores de `AccountFailure`:**

| Valor | Significado |
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

### 5.3. `ProfileFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `ProfileFailure` | Motivo de rechazo. |

**Valores de `ProfileFailure`:**

| Valor | Significado |
| --- | --- |
| `InvalidAccountId` | El identificador de la cuenta destino no es positivo. |
| `AccountNotFound` | La cuenta consultada no existe. |
| `InvalidUsername` | El nombre propuesto no cumple la política. |
| `UsernameUnavailable` | El nombre corresponde a otra cuenta. |
| `InvalidImage` | Imagen ausente o con contenido, formato o dimensiones inválidos. |
| `InactiveAccount` | La cuenta solicitante ya no está activa. |

### 5.4. `PreferencesFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `PreferencesFailure` | Motivo de rechazo. |

**Valores de `PreferencesFailure`:**

| Valor | Significado |
| --- | --- |
| `MissingPreferences` | El objeto de preferencias está ausente. |
| `InvalidLocale` | El idioma está ausente o no está admitido. |
| `InvalidTimeZone` | La zona horaria indicada está vacía o no es reconocida. |
| `InvalidPieceStyle` | El estilo está ausente o no está admitido. |
| `InvalidImage` | Imagen ausente o con formato, tamaño o contenido inválidos. |
| `InactiveAccount` | La cuenta titular ya no está activa. |

### 5.5. `LeaderboardFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `LeaderboardFailure` | Motivo de rechazo. |

**Valores de `LeaderboardFailure`:**

| Valor | Significado |
| --- | --- |
| `InvalidMaximumEntries` | El límite no está entre 1 y 100 inclusive. |

### 5.6. `FriendshipFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `FriendshipFailure` | Motivo de rechazo. |

**Valores de `FriendshipFailure`:**

| Valor | Significado |
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

### 5.7. `MatchFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `MatchFailure` | Motivo de rechazo. |

**Valores de `MatchFailure`:**

| Valor | Significado |
| --- | --- |
| `InvalidMatchId` | El identificador de partida no es positivo. |
| `InvalidConfiguration` | Configuración ausente o con opciones no admitidas. |
| `InvalidTurnDuration` | La duración no está entre 10 y 120 segundos. |
| `MatchNotFound` | La partida indicada no existe. |
| `CannotJoinOwnMatch` | El solicitante intenta unirse a su propia sala. |
| `InvalidMatchCode` | El código no cumple el formato. |
| `MatchCodeUnavailable` | El código no identifica una sala actualmente admisible. |
| `Unauthorized` | La sesión no tiene el rol requerido; la ampliación privada incluye a quien no es destinatario reservado o intenta invitarse a sí mismo. |
| `MatchNotWaiting` | La partida ya no espera y no admite la acción de sala solicitada. |
| `InactiveAccount` | La cuenta registrada solicitante ya no está activa. |
| `MissingPlacement` | El objeto de colocación está ausente. |
| `InvalidMatchStateVersion` | La versión del estado enviada es negativa. |
| `StaleMatchStateVersion` | La versión enviada difiere de la confirmada. |
| `MatchNotInProgress` | El estado no admite la acción de juego solicitada. |
| `NotPlayersTurn` | El participante no tiene el turno. |
| `InvalidPosition` | Casilla fuera del tablero, ocupada o sin captura legal. |
| `SpectatingUnavailable` | No se puede iniciar la observación en el estado solicitado. |

### 5.8. `ChatFault`

| Miembro | Tipo | Información |
| --- | --- | --- |
| `Reason` | `ChatFailure` | Motivo de rechazo. |
| `MuteEndsAtUtc` | `DateTime?` | Requerido para `PlayerMuted`; `null` en los demás motivos. |

**Valores de `ChatFailure`:**

| Valor | Significado |
| --- | --- |
| `EmptyMessage` | El contenido está ausente o queda vacío después de recortar espacios. |
| `MessageTooLong` | El texto recortado excede las 200 unidades de código UTF-16 permitidas. |
| `MessageBlocked` | La moderación rechaza el contenido sin aplicar un nuevo silenciamiento. |
| `PlayerMuted` | Hay un silenciamiento vigente, incluido el recién aplicado por infracciones. |

### 5.9. `ServiceFault`

Falla técnica controlada del servidor.

| Miembro | Tipo | Información |
| --- | --- | --- |
| `FailureCode` | `string` | Código técnico estable y no localizado. |
| `CorrelationId` | `string` | Relaciona el fault con el diagnóstico de esa invocación. |

### 5.10. Excepciones locales de comunicación

| Excepción | Condición |
| --- | --- |
| `CommunicationException` | Falla del canal o transporte. |
| `TimeoutException` | Tiempo de espera excedido. |
| `SerializationException` | Falla de serialización observada por el cliente. |
| `InvalidOperationException` | Estado o configuración local del proxy no válido. |
