---
materia: Tecnologias
fecha: 19-10-2026
tipo: apunte
---
# Revisión del proyecto 1
- Darle prioridad la modo online ya que será el evaluado.
	- El modo offline se consideraria un extra
- Partir de un supuesto
	- Limite de por ejemplo 4 partidas de 2 jugadores
	- Comprobar la concurrencia sería bueno (Cuantos soporta el sistema)
- Server y cliente
	- Considerar que logica se quedara en un cliente y cual en el server
	- Pensar segun rendimiento
		- **Opcion 1:** Servidor con gran logica
			- Transfiere los datos de las partidas a todos los clientes
		- **Opción 2:** Servidor solo comunica a clientes
	- Buscar como los clientes podrian disminuir las peticiones invalidas de jugadas
	- Trade-off de codigo repetido o acoplamiento por modificación de capa compartida
	- **Conclusión:** se va el motor de juego del cliente
- Two-tier
	- Si se quieren 3 niveles se puede simular la tercera con un **Contenedor**
- Proyectos y soluciones
	- Contratos en el server
	- Pruebas separados de cliente y server
		- Pruebas de sistema **(No obligatorio)**
		- Pruebas de integración **Obligatorias**
	- Adaptación a objetos del dominio
		- Evitar conflictos con los de Entity (por los nombres)
		- Usar DTOs pero propios

Decisiones tomadas para el apartado del diseño:
- Se descarta el modo offline para la versión entregable
	- Se considera un extra, por lo que solo si el tiempo lo permite, será una funcionalidad extra al final (en el mantenimiento).
- El supuesto de "20 jugadores concurrentes" pasara a ser de 8 jugadores concurrentes
	- Para la parte de desarrollo y tecnologica, se buscara la manera de comprobar el limite de jugadores cuando el sistema este en funcionamiento
- Trade off del modulo de juego en cliente y server
	- Se elimina el modulo compartido entre cliente y server
	- El modulo y motor de juego se mantiene en el server
	- El cliente solo tendrá una versión ligera del modulo de juegos (solo validaciones de jugadas posibles):
		- Permite retroalimentación en la interfaz
		- Mejora usabilidad
		- Disminuye la posibilidad de jugadas invalidas desde el cliente, disminuyendo las solicitudes al server (performance)
		- Se tendrá este código repetido e independiente en cliente y server
- El sistema pasara a ser 3 niveles
	- Cliente
	- Servidor
	- Base de datos

# Revisión de proyecto 2

**Estandar**
- En los logs, añadir un ejemplo en cada caso
- Considerar que logs deben tratarse distinto
	- Si hay logs criticos que deben notificar a alguien
	- 

**Código**
- En las excepciones consideran si agruparlas con when en el catch o si es necesario conocerlas de manera individual
- Tener dos soluciones
- Contracs
	- Con los data contrac no hay tanto problea
	- Con los services contracs y callbacks, priorizar

# Revisión del proyecto 3

**Contratos:**
- **Async**: los métodos del repositorio si es valido su uso
	- el método del contrato no deberia llevar async, eso lo resuelve wcf
	- El cliente y server si pueden utilizar para evitar bloquearlo
- **Callbacks**:
	- Okey
- **Nombrado de entradas y salidas**
	- Sobreingeniería de nombrado
	- Aluden más a una acción que a un objeto
	- Porque un outcome y no otro objeto (decidir por método cual es mejor opción)
- **Excepciones:**
	- Pensar en que momento lanzar las excepciones del server al cliente y no solo se mantenga en el server

**Código:**
- **Ejecución:**
	- Correcto

# Revisión del proyecto 4
no agrupar codigos de retorno, si lo hacemos justificar y hacerlo BIEN

definir servicios → hacer mas con menos, sobre todo en las interfaces de callback

una interfaz con 10 metodos → violacion de responsabilidades

callback → 2, 3 metodos max, 1 es lo mejor

Receta para saber si estamos enfermos:

Si la interfaz de callback tiene algo como OnNotifyPlayerRemove(…), OnNotifyPlayerAdded(…), OnNotifyPlayerDead(…) estamos enfermos porque muy seguramente son metodos redundantes, se pueden resolver con uno solo, ahi si podemos tener diferentes estados con ENUM

revisar nombres de parametros para eliminar sufijos y prefijos innecesarios

revisar capas en las dos soluciones

cliente las normales eso ahi todo bien

que las capas no tengan dependencias raras ahi

logeo a bitacoras ya debemos tenerlo controlado y categorizado en el estandar

revisar violaciones de codigo

documentar si tenemos encadenamiento de excepciones

optimizacion en transferencia de imagenes por ejemplo

el ultimo dia vamos a tener una revision sin ia pero con acceso a internet para poder arreglar rapido algun problema en aprox 10 min por si tenemos algun error

servidor:

en un lugar solo los contratos

en otro solo las implementaciones

explicitar que estamos usando llamadas a procedimientos remotos 
no ocultar excepciones
por qué tantos void de retorno, revisar tipos de respuesta para saber si convienen los primitivos (boolean?)
revisar que los parámetros sean entendibles a nivel de lectura
cambios en la bd que se deriven de los contratos, preferencia de img personalizada en bd
estandarizar descripciones de cada contrato
documentar 3 parámetros máximo primitivos todo lo demás wrapped
que revise el estándar y proponga updates
refactorizar capa de persistencia hacia un repository mas puro con interfaces y luego implementaciones
nuestras excepciones están bien, documentar como quedamos
revisar en el contrato de leaderboards la excepción para mantener consistencia
revisar en friendship service sobreingenieria en el nombrado de los objetos
considerar reconexión
accountsummary en findplayers poco semántico
joinmatch y joinmatchbycode son el mismo
onmatchsettingschanged es innecesario (revisar por que esta)

# Revisión del proyecto 5

Return que impacta la legibilidad de código: Return con `{}`, con cuerpos de inicialización VS una simple asignación
- Clase MatchLobbyService linea 257
- Porque validar el nulo y no un valor para validación
	- En que momento si era nulo
	- En que momento me dice que algo sucedio
	- Aplicar el razonamiento de los enums con el estado de resultado

En los métodos `TryGetGuesto(Out TipoObjeto _objeto)` porque es necesario que sea un Try y no porque un retorno normal

En las Excepciones propias, razonar porque la necesitamos y porque no el uso de Excepciones del sistema
- Seguir lanzando excepciones y cachando en la capa de Aplicación? teniendo en cuenta el Manejador centralizado

Definir un mecanismo que permita comprobar la estructura de archivos, flujos verificando que se sigue los patrones definidos (Service, ApplicationService, Repository)

Revisar los nombres ambiguos `Get`
Revisar números mágicos

Definir:
- Que patrón de DTO vamos a ocupar
- Como se estructurara Models y si se separara o no
- Las excepciones personalizadas, decidir si se van
- Que patrón estamos ocupando en WPF (MVC, MVVM)

# Revisión del proyecto 6

- Revisar el nombre de los métodos (aun no son descriptivos)
- En MatchStateSnapchot:
	- Revisar bien porque existe y porque existe asi
	- Considerar el ancho de banda que se esta consumiendo
	- Revisar las estadisticas del envio, para revisar si es la opción adecuada ante performance o gran cantidad de mensajes (sobrecarga)
- En GameplayMatchApplicationService:
	- El método PlacePiece tiene 2 responsabilidades
		- Verifica si es valido
		- Lo pone
		- *Buscar refactorización y división*
		- *Revisar el resto de métodos, que antes igual están realizando validaciones*
- Todo lo que se hace debe tener facilidad de prueba
- Pruebas:
	- Deben ser atomicas
	- Deben tener elementos verificables concretos que muestren que la prueba es exitosa
	- Las pruebas tienen varios Assert 
	- Las pruebas deben tener:
		- Resultado esperado
		- Resultado obtenido
	- Nombrado:
		- `Test_FuncionalidadProbando_FlujoProbando_SiEsExitoso` (**Sugerencia**)
		- Se puede pensar si omitir el Test, si ya esta en un proyecto de prueba
	- Deben seguir el principio de responsabilidad unica
	- Los comentarios de Arrange... esta de más
- Logs
	- Si no se logra acceder al archivo, mandarlo al visor de evento del Sistema operativo
- Bug en el cliente
	- El cliente deja de ver el resto de jugadores activos
	- Bug donde no logra realizar una operación con el server
	- Bug donde pierde la conexión pero continua o nose q
- Formato de los correos
	- Mejora (decisión propia)

- Revisar **Rendimiento**, **Facilidad de prueba** y **Facilidad de modificación** del proyecto

# Revisión del proyecto 7

**Servidor**
`GameplayServiceAPI`
- Revisar quien trabaja con las excepciones que se lanzan en el código
	- Las excepciones como `InvalidOperationValue`
- Revisar la linea 23 y 24 por código duplicado
- Revisar lo del los TimeSpan
	- Revisar la justificación de porque se hace asi y porque no se modifica la configuración de WCF

**Cliente**
- Revisar el recursos que se nos esta consumiendo el timer
- Debemos justificar de porque es la mejor opción

El plan de migración del Cliente

**Sonarqube y reporte**
- Cumplir las métricas

**Pruebas y excepciones**
- Revisar los casos que se estan probando
- Revisar los casos que provocan o lanzan una excepción, deben probarse
- En las excepciones que suceden:
	- revisar quien debe saber que ocurrio una excepción
	- si se le propagan excepciones al usuario
		- Revisar el feedback
		- Revisar que la excepción que se le lanza, sea realmente algo que debe conocer el usuario
			- Revisar que no sea de diseño, programación
- Revisar los logs
	- Revisar el tipo de hora que se registra en los logs

**Resumen:**
- Terminar el resto de funcionalidades
- Justificar y revisar las decisiones de implementación a partir de atributos de calidad y estándares
	- El manejo de excepciones
	- La configuración de la comunicación
	- Como se realiza la persistencia
- Revisar y probar distintos casos de prueba, incluyendo excepciones

