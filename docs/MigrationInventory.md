# Inventario actual de migración

Este inventario muestra únicamente el estado vigente y el siguiente trabajo. El historial
de cambios permanece en Git y no se repite aquí.

## Criterio de finalización

Un servicio queda migrado cuando su flujo es legible, sigue la estructura descrita en
`ServerStructure.md`, no contiene capas o modelos duplicados, comunica correctamente los
fallos esperados y cuenta con verificación proporcional. Todo el servidor se completará
antes de comenzar la revisión equivalente del cliente.

## Estado del servidor

| Funcionalidad | Estado | Siguiente trabajo |
| --- | --- | --- |
| Manejo global de errores | Implementado | Confirmar su uso uniforme al migrar cada ServiceApi. |
| Perfil de jugador | Migrado | Añadir o completar pruebas directas de repositorio y revisar reglas transversales finales. |
| Clasificación | Migrado | Añadir o completar pruebas directas de repositorio y revisar reglas transversales finales. |
| Sesiones | Parcial | Eliminar APIs propias con `out`, excepciones de rechazo y ceremonia restante. |
| Verificación y correo | Parcial | Revisar el bloqueo por identidad, nombres, modelos y pruebas de concurrencia. |
| Partidas y lobby | Parcial | Unificar `MatchResult<T>` y `MatchStatus`, retirar resultados duplicados y excepciones esperadas, y reducir el alcance de los bloqueos. |
| Chat de partida | Parcial | Alinear resultados, repositorio, callbacks y manejo de errores con la migración de partidas. |
| Amistades y presencia | Pendiente | Introducir el ApplicationService necesario, separar estado de transporte y simplificar concurrencia y publicación. |
| Registro de cuenta | Pendiente | Sustituir `UseCase`, `Handler` y Store heredados por el flujo acordado y un repositorio agnóstico a tecnología. |
| Juego después del lobby | Incompleto | Implementar y migrar las operaciones que actualmente permanecen sin soporte. |
| Composición del host | Parcial | Uniformar la construcción después de estabilizar todos los servicios. |

## Orden acordado

1. Partidas, lobby y chat.
2. Amistades y presencia.
3. Sesiones, verificación y correo.
4. Registro de cuenta.
5. Juego y operaciones todavía no soportadas.
6. Revisión transversal de perfil, clasificación, repositorios y host.
7. Verificación completa del servidor.
8. Revisión del cliente con el mismo enfoque.

## Evidencia disponible

En la revisión del 7 de noviembre de 2026, la solución del servidor compiló, las 198
pruebas aisladas pasaron y las comprobaciones reales de base de datos de partidas pasaron.
Esta evidencia corresponde al código revisado en ese momento; cada migración posterior
debe volver a ejecutar las comprobaciones afectadas.

## Límites documentales

- El catálogo WCF conserva la autoridad sobre contratos y compatibilidad remota.
- Las observaciones académicas conservan el registro original de cada revisión.
- Este archivo no describe pasos históricos, comandos de despliegue ni propuestas del
  estándar.
