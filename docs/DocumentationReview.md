# Revisión inicial de fuentes documentales

Fecha: 19 de octubre de 2026.

## Alcance y resultado

Se inventariaron las cuatro carpetas nuevas y se revisaron la estructura de los
documentos, sus fuentes, decisiones y puntos de conciliación con la documentación
preexistente. Es una revisión documental inicial, no una auditoría completa de código,
una validación visual de todas las figuras ni una comprobación de cumplimiento académico.

Se incorporaron 12 archivos Markdown. Al inicio de la revisión, `docs` contenía 14 Markdown,
202 imágenes PNG y un SVG; `ValidationRules.md` permanece en la raíz del repositorio.
Los enlaces locales comprobados en los dos archivos de este inventario son relativos.

Actualización del 19 de octubre de 2026: se recibió la plantilla académica del SAD con
las listas de comprobación para ambas entregas. El equipo confirmó que `Documento-Entrega.md`
es una entrega parcial histórica que debe conservarse intacta.

Aclaración posterior del equipo: todos los materiales serán contexto histórico para la
construcción y serán editados principalmente de forma manual por sus integrantes. No se
creará ningún SAD ni se mantendrán documentos automáticamente como parte de este trabajo.
Los agentes podrán sugerir adiciones o modificaciones, especialmente en el estándar,
pero solo las aplicarán ante una solicitud explícita del equipo.

La existencia de los materiales permite continuar con la organización documental.
Las discrepancias siguientes son información para interpretar las fuentes y sugerencias
para futuras solicitudes. No autorizan cambios en documentos, no exigen corregir todas
las fuentes antes de preparar el contexto y no prueban incumplimientos del código actual.

## Hallazgos y acciones propuestas

### DOC-01: navegación heredada de la exportación

Fuente: [información general](Design%20Project/Proyecto%20Info%20General.md), enlaces
finales a las cinco iteraciones y al documento de entrega.

Los seis enlaces apuntaban a una subcarpeta `Proyecto` y a nombres con identificadores
de exportación que no correspondían a los archivos aportados. Los destinos existen con
otros nombres en la misma carpeta que el documento general.

Estado: resuelto por solicitud explícita del equipo. Se corrigieron exclusivamente los
seis destinos hacia las cinco iteraciones y el SAD, sin cambiar el texto del diseño.

Comprobación posterior: los seis enlaces son de navegación a documentos, no a imágenes.
Se verificaron 203 referencias locales de imágenes en los Markdown de `docs`; todas
resuelven a archivos existentes. La comprobación no evalúa su contenido visual.
No fue necesario reparar referencias de imágenes.

### DOC-02: supuestos actuales e históricos dentro del mismo documento

Fuentes: [información general](Design%20Project/Proyecto%20Info%20General.md), secciones
«Supuestos», «Acuerdos con STK-06» y «Revisar entradas»;
[quinta iteración](Design%20Project/Iteraci%C3%B3n%205%20-Refinamiento.md);
[SAD](Design%20Project/Documento-Entrega.md), secciones 1.2 y 3.1.

El documento general actualiza el alcance a 8 jugadores y pospone el modo offline,
pero vuelve a enumerar al final los supuestos de 20 jugadores y modo offline sin
marcarlos allí como históricos. Las primeras iteraciones conservan decisiones antiguas.
El SAD sí distingue las decisiones sustituidas: ADR-001 por ADR-004 y ADR-002 por ADR-005.

Sugerencia para una futura solicitud: aclarar las referencias repetidas del documento general.
Las fuentes e iteraciones se conservan como historial. Los acuerdos registrados describen prioridad online,
8 jugadores en 4 partidas, tres niveles y motor autoritativo en servidor con validador
ligero independiente en cliente. No se ha demostrado aquí su implementación o capacidad.

### DOC-03: observación sobre asincronía en contratos WCF

Fuentes: [observaciones](Academic%20Observations/Observaciones.md), revisión 3;
[catálogo WCF](WcfIntegrationContracts.md), secciones 2 y 3.1.

La observación indica que el método del contrato no debería llevar `Async` y distingue
el trabajo asíncrono de repositorios, cliente y servidor. El catálogo documenta `Async`
y `Task` cuando hay espera real de recursos.

Sugerencia para una tarea que afecte esos contratos: precisar si la observación afecta al nombre público de la operación,
a su firma CLR, al proxy generado o a su implementación. Registrar la decisión y su
justificación antes de cambiar el protocolo, regenerar proxies o imponer una regla.
La revisión documental no determina por sí sola cuál interpretación es la correcta.

### DOC-04: excepciones y resultados remotos pendientes de conciliación

Fuentes: [observaciones](Academic%20Observations/Observaciones.md), revisiones 2 y 3;
[catálogo WCF](WcfIntegrationContracts.md), secciones 14 y 15;
[estándar](Coding%20Standard/C%23%20Video%20Game%20Development%20Standard.md), sección 6.

Las notas piden revisar la representación de entradas y salidas por operación y cuándo
comunicar fallos al cliente. El catálogo ya prescribe outcomes tipados y un `ServiceFault`
controlado, mientras el estándar prohíbe exponer detalles internos en respuestas al jugador.

Sugerencia para una futura solicitud: documentar el criterio por operación y distinguir rechazo esperado,
fallo técnico y mensaje localizado. No interpretar la nota como autorización para
transferir excepciones internas o reemplazar automáticamente todos los outcomes.

### DOC-05: estándar completo frente al registro de cumplimiento

Fuentes: [estándar](Coding%20Standard/C%23%20Video%20Game%20Development%20Standard.md),
secciones 5.8 y 6; [registro de cumplimiento](CodingStandardCompliance.md).

El estándar establece un máximo de tres parámetros, exige aprobación documentada para
excepciones y permite un cuarto parámetro `ILogger<T>` en el caso descrito en 5.8.
El registro anterior acepta categorías más amplias de excepciones. También aplaza logging
centralizado para el hito de ejecución local, mientras el estándar completo lo exige.

Sugerencia para una tarea afectada: verificar la vigencia y el origen de cada excepción y aplazamiento.
Conservarlos como registro histórico hasta conciliar su alcance. No convertir el aplazamiento
en una exención permanente ni afirmar cumplimiento actual sin examinar el código.

### DOC-06: apartados todavía sin completar

Fuentes: [estándar](Coding%20Standard/C%23%20Video%20Game%20Development%20Standard.md),
secciones 10 y 11; [SAD](Design%20Project/Documento-Entrega.md), secciones 4.3, 5 y 7;
[información general](Design%20Project/Proyecto%20Info%20General.md), «Definition of Done».

Las secciones de colaboración y cumplimiento automatizado del estándar están vacías.
El SAD contiene tablas y marcadores para entrega final, incluyendo evidencias y transición
a construcción. La sección «Definition of Done» del documento general no tiene contenido.

Sugerencia si el equipo solicita completar una entrega: registrar primero su alcance.
Los marcadores para entrega final no son por sí mismos un incumplimiento de la entrega parcial.
Las evidencias deben proceder de verificaciones realizadas, no de resultados supuestos.

La [plantilla recibida](Design%20Project/documento_de_arquitectura_de_software_plantilla.md)
confirma expresamente que las secciones ajenas a la entrega en curso pueden quedar vacías.
El equipo indicó conservar intacto el SAD parcial: sus apartados de entrega final no se
completarán allí. No se crearán documentos ni se completarán estos apartados como parte
de la preparación del contexto. Una tarea documental futura requerirá solicitud explícita.

### DOC-07: plantilla recibida y referencias académicas adicionales

Se recibió la [plantilla del SAD](Design%20Project/documento_de_arquitectura_de_software_plantilla.md).
Incluye instrucciones, alcance por entrega y listas de comprobación; ya no se consideran
materiales faltantes. La versión no está rotulada explícitamente en el archivo aportado,
por lo que no se certifica que corresponda a la denominación v5.0 citada en el SAD.

En «Cómo usar esta plantilla», punto 6, se establece que el Manual o la Evaluación vigentes
prevalecen sobre ella y que la rúbrica distingue un trabajo sobresaliente. También se remite
a un ejemplo resuelto del Sistema de Gestión Hotelera en el punto 5. No se localizaron
archivos independientes identificados como esos materiales entre los aportados.

Continuación permitida: organizar el contexto y la especificación neutral con las fuentes
disponibles. Incorporar el Manual, la Evaluación o rúbrica y el ejemplo si el equipo dispone
de ellos; hasta entonces, no certificar cobertura completa de los criterios académicos.
No inventar requisitos para sustituir las referencias ausentes.

### DOC-08: separar alcance esperado de evidencia de funcionamiento

Fuentes: [observaciones](Academic%20Observations/Observaciones.md), revisión 1;
[SAD](Design%20Project/Documento-Entrega.md), secciones 2.6 y 7;
[catálogo WCF](WcfIntegrationContracts.md), sección 5.1;
[avance de internacionalización y datos](Project%20Specs/Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces.md).

La capacidad de 8 jugadores es un supuesto operativo que debe comprobarse. Las tablas
de decisiones y drivers abordados no demuestran que una función esté implementada.
Los avances de Tecnologías describen distintas etapas y no sustituyen la matriz de
integración actual ni una prueba ejecutada.

Criterio de lectura: distinguir requisito, decisión, estado de implementación y evidencia
al usar los materiales como contexto. Las notas indican pruebas de integración obligatorias y pruebas
de sistema no obligatorias; conservar esa distinción al definir la verificación académica.

## Continuación centrada en el contexto

1. Utilizar el índice para encontrar las fuentes pertinentes y sus límites históricos.
2. Preparar la especificación neutral del equipo: responsabilidades, fuentes, procedimientos,
   entregables y reglas para sugerir cambios sin modificar automáticamente los materiales.
3. Organizar la bóveda y el piloto de colaboración para facilitar la edición manual del equipo.
4. Crear posteriormente la adaptación local de agentes y procedimientos, excluida de Git.

Las diferencias documentales se consultarán cuando afecten una tarea. Se podrán sugerir
mejoras con su justificación, pero los materiales permanecerán intactos hasta que el equipo
solicite una edición concreta. No se creará un SAD, otra versión del SAD ni nuevos entregables
de diseño como consecuencia automática de este registro.

No se instalaron herramientas, configuraron agentes, ejecutaron pruebas funcionales,
modificaron contratos ni generaron evidencias de rendimiento durante esta revisión.
