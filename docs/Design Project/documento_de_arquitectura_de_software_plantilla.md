UNIVERSIDAD VERACRUZANA

FACULTAD DE ESTADÍSTICA E INFORMÁTICA

LICENCIATURA EN INGENIERÍA DE SOFTWARE

Diseño de Software

| Proyecto | \[Nombre del videojuego o sistema\] | 
| ----- | ----- | 
| Equipo e integrantes | $$ Identificador del equipo / nombre completo de cada integrante $$ | 
| Versión del documento | \[v0.1, v1.0...\] | 
| Entrega | $$ Primera entrega / Entrega final $$ | 
| Fecha | $$ dd/mm/aaaa $$ | 

# Documento de Arquitectura de Software (SAD)

## Plantilla del proyecto final

### Cómo usar esta plantilla

1. Llenen lo que corresponde a la entrega en curso (primera entrega o entrega final). Lo demás puede quedar vacío por ahora.

2. Documenten a fondo sólo lo que obliga a la arquitectura a tomar cierta forma. Si algo no aplica, díganlo en una línea y expliquen por qué.

3. Usen sólo siete identificadores: QAS (escenario de calidad), ASR, ADR, C (módulo o componente), V (vista), EV (evidencia) y R (riesgo o pendiente). Todo lo demás se nombra.

4. Cada decisión se escribe una sola vez, en su ficha de la sección 3. Las demás secciones la citan por su número de ADR.

5. Antes de empezar, lean el ejemplo resuelto del Sistema de Gestión Hotelera: muestra cada sección bien llenada.

6. Esta plantilla pide lo necesario para un trabajo logrado; lo que distingue un trabajo sobresaliente está en la rúbrica. Si algo aquí contradice el Manual o la Evaluación vigentes, mandan esos documentos. Si ya empezaron con otra versión de la plantilla, trasladen su contenido: el formato anterior no se recalifica.

La cadena que el documento debe permitir seguir: escenario, función o restricción $\rightarrow$ ASR $\rightarrow$ ADR $\rightarrow$ elemento C $\rightarrow$ vista V $\rightarrow$ evidencia EV $\rightarrow$ riesgo residual R

## 1. Control y glosario

*Primera entrega*

Mantengan esta sección breve y actualícenla en cada entrega.

### 1.1 Control e historial

| Sistema | \[Nombre y descripción en una línea\] |  | 
| ----- | ----- | ----- | 
| Propósito del documento | \[Qué permitirá hacer a quien lo lea\] |  | 
| Audiencia | \[Quién lo leerá y para qué\] |  | 
| Alcance | \[Qué parte del sistema cubre y qué queda fuera\] |  | 
| **Versión** | **Fecha** | **Cambios principales** | 
| \[v0.1\] | $$ dd/mm/aaaa $$ | \[Qué se agregó o corrigió\] | 

### 1.2 Resumen de la arquitectura

Máximo una página: problema, drivers principales, estructura general, decisiones esenciales y riesgos abiertos. Conviene escribirlo al final de cada entrega.

\[Resumen.\]

### 1.3 Glosario

| Término | Significado en este proyecto | 
| ----- | ----- | 
| \[Término\] | \[Definición\] | 

### 1.4 Uso de IA generativa

Una fila por uso significativo. No anexen conversaciones.

| Qué pidieron | Qué aceptaron y qué rechazaron | Cómo lo verificaron | 
| ----- | ----- | ----- | 
| \[...\] | \[...\] | \[...\] | 

## 2. Drivers

*Primera entrega*

Expliquen qué obliga a la arquitectura a tomar cierta forma. Un ASR puede venir de un escenario de calidad, pero también de una función, una restricción, un objetivo de negocio o una condición del equipo.

### 2.1 Contexto y frontera

En un párrafo: qué está dentro del sistema, qué queda fuera y con qué sistemas externos se comunica. El diagrama va en la sección 6 como V-01; aquí sólo refiéranlo.

\[Descripción breve.\]

### 2.2 Interesados y preocupaciones

Incluyan sólo a quienes tienen una preocupación que puede cambiar la arquitectura. Una preocupación es lo que le importa a alguien, no una función del sistema.

| Interesado | Qué le preocupa | 
| ----- | ----- | 
| \[Rol\] | \[...\] | 

### 2.3 Funciones clave

Usen los casos de uso o las historias de usuario que ya tienen, con sus identificadores. Señalen cuáles obligan a tomar decisiones arquitectónicas. Las demás pueden seguir la estructura general del sistema y documentarse sólo cuando sea necesario.

| Función (CU o HU) | ¿Arquitectónicamente significativa? | Por qué | 
| ----- | ----- | ----- | 
| \[CU-01 nombre\] | $$ Sí / No $$ | \[Qué la vuelve difícil: concurrencia, dinero, estado compartido, fallas...\] | 

### 2.4 Restricciones, supuestos y condiciones del negocio

Una restricción viene impuesta: el equipo no la decide. Un supuesto es algo que creen cierto pero no han comprobado. Si una fila habla de tiempos de respuesta o de carga, probablemente es un escenario (2.5).

| Tipo | Enunciado | Qué pasa si cambia o resulta falso | 
| ----- | ----- | ----- | 
| Restricción | \[...\] | \[...\] | 
| Supuesto | \[...\] | \[...\] | 
| Objetivo de negocio | \[...\] | \[...\] | 
| Condición organizacional | \[Equipo, calendario, soporte...\] | \[...\] | 

### 2.5 Escenarios de calidad y árbol de utilidad

Entre 8 y 12 escenarios medibles; copien la ficha para cada uno. La respuesta dice qué hace el sistema y la medida dice cómo se comprueba. No escriban la solución dentro del escenario.

| QAS- \[atributo de calidad\] | \[nombre corto\] | 
| ----- | ----- | 
| Fuente | \[Quién o qué provoca el estímulo\] | 
| Estímulo | \[Qué ocurre\] | 
| Entorno | \[En qué condición está el sistema\] | 
| Artefacto | \[Qué parte del sistema se ve afectada\] | 
| Respuesta | \[Qué hace el sistema\] | 
| Medida | \[Número verificable y cómo se comprueba\] | 
| Prioridad | \[(importancia, dificultad) con A, M o B\] | 

**Escenario de seguridad.** Al menos uno. Agreguen a su ficha tres filas: activo que se protege, actor de amenaza y límite de confianza.

**Árbol de utilidad:** insértenlo aquí o usen una tabla equivalente. Prioricen por importancia y dificultad.

\[Árbol de utilidad.\]

### 2.6 ASR prioritarios

Entre 5 y 7. Si todos vienen de escenarios, expliquen en una línea por qué ninguna función, restricción, objetivo o condición del equipo obliga a estructurar el sistema.

| ASR | Qué debe cumplir la arquitectura | Procedencia | Viene de | 
| ----- | ----- | ----- | ----- | 
| ASR-01 | \[Enunciado\] | $$ Función / Escenario / Restricción / Objetivo de negocio / Condición organizacional $$ | $$ QAS-\_ / CU-\_ o nombre de la restricción $$ | 

## 3. Decisiones

*Primera entrega: 4 a 6 ADR*

Una ficha por decisión esencial. Cada decisión compara al menos dos alternativas reales. Una decisión que cambia no se borra: se marca como sustituida y la nueva la menciona.

### 3.1 Índice de decisiones

| ADR | Decisión | Estado | ASR que atiende | 
| ----- | ----- | ----- | ----- | 
| ADR-001 | \[Título en forma de decisión\] | $$ Propuesto / Aceptado / Rechazado / Sustituido por ADR-\_ $$ | \[ASR-\_\] | 

### 3.2 Fichas de decisión

| ADR- \[título en forma de decisión\] |  | 
| ----- | ----- | 
| Estado | $$ Propuesto / Aceptado / Rechazado / Sustituido por ADR-\_ $$ | 
| Contexto | \[Qué obliga a decidir y qué ASR están en juego\] | 
| Alternativas | \[A. ... A favor: ... En contra: ...\] | 
|  | \[B. ... A favor: ... En contra: ...\] | 
| Criterios | \[Con qué se compararon, a partir de los ASR\] | 
| Decisión | \[Qué se eligió y, si aplica, qué táctica o patrón usa\] | 
| Consecuencias | \[Qué se gana y qué se paga\] | 
| Riesgos | $$ R-\_ $$ | 
| Elementos afectados | $$ C-\_ $$ | 

## 4. Estructura

*Primera entrega inicia*

*Entrega final completa*

Describan los elementos y sus reglas con texto y tablas. Los diagramas van en la sección 6 y usan estos mismos identificadores.

### 4.1 Módulos y componentes

| ID | Nombre y tipo | Responsabilidad | Provee / requiere | 
| ----- | ----- | ----- | ----- | 
| C-01 | \[Nombre módulo o componente\] | \[Una frase\] | \[Interfaces\] | 

Lo repetitivo se resuelve por convención: escriban la convención una vez, debajo de la tabla.

### 4.2 Interfaces críticas

*Entrega final*

Sólo las interfaces cuyo incumplimiento produce un riesgo real: dinero, estado compartido, datos personales...

| Interfaz | Provista por / usada por | Operaciones | Contrato: condiciones de entrada, resultado garantizado y errores relevantes | 
| ----- | ----- | ----- | ----- | 
| \[Nombre\] | $$ C-\_ / C-\_ $$ | \[...\] | \[...\] | 

### 4.3 Reglas de dependencia

*Entrega final*

| Regla | Permitida o prohibida | Por qué (ADR) | 
| ----- | ----- | ----- | 
| \[...\] | \[...\] | \[ADR-\_\] | 

## 5. Arquitectura en ejecución

*Entrega final*

Cada preocupación tiene una decisión, o un «no aplica» con su razón. No es una lista de tecnologías: toda tecnología mencionada se conecta con un ADR o un ASR.

| Preocupación | Decisión, o «no aplica porque...» | ADR / ASR | Vista o evidencia | 
| ----- | ----- | ----- | ----- | 
| Distribución y comunicación | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Datos, estado y propiedad | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Concurrencia y coordinación | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Resiliencia y disponibilidad | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Seguridad y límites de confianza | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Despliegue y entornos | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Observabilidad y operación | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 
| Costo operacional | \[...\] | \[...\] | $$ V-\_ / EV-\_ $$ | 

## 6. Vistas

*Primera entrega: V-01 y V-02*

*Entrega final completa*

Una vista existe para responder a una audiencia concreta. Usen la notación que mejor sirva a ese propósito; en el curso se practican UML y C4.

### 6.1 Tipos de vista que usaremos

En ISO 42010 estos tipos se llaman viewpoints. No tienen que inventarlos: elijan los que necesiten. Agreguen otro sólo si una preocupación importante queda sin atender.

| Tipo de vista | Qué preocupaciones atiende | Representaciones útiles | 
| ----- | ----- | ----- | 
| Contexto | Frontera del sistema, usuarios y sistemas externos | Diagrama de contexto; C4 nivel 1 | 
| Estructura | Módulos, componentes, interfaces y dependencias | UML de componentes o de paquetes; C4 niveles 2 y 3 | 
| Ejecución | Interacciones, estado compartido, concurrencia y fallas | UML de secuencia, de actividad o máquina de estados | 
| Despliegue | Nodos, comunicación, límites de confianza y redundancia | UML de despliegue | 

### 6.2 Vistas

Copien la ficha para cada vista y coloquen el diagrama justo debajo. Declaren al menos una correspondencia con otra vista: qué debe coincidir entre ambas.

| V- \[nombre de la vista\] |  | 
| ----- | ----- | 
| Tipo de vista | $$ Contexto / Estructura / Ejecución / Despliegue $$ | 
| Para quién y para qué | \[Audiencia y lo que la vista le permite entender o decidir\] | 
| Preocupaciones que atiende | \[...\] | 
| Notación y leyenda | \[UML, C4, tabla...; qué significa cada símbolo si no es obvio\] | 
| Se corresponde con | \[V-\_: qué debe coincidir entre ambas\] | 

\[Diagrama\]

## 7. Evaluación, trazabilidad y transición

*Primera entrega: sólo 7.3*

*Entrega final*

Muestren con evidencia qué tan bien responde la arquitectura a sus ASR, qué queda pendiente y qué necesita saber quien la construya.

### 7.1 Evaluación y trazabilidad de los ASR

*Entrega final*

Una fila por ASR. Esta tabla es la matriz de trazabilidad: no la repitan en otro lugar. En «Resultado» no escriban sólo «cumple»: digan qué mostró la evidencia.

| ASR | ADR | Elementos | Vista | Evidencia | Resultado | Riesgo residual | 
| ----- | ----- | ----- | ----- | ----- | ----- | ----- | 
| ASR-01 | \[...\] | \[...\] | V-\_ | \[EV-\_\] | \[Qué mostró la evidencia\] | $$ R-\_ $$ | 

### 7.2 Evidencias

*Entrega final*

Registren aquí cada EV que citen. Una evidencia puede ser una revisión por escenario, una medición, una prueba o un análisis. Es obligatorio incluir al menos una verificación empírica ejecutada y una comprobación de conformidad.

| EV | Tipo | Qué se hizo y qué resultó | Archivo o referencia | 
| ----- | ----- | ----- | ----- | 
| EV-01 | $$ Revisión / Medición / Prueba / Análisis $$ | \[...\] | $$ evidencia/... $$ | 

#### Verificación empírica. EV-\_

| Verificación empírica. EV-\_ |  | 
| ----- | ----- | 
| Afirmación que se pone a prueba | \[Qué esperan de la arquitectura\] | 
| Cómo se probó | \[Prototipo pequeño (spike), medición o prueba; condiciones\] | 
| Resultado | \[Qué se observó, con números\] | 
| Qué cambia en la arquitectura | \[Se confirma, se matiza o se refuta; ajuste realizado\] | 

*No necesitan construir el sistema completo: un prototipo pequeño basta si pone a prueba la afirmación.*

#### Comprobación de conformidad. EV-\_

| Comprobación de conformidad. EV-\_ |  | 
| ----- | ----- | 
| Decisión que protege | \[ADR o regla de dependencia\] | 
| Procedimiento | \[Pasos que otra persona puede repetir. Automatizarlo es deseable, no obligatorio\] | 
| Resultado esperado | \[Cuándo se considera que la regla se violó\] | 

### 7.3 Riesgos y pendientes

*Primera entrega inicia*

Se inicia en la primera entrega y se mantiene hasta la entrega final.

| R | Tipo | Descripción | Qué se hará | 
| ----- | ----- | ----- | ----- | 
| R-01 | $$ Riesgo / Deuda / Decisión abierta / Pendiente $$ | \[...\] | \[...\] | 

### 7.4 Transición a Construcción

*Entrega final*

Instrucciones accionables para quien implemente: decisiones que no deben romperse, interfaces críticas, comprobaciones que deben ejecutarse y orden sugerido de trabajo. Si ya hay código, registren las diferencias con el diseño; si no, cómo se comprobarán.

\[Guía breve.\]

| Elemento o ADR | Qué dice el diseño | Qué hay en el código, o cómo se comprobará | Qué se hará | 
| ----- | ----- | ----- | ----- | 
| $$ ADR / C $$ | \[...\] | \[...\] | \[...\] | 

## Anexos

Opcionales y sólo para material extenso. Una decisión nueva no puede aparecer únicamente en un anexo.

## Lista de comprobación

### Lista de comprobación Primera entrega

* \[ \] Contexto, interesados con sus preocupaciones y funciones clave identificados.

* \[ \] Restricciones, supuestos, objetivos y condiciones separados por tipo.

* \[ \] Entre 8 y 12 escenarios medibles; al menos uno de seguridad con activo, actor de amenaza y límite de confianza.

* \[ \] Árbol de utilidad priorizado por importancia y dificultad.

* \[ \] Entre 5 y 7 ASR, cada uno con su procedencia.

* \[ \] Entre 4 y 6 ADR con estado; cada uno compara al menos dos alternativas reales.

* \[ \] Elementos principales en 4.1 y vistas V-01 (contexto) y V-02 (estructura).

* \[ \] Riesgos y pendientes iniciados en 7.3; declaración de IA actualizada.

### Lista de comprobación Entrega final

* \[ \] Las siete secciones están presentes y no hay relleno sin propósito.

* \[ \] Interfaces críticas con contrato y reglas de dependencia declaradas.

* \[ \] Cada preocupación de ejecución tiene una decisión o un «no aplica» justificado.

* \[ \] Cada vista declara audiencia, propósito y al menos una correspondencia.

* \[ \] La tabla 7.1 permite seguir cada ASR hasta su evidencia y su riesgo residual.

* \[ \] Hay al menos una verificación empírica ejecutada y una comprobación de conformidad repetible.

* \[ \] Hay diferencias registradas con el código, o un plan para comprobarlas, y una guía de transición.

* \[ \] Ambos integrantes pueden explicar cualquier decisión del documento.

* \[ \] Entrega. El paquete se arma conforme al Manual vigente. El PDF de este documento es la versión que se califica.