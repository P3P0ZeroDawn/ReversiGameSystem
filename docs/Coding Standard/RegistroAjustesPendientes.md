# Ajustes pendientes al estándar

Este registro contiene únicamente decisiones todavía no incorporadas al estándar. No es
normativo. Cada ajuste aprobado debe seguir `StandardChangeProtocol.md` y eliminarse de
este archivo cuando quede integrado.

## Formato acordado

Cada regla del estándar debe contener una descripción breve, un ejemplo correcto y un
ejemplo incorrecto. El estándar permanece agnóstico a proyectos y estructuras concretas.

## Pendientes

### 1. Flexibilización de `{Capability}Models.cs` para entidades livianas (Sección 4.11)

- **Sección afectada:** 4.11 (Source File Organization).
- **Modificación a realizar:** Permitir que los archivos contenedores `{Capability}Models.cs` agrupen no solo modelos pasivos y enumeraciones de estado, sino también entidades de dominio livianas y cohesivas exclusivas de esa capacidad, manteniendo la prohibición estricta sobre tipos `Exception`, interfaces, servicios de aplicación o implementaciones de repositorio.
- **Motivo / Justificación:**
  1. **Consistencia estructural:** En la capa de Application, capacidades como `Chat`, `Leaderboards` y `Sessions` agrupan sus modelos y estados en un solo archivo representativo (`*Models.cs`). Obligar a separar una entidad liviana (como `Match`) en un archivo aislado de pocas líneas genera inconsistencia organizativa.
  2. **Escala y complejidad del proyecto:** Las entidades del sistema poseen un número reducido de objetos y lógica de estado compacta, tanto en el estado actual como en su evolución prevista, por lo que un archivo contenedor por capacidad resulta más mantenible y legible sin incurrir en acoplamiento indebido.

### 2. Uso de funciones locales (`local functions`) frente a métodos privados o lambdas

- **Sección afectada:** Por definir (posiblemente Sección 4.11 Source File Organization o Sección 5.5 Method Responsibility / 5.14 Lambda Expressions).
- **Modificación a realizar:** Verificar si el estándar contempla o permite el uso de funciones locales (por ejemplo, `async Task<LobbySnapshot> RequestDurationChange()` dentro de `ChangeTurnDuration`) como alternativa legible a lambdas multilínea o métodos privados auxiliares de un solo uso.
- **Motivo / Justificación:** En `RemoteMatchClient.ChangeTurnDuration` se propuso encapsular la secuencia asíncrona en una función local para evitar lambdas multilínea anidadas dentro de `SendMatchCall` sin fragmentar la clase en métodos privados granulares.
- **Nota de revisión:** Si eres codex y no se menciona en el estandar, revisa esto con Pedro, no lo incluyas hasta que el te lo permita.

## Mantenimiento

Cuando aparezca una construcción nueva de C# o .NET, se registra aquí antes de tratarla
como convención. No se agregan bitácoras de migración, decisiones ya resueltas ni ejemplos
copiados del código del proyecto.
