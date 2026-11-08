# Documentación de Reversi

Este índice separa las fuentes vigentes de los entregables históricos. El código y las
pruebas deben consultarse para confirmar el estado implementado.

## Fuentes vigentes

| Fuente | Uso |
| --- | --- |
| [Estándar de C#](Coding%20Standard/CSharp%20Video%20Game%20Development%20Standard.md) | Reglas generales de código, errores, seguridad y pruebas. |
| [Registro de ajustes del estándar](Coding%20Standard/RegistroAjustesPendientes.md) | Indica si existe alguna decisión aprobada todavía no incorporada al estándar. |
| [Procedimiento del estándar](Coding%20Standard/StandardChangeProtocol.md) | Evidencia y formato requeridos para modificar el estándar. |
| [Observaciones académicas](Academic%20Observations/Observaciones.md) | Registro original de las revisiones académicas. |
| [Estructura del servidor](ServerStructure.md) | Recorrido, responsabilidades, archivos y fronteras de una funcionalidad. |
| [Catálogo WCF](WcfIntegrationContracts.md) | Firmas, callbacks, datos serializados, faults y enumeraciones. |
| [Inventario de migración](MigrationInventory.md) | Estado actual y siguiente trabajo de cada servicio. |
| [Reglas de validación](../ValidationRules.md) | Políticas funcionales de entrada y comportamiento. |

## Entregables y diseño histórico

- [Información general](Design%20Project/Proyecto%20Info%20General.md).
- [Documento de arquitectura entregado](Design%20Project/Documento-Entrega.md).
- [Plantilla académica](Design%20Project/documento_de_arquitectura_de_software_plantilla.md).
- [Primera entrega](Project%20Specs/PRIMERA%20ENTREGA%20PROYECTO.md).
- [Segunda entrega](Project%20Specs/SEGUNDA%20ENTREGA%20PROYECTO.md).
- Iteraciones ADD dentro de `Design Project`.

Estos archivos explican requisitos y decisiones anteriores, pero no demuestran por sí solos
que una función siga implementada de esa manera. `Documento-Entrega.md` se conserva intacto
y no se crea un SAD adicional.

## Forma de trabajo

- Leer solamente la fuente y sección necesarias para la tarea.
- Distinguir requisitos, decisiones históricas, código observado y recomendaciones.
- No modificar documentos históricos como consecuencia automática de cambiar código.
- Redactar el estándar en inglés y entregar su justificación y evidencia por separado en
  español, conforme a su procedimiento.
- Mantener el inventario y los índices breves; Git conserva el historial.

## Coordinación

La [especificación del equipo](agent-team/TeamSpecification.md) contiene los acuerdos de
trabajo. Se complementa con [procedimientos](agent-team/Procedures.md) y la
[guía de reconstrucción y validación](agent-team/ReconstructionAndValidation.md).
