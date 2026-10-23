# Inventario de la migración a dos soluciones

Registro de trabajo para reunir cambios de implementación, ajustes de base de datos,
configuración, verificaciones y documentación que deben conciliarse al cerrar la migración.
No sustituye el catálogo WCF ni actualiza automáticamente los entregables de diseño o el estándar.

## Estados

- **Implementado:** existe código; las comprobaciones realizadas se indican por separado.
- **Pendiente:** falta ejecutar, implementar o documentar el cambio descrito.
- **En curso:** trabajo iniciado, todavía sin aceptación final.

## 0. Estructura definitiva de las soluciones

La estructura queda en dos soluciones independientes y nueve proyectos:

| Solución | Proyectos y responsabilidad |
| --- | --- |
| `ReversiGameSystem.Client.slnx` | `Client.Presentation`: WPF, validación preventiva y consumo del proxy generado. `Client.Tests`: pruebas del ensamblado cliente real. |
| `ReversiGameSystem.Server.slnx` | `Domain`: reglas autorizadas; `Server.Application`: casos de uso y puertos; `Server.Contracts`: frontera WCF; `Server.Persistence`: datos, archivos y correo; `Server.ServiceApi`: adaptadores WCF; `Server.Host`: composición y publicación; `Server.Tests`: pruebas del servidor. |

Se retiraron la solución agregada, los proyectos `Contracts` y `LocalRuntime`, y el proyecto
mixto `ReversiGameSystem.Tests`. Su cobertura vigente se trasladó a las dos suites finales.
El cliente no referencia proyectos del servidor, Domain ni EF, y las pruebas no compilan
copias enlazadas de fuentes de producción. Las políticas preventivas del cliente son
independientes de las validaciones autorizadas del servidor.

La referencia de servicio queda en `Connected Services/Server/Reference.cs`. El script
`tools/UpdateServiceReference.ps1` compila los contratos, exporta un conjunto de metadatos
y regenera el proxy con svcutil, sin necesitar un servidor en ejecución. Evita los esquemas
duplicados de combinar varios MEX; el código generado conserva el formato de la herramienta.

EF se restaura mediante `PackageReference`, sin depender de una carpeta raíz `packages`.
Se excluyen sus targets heredados de compilación: la extracción portable del EDMX del proyecto
mantiene explícitos los recursos CSDL/SSDL/MSL.

El cierre de estructura no acredita la implementación de todos los servicios del catálogo.
Las funciones pendientes se conservan identificadas más abajo; el estándar todavía no se modifica.

## 1. Contratos WCF

| Elemento | Estado | Seguimiento |
| --- | --- | --- |
| Nueve servicios, 44 operaciones y cuatro callbacks con nueve métodos | Implementado | Firmas, datos y faults del catálogo trasladados a C#. |
| Tipos antiguos de perfil y clasificación sin consumidores | Implementado | Nueve tipos retirados. |
| Pruebas de contratos | Implementado | Paso anterior: 29 pruebas del servidor y dos del cliente aprobadas; ambas soluciones compilaron. |
| Acceso heredado y `ServiceFault.IsTransient` | Pendiente | Retirar al migrar sus consumidores y regenerar referencias; evitar romper registro durante el cambio de login. |

## 2. Códigos de verificación y correo

### Decisiones aceptadas

- Nombres propios del propósito, independientes de SMTP, MailKit o EF; esas tecnologías son
  detalles de los adaptadores del servidor.
- Casos de uso separados para inicio de sesión y creación de cuenta; compartir generación,
  protección, persistencia y envío de códigos.
- Seis dígitos ASCII generados criptográficamente; preservar ceros iniciales.
- Vigencia inicial de cinco minutos y máximo de cinco intentos por código.
- Representación HMAC vinculada al propósito y a los datos autorizados; no guardar el código
  en claro ni registrarlo junto con contraseñas, secretos o tokens.
- Uso único; un código nuevo confirmado sustituye al pendiente del mismo proceso.
- Login: validar contraseña antes de enviar, volver a comprobar cuenta activa al confirmar,
  y crear sesión únicamente después de verificar el código.
- Registro: vincular código con nombre y correo; completar registro no abre sesión.
- Envío mediante MailKit con TLS obligatorio y límite total de cinco segundos para conexión,
  autenticación y envío. Cancelar la operación real; no limitar solamente la espera del llamador.
- Confirmar únicamente aceptación del mensaje por el proveedor; no prometer entrega al buzón.
- Sin colas ni reintentos automáticos en esta primera implementación; SMTP queda fuera de
  las transacciones SQL y de los bloqueos del estado de partidas.
- Las firmas WCF permanecen sin `Task` ni `Async`; la E/S interna puede usar asincronía.

### Inventario de implementación

| ID | Área | Estado | Trabajo y criterio de cierre |
| --- | --- | --- | --- |
| COR-01 | Aplicación | Implementado | Generación, vencimiento, asociación y protección compartidos; consumo durable antes de crear sesión. El repositorio acepta una sola de 16 confirmaciones concurrentes en la prueba SQL. |
| COR-02 | Correo | Implementado | `IVerificationEmailSender` y `VerificationEmailSender`, mensaje de texto bilingüe con propósito y vencimiento, configuración inyectada. Entrega externa todavía pendiente de COR-07. |
| COR-03 | Configuración | Implementado | `MailSettings` y lector común `EnvironmentSettings`; variables del proceso con prioridad y `.env` local. `.env.example` contiene únicamente ejemplos. |
| COR-04 | Persistencia | Implementado | `VerificationCodeRepository` reutiliza `VerificationCode` con comandos parametrizados mediante EF y transacciones serializables; SMTP queda fuera de la transacción. |
| COR-05 | Base de datos | Implementado | Script `database/migrations/20261022_VerificationCodeAttempts.sql`: agrega `identityKey` nullable, `failedAttempts` con default y CHECK de 0 a 5, e índice único filtrado. Aplicado dos veces en BD desechable; conserva filas históricas. Su aplicación en la BD del proyecto sigue pendiente de COR-12. |
| COR-06 | Modelo EF | Pendiente | Conciliar cambios de esquema con EDMX y regeneración; no modificar a mano las entidades generadas. Registrar si el repositorio usa SQL parametrizado mediante EF. |
| COR-07 | Proveedor | Pendiente | Configurar host, puerto, seguridad, remitente y credenciales reales mediante entorno. Verificar autenticación y aceptación de un correo de prueba autorizado. |
| COR-08 | Seguridad | En curso | Ocho escenarios SQL reales verifican uso único, cinco fallos, quinto intento correcto, caducidad exacta, sustitución, propósito/identidad, concurrencia y conservación histórica. Falta definir límite de reenvíos y su comunicación sin inventar reasons ajenos al catálogo. |
| COR-09 | Fallos | En curso | Prueba real de conexión local detenida verifica cancelación SMTP a los cinco segundos, con tolerancia de planificación de dos segundos. El fallo de envío no sustituye el código anterior. Si el proceso cae después de consumir y antes de crear sesión, el usuario solicita otro código: no se restaura un código consumido ni se crean dos sesiones. Faltan pruebas de caída del proceso y fallo real del proveedor/BD del entorno final. |
| COR-10 | Diseño | Pendiente | Actualizar posteriormente C-11, dependencias, flujo de ejecución, configuración, política de códigos y tratamiento de fallos en los documentos que el equipo indique. |
| COR-11 | Operación | Pendiente | Documentar instalación del proveedor, rotación del secreto HMAC, efecto sobre códigos pendientes, despliegue y limpieza de códigos históricos. |
| COR-12 | Despliegue BD | Pendiente | Revisar restricciones y respaldo del esquema real, aplicar el script en la BD del proyecto y conciliar EDMX. Ninguna prueba de este paso modifica la BD real. |
| COR-13 | Localización | Pendiente | El mensaje inicial contiene español e inglés. Valorar selección de idioma del correo según la cuenta; el puerto actual no recibe idioma. |
| COR-14 | Compilación | Implementado | La prueba SQL detectó que los targets heredados podían omitir los metadatos del modelo al compilar con el SDK. El proyecto ahora extrae CSDL/SSDL/MSL del EDMX mediante tareas estándar de MSBuild y los incrusta con nombres explícitos. La prueba de recursos del ensamblado evita una compilación aparentemente correcta sin modelo utilizable. |

### Variables previstas

`REVERSI_MAIL_HOST`, `REVERSI_MAIL_PORT`, `REVERSI_MAIL_SECURITY`,
`REVERSI_MAIL_USERNAME`, `REVERSI_MAIL_PASSWORD`, `REVERSI_MAIL_FROM_ADDRESS`,
`REVERSI_MAIL_FROM_NAME` y `REVERSI_VERIFICATION_SECRET`.

El secreto de verificación es independiente de la contraseña del correo y contiene al menos
32 bytes aleatorios codificados en Base64. Los valores reales permanecen fuera del repositorio
y del cliente. `REVERSI_MAIL_SECURITY` admite `StartTls` (upgrade obligatorio) o `Tls`
(cifrado desde la conexión), nunca texto plano. La contraseña es la credencial que autorice
el proveedor seleccionado. Cambiar el secreto invalida los códigos pendientes.

El despachador admite dos envíos simultáneos y rechaza saturación como falla técnica;
no crea una cola de trabajo ni comparte bloqueos con partidas. La emisión y sustitución
se serializan por identidad dentro del proceso servidor, mediante 32 bloqueos acotados.
La persistencia protege el consumo concurrente también en SQL. Un despliegue con varios
procesos emisores requeriría coordinar además el orden de emisión entre procesos.

## 3. Migración del inicio y cierre de sesión

| Elemento | Estado | Trabajo y criterio de cierre |
| --- | --- | --- |
| Servidor y endpoint de `ISessionService` | Implementado | Solicitud, confirmación, invitado y revocación; `net.tcp` en puerto 8091 con transporte Windows cifrado. Registro heredado conserva puerto 8090. |
| Consumo desde WPF | Implementado | `SessionClient`, proxy regenerado desde el ensamblado de contratos y diálogos de código. Cerrar sesión conserva estado local si el servidor no confirma revocación. |
| Login anterior | Implementado | `LoginAsync` retirado del contrato publicado y del adaptador; eliminado `RemoteAccountAccessService`. Se retiraron también `AuthenticationCredentials`, `AuthenticationOutcome`, `AuthenticationStatus` de Server.Contracts y sus mapeos sin consumidores. El contrato heredado conserva únicamente registro. |
| Dependencia de `LocalRuntime` | Implementado | Proyecto retirado. Registro, perfil propio, imagen, nombre, estadísticas y clasificación pasan por WCF. La cobertura vigente se trasladó; se retiraron `LoginHandler` y `LoginUseCase`, cuyo acceso directo había quedado sin consumidores de producción. |
| Comprobación completa | En curso | Ambas soluciones compilaron y se verificaron SQL temporal, WCF y proxy real de invitado. Para aceptación de entorno final faltan BD real y proveedor de correo configurados, con login completo desde WPF. |

### Limpieza y diagnóstico por conciliar

- `GuestSessionService` del prototipo y su política local permanecen referenciados por las
  pruebas cliente ya trasladadas; revisar su retiro junto con el prototipo. No son el mecanismo usado
  ahora por la pantalla de acceso.
- Los handlers de autenticación locales y los modelos internos del cliente permanecen donde
  aún tienen consumidores. `ServiceFault.IsTransient` se conserva por compatibilidad con el
  registro heredado; no extender ese patrón a servicios nuevos.
- El proxy se genera con svcutil desde un único conjunto de metadatos, sin avisos de esquemas
  repetidos. No se editan manualmente sus operaciones o tipos.
- Los fallos técnicos registran CorrelationId y una cadena acotada de tipos, HResult y
  ubicaciones de pila; se excluyen Message y Data del proveedor para evitar secretos.
- Las observaciones independientes sobre diagnóstico, documentación XML y tolerancia del
  timeout dieron lugar a correcciones. Las verificaciones y correcciones finales del
  coordinador son autoinspección; no equivalen a otra revisión independiente completa.

## 4. Perfil, clasificación y pendientes funcionales

`PlayerProfileService` publica las cuatro operaciones del catálogo y `LeaderboardService`
publica la consulta de clasificación. Comparten con sesiones el almacén autenticado del host.
Los endpoints usan transporte Windows cifrado: registro 8090, sesiones 8091, perfil 8092 y
clasificación 8093. Los repositorios, imágenes y credenciales permanecen en el servidor.

La imagen viaja como contenido, sin rutas físicas. La escritura valida formato y tamaño,
genera un nombre interno y compensa el archivo nuevo si falla su asociación en BD. Los
archivos anteriores se conservan tras reemplazos exitosos; falta definir su recolección
para evitar acumulación sin interrumpir lecturas concurrentes.

- Registro todavía consume `IAccountAccessService.RegisterAccountAsync`, el contrato
  temporal preexistente. Falta completar el flujo por código de `IAccountService` y retirar
  ese endpoint y `ServiceFault.IsTransient`; los contratos nuevos mantienen firmas síncronas.
- Falta conectar navegación y consulta de perfiles ajenos mediante identificador de cuenta:
  el prototipo de amigos todavía navega con nombre. El perfil propio ya usa el servicio real.
- Las funciones de partidas, amistad, chat y preferencias que siguen como prototipos requieren
  sus implementaciones remotas. La presencia de sus contratos en el proxy no las publica.
- Las pruebas WCF de perfil y clasificación usan sustitutos de persistencia. Falta validar
  esos repositorios contra el esquema y datos del entorno final.

## 5. Conciliación pendiente

- Conciliar documentación de diseño, contratos y operación con el inventario realmente entregado.
  Los diagramas y documentos históricos conservan su carácter de guía de construcción;
  su actualización requiere el alcance que indique el equipo.
- Revisar y estabilizar el estándar al terminar todos los pasos, por instrucción del equipo;
  no modificarlo durante este paso.
- Conciliar la firma heredada de cuatro parámetros de `IAuthenticatedSessionStore.Create`
  con el límite actual del estándar y los tipos inmutables existentes, sin agrupar dependencias
  artificialmente. Los nuevos consumidores conservan por ahora esa firma ya publicada internamente.

## Fuentes de referencia

- [Catálogo interno WCF](WcfIntegrationContracts.md), secciones 7, 8, 17, 21 y 23.
- [Catálogo de entrega](WcfContractsCatalog.md).
- [Diseño](Design%20Project/Documento-Entrega.md), componente C-11 y separación de responsabilidades.
- [Reglas de validación](../ValidationRules.md), códigos de verificación.
- Código actual de composición del servidor, configuración y persistencia.

## Evidencia del paso de sesión y correo

- Compilación del servidor con el SDK y del cliente con MSBuild de Visual Studio.
- Resultado final: **44 pruebas del servidor, ocho escenarios SQL, cuatro pruebas del
  cliente y una prueba del proxy contra el host real aprobados**. Las compilaciones finales
  terminaron sin errores ni advertencias del compilador.
- Pruebas del servidor: contratos, serialización, casos de sesión, WCF real `net.pipe`,
  protección del código, fallo de envío, configuración y cancelación SMTP contra un socket
  local detenido. Los casos de sesión usan sustitutos del repositorio y del envío donde
  no se pretende acreditar una integración real.
- Modo explícito `ReversiGameSystem.Server.Tests.exe --database`: ocho escenarios contra
  `VerificationCodeRepository` real. Crea una BD LocalDB con nombre aleatorio validado,
  prepara el esquema original, aplica dos veces el script, verifica las operaciones y
  elimina exclusivamente esa BD en `finally`; restaura la variable de conexión del proceso.
- Cuatro pruebas del cliente: mapeo de sesión, rechazo de token ausente, vencimiento y
  recursos de los dos idiomas.
- Modo explícito `ReversiGameSystem.Client.Tests.exe --session-integration`: adaptador y
  proxy generados reales contra el host temporal, con `net.tcp` y seguridad Windows.
  Verifica apertura de invitado, fault por nombre duplicado, cierre idempotente y reutilización
  del nombre con otro token. El host usa configuración ficticia y se detiene al terminar.
- El proyecto de pruebas cliente referencia su ensamblado cliente real; no agrega referencias
  al servidor ni compila copias enlazadas del proxy.

El script no está aplicado en la BD del proyecto. No se han usado credenciales reales de
correo ni se ha acreditado entrega externa o login completo contra BD/proveedor reales.
La prueba SMTP cubre espera del saludo; faltan escenarios específicos de DNS, TLS y
autenticación que agoten el presupuesto de entrega. El estándar queda sin modificar.

## Evidencia del cierre de estructura

- Ambas soluciones finales compilaron con MSBuild de Visual Studio, sin errores ni
  advertencias del compilador. Se verificaron dos soluciones y nueve proyectos.
- **121 pruebas del servidor y 132 del cliente aprobadas**. La cobertura trasladada usa
  los ensamblados reales; se sustituyeron pruebas de DTO y runtime retirados por las del
  catálogo vigente y doce escenarios WCF de registro, perfil y clasificación.
- Ocho comprobaciones SQL aprobadas en una base LocalDB desechable tras pasar EF a
  `PackageReference`; no se modificó la BD del proyecto.
- Ciclo de invitado aprobado mediante el proxy generado contra el host real temporal,
  con transporte Windows cifrado. El host se detuvo al terminar.
- La prueba de dependencias y la inspección de la salida del cliente no encontraron
  ensamblados Server, Domain, LocalRuntime, EF ni MailKit.
- La regeneración offline del proxy terminó sin errores ni avisos de esquemas duplicados.
- Se eliminaron fuentes y proyectos retirados, además de sus residuos de compilación.
  Se preservaron preferencias locales del IDE y respaldos locales fuera de la entrega.
- Hubo revisión independiente de las fronteras y correcciones posteriores; la integración
  y comprobación finales del coordinador son autoinspección.

Este cierre habilita trabajar sobre el estándar con la estructura definida. No acredita
correo externo, login completo con proveedores reales ni la terminación funcional de los
servicios pendientes indicados en este inventario.
