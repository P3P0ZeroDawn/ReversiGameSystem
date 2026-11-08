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