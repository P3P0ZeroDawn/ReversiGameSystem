# Documentación de Reversi

Este índice organiza las fuentes disponibles para el equipo humano y para cualquier
herramienta de asistencia. No depende de un proveedor, modelo ni formato de agentes.

Inventario inicial: 19 de octubre de 2026. Todos los materiales se consideran referencias
históricas para guiar la construcción del proyecto. Esa clasificación no los descarta:
el estándar, los requisitos, los acuerdos y las decisiones documentadas aportan el contexto
que debe consultar el equipo. Su incorporación no demuestra por sí sola su vigencia ni
que las funciones descritas estén implementadas.

## Fuentes disponibles

| Fuente | Uso | Precaución |
| --- | --- | --- |
| [Estándar de desarrollo](Coding%20Standard/C%23%20Video%20Game%20Development%20Standard.md) | Reglas de código, errores, logging, seguridad y pruebas. | Conciliar las excepciones existentes; las secciones 10 y 11 están vacías. |
| [Observaciones académicas](Academic%20Observations/Observaciones.md) | Notas de tres revisiones y acuerdos de alcance. | Distinguir comentarios, decisiones y puntos todavía por resolver. |
| [Información general de diseño](Design%20Project/Proyecto%20Info%20General.md) | Interesados, requisitos, restricciones, escenarios y acuerdos. | Contiene supuestos históricos repetidos; los seis enlaces de navegación se repararon. |
| [Documento de arquitectura SAD](Design%20Project/Documento-Entrega.md) | Síntesis arquitectónica, ASR, ADR, componentes, vistas y riesgos. | Es una entrega parcial v0.1; conserva apartados para la entrega final. |
| [Plantilla académica del SAD](Design%20Project/documento_de_arquitectura_de_software_plantilla.md) | Estructura, instrucciones y listas de comprobación de la primera entrega y la final. | Remite al Manual y a la Evaluación vigentes; no los sustituye. |
| [Primera entrega de Tecnologías](Project%20Specs/PRIMERA%20ENTREGA%20PROYECTO.md) | Análisis CRUD, casos de uso CU-01 a CU-28 y diseño de datos. | Es un entregable del equipo; no sustituye una rúbrica o consigna original. |
| [Segunda entrega de Tecnologías](Project%20Specs/SEGUNDA%20ENTREGA%20PROYECTO.md) | Explicación y capturas de internacionalización. | Las capturas y descripciones corresponden al avance documentado. |
| [Internacionalización y acceso a datos](Project%20Specs/Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces.md) | Registro del avance de localización e integración con Entity Framework. | Describe una etapa con ejecución local; no usar como estado actual de WCF. |
| [Catálogo de integración WCF](WcfIntegrationContracts.md) | Contratos, callbacks, límites, errores y matriz de integración. | Conciliar la revisión académica más reciente antes de cambiar contratos. |
| [Registro de cumplimiento](CodingStandardCompliance.md) | Reglas aplicadas, excepciones y un aplazamiento de logging. | Es un registro de una corrección anterior, no el estándar completo. |
| [Reglas de validación](../ValidationRules.md) | Políticas de entrada y comportamiento de los flujos. | Conserva descripciones del prototipo que deben contrastarse con el código actual. |

## Historial de diseño con ADD

Las iteraciones registran el proceso de diseño. El SAD es el documento que sintetiza
sus resultados; ambos tienen propósitos distintos y deben conservar su trazabilidad.

1. [Primera iteración](Design%20Project/Primera%20iteraci%C3%B3n.md).
2. [Segunda iteración](Design%20Project/Segunda%20iteraci%C3%B3n.md).
3. [Tercera iteración](Design%20Project/Tercera%20iteraci%C3%B3n.md).
4. [Cuarta iteración](Design%20Project/Cuarta%20iteraci%C3%B3n.md).
5. [Quinta iteración: refinamiento](Design%20Project/Iteraci%C3%B3n%205%20-Refinamiento.md).

Las decisiones históricas no deben convertirse en instrucciones vigentes por el solo
hecho de aparecer en una iteración. El SAD marca ADR-001 y ADR-002 como sustituidos
por ADR-004 y ADR-005, respectivamente.

## Uso y modificación de los materiales

Por indicación del equipo el 19 de octubre de 2026, todos los materiales se conservarán
como contexto histórico y no tendrán mantenimiento automático. El equipo humano será
el principal responsable de editarlos manualmente. Una solicitud de implementación de
código no autoriza por sí sola a actualizar los documentos de referencia.

Los agentes pueden sugerir agregar, corregir o aclarar información, especialmente en el
estándar, cuando detecten una necesidad durante la construcción. Cada sugerencia debe
identificar el documento y la sección, la razón y el efecto esperado. Solo se aplicará
si el equipo solicita explícitamente esa modificación; la sugerencia no cambia la regla.
Una petición explícita de edición autoriza el trabajo en su alcance sin otra confirmación.

Toda propuesta o edición del estándar debe seguir el
[procedimiento de modificación del estándar](Coding%20Standard/StandardChangeProtocol.md).
El contenido del estándar se prepara en inglés. La “Auditable Verification Chain” y
“De qué parte del proyecto obtuve la información” se entregan aparte, en español, y no
se incorporan al estándar. Ese procedimiento no autoriza ediciones automáticas.

`Documento-Entrega.md` se conserva intacto como entrega parcial histórica. La preparación
del contexto y del equipo de agentes no incluye crear, copiar ni completar un SAD nuevo,
ni completar entregables o reescribir las fuentes. Cualquier trabajo documental futuro
se realizará únicamente cuando el equipo lo solicite.

La plantilla permite dejar vacías las secciones que no corresponden a la entrega en curso.
Por ello, un marcador de entrega final en el SAD parcial no es por sí mismo un defecto.

## Lectura según la tarea

- Para cambios de código, consultar las secciones pertinentes del estándar y las
  excepciones registradas. Si una diferencia afecta materialmente la tarea, señalarla
  y aclarar la decisión aplicable sin editar la fuente ni inventar excepciones.
- Para arquitectura, consultar el SAD histórico y las iteraciones que justifican la decisión,
  contrastándolos con acuerdos posteriores y con las instrucciones explícitas del equipo.
- Para comportamiento funcional, consultar el caso de uso correspondiente y sus reglas.
- Para integración remota, consultar el catálogo WCF y las observaciones relacionadas.
- Para describir el estado implementado, verificar código y evidencia; una decisión,
  captura o tabla histórica no demuestra por sí sola el comportamiento actual.
- Para cambios manuales, leer la versión disponible en disco al comenzar la tarea pertinente;
  no depender de copias o resúmenes antiguos de una conversación.
- Para cambios documentales, proponer ajustes cuando sean útiles y aplicarlos solo por
  solicitud explícita, conservando el origen y sin presentar un borrador como acuerdo.
- Las instrucciones explícitas actuales del equipo prevalecen sobre propuestas de este
  índice. Distinguir lo que dicen los materiales, lo observado en código y las recomendaciones
  del agente; no convertir una recomendación en decisión aprobada.

## Especificación neutral del equipo

La [especificación del equipo](agent-team/TeamSpecification.md) define siete roles y las
reglas comunes para trabajar con el contexto del proyecto. Se complementa con:

- [Plantilla de rol](agent-team/RoleTemplate.md).
- [Catálogo de procedimientos compartidos](agent-team/Procedures.md).
- [Guía de reconstrucción y validación](agent-team/ReconstructionAndValidation.md).

Son documentos compartidos independientes de herramientas y proveedores. No registran
agentes ejecutables ni instalan skills. Los perfiles consultan las fuentes históricas y
el procedimiento del estándar, respetando la edición explícita y el mantenimiento manual.

## Pendientes y siguiente paso

El [registro de revisión documental](DocumentationReview.md) detalla las diferencias,
los materiales que no se localizaron y las sugerencias que pueden retomarse si se solicitan.

El objetivo es utilizar el contexto y la especificación neutral de roles y procedimientos
para guiar la construcción. La adaptación ejecutable local sigue pendiente. Las diferencias detectadas
sirven para orientar la consulta y señalar dudas relevantes; no son una lista autorizada
de correcciones a los materiales históricos ni obligan a resolverlas todas antes de avanzar.

La organización de la bóveda y el piloto de colaboración facilitarán las ediciones manuales
del equipo. La ausencia del Manual o la Evaluación no impide organizar los materiales ni
preparar el contexto, pero limita una revisión completa de cumplimiento académico.

## Archivos y herramientas

Los documentos y sus imágenes forman parte del material del proyecto y son candidatos
a control de versiones. Se conservan las rutas de los adjuntos para no romper enlaces.
Este índice no instala ni configura un editor, servicio de colaboración o agente.

La adaptación ejecutable de agentes, las preferencias personales y la instalación local
de procedimientos quedarán fuera del control de versiones cuando se configuren.
La especificación neutral y los procedimientos compartidos están documentados por separado
y respetan la política de consulta, sugerencias y edición explícita descrita aquí.
