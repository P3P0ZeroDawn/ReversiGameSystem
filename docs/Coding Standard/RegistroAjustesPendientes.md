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

## Mantenimiento

Cuando aparezca una construcción nueva de C# o .NET, se registra aquí antes de tratarla
como convención. No se agregan bitácoras de migración, decisiones ya resueltas ni ejemplos
copiados del código del proyecto.
