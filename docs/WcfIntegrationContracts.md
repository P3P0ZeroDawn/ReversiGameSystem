# Catálogo de contratos para la integración WCF

## 1. Propósito

Este documento define y mantiene la frontera de comunicación entre el cliente de escritorio
y el servidor de Reversi. Su objetivo es fijar los servicios, operaciones, callbacks y objetos
serializados que forman o formarán la API WCF del sistema, y registrar el avance de su
integración.

El servidor es el propietario del código fuente de los contratos. El cliente no referencia
el ensamblado `ReversiGameSystem.Server.Contracts`; consume una referencia de servicio
generada a partir de los metadatos publicados por WCF. Por ello, los tipos generados en el
cliente son representaciones del contrato remoto y no un proyecto compartido.

Este documento es una especificación de arquitectura. No afirma que todas las operaciones
descritas ya estén implementadas.

## 2. Alcance y principios

- El servidor concentra las reglas de negocio, la administración de sesiones, la persistencia
  y el estado autoritativo de las partidas.
- El cliente conserva la presentación, la navegación, la localización, la captura de entradas
  y una validación preventiva mínima antes de invocar al servidor.
- Ninguna entidad generada por Entity Framework puede aparecer en una firma WCF.
- Los objetos de comunicación usan nombres relacionados con el dominio y no los sufijos
  `Request`, `Response` o `Dto`.
- Los errores esperados se representan mediante outcomes tipados.
- Los errores técnicos inesperados se convierten en un `ServiceFault` controlado.
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
- `Task` y el sufijo `Async` se usan únicamente cuando existe espera real de red, base de
  datos u otro recurso con una API asíncrona. Las operaciones en memoria y las APIs de E/S
  implementadas de forma síncrona no fabrican tareas con `Task.FromResult` o
  `Task.CompletedTask`.

## 3. Tipos de contrato

### 3.1 Service contract

Una interfaz marcada con `[ServiceContract]` define operaciones que el cliente puede invocar
en el servidor. Cada método remoto se marca con `[OperationContract]`.

Las operaciones de este catálogo reciben un único data contract de entrada. Este criterio
mantiene firmas estables cuando una operación necesita agregar datos y evita publicar listas
de parámetros primitivos.

Una operación devuelve `Task<TOutput>` y usa el sufijo `Async` cuando su implementación
espera trabajo asíncrono real. Una operación puramente síncrona devuelve `TOutput` y no usa
ese sufijo. El tipo indicado en la columna **Salida** corresponde al resultado lógico y no
obliga por sí mismo a envolverlo en `Task`.

### 3.2 Callback contract

Una interfaz callback permite que el servidor notifique al cliente sin esperar una nueva
consulta. Los callbacks de este catálogo no devuelven valores y usan
`[OperationContract(IsOneWay = true)]`.

Por tanto, cada callback tiene una firma
`void Method(TNotification notification)` con un único data contract de notificación.

`IsOneWay = true` significa que no existe una respuesta contractual para la operación. No
garantiza por sí mismo la entrega, la ejecución inmediata ni la ausencia absoluta de bloqueos
de transporte.

### 3.3 Data contract

Una clase o enumeración marcada con `[DataContract]` representa información serializable que
cruza la frontera WCF. Sus miembros públicos de comunicación se declaran explícitamente con
`[DataMember]`.

Los data contracts no son entidades de persistencia ni objetos de interfaz gráfica. La API
de servicio del servidor se encarga de mapearlos a los modelos de aplicación y dominio.

## 4. Espacios de nombres contractuales

| Categoría | Namespace WCF |
| --- | --- |
| Servicios | `urn:reversi:services:v1` |
| Datos | `urn:reversi:contracts:v1` |
| Diagnósticos | `urn:reversi:diagnostics:v1` |

Los namespaces WCF forman parte de la compatibilidad del protocolo y no deben cambiarse solo
por una reorganización interna de namespaces de C#.

## 5. Estados de implementación

| Estado | Significado |
| --- | --- |
| `Implemented` | Ya existe un caso de uso funcional en la capa de servidor. |
| `Prototype migration` | Existe comportamiento local o prototipo que debe trasladarse al servidor. |
| `Planned` | La operación es necesaria, pero aún no tiene implementación completa. |
| `Deferred` | Se reserva en la arquitectura, pero no se implementará durante la separación inicial. |

El estado de una operación en las tablas siguientes describe la disponibilidad del caso de
uso en el servidor. No implica por sí mismo que ya exista un adaptador WCF, que el host lo
publique o que la pantalla correspondiente del cliente use el proxy.

### 5.1 Estado actual de integración

| Flujo | Contrato | Adaptador WCF | Endpoint publicado | Proxy generado | Cliente integrado |
| --- | --- | --- | --- | --- | --- |
| Crear cuenta | Sí | Sí | Sí | Sí | No |
| Iniciar sesión | Sí | Sí | Sí | Sí | Sí |
| Consultar perfil | Sí | No | No | No | No |
| Consultar clasificación | Sí | No | No | No | No |

Esta matriz representa el estado actual del código. Debe actualizarse cuando un flujo avance
de una etapa a otra. En particular, `Implemented` en una tabla de operaciones y `Sí` en la
columna **Cliente integrado** expresan alcances diferentes.

## 6. Resumen de service contracts

| Contrato | Responsabilidad | Callback |
| --- | --- | --- |
| `IAccountAccessService` | Registro, autenticación, sesiones y seguridad de la cuenta. | No |
| `IPlayerProfileService` | Perfil, estadísticas, imágenes y preferencias persistentes. | No |
| `ILeaderboardService` | Consulta de la clasificación global. | No |
| `IFriendshipService` | Búsqueda, invitaciones, amistades y presencia. | Sí |
| `IMatchLobbyService` | Creación, descubrimiento, configuración y unión a salas. | Sí |
| `IGameplayMatchService` | Tablero, turnos, movimientos, finalización e historial. | Sí |
| `IMatchChatService` | Mensajes de partida y notificaciones de moderación. | Sí |

## 7. `IAccountAccessService`

### 7.1 Responsabilidad

Administra el acceso al sistema, el ciclo de vida de las sesiones y las operaciones de
seguridad que requieren comprobar las credenciales del propietario de una cuenta.

No utiliza callback porque todas sus operaciones se originan en el cliente y producen una
respuesta inmediata.

### 7.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `RegisterAccountAsync` | `AccountRegistrationCredentials` | `AccountRegistrationOutcome` | `Implemented` |
| `LoginAsync` | `AuthenticationCredentials` | `AuthenticationOutcome` | `Implemented` |
| `OpenGuestSessionAsync` | `GuestIdentity` | `GuestSessionOutcome` | `Prototype migration` |
| `CloseSessionAsync` | `SessionIdentity` | `SessionClosureOutcome` | `Planned` |
| `ChangePasswordAsync` | `PasswordChange` | `PasswordChangeOutcome` | `Planned` |
| `DeactivateAccountAsync` | `AccountDeactivation` | `AccountDeactivationOutcome` | `Planned` |
| `SendVerificationCodeAsync` | `EmailVerificationChallenge` | `EmailVerificationDispatchOutcome` | `Deferred` |
| `VerifyEmailCodeAsync` | `EmailVerificationProof` | `EmailVerificationOutcome` | `Deferred` |
| `ResetPasswordAsync` | `PasswordReset` | `PasswordResetOutcome` | `Deferred` |

### 7.3 Datos de entrada y salida

#### `AccountRegistrationCredentials`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Username` | `string` | Sí | Nombre público solicitado. |
| `EmailAddress` | `string` | Sí | Correo utilizado por la cuenta. |
| `Password` | `string` | Sí | Contraseña en tránsito; nunca se registra. |
| `PreferredLocaleCode` | `string` | Sí | Cultura inicial de la cuenta. |
| `ProfileImage` | `ProfileImageContent?` | No | Imagen inicial opcional. |

#### `AccountRegistrationOutcome`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Status` | `AccountRegistrationStatus` | Sí | Resultado esperado del registro. |
| `RegisteredAccount` | `AccountReference?` | No | Identidad creada cuando el registro tiene éxito. |

Estados mínimos: `Success`, `UsernameUnavailable`, `EmailUnavailable`, `InvalidUsername`,
`InvalidEmail`, `InvalidPassword`, `InvalidLocale`, `InvalidProfileImage`,
`PersistenceUnavailable`.

#### `AuthenticationCredentials`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `LoginIdentifier` | `string` | Sí | Username o correo aceptado por el caso de uso. |
| `Password` | `string` | Sí | Contraseña en tránsito; nunca se registra. |

#### `AuthenticationOutcome`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Status` | `AuthenticationStatus` | Sí | Resultado esperado de la autenticación. |
| `Session` | `AuthenticatedSession?` | No | Sesión creada cuando la autenticación tiene éxito. |

Estados mínimos: `Success`, `InvalidCredentials`, `InactiveAccount`, `PersistenceUnavailable`.

#### `AuthenticatedSession`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `SessionToken` | `string` | Sí | Token opaco emitido por el servidor. |
| `Account` | `AccountReference` | Sí | Identidad pública de la cuenta autenticada. |
| `EmailAddress` | `string` | Sí | Correo utilizado por pantallas autorizadas. |
| `PreferredLocaleCode` | `string` | Sí | Cultura persistida para la cuenta. |
| `ExpiresAtUtc` | `DateTime` | Sí | Expiración de la sesión. |

#### `GuestIdentity` y `GuestSessionOutcome`

`GuestIdentity` contiene `DisplayName`. `GuestSessionOutcome` contiene un
`GuestSessionStatus` y, cuando corresponde, una `GuestSession` con token opaco, nombre y
expiración. El servidor administra la disponibilidad del nombre y la vigencia de la sesión.

#### Operaciones de seguridad planeadas

- `PasswordChange` contiene `Session`, `CurrentPassword` y `NewPassword`.
- `AccountDeactivation` contiene `Session` y la confirmación necesaria para la operación.
- `EmailVerificationChallenge` identifica el correo y el propósito de la verificación.
- `EmailVerificationProof` contiene el identificador del desafío y el código ingresado.
- `PasswordReset` contiene la prueba de verificación y la nueva contraseña.

Las contraseñas y códigos nunca forman parte de logs, faults ni callbacks.

## 8. `IPlayerProfileService`

### 8.1 Responsabilidad

Administra la consulta de perfiles, las estadísticas visibles, la identidad pública, las
imágenes y las preferencias persistentes de una cuenta.

No utiliza callback porque sus operaciones son consultas o modificaciones iniciadas por el
cliente.

### 8.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `GetPlayerProfileAsync` | `PlayerProfileCriteria` | `PlayerProfileLookupOutcome` | `Implemented` |
| `GetProfileImageAsync` | `ProfileImageCriteria` | `ProfileImageLookupOutcome` | `Planned` |
| `ChangeUsernameAsync` | `UsernameChange` | `UsernameChangeOutcome` | `Planned` |
| `UpdateProfileImageAsync` | `ProfileImageChange` | `ProfileImageChangeOutcome` | `Planned` |
| `GetPlayerPreferencesAsync` | `SessionIdentity` | `PlayerPreferencesLookupOutcome` | `Planned` |
| `UpdateAccountSettingsAsync` | `PlayerPreferencesChange` | `PlayerPreferencesChangeOutcome` | `Planned` |
| `GetBoardBackgroundAsync` | `SessionIdentity` | `BoardBackgroundLookupOutcome` | `Planned` |
| `ChangeBoardBackgroundAsync` | `BoardBackgroundChange` | `BoardBackgroundChangeOutcome` | `Planned` |

### 8.3 Datos de entrada y salida

#### `PlayerProfileCriteria`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Session` | `SessionIdentity` | Sí | Autoriza la consulta. |
| `TargetAccount` | `AccountReference?` | No | Cuenta objetivo; `null` significa el perfil propio. |

#### `PlayerProfileSnapshot`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Account` | `AccountReference` | Sí | Identidad pública de la cuenta del jugador. |
| `HasProfileImage` | `bool` | Sí | Indica si puede solicitarse una imagen para esta cuenta. |
| `Statistics` | `PlayerStatistics` | Sí | Estadísticas de partidas terminadas. |

#### `PlayerStatistics`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `MatchesPlayed` | `int` | Sí |
| `Wins` | `int` | Sí |
| `Losses` | `int` | Sí |
| `Draws` | `int` | Sí |
| `WinRate` | `double` | Sí |

#### Imágenes

- `ProfileImageCriteria` contiene `Session` y un `TargetAccount` opcional; `null` solicita
  la imagen de la cuenta autenticada.
- `ProfileImageLookupOutcome` contiene el estado de la consulta y, cuando corresponde, un
  `ProfileImageContent`.
- `ProfileImageContent` contiene `MediaType` y `Content` como `byte[]`.
- `ProfileImageChange` contiene `Session` y el nuevo contenido.
- El cliente puede previsualizar la imagen, pero el servidor valida tamaño, formato y
  contenido antes de almacenarla.
- La URI usada por la persistencia se resuelve internamente a partir de la cuenta y nunca
  cruza la frontera WCF.

#### Preferencias

`PlayerPreferences` contiene `PreferredLocaleCode`, `TimeZoneId`,
`HasCustomBoardBackground` y `PieceStyleCode`. `PlayerPreferencesChange` contiene la sesión
y los valores que se desean persistir.

`BoardBackgroundLookupOutcome` contiene el estado de la consulta y, cuando corresponde, un
`BoardBackgroundContent` con `MediaType` y `Content`. `BoardBackgroundChange` contiene la
sesión y el nuevo contenido. La URI persistida se mantiene como detalle interno del servidor.

## 9. `ILeaderboardService`

### 9.1 Responsabilidad

Proporciona una fotografía ordenada de la clasificación global. No utiliza callback.

### 9.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `GetLeaderboardAsync` | `LeaderboardCriteria` | `LeaderboardStandings` | `Implemented` |

### 9.3 Datos de entrada y salida

- `LeaderboardCriteria` contiene `MaximumEntries`.
- `LeaderboardStandings` contiene `GeneratedAtUtc` y una colección de
  `LeaderboardPosition`.
- `LeaderboardPosition` contiene `Position`, `Account`, `HasProfileImage`, `MatchesPlayed`,
  `Wins` y `WinRate`.

Las posiciones no contienen el binario completo de la imagen. El cliente puede obtenerlo
mediante `IPlayerProfileService` usando el `AccountReference` recibido.

## 10. `IFriendshipService`

### 10.1 Responsabilidad

Administra búsqueda de jugadores, invitaciones, amistades y presencia. Utiliza
`IFriendshipServiceCallback` porque varios cambios se originan en otro cliente.

### 10.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `OpenFriendshipChannelAsync` | `SessionIdentity` | `FriendshipChannelOutcome` | `Planned` |
| `FindPlayersAsync` | `PlayerSearchCriteria` | `PlayerSearchMatches` | `Prototype migration` |
| `SendFriendRequestAsync` | `FriendInvitation` | `FriendInvitationOutcome` | `Prototype migration` |
| `GetPendingInvitationsAsync` | `SessionIdentity` | `FriendInvitationCollection` | `Prototype migration` |
| `RespondToFriendRequestAsync` | `FriendInvitationDecision` | `FriendInvitationResolutionOutcome` | `Prototype migration` |
| `GetFriendsListAsync` | `SessionIdentity` | `FriendCollection` | `Prototype migration` |
| `RemoveFriendAsync` | `FriendRemoval` | `FriendRemovalOutcome` | `Prototype migration` |

### 10.3 Datos principales

- `PlayerSearchCriteria`: sesión y texto de búsqueda.
- `PlayerSearchMatches`: estado de consulta y colección de `AccountSummary`.
- `AccountSummary`: `AccountReference` y disponibilidad de imagen de perfil.
- `FriendInvitation`: sesión y `AccountReference` destinataria.
- `FriendInvitationDecision`: sesión, identificador de invitación y decisión.
- `FriendInvitationSummary`: identificador, cuentas remitente y destinataria, estado y fecha
  UTC.
- `FriendSummary`: `AccountReference`, disponibilidad de imagen de perfil y presencia.
- `FriendRemoval`: sesión y `AccountReference` que se desea eliminar de las amistades.

### 10.4 `IFriendshipServiceCallback`

| Callback | Datos | Propósito |
| --- | --- | --- |
| `OnFriendRequestReceived` | `FriendshipInvitationNotification` | Informar una nueva invitación. |
| `OnFriendRequestResponded` | `FriendshipResponseNotification` | Informar aceptación o rechazo. |
| `OnFriendOnlineStatusChanged` | `PlayerPresenceNotification` | Actualizar la presencia de un amigo. |
| `OnFriendshipEnded` | `FriendshipEndedNotification` | Informar que una amistad terminó. |

Cada notificación incluye `EventId`, `OccurredAtUtc` y, cuando exista una operación
originadora, `CorrelationId`.

## 11. `IMatchLobbyService`

### 11.1 Responsabilidad

Administra creación, descubrimiento, configuración y unión a salas de espera. Utiliza
`IMatchLobbyServiceCallback` para sincronizar cambios entre participantes.

### 11.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `OpenLobbyChannelAsync` | `SessionIdentity` | `LobbyChannelOutcome` | `Planned` |
| `CreateMatchAsync` | `MatchConfiguration` | `MatchCreationOutcome` | `Prototype migration` |
| `GetAvailableMatchesAsync` | `MatchSearchCriteria` | `AvailableMatchCollection` | `Prototype migration` |
| `JoinMatchAsync` | `MatchJoinSelection` | `MatchJoinOutcome` | `Prototype migration` |
| `UpdateMatchSettingsAsync` | `MatchSettingsChange` | `MatchSettingsChangeOutcome` | `Prototype migration` |
| `StartSystemMatchAsync` | `SystemMatchConfiguration` | `MatchStartOutcome` | `Prototype migration` |
| `LeaveLobbyAsync` | `LobbyDeparture` | `LobbyDepartureOutcome` | `Prototype migration` |

### 11.3 Datos principales

#### `MatchConfiguration`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `Session` | `SessionIdentity` | Sí |
| `TurnDurationSeconds` | `int` | Sí |
| `OpponentKind` | `OpponentKind` | Sí |
| `CountsTowardStatistics` | `bool` | Sí |

#### `MatchJoinSelection`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Session` | `SessionIdentity` | Sí | Participante que intenta unirse. |
| `Match` | `MatchReference?` | No | Unión mediante selección de catálogo. |
| `MatchCode` | `string?` | No | Unión mediante código. |

Debe existir exactamente uno entre `Match` y `MatchCode`. El servidor revalida que la sala
siga disponible y que el participante pueda unirse.

#### `LobbySnapshot`

Contiene `Match`, `MatchCode`, `Creator`, participantes, configuración y estado actual de la
sala. No contiene entidades de persistencia.

### 11.4 `IMatchLobbyServiceCallback`

| Callback | Datos | Propósito |
| --- | --- | --- |
| `OnParticipantJoinedLobby` | `LobbyParticipantNotification` | Informar que se unió un participante. |
| `OnParticipantLeftLobby` | `LobbyParticipantNotification` | Informar que un participante salió. |
| `OnMatchSettingsChanged` | `MatchSettingsNotification` | Sincronizar configuración. |
| `OnMatchStarted` | `MatchStartNotification` | Indicar la transición al tablero. |
| `OnLobbyClosed` | `LobbyClosureNotification` | Informar que la sala fue cerrada. |

## 12. `IGameplayMatchService`

### 12.1 Responsabilidad

Mantiene el estado autoritativo del tablero, turnos, temporizadores, movimientos,
finalización, observación e historial. Utiliza `IGameplayMatchServiceCallback`.

### 12.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `OpenMatchChannelAsync` | `MatchConnection` | `MatchConnectionOutcome` | `Planned` |
| `GetMatchStateAsync` | `MatchIdentity` | `MatchStateLookupOutcome` | `Prototype migration` |
| `PlacePieceAsync` | `PiecePlacement` | `PiecePlacementOutcome` | `Prototype migration` |
| `PassTurnAsync` | `TurnPass` | `TurnPassOutcome` | `Prototype migration` |
| `ForfeitMatchAsync` | `MatchForfeit` | `MatchConclusion` | `Prototype migration` |
| `ObserveMatchAsync` | `MatchObservation` | `MatchObservationOutcome` | `Prototype migration` |
| `GetMatchHistoryAsync` | `MatchHistoryCriteria` | `MatchHistory` | `Prototype migration` |
| `GetMatchDetailsAsync` | `MatchIdentity` | `MatchDetailsLookupOutcome` | `Prototype migration` |
| `GetMovementHistoryAsync` | `MatchIdentity` | `MovementHistory` | `Prototype migration` |

### 12.3 Datos principales

#### `PiecePlacement`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Session` | `SessionIdentity` | Sí | Identifica al participante. |
| `Match` | `MatchReference` | Sí | Partida objetivo. |
| `Position` | `BoardPosition` | Sí | Fila y columna seleccionadas. |
| `ExpectedRevision` | `long` | Sí | Revisión del tablero visible en el cliente. |

El servidor rechaza una posición fuera del tablero, no permitida, enviada fuera de turno o
basada en una revisión anterior.

#### `TurnPass`

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `Session` | `SessionIdentity` | Sí | Identifica al participante. |
| `Match` | `MatchReference` | Sí | Partida objetivo. |
| `ExpectedRevision` | `long` | Sí | Revisión del tablero visible en el cliente. |

`PassTurnAsync` no permite que un participante omita voluntariamente un turno. La operación
solo puede aceptarse cuando el participante tiene el turno y no existe ninguna posición
válida en la revisión indicada. El servidor comprueba ambas condiciones y también puede
ejecutar el pase automáticamente; el cliente nunca es la autoridad sobre esta decisión.

#### `MatchStateSnapshot`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `Match` | `MatchReference` | Sí |
| `Revision` | `long` | Sí |
| `Board` | `BoardSnapshot` | Sí |
| `CurrentTurn` | `ParticipantReference?` | No |
| `TurnStartedAtUtc` | `DateTime?` | No |
| `TurnDurationSeconds` | `int` | Sí |
| `ValidPositions` | `BoardPosition[]` | Sí |
| `Participants` | `MatchParticipantSummary[]` | Sí |
| `State` | `MatchState` | Sí |

`ValidPositions` corresponde al participante autorizado que recibe el snapshot. Un espectador
recibe una colección vacía.

#### `BoardSnapshot`

Contiene una representación determinista de las 64 posiciones. La primera implementación
podrá usar un arreglo ordenado de `BoardCell`, donde cada elemento contiene `Row`, `Column` y
`DiscColor`. El orden forma parte del contrato y debe verificarse con pruebas de serialización.

#### `MatchConclusion`

Contiene `Match`, motivo de finalización, ganador opcional, conteo final de piezas y
`EndedAtUtc`.

El cliente no informa expiraciones de turno. El servidor controla el reloj y emite la
notificación correspondiente.

### 12.4 `IGameplayMatchServiceCallback`

| Callback | Datos | Propósito |
| --- | --- | --- |
| `OnBoardStateUpdated` | `BoardMoveNotification` | Entregar el nuevo `MatchStateSnapshot`. |
| `OnTurnTimedOut` | `TurnTimeoutNotification` | Informar expiración y transición de turno. |
| `OnMatchEnded` | `MatchConclusionNotification` | Informar la finalización autoritativa. |
| `OnParticipantConnectionChanged` | `ParticipantConnectionNotification` | Informar desconexión o reconexión relevante. |

`BoardMoveNotification` contiene la identidad del evento y el `MatchStateSnapshot`
autoritativo. `MatchConclusionNotification` contiene la identidad del evento y el
`MatchConclusion` correspondiente.

## 13. `IMatchChatService`

### 13.1 Responsabilidad

Administra mensajes asociados a una partida y comunica sanciones de moderación. Utiliza
`IMatchChatServiceCallback`.

### 13.2 Operaciones

| Operación | Entrada | Salida | Estado |
| --- | --- | --- | --- |
| `OpenChatChannelAsync` | `MatchConnection` | `ChatChannelOutcome` | `Planned` |
| `SendChatMessageAsync` | `ChatMessageSubmission` | `ChatSubmissionOutcome` | `Prototype migration` |
| `GetChatHistoryAsync` | `ChatHistoryCriteria` | `ChatTranscript` | `Prototype migration` |

### 13.3 Datos principales

#### `ChatMessageSubmission`

| Miembro | Tipo | Obligatorio |
| --- | --- | --- |
| `Session` | `SessionIdentity` | Sí |
| `Match` | `MatchReference` | Sí |
| `Content` | `string` | Sí |

#### `ChatMessageNotification`

Contiene `EventId`, `CorrelationId`, `Match`, `MessageNumber`, `Sender`, `Content` y
`SentAtUtc`.

#### `ChatModerationNotification`

Contiene `EventId`, `Match`, una razón estable, `StartsAtUtc` y `EndsAtUtc`. No expone la
expresión interna que activó una regla de moderación.

### 13.4 `IMatchChatServiceCallback`

| Callback | Datos | Propósito |
| --- | --- | --- |
| `OnChatMessageReceived` | `ChatMessageNotification` | Entregar un mensaje nuevo. |
| `OnPlayerMuted` | `ChatModerationNotification` | Informar una sanción de chat. |

## 14. Contratos comunes

### 14.1 Identidades y referencias

| Tipo | Miembros principales | Uso |
| --- | --- | --- |
| `SessionIdentity` | `SessionToken` | Autoriza una operación sin aceptar un `AccountId` no confiable. |
| `AccountReference` | `AccountId`, `Username` | Identidad pública de una cuenta. |
| `MatchReference` | `MatchId` | Referencia estable de una partida. |
| `ParticipantReference` | `Match`, `SeatNumber`, `DisplayName` | Participante dentro de una partida. |

`AccountReference` también identifica al jugador registrado en perfiles, amistades y
clasificaciones. El modelo actual no define una identidad `PlayerId` independiente.

### 14.2 Resultados esperados

Los tipos terminados en `Outcome` representan un resultado esperado de negocio. Cada uno
contiene un enum `Status` específico y solo incluye datos adicionales cuando el estado lo
permite.

No se utilizará un único enum genérico para todas las operaciones. Un estado como
`InvalidCredentials` no tiene sentido en `PiecePlacementOutcome`, y un estado como
`StaleRevision` no tiene sentido durante el registro de una cuenta.

### 14.3 Colecciones

Las colecciones contractuales usan arrays o tipos concretos compatibles con
`DataContractSerializer`. No exponen consultas diferidas, `IQueryable`, contextos de Entity
Framework ni colecciones de navegación.

## 15. Contexto diagnóstico y logging

### 15.1 Regla principal

`ILogger<T>` es una dependencia local de cada proceso y nunca forma parte de un contrato WCF.
No se serializa, no se entrega al proxy y no aparece como parámetro de una operación.

- El cliente registra fallas que puede observar directamente en el proxy, los callbacks y la
  presentación.
- El servidor registra fallas de servicios, casos de uso, persistencia y recursos propios.
- Los proveedores se configuran únicamente en los composition roots ejecutables.

### 15.2 Correlation ID

Cada operación remota utiliza un `CorrelationId` con formato GUID. Se transporta mediante un
encabezado SOAP y no se agrega a todos los objetos del dominio.

| Elemento | Valor |
| --- | --- |
| Header | `CorrelationId` |
| Namespace | `urn:reversi:diagnostics:v1` |
| Formato | GUID en representación textual estable |

El cliente genera el identificador al iniciar una operación lógica. Si está ausente o no es
válido, el servidor genera uno nuevo. El servidor lo agrega a un scope de logging y lo
conserva al convertir una excepción inesperada en un fault.

### 15.3 `ServiceFault`

`ServiceFault` es el único contrato técnico común para fallas inesperadas.

| Miembro | Tipo | Obligatorio | Propósito |
| --- | --- | --- | --- |
| `FailureCode` | `string` | Sí | Código técnico estable y no localizado. |
| `CorrelationId` | `string` | Sí | Permite localizar el evento en los logs. |
| `IsTransient` | `bool` | Sí | Indica si reintentar podría ser razonable. |

Las operaciones request-reply declaran `[FaultContract(typeof(ServiceFault))]`. Los errores
esperados no se convierten en faults; se representan con el outcome específico.

`ServiceFault` no contiene mensajes de excepciones, stack traces, rutas, consultas SQL,
cadenas de conexión, nombres de clases internas ni datos personales.

### 15.4 Excepciones relacionadas con los contratos

Las excepciones .NET no forman parte de los data contracts ni se serializan directamente.
Una condición esperada de negocio se representa mediante el `Status` del outcome específico.
Una excepción inesperada que alcanza la frontera del servicio se registra en el servidor y
se convierte en `FaultException<ServiceFault>`. De esta manera, el cliente no depende de
tipos de excepción de las capas de aplicación, persistencia o infraestructura.

| Contrato | Rechazos esperados | Falla técnica expuesta | Estado actual |
| --- | --- | --- | --- |
| `IAccountAccessService` | Estados de `AccountRegistrationOutcome`, `AuthenticationOutcome` y los outcomes de seguridad correspondientes. | `FaultException<ServiceFault>`. Los códigos implementados son `ACCOUNT_REGISTRATION_FAILURE` y `ACCOUNT_LOGIN_FAILURE`. | Implementado para registro e inicio de sesión. |
| `IPlayerProfileService` | Estados de consulta, imagen, preferencias y modificación de perfil. | `FaultException<ServiceFault>` declarado para `GetPlayerProfileAsync`; los códigos estables se definirán al implementar el adaptador. | Contrato creado; adaptador pendiente. |
| `ILeaderboardService` | `LeaderboardStatus`, incluidos criterios inválidos y persistencia no disponible. | `FaultException<ServiceFault>` declarado; el código estable se definirá al implementar el adaptador. | Contrato creado; adaptador pendiente. |
| `IFriendshipService` | Outcomes específicos de canal, búsqueda, invitación, resolución y eliminación. | `FaultException<ServiceFault>` para operaciones request-reply. | Planeado. |
| `IMatchLobbyService` | Outcomes específicos de canal, creación, unión, configuración, inicio y salida. | `FaultException<ServiceFault>` para operaciones request-reply. | Planeado. |
| `IGameplayMatchService` | Outcomes específicos de conexión, estado, movimiento, pase, abandono y observación. | `FaultException<ServiceFault>` para operaciones request-reply. | Planeado. |
| `IMatchChatService` | Outcomes específicos de canal, envío e historial. | `FaultException<ServiceFault>` para operaciones request-reply. | Planeado. |

Desde el cliente, cualquier operación request-reply puede relacionarse además con estas
excepciones locales de infraestructura:

| Excepción | Significado | Tratamiento del cliente |
| --- | --- | --- |
| `FaultException<ServiceFault>` | El servidor recibió la operación, pero ocurrió una falla inesperada controlada. | Registrar `FailureCode` y `CorrelationId` sin datos sensibles; presentar indisponibilidad. |
| `CommunicationException` | El canal o el transporte no pudo completar la comunicación. | Cerrar o abortar el canal y presentar indisponibilidad. |
| `TimeoutException` | La operación excedió el tiempo configurado. | Abortar el canal y presentar indisponibilidad; no reintentar automáticamente una operación mutante. |
| `SerializationException` | La respuesta no cumple el contrato que conoce el cliente. | Registrar como incompatibilidad contractual y presentar indisponibilidad. |
| `InvalidOperationException` | El proxy, endpoint o canal se encuentra en un estado o configuración inválidos. | Registrar la configuración afectada sin secretos y presentar indisponibilidad. |

#### 15.4.1 Mensajes visibles conservados durante la migración

Los mensajes que ya existían en el cliente no se incorporan a `ServiceFault` ni a los
outcomes. El servidor devuelve estados y el cliente selecciona recursos localizados. Esto
conserva las respuestas visibles anteriores sin acoplar el contrato a un idioma.

| Operación o condición | Estado o excepción | Recursos existentes | Respuesta visible actual en `es-MX` |
| --- | --- | --- | --- |
| Inicio de sesión con campos vacíos | Validación local | `LoginScreen_MissingCredentialsTitle` y `LoginScreen_MissingCredentialsMessage` | **Faltan datos.** Ingresa tu usuario o correo electrónico y contraseña para continuar. |
| Inicio de sesión rechazado | `InvalidCredentials` | `LoginScreen_InvalidCredentialsTitle` y `LoginScreen_InvalidCredentialsMessage` | **No se pudo iniciar sesión.** El usuario, correo electrónico o contraseña son incorrectos. |
| Cuenta inactiva | `InactiveAccount` | `LoginScreen_AccountInactiveTitle` y `LoginScreen_AccountInactiveMessage` | **Cuenta no disponible.** Esta cuenta no está activa. |
| Inicio de sesión sin servicio | `PersistenceUnavailable`, `FaultException<ServiceFault>`, `CommunicationException`, `TimeoutException`, `SerializationException` o `InvalidOperationException` | `LoginScreen_ServiceUnavailableTitle` y `LoginScreen_ServiceUnavailableMessage` | **Inicio de sesión no disponible.** No pudimos comunicarnos con el servicio de cuentas. Inténtalo de nuevo. |
| Registro exitoso | `Success` | `AccountCreation_SuccessTitle` y `AccountCreation_SuccessMessage` | **Cuenta creada.** Tu cuenta se creó correctamente. Ya puedes iniciar sesión. |
| Username ocupado | `UsernameUnavailable` | `AccountCreation_UsernameAlreadyTakenTitle` y `AccountCreation_UsernameAlreadyTakenMessage` | **Nombre de usuario ocupado.** Ese nombre de usuario ya está en uso. Elige otro. |
| Correo ocupado | `EmailUnavailable` | `AccountCreation_EmailAlreadyTakenTitle` y `AccountCreation_EmailAlreadyTakenMessage` | **Correo electrónico ya registrado.** Ese correo electrónico ya está asociado a una cuenta. Usa otro. |
| Username inválido | `InvalidUsername` | `AccountCreation_InvalidUsernameFormatTitle` y `AccountCreation_InvalidUsernameFormatMessage` | **Nombre de usuario inválido.** Usa de 3 a 32 caracteres. Comienza con una letra o número y después usa solo letras, números o guiones bajos. |
| Correo inválido | `InvalidEmail` | `AccountCreation_InvalidEmailFormatTitle` y el recurso específico de estructura, nombre, dominio o longitud | **Correo electrónico no válido.** El detalle indica si falló la estructura, el nombre, el dominio o la longitud. |
| Contraseña inválida | `InvalidPassword` | `AccountCreation_InvalidPasswordFormatTitle` y el recurso específico de longitud, mayúscula, minúscula, dígito, símbolo o caracteres no permitidos | **La contraseña no cumple los requisitos.** El detalle indica el requisito específico incumplido. |
| Imagen inválida | `InvalidProfileImage` | `AccountCreation_ImageNotSupportedTitle` y `AccountCreation_ImageNotSupportedMessage` | **Foto de perfil no compatible.** Elige una imagen PNG o JPEG de máximo 5 MiB y 4096 × 4096 píxeles. |
| Registro sin servicio | `PersistenceUnavailable`, fallo técnico o transporte no disponible | `AccountCreation_ServerConnectionFailedTitle` y `AccountCreation_ServerConnectionFailedMessage` | **Conexión no disponible.** No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo. |
| Respuesta de registro desconocida o incompatible | Estado no reconocido | `AccountCreation_PersistenceErrorTitle` y `AccountCreation_PersistenceErrorMessage` | **No se guardó la cuenta.** No pudimos crear tu cuenta. Inténtalo de nuevo más tarde. |

`InvalidLocale` todavía no tiene un mensaje dedicado en los recursos existentes. Debe
agregarse un recurso localizado antes de migrar la creación de cuenta al proxy; no debe
reutilizarse silenciosamente un mensaje que describa otra causa.

Los demás contratos conservarán los siguientes grupos de respuestas ya existentes cuando
sean migrados. Cada operación seleccionará la pareja de recursos que corresponda a su
contexto, sin transportar el texto desde el servidor.

| Contrato | Familias de recursos existentes |
| --- | --- |
| `IPlayerProfileService` | `PlayerStatistics_ServerConnectionFailed`, `PlayerStatistics_RetrievalError`, `ProfileImageModification_ServerConnectionFailed` y `ProfileImageModification_PersistenceError` |
| `ILeaderboardService` | `MainMenu_TopWinnersUnavailable` |
| `IFriendshipService` | Recursos `FriendRequest_*`, `FriendRequests_*`, `FriendRequestResponse_*`, `Friends_*` y `FriendRemoval_*` para conexión, consulta y persistencia |
| `IMatchLobbyService` | Recursos `MatchCreation_*`, `AvailableMatches_*`, `MatchSettings_*` y `JoinMatch_*` para conexión, validación y persistencia |
| `IGameplayMatchService` | Recursos `Game_*`, `AbandonMatch_*`, `MatchHistory_*` y `MatchInformation_*` para conexión, movimiento, resultado y consulta |
| `IMatchChatService` | Recursos `Chat_*` y `ChatRead_*` para conexión, autorización, moderación, persistencia y consulta |

Los callbacks `IFriendshipServiceCallback`, `IMatchLobbyServiceCallback`,
`IGameplayMatchServiceCallback` e `IMatchChatServiceCallback` son operaciones one-way. No
devuelven un fault contractual al servidor. Una excepción local al procesar el callback se
registra en el cliente; una `CommunicationException` o un `TimeoutException` al enviarlo se
registra en el servidor y el canal defectuoso deja de utilizarse.

Las excepciones internas concretas, como errores de Entity Framework, SQL Server, archivos,
criptografía o validación de argumentos, se traducen primero al outcome correspondiente
cuando representan una condición prevista. Si no existe una traducción segura, se convierten
en `ServiceFault`; nunca se exponen por su nombre, mensaje o stack trace.

### 15.5 Responsabilidad de logging

| Situación | Responsable |
| --- | --- |
| El proxy no puede conectar o excede el tiempo permitido. | Cliente |
| El canal WCF entra en estado `Faulted`. | Cliente |
| Ocurre una excepción inesperada durante una operación remota. | Servidor |
| Falla un caso de uso, Entity Framework, SQL Server o un recurso del servidor. | Servidor |
| Falla el envío de un callback. | Servidor |
| Falla el procesamiento local de un callback recibido. | Cliente |
| Una validación esperada rechaza una operación. | Outcome; normalmente no se registra como `Error`. |

La misma excepción se registra una sola vez en la frontera con contexto suficiente para
describir la falla final.

### 15.6 Datos prohibidos en logs

- Contraseñas, hashes y salts.
- Tokens de sesión.
- Códigos de verificación.
- Cadenas de conexión y claves.
- Contenido binario de imágenes.
- Objetos contractuales completos sin revisión de sus campos.
- Correos electrónicos sin una necesidad revisada y minimización apropiada.
- Contenido completo del chat.
- Expresiones internas detectadas por el moderador cuando revelen el mensaje original.

### 15.7 Diagnóstico de callbacks

Las notificaciones incluyen `EventId` y `OccurredAtUtc`. Cuando un evento proviene de una
operación remota, también conserva su `CorrelationId`. Los estados de partida incluyen
`Revision` para identificar orden y obsolescencia sin depender únicamente del tiempo.

Si falla un callback, el servidor registra la falla, identifica la suscripción afectada y
evita que un cliente defectuoso impida notificar a los demás. No repite indefinidamente una
notificación sobre un canal en estado `Faulted`.

## 16. Responsabilidades exclusivas del cliente

Las siguientes acciones no necesitan un contrato WCF:

- Navegar entre páginas y abrir ventanas.
- Mostrar diálogos y mensajes localizados.
- Cargar recursos e instrucciones empacados con el cliente.
- Seleccionar archivos locales y generar previsualizaciones.
- Renderizar y animar el tablero.
- Formatear fechas, porcentajes y textos para la cultura activa.
- Comprobar que una fila y columna estén dentro del tablero.
- Comprobar que una posición pertenezca a `ValidPositions`.
- Evitar un segundo envío mientras una jugada anterior sigue pendiente.

La selección de archivos y la previsualización son locales; la aceptación final de una imagen
es responsabilidad del servidor.

## 17. Nombres reservados por persistencia

Los siguientes nombres corresponden actualmente a entidades generadas por Entity Framework y
no deben reutilizarse como nombres exactos de data contracts:

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

Ejemplos contractuales apropiados incluyen `AccountReference`, `PlayerPreferences`,
`ChatMessageNotification`, `ChatModerationNotification`, `MatchStateSnapshot`,
`PiecePlacement` y
`FriendInvitationSummary`.

## 18. Trazabilidad con la interfaz actual

| Pantalla o flujo | Operaciones remotas previstas |
| --- | --- |
| Creación de cuenta | `RegisterAccountAsync` |
| Inicio de sesión | `LoginAsync`, `OpenGuestSessionAsync` |
| Recuperación de contraseña | Operaciones de verificación y `ResetPasswordAsync`, diferidas |
| Perfil propio o de otro jugador | `GetPlayerProfileAsync`, `GetProfileImageAsync` |
| Modificación de cuenta | `ChangeUsernameAsync`, `ChangePasswordAsync`, `UpdateProfileImageAsync`, `DeactivateAccountAsync` |
| Menú principal | `GetLeaderboardAsync`, `GetFriendsListAsync` |
| Solicitudes de amistad | Búsqueda, envío, consulta y resolución en `IFriendshipService` |
| Configuración | Operaciones de preferencias e imágenes de `IPlayerProfileService` |
| Partidas disponibles | `GetAvailableMatchesAsync`, `JoinMatchAsync` |
| Creación y espera de partida | Operaciones y callbacks de `IMatchLobbyService` |
| Tablero | Operaciones y callbacks de `IGameplayMatchService` |
| Chat | Operaciones y callbacks de `IMatchChatService` |
| Historial y detalle | Operaciones de historial de `IGameplayMatchService` |

## 19. Generación del proxy

La primera referencia de servicio ya fue generada a partir de los metadatos publicados por
el host. Actualmente contiene las operaciones `RegisterAccountAsync` y `LoginAsync` de
`IAccountAccessService`. La pantalla de inicio de sesión ya utiliza esta referencia; la
creación de cuenta continúa pendiente de migración.

| Elemento | Configuración actual |
| --- | --- |
| Namespace generado | `ReversiGameSystem.ServerProxy` |
| Endpoint del cliente | `AccountAccessEndpoint` |
| Dirección local | `net.tcp://localhost:8090/ReversiGameSystem/AccountAccess` |
| Binding | `netTcpBinding` |
| Seguridad de transporte | Credenciales de Windows con `EncryptAndSign` |
| Configuración | `ReversiGameSystem.Client.Presentation/App.config` |

La dirección `localhost` corresponde al entorno local de desarrollo. La dirección de un
servidor compartido deberá cambiarse por configuración, sin modificar el código del proxy.

- La reutilización de tipos de ensamblados referenciados debe permanecer desactivada.
- El namespace generado del cliente debe dejar claro que se trata de un proxy.
- El código generado no se edita manualmente.
- La dirección y el binding se guardan en `Client.Presentation/App.config`.
- El código generado se marca como generado para los analizadores.
- Cualquier cambio incompatible exige actualizar el contrato, sus pruebas de serialización y
  la referencia del cliente de forma coordinada.

## 20. Reglas de versionamiento

- Los nombres, namespaces WCF y órdenes de `[DataMember]` publicados se consideran parte del
  protocolo.
- Los miembros existentes no se reordenan ni reutilizan para otro significado.
- Los miembros opcionales nuevos se agregan al final con un nuevo orden.
- Un miembro obligatorio no se elimina sin una nueva versión del contrato.
- Las enumeraciones deben tratar valores desconocidos de manera controlada.
- Los cambios se verifican mediante pruebas de serialización y una prueba de integración WCF.
- El catálogo se actualiza antes de regenerar el proxy.

## 21. Secuencia de implementación

1. `Completed`: aprobar el catálogo inicial.
2. `Completed`: crear `ReversiGameSystem.Server.Contracts` y las pruebas contractuales.
3. `Completed`: crear el adaptador y el host WCF iniciales para acceso a cuentas.
4. `Completed`: publicar metadatos y generar el proxy de `IAccountAccessService`.
5. `In progress`: migrar los flujos del cliente; el inicio de sesión ya usa WCF y la creación
   de cuenta es el siguiente flujo de acceso a cuentas.
6. `Planned`: publicar y migrar las consultas de perfil y clasificación.
7. `Planned`: migrar amistades, lobby, gameplay y chat por grupos funcionales.
8. `Planned`: eliminar `ReversiGameSystem.LocalRuntime` y los prototipos locales cuando ya no
   tengan consumidores.
9. `Continuous`: actualizar el catálogo cuando una operación o etapa de integración cambie
   de estado.

