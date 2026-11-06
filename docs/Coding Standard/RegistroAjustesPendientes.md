# Registro acotado de ajustes pendientes al estándar

## Propósito y alcance

Este archivo conserva decisiones y observaciones puntuales que deberán revisarse cuando el
equipo autorice una modificación del estándar de C#. No es normativo, no sustituye al
estándar vigente y no aprueba por sí mismo ninguna excepción o regla nueva.

Cada cambio futuro deberá seguir `StandardChangeProtocol.md`, redactarse en inglés dentro
del estándar y contar con fuentes verificables cuando incluya afirmaciones técnicas.

## Criterio de simplificación acordado

El estándar se reducirá gradualmente para que cada tema desarrollado contenga solamente:

1. Una explicación breve y comprobable de la regla.
2. Un ejemplo correcto pequeño.
3. Un ejemplo incorrecto pequeño.

La simplificación debe conservar el significado normativo, distinguir las convenciones del
proyecto de las reglas de C#/.NET y mantener únicamente las citas y referencias necesarias.
Antes de aplicar este formato global deberá reconciliarse con los entregables exigidos por
`StandardChangeProtocol.md`.

## Ajustes pendientes

### STD-P01 — Operador de coalescencia nula `??`

- **Secciones relacionadas:** 5.10, 5.11 y los ejemplos que actualmente usan `??`.
- **Situación actual:** el operador aparece en ejemplos de serialización, dependencias y
  normalización, pero el estándar no explica su significado ni cuándo usarlo.
- **Ajuste pendiente:** incorporar una regla breve que diferencie un valor alternativo
  válido de un valor predeterminado que ocultaría un error. Incluir `?? throw` solamente
  cuando la ausencia impida construir o continuar una operación válida.
- **Ejemplo objetivo:** justificar una asignación como
  `_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));` cuando
  `sessions` es una dependencia obligatoria del constructor. Si el argumento no es nulo,
  se asigna a `_sessions`; si es nulo, la construcción se detiene inmediatamente con una
  excepción que identifica el parámetro. El ejemplo incorrecto deberá mostrar la asignación
  silenciosa de `null` o un valor alternativo que deje al objeto en un estado inválido.
- **Clasificación prevista:** sintaxis y evaluación definidas por C#, combinadas con una
  convención del proyecto para rechazar dependencias obligatorias ausentes en el constructor.
- **Antes de editar:** comprobar por separado si `?.` requiere una regla propia; su aparición
  junto con `??` no debe aprobarlo implícitamente.

### STD-P02 — Cuerpos de expresión

- **Secciones relacionadas:** 4, 5.19 y cualquier ejemplo con miembros de cuerpo de
  expresión.
- **Situación actual:** 5.19 los menciona incidentalmente, pero el estándar no define su
  sintaxis ni una regla revisable. El equipo decidió no utilizarlos en código manual.
- **Ajuste pendiente:** establecer cuerpos de bloque como convención del proyecto y retirar
  menciones que parezcan autorizar cuerpos de expresión. El ejemplo correcto deberá usar
  llaves y una instrucción explícita; el incorrecto mostrará el mismo miembro condensado.
- **Distinción necesaria:** una expresión lambda no es un miembro con cuerpo de expresión.
  Las lambdas simples permanecen cubiertas por 5.14.

### STD-P03 — Sufijo `Async` y bloqueo de tareas

- **Sección relacionada:** 3.16.
- **Situación actual:** el estándar ya exige `Async` para métodos asíncronos. Algunos flujos
  existentes exponen métodos síncronos que internamente usan `GetAwaiter().GetResult()`.
- **Ajuste pendiente:** conservar la regla del sufijo en forma breve y aclarar que renombrar
  un método no lo vuelve asíncrono. La migración debe decidir primero si la operación retorna
  `Task` o permanece realmente síncrona; después se aplica el nombre correspondiente.

### STD-P04 — Nombres de servicios de aplicación

- **Secciones relacionadas:** 3.3 y 3.16.
- **Situación actual:** conviven `UseCase`, `Handler`, `Reader` y `Editor` para clases que
  coordinan operaciones de una misma capacidad de aplicación. La fragmentación añade tipos
  pequeños y dificulta reconocer el punto de entrada interno de cada capacidad.
- **Ajuste pendiente:** registrar como convención del proyecto el nombre
  `{Capability}ApplicationService` para una clase sin estado que agrupe y coordine operaciones
  cohesivas de una capacidad en la capa de aplicación. Evitar `UseCase`, `Handler`, `Reader`,
  `Editor` y `Operations` cuando sólo clasifican el mecanismo o dividen operaciones que no
  necesitan responsabilidades independientes.
- **Ejemplos objetivo:** `PlayerProfileApplicationService` como correcto y
  `PlayerProfileOperations` como incorrecto para esa misma responsabilidad.

### STD-P05 — Nombres de repositorios agnósticos a tecnología

- **Secciones relacionadas:** 3.3 y 3.16.
- **Situación actual:** quedan implementaciones con nombres como
  `EntityFrameworkPlayerProfileStore`, mientras las migraciones recientes usan nombres
  como `LeaderboardRepository`.
- **Ajuste pendiente:** definir como convención del proyecto que interfaces e
  implementaciones de repositorio se nombren por la información o agregado que administran,
  sin incluir `EntityFramework`, SQL Server u otra tecnología de persistencia.
- **Ejemplos objetivo:** `PlayerProfileRepository` como correcto y
  `EntityFrameworkPlayerProfileRepository` como incorrecto.

### STD-P06 — Ejemplo de autorización para cambio de nombre

- **Sección relacionada:** 8.1.
- **Situación actual:** la explicación del ejemplo nombra
  `PlayerProfileEditor.ChangeUsername`. La migración acordada sustituirá esa agrupación por
  `ChangeUsernameUseCase`.
- **Ajuste pendiente:** actualizar únicamente la procedencia y los identificadores del
  ejemplo cuando exista el nuevo caso de uso. Conservar la regla: resolver la cuenta desde
  la sesión, validar el nombre en el servidor y no confiar en la validación del cliente.

### STD-P07 — Ejemplo de validación del leaderboard

- **Sección relacionada:** 8.2.
- **Situación actual:** el ejemplo todavía usa `GetLeaderboardHandler`, `LeaderboardQuery`,
  un store y `ServiceUnavailable` como resultado. La implementación aprobada usa
  `GetLeaderboardUseCase`, un límite directo y `ILeaderboardRepository`.
- **Ajuste pendiente:** reemplazar el ejemplo por el caso de uso actual. La validación
  esperada devuelve `InvalidMaximumEntries`; una falla técnica del repositorio se propaga
  como excepción y no se presenta como un resultado falso de indisponibilidad.

### STD-P08 — Resultados esperados y excepciones técnicas

- **Secciones relacionadas:** 6.1, 6.8, 8.1 y 8.2.
- **Situación actual:** el equipo aprobó resultados para validaciones o rechazos esperados
  dentro de aplicación, y excepciones para fallas técnicas inesperadas.
- **Ajuste pendiente:** expresar esa separación una sola vez y referenciarla desde las demás
  secciones. La frontera WCF traduce un rechazo esperado al fault contractual aplicable y
  una excepción técnica a `ServiceFault`.
- **Límite:** no exigir un tipo `Result` universal ni convertir colecciones vacías o valores
  opcionales válidos en errores.

### STD-P09 — Manejador global de errores WCF

- **Secciones relacionadas:** 6.1 y 6.8.
- **Situación actual:** 6.8 permite un manejador centralizado, pero el ejemplo es genérico y
  no muestra la separación real entre `ProvideFault` y `HandleError`.
- **Ajuste pendiente:** usar un ejemplo mínimo basado en `ServiceErrorBehavior`: conservar
  faults contractuales, registrar una sola vez la excepción inesperada y devolver un
  `ServiceFault` sin detalles internos. Aclarar que `HandleError` indica si es seguro
  continuar y que el manejador WCF no cubre tareas o hilos ajenos a una operación remota.
- **Límite:** el `catch (Exception)` general sólo se justifica en esta última frontera o en
  una recuperación local comprobable; no se autoriza como captura silenciosa intermedia.

### STD-P10 — Abstracciones `Command`, `Handler` y `Mapper`

- **Secciones relacionadas:** 3.3, 5.5 y 5.6.
- **Situación actual:** se introdujeron comandos, handlers y mappers sin una decisión previa
  del equipo. Algunos sólo trasladan los mismos datos entre tipos o esconden una operación
  que puede expresarse directamente como caso de uso.
- **Ajuste pendiente:** exigir que una clase de este tipo represente una responsabilidad
  concreta y explicable. Un mapeo usado por una sola frontera puede permanecer como método
  privado; un `Command` no debe duplicar un DTO sin aportar un modelo de entrada propio; un
  `Handler` no debe sustituir el nombre del caso de uso por costumbre.
- **Límite:** la regla no prohibirá estos patrones. Su uso deberá justificarse con una
  necesidad observable del proyecto, no con la existencia del patrón en otros sistemas.

### STD-P11 — Expresiones `switch` y patrones combinados

- **Secciones relacionadas:** 5.12 y las referencias incidentales de 5.19.
- **Situación actual:** el código contiene expresiones `switch` y patrones con `or`, pero el
  estándar no los desarrolla como construcciones revisables. Una mención incidental no es
  suficiente bajo el criterio acordado por el equipo.
- **Ajuste pendiente:** decidir por separado si se incorporará una regla breve con ejemplos
  o si se reemplazarán por instrucciones de bloque ya cubiertas. Hasta esa decisión, no
  interpretar su presencia en código existente como justificación normativa.

## Observaciones ya resueltas que deben conservarse

- Las lambdas simples están definidas en 5.14 y permanecen permitidas dentro de sus límites.
- `Async` ya es una convención vigente en 3.16; la tarea pendiente es simplificar y aplicar
  correctamente la regla, no volver a decidirla.
- La validación preventiva del cliente no sustituye la validación ni la autorización del
  servidor descritas en 8.1 y 8.2.

## Forma de mantenimiento

- Agregar una entrada sólo cuando exista una observación concreta del proyecto o una
  decisión explícita del equipo.
- No copiar secciones completas del estándar ni convertir este archivo en un estándar
  paralelo.
- Marcar o retirar una entrada cuando el cambio correspondiente sea aplicado y verificado.
- Mantener separados los cambios de código, las propuestas normativas y las ediciones
  autorizadas del estándar.
