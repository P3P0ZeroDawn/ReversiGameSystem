# Estructura del servidor

Este documento resume la estructura vigente que debe poder recorrerse y explicarse. No
reemplaza los casos de uso, los contratos WCF ni el estándar de C#.

## Arquitectura adoptada

El servidor utiliza una **arquitectura en capas con inversión de dependencias para
persistencia y adaptadores en sus fronteras**. No se adopta Clean Architecture ni
arquitectura hexagonal como patrón completo.

Las dependencias de compilación permitidas son:

```text
Host → ServiceApi
Host → Persistence
Host → Application
Host → Contracts

ServiceApi → Application
ServiceApi → Contracts
Persistence → Application
Application → Domain
```

- **Host** es la raíz de composición. Crea las implementaciones y controla el ciclo de
  vida de los hosts WCF.
- **Contracts** define exclusivamente la superficie remota serializable. No contiene
  reglas del servidor.
- **ServiceApi** adapta WCF a las operaciones de Application y traduce sus resultados a
  respuestas o faults contractuales.
- **Application** coordina cada funcionalidad y declara las interfaces que necesita para
  acceder a estado externo.
- **Domain** contiene políticas e invariantes independientes de WCF y persistencia.
- **Persistence** implementa las interfaces de Application mediante base de datos,
  sistema de archivos o servicios externos.

`Application` no referencia `Contracts` ni `Persistence`. `Persistence` depende de las
interfaces definidas por `Application`; esta dirección permite sustituir la implementación
sin cambiar el caso de uso.

## Recorrido general

```text
ServiceApi → ApplicationService → IRepository → Repository
```

- **ServiceApi** recibe tipos del contrato WCF, llama a Application y traduce el resultado
  a una respuesta o fault contractual. Aquí termina el transporte remoto.
- **ApplicationService** valida el contexto de la operación y coordina el caso de uso. No
  conoce WCF, Entity Framework ni la interfaz gráfica.
- **IRepository** declara únicamente las operaciones de estado o persistencia que necesita
  Application.
- **Repository** implementa esas operaciones y transforma las entidades de persistencia en
  modelos internos.

No se crea un repositorio cuando una funcionalidad no almacena ni consulta estado externo.

## Archivos de una funcionalidad

Una carpeta de Application contiene solamente los archivos que la funcionalidad necesita:

```text
{Capability}/
├── {Capability}ApplicationService.cs
├── I{Capability}Repository.cs       si existe persistencia o estado externo
├── {Capability}Models.cs            si agrupa modelos pasivos pequeños
└── {Capability}Result.cs            si existen resultados esperados
```

Una entidad con comportamiento importante permanece en su propio archivo. `Models.cs` no
contiene servicios, repositorios, excepciones ni copias de DTOs de otras capas.

## Modelos y fronteras

- Los DTOs existen en Contracts porque cruzan WCF.
- ServiceApi realiza una sola conversión entre el DTO contractual y un modelo con significado
  para la funcionalidad.
- El mismo modelo interno se reutiliza entre Application y su repositorio. No se crean DTOs
  de Application ni DTOs de Persistence para copiar los mismos miembros.
- Un modelo interno debe representar una entrada, resultado o concepto real del caso de uso;
  no existe únicamente para transportar datos hacia la siguiente capa.
- Las entidades generadas por Entity Framework permanecen dentro de Persistence.
- Un mapper separado solo se conserva cuando la misma conversión se reutiliza en más de una
  frontera y constituye una responsabilidad reconocible.

### Modelo interno de cuentas

`NewAccount` pertenece a Contracts y solo existe en la frontera WCF. `AccountService`
extrae sus valores y crea los modelos internos que necesita Application.

`PlayerAccount` representa la cuenta que se registra y comparte estos datos con el
repositorio:

- `Username`
- `EmailAddress`
- `PasswordHash`
- `PreferredLocaleCode`
- `ProfileImage`
- `RegisteredAtUtc`

`ProfileImage` agrupa `MediaType` y `Content`. El contenido binario se copia al entrar al
modelo para impedir que otro componente modifique el mismo arreglo.

`RegistrationCodeResult` comunica el estado esperado de la solicitud y su vencimiento.
`AccountRegistrationResult` comunica el estado del registro y, cuando corresponde, el
identificador y nombre creados. Son resultados de comportamiento de Application, no DTOs
serializables ni copias de `NewAccount`.

El recorrido objetivo es:

```text
NewAccount de WCF
    → AccountService
    → PlayerAccount
    → AccountApplicationService
    → IAccountRepository
    → AccountRepository
```

La recuperación de contraseña utiliza una respuesta no reveladora. Una solicitud con un
correo de formato válido devuelve un vencimiento plausible aunque no exista una cuenta
activa. En ese caso no se guarda ni se envía un código. La verificación posterior conserva
el rechazo contractual `InvalidVerificationCode`.

## Estado, concurrencia y errores

El componente que declara una colección o estado mutable es su propietario y protege sus
invariantes. WCF permite llamadas concurrentes a los servicios configurados con
`ConcurrencyMode.Multiple`; no sustituye la sincronización del estado compartido ni cubre
temporizadores, callbacks o eventos.

- `lock` protege una sección síncrona corta que lee o modifica estado relacionado.
- `SemaphoreSlim` limita operaciones que deben esperar de forma asíncrona o atravesar
  `await`.
- `Interlocked` actualiza una sola variable compartida de manera atómica.

Estas herramientas no se agregan por costumbre. Cada uso debe identificar el estado o
recurso protegido y se elimina cuando otra sincronización ya garantiza la misma invariante.

Application devuelve estados para validaciones, autorizaciones y conflictos esperados. Las
excepciones técnicas inesperadas se propagan hasta el manejador global de ServiceApi. Se
prefieren las excepciones estándar de .NET y Application no declara excepciones propias.

## Cómo recorrer una funcionalidad

1. Localiza la operación en la interfaz de Contracts.
2. Abre su implementación en ServiceApi.
3. Sigue la llamada al método de ApplicationService.
4. Identifica las reglas y el resultado esperado.
5. Sigue la interfaz IRepository hasta su implementación en Persistence.
6. Localiza la colección o tabla que posee el estado.
7. Regresa a ServiceApi para observar cómo se forma la respuesta o el fault.
8. Revisa las pruebas que cubren el mismo recorrido.
