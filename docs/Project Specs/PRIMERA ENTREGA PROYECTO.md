# PRIMERA ENTREGA PROYECTO

Fecha de creación: 7 de septiembre de 2026 19:00
Clase: Tecnologías para la Construcción de Software

El proyecto consiste en el desarrollo de un **Sistema de Juego de Reversi (SJR)** con arquitectura cliente-servidor, orientado a permitir que los usuarios jueguen partidas de Reversi contra otros jugadores, invitados o un oponente controlado por el sistema. La aplicación contempla la creación y administración de cuentas, personalización de preferencias, creación y unión a partidas mediante códigos, registro de movimientos, historial y estadísticas, gestión de amistades y comunicación mediante chat con mecanismos de moderación y sanciones temporales.

El sistema también considera aspectos de **internacionalización y localización**, permitiendo manejar distintos idiomas y configuraciones regionales sin depender de textos específicos almacenados en la base de datos. La persistencia de la información se implementa en **SQL Server**, utilizando un modelo relacional normalizado, restricciones de integridad y mecanismos de seguridad basados en autenticación y permisos específicos para el acceso a la base de datos.

**Prototipos de análisis CRUD**

Crear cuenta

Actor: 

- Jugador

Datos de entrada: 

- Nombre de usuario
- Correo electrónico
- Contraseña
- Confirmación de contraseña
- Foto de perfil, opcional
- Código de verificación.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta si el nombre de usuario ya está registrado
- Se consulta si el correo electrónico ya está registrado
- Se consulta el código de verificación ingresado por el usuario para validarlo.
- Se crea un nuevo registro de jugador con:
    - Identificador del jugador.
    - Nombre de usuario.
    - Correo electrónico.
    - Contraseña almacenada de forma segura.
    - Foto de perfil.
    - Estado de la cuenta.
    - Fecha de registro.

Reglas de negocio asociadas: 

- El nombre de usuario no puede estar registrado previamente.
- El correo electrónico no puede estar asociado a otra cuenta.
- El correo debe tener un formato válido.
- La contraseña debe cumplir con la política de contraseñas establecida por el sistema.
- La contraseña y su confirmación deben coincidir.
- La contraseña no debe almacenarse en texto plano.
- La foto de perfil es opcional.
- Si se proporciona una foto, debe ser un archivo de imagen válido.
- El código de verificación ingresado debe ser correcto.

Resultado esperado de la operación:

- Se crea una nueva cuenta de jugador y queda disponible para utilizar las funcionalidades del sistema.

![Crear_cuenta.png](PRIMERA%20ENTREGA%20PROYECTO/ca1b9f9a-34fe-4162-8155-b3c38ed911e4.png)

Mostrar información de cuenta

Actor: 

- Jugador

Datos de entrada: 

- Identificador del jugador cuya información será consultada.

Datos que se consultan, crean, modifican o eliminan: 

- Nombre de usuario.
- Foto de perfil.
- Información pública del jugador.
- Estadísticas disponibles del jugador.
- En el caso de la cuenta propia, información privada como correo electrónico.

Reglas de negocio asociadas: 

- La contraseña nunca debe mostrarse.
- Un jugador solamente puede consultar información privada de su propia cuenta.
- La información de otros jugadores debe limitarse a los datos definidos como públicos.
- La información mostrada debe corresponder a una cuenta existente y activa.

Resultado esperado de la operación:

- El sistema muestra la información correspondiente al jugador solicitado.

![mostrarinformacioncuenta.png](PRIMERA%20ENTREGA%20PROYECTO/e092abab-57c9-41e4-970b-40fe77f9648b.png)

Modificar nombre de usuario

Actor: 

- Jugador.

Datos de entrada requeridos:

- Nuevo nombre de usuario.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta el jugador autenticado.
- Se consulta si el nuevo nombre ya está utilizado.
- Se modifica el nombre de usuario de la cuenta.

Reglas de negocio asociadas:

- Solamente el propietario de la cuenta puede cambiar su nombre.
- El nuevo nombre no puede estar siendo utilizado por otro jugador.
- El nombre no puede quedar vacío.
- Debe cumplir con las reglas de formato definidas para los nombres de usuario.

Resultado esperado de la operación:

- El nombre de usuario anterior es sustituido por el nuevo nombre.

![modificarnombreusuario.png](PRIMERA%20ENTREGA%20PROYECTO/6adde08a-f6ec-41a7-8fd1-0868c564c3cc.png)

Modificar contraseña

Actor: 

- Jugador.

Datos de entrada requeridos:

- Contraseña actual.
- Nueva contraseña.
- Confirmación de la nueva contraseña.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la contraseña almacenada del jugador para validar su identidad.
- Se modifica la contraseña asociada a la cuenta.

Reglas de negocio asociadas:

- El jugador debe estar autenticado.
- La contraseña actual debe ser correcta.
- La nueva contraseña y su confirmación deben coincidir.
- La nueva contraseña debe cumplir la política de seguridad establecida.
- La nueva contraseña debe almacenarse de forma segura y nunca en texto plano.

Resultado esperado de la operación:

- La contraseña de la cuenta queda actualizada y puede utilizarse posteriormente para autenticarse.

![modificarcontrasena.png](PRIMERA%20ENTREGA%20PROYECTO/da7bac55-28c9-4ea6-af0e-4213cd8bf8d1.png)

Reiniciar contraseña

Actor: 

- Jugador.

Datos de entrada requeridos:

- Correo electrónico
- Código de verificación enviado al correo
- Nueva contraseña
- Confirmación de nueva contraseña

Datos que se consultan, crean, modifican o eliminan:

- Se consulta si el correo electrónico ya está registrado.
- Se consulta el código ingresado para validar la identidad del jugador.
- Se modifica la contraseña asociada a la cuenta.

Reglas de negocio asociadas:

- El correo electrónico debe pertenecer a una cuenta registrada y activa.
- El código de verificación debe corresponder al jugador y al proceso de reinicio de contraseña.
- El código debe encontrarse vigente y no haber sido utilizado previamente.
- Una vez utilizado correctamente, el código debe invalidarse.
- La nueva contraseña y su confirmación deben coincidir.
- La nueva contraseña debe cumplir con la política de seguridad establecida.
- La nueva contraseña debe almacenarse de forma segura y nunca en texto plano.

Resultado esperado de la operación:

- La contraseña de la cuenta queda actualizada y puede utilizarse posteriormente para autenticarse.

![reiniciarcontrasea.png](PRIMERA%20ENTREGA%20PROYECTO/f30881f8-f7f0-4b3b-9bae-fd525f0ecd76.png)

Desactivar cuenta

Actor: 

- Jugador.

Datos de entrada requeridos:

- Confirmación de desactivación de la cuenta.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la cuenta del jugador.
- Se consulta si existen partidas activas.
- Se modifica el estado de la cuenta para marcarla como desactivada.

Reglas de negocio asociadas:

- Solamente el propietario puede desactivar su cuenta.
- La desactivación debe requerir confirmación.
- Una partida activa debe resolverse antes de completar la desactivación.
- Debe conservarse la información histórica necesaria de las partidas.
- Los movimientos y resultados históricos no deben perderse por desactivar la cuenta.
- Por motivos de trazabilidad, no se eliminará físicamente el registro del jugador.

Resultado esperado de la operación:

- La cuenta queda desactivada y el jugador ya no puede utilizarla, conservándose la información necesaria para el historial del sistema.

![desactivarcuenta.png](PRIMERA%20ENTREGA%20PROYECTO/9be4785b-9626-42d5-8051-9c71dd448499.png)

Guardar foto de perfil

Actor: 

- Jugador.

Datos de entrada requeridos:

- Nueva imagen de perfil.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la cuenta autenticada.
- Se modifica la referencia a la foto de perfil del jugador.
- Si existía una foto anterior, es sustituida por la nueva.

Reglas de negocio asociadas:

- Solamente el propietario de la cuenta puede modificar su foto.
- El archivo debe corresponder a un formato de imagen permitido.
- La imagen debe cumplir las restricciones de almacenamiento definidas por el sistema.
- La imagen de perfil puede utilizarse como apariencia de las fichas durante una partida cuando el jugador seleccione dicha personalización.

Resultado esperado de la operación:

- La nueva fotografía queda asociada al perfil del jugador.

![guardarfotoperfil.png](PRIMERA%20ENTREGA%20PROYECTO/a3444424-da4b-4811-b90a-79241f4a7e3a.png)

Mostrar estadísticas del jugador

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador del jugador.

Datos que se consultan, crean, modifican o eliminan:

- Partidas finalizadas en las que participó el jugador.
- Resultado obtenido en cada partida.
- Total de partidas jugadas.
- Total de victorias.
- Total de derrotas.
- Total de empates, si aplican.
- Porcentaje de victorias, si se desea mostrar.

Reglas de negocio asociadas:

- Solamente deben considerarse partidas finalizadas para las estadísticas de victoria, derrota o empate.
- Las partidas abandonadas deben contabilizarse conforme a las reglas de resultado establecidas.
- Una misma partida solamente puede contabilizarse una vez.
- Las estadísticas deben corresponder al jugador consultado.
- No es necesario almacenar los totales de forma independiente si pueden calcularse a partir del historial de partidas.

Resultado esperado de la operación:

- El sistema muestra las estadísticas actualizadas del jugador.

![mostrarestadisticasjugador.png](PRIMERA%20ENTREGA%20PROYECTO/ffb57498-13bf-4db2-894c-d3d656797d2c.png)

Guardar fondo del tablero

Actor: 

- Jugador.

Datos de entrada requeridos:

- Imagen seleccionada como fondo del tablero.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la configuración del jugador.
- Se crea o modifica la referencia al fondo personalizado.

Reglas de negocio asociadas:

- El fondo pertenece a la configuración personal del jugador.
- Debe ser una imagen válida.
- La personalización del tablero solamente será visible para el jugador que la configuró.
- La personalización no debe modificar el estado real de la partida ni la vista del oponente.

Resultado esperado de la operación:

- El fondo queda almacenado como preferencia del jugador y se utiliza en su vista del tablero.

![guardarfondotablero.png](PRIMERA%20ENTREGA%20PROYECTO/367d90a8-36f1-4aa2-8f2f-7424adeed179.png)

Crear partida

Actor: 

- Jugador.

Datos de entrada requeridos:

- Jugador creador.
- Tipo de oponente:
    - Otro jugador.
    - Oponente controlado por el sistema.
- Tiempo disponible por turno, cuando se desee modificar el valor predeterminado.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la cuenta del jugador creador.
- Se crea una nueva partida.
- Se establece la configuración inicial de la partida.
- Se establece el estado inicial del tablero.
- Se registra el jugador creador como participante.
- Se genera y asocia un **Código de partida** único entre las partidas disponibles.

Reglas de negocio asociadas:

- Una partida debe enfrentar exactamente a dos participantes.
- El tiempo predeterminado para cada turno es de 40 segundos.
- El tiempo puede configurarse al crear la partida.
- El tablero debe comenzar utilizando la distribución inicial válida de Reversi.
- Una partida que espera a otro jugador debe encontrarse en un estado que permita identificarla como disponible.
- La partida no puede comenzar contra otro jugador hasta contar con ambos participantes.
- El **Código de partida** solamente puede utilizarse mientras la partida acepte un segundo participante.

Resultado esperado de la operación:

- Se registra una nueva partida con su configuración y estado inicial.

![crearpartida.png](PRIMERA%20ENTREGA%20PROYECTO/e80dc3cd-25d9-4928-b917-529073c5836e.png)

Mostrar información de partida

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la partida.

Datos que se consultan, crean, modifican o eliminan:

- Jugadores participantes.
- Estado de la partida.
- Configuración de tiempo.
- Jugador cuyo turno se encuentra activo.
- Estado actual del tablero.
- Cantidad de fichas de cada jugador.
- Movimientos realizados.
- Resultado de la partida, cuando haya finalizado.

Reglas de negocio asociadas:

- La información debe corresponder a una partida existente.
- El tablero mostrado debe representar el último estado válido de la partida.
- El jugador activo debe coincidir con el turno registrado.
- Una partida finalizada debe conservar su resultado para consultas posteriores.

Resultado esperado de la operación:

- Se muestra el estado y la información correspondiente a la partida solicitada.

![mostrarinformacionpartida.png](PRIMERA%20ENTREGA%20PROYECTO/13c27cc4-a130-46a7-a848-b1979783bd02.png)

Mostrar partidas disponibles

Actor: 

- Jugador.

Datos de entrada requeridos:

- No requiere información obligatoria adicional.

Datos que se consultan, crean, modifican o eliminan:

- Partidas creadas que se encuentren esperando a un segundo jugador.
- Jugador creador.
- Configuración relevante de la partida.
- Tiempo establecido por turno.
- Estado de disponibilidad.

Reglas de negocio asociadas:

- Solamente deben mostrarse partidas que todavía puedan aceptar otro jugador.
- No deben aparecer partidas finalizadas.
- No deben aparecer partidas que ya tengan los dos jugadores necesarios.
- Las partidas contra un oponente controlado por el sistema no necesitan aparecer como disponibles.

Resultado esperado de la operación:

- El jugador obtiene una lista actualizada de las partidas a las que puede unirse.

![mostrarpartidasdisponibles.png](PRIMERA%20ENTREGA%20PROYECTO/3a18b01b-92ee-407c-9eef-37563ec56781.png)

Modificar partida

Actor: 

- Jugador creador de la partida / Sistema.

Datos de entrada requeridos:

- Identificador de partida.
- Dato que se desea modificar.

Datos que se consultan, crean, modifican o eliminan:

- Configuración de la partida.
- Estado de la partida.

Reglas de negocio asociadas:

- El jugador creador solamente puede modificar la configuración que el sistema permita.
- Las configuraciones previas al juego no deben poder modificarse una vez iniciada la partida.
- El estado de la partida debe seguir transiciones válidas, por ejemplo:
    - Esperando jugador.
    - En curso.
    - Finalizada.
    - Abandonada.
- El tablero no debe modificarse directamente mediante esta funcionalidad; los cambios al tablero deben producirse mediante movimientos válidos.

Resultado esperado de la operación:

- La información permitida de la partida queda actualizada sin alterar indebidamente su estado de juego.

![modificarpartida.png](PRIMERA%20ENTREGA%20PROYECTO/1059084b-18f6-425f-82c5-67da408cab08.png)

Unirse a partida

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador del participante o sesión temporal.
- Identificador de la partida **o Código de partida**, según el mecanismo utilizado para unirse.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la partida.
- Se consulta su estado.
- Se consulta la cantidad de participantes.
- Se agrega al segundo jugador.
- Se modifica el estado de la partida.

Reglas de negocio asociadas:

- La partida debe encontrarse disponible.
- La partida solamente puede tener dos participantes.
- Un jugador no puede ocupar ambos lugares.
- Al incorporarse el segundo participante, la partida puede pasar de `Esperando` a `En curso`.
- Debe establecerse el turno inicial correspondiente.

Resultado esperado de la operación:

- El jugador queda registrado como segundo participante y la partida queda preparada para comenzar.

![unirseapartida.png](PRIMERA%20ENTREGA%20PROYECTO/9dd5d8a8-9555-4c6c-8d70-9bbbbb04f727.png)

Abandonar partida

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la partida.
- Jugador que abandona.

Datos que se consultan, crean, modifican o eliminan:

- Estado de la partida.
- Participantes.
- Resultado.
- Motivo de finalización.

Reglas de negocio asociadas:

- Solamente un participante puede abandonar la partida.
- El abandono termina la partida.
- El sistema debe registrar que la finalización ocurrió por abandono.
- El resultado debe asignarse conforme a las reglas establecidas para este caso.
- Los movimientos realizados antes del abandono deben conservarse.

Resultado esperado de la operación:

- La partida queda finalizada y se registra que terminó debido al abandono de uno de los jugadores.

![abandonarpartida.png](PRIMERA%20ENTREGA%20PROYECTO/672b4b4e-b1bf-472a-a84c-d3b7e6f0619a.png)

Crear movimiento

Actor: 

- Jugador / Sistema.

Datos de entrada requeridos:

- Identificador de la partida.
- Jugador que realiza el movimiento.
- Posición seleccionada:
    - Fila.
    - Columna.

Datos que se consultan, crean, modifican o eliminan:

- Estado actual del tablero.
- Jugador cuyo turno está activo.
- Posiciones válidas.
- Tiempo restante del turno.
- Se crea el registro del movimiento.
- Se actualiza el estado del tablero.
- Se actualiza el turno.

Reglas de negocio asociadas:

- La partida debe estar en curso.
- Solamente puede realizar un movimiento el jugador cuyo turno se encuentre activo.
- Solamente pueden seleccionarse posiciones válidas de Reversi.
- La interfaz debe identificar las posiciones en las que se puede jugar.
- El movimiento debe voltear las fichas correspondientes conforme a las reglas de Reversi.
- Después del movimiento se cambia el turno.
- El tiempo normal disponible es el configurado para la partida, siendo 40 segundos por defecto.
- Si el tiempo del jugador se agota, el sistema realiza automáticamente una jugada válida seleccionada de forma aleatoria.
- Si un jugador agota el tiempo durante tres turnos consecutivos, pierde por inactividad.
- Si un jugador no tiene movimientos válidos, debe aplicarse la regla correspondiente de paso de turno.
- Si ninguno de los jugadores tiene movimientos disponibles, la partida termina.

Resultado esperado de la operación:

- El movimiento queda registrado, el tablero se actualiza correctamente y se determina el siguiente turno o, cuando corresponda, el final de la partida.

![Crear movimiento(actualizado).png](PRIMERA%20ENTREGA%20PROYECTO/Crear_movimiento(actualizado).png)

Mostrar información de movimiento

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la partida y/o identificador del movimiento.

Datos que se consultan, crean, modifican o eliminan:

- Jugador que realizó el movimiento.
- Posición elegida.
- Número o secuencia del movimiento.
- Fecha/hora del movimiento.
- Fichas afectadas.
- Indicación de si el movimiento fue realizado por el jugador o automáticamente por el sistema.

Reglas de negocio asociadas:

- El movimiento debe pertenecer a la partida consultada.
- Los movimientos deben conservar su orden.
- Los movimientos históricos no deben modificarse.
- Debe existir información suficiente para reconstruir posteriormente el desarrollo de una partida.

Resultado esperado de la operación:

- Se muestra la información correspondiente al movimiento solicitado o el historial ordenado de movimientos de la partida.

![Mostrar información de movimiento.png](PRIMERA%20ENTREGA%20PROYECTO/Mostrar_informacin_de_movimiento.png)

Enviar solicitud de amistad

Actor: 

- Jugador.

Datos de entrada requeridos:

- Jugador que envía la solicitud.
- Jugador destinatario.

Datos que se consultan, crean, modifican o eliminan:

- Se consultan ambas cuentas.
- Se consulta si ya existe una relación entre ambas.
- Se crea una solicitud o relación de amistad.

Reglas de negocio asociadas:

- Ambos jugadores deben existir.
- Un jugador no puede agregarse a sí mismo.
- No debe crearse una relación duplicada.
- Si se utilizan solicitudes, inicialmente la relación debe quedar en estado pendiente.
- Una amistad solamente se considera establecida después de que el otro jugador la acepte.

Resultado esperado de la operación:

- Se registra una nueva solicitud de amistad entre los jugadores.

![Enviar solicitud de amistad.png](PRIMERA%20ENTREGA%20PROYECTO/Enviar_solicitud_de_amistad.png)

Mostrar amigos

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador del jugador autenticado.

Datos que se consultan, crean, modifican o eliminan:

- Relaciones de amistad aceptadas.
- Nombre de usuario de los amigos.
- Foto de perfil.
- Información pública necesaria.

Reglas de negocio asociadas:

- Solamente deben mostrarse las relaciones de amistad válidas.
- Las solicitudes pendientes no deben mostrarse como amistades confirmadas.
- No debe exponerse información privada de las cuentas relacionadas.

Resultado esperado de la operación:

- Se muestra la lista actual de amigos del jugador.

![Mostrar amigos.png](PRIMERA%20ENTREGA%20PROYECTO/Mostrar_amigos.png)

Responder solicitud de amistad

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la solicitud o relación.
- Nuevo estado.

Por ejemplo:

- Aceptada.
- Rechazada.

Datos que se consultan, crean, modifican o eliminan:

- Relación existente.
- Jugador solicitante.
- Jugador destinatario.
- Estado de la relación.

Reglas de negocio asociadas:

- Solamente el destinatario de una solicitud pendiente puede aceptarla o rechazarla.
- Una solicitud debe existir antes de poder modificar su estado.
- Una solicitud ya resuelta no debe volver a procesarse indebidamente.

Resultado esperado de la operación:

- El estado de la solicitud queda actualizado y, si fue aceptada, ambos jugadores pasan a tener una relación de amistad.

![Responder solicitud de amistad.png](PRIMERA%20ENTREGA%20PROYECTO/Responder_solicitud_de_amistad.png)

Eliminar amigo

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador del amigo o de la relación de amistad.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la relación de amistad.
- Se elimina o desactiva la relación.

Reglas de negocio asociadas:

- La relación debe existir.
- El jugador que realiza la operación debe pertenecer a la relación.
- Eliminar una amistad no debe eliminar la cuenta del otro jugador.
- Tampoco debe eliminar partidas o mensajes históricos existentes.

Resultado esperado de la operación:

- La relación de amistad deja de estar activa y el jugador deja de aparecer en la lista de amigos.

![Eliminar amigo.png](PRIMERA%20ENTREGA%20PROYECTO/Eliminar_amigo.png)

Crear mensaje

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la partida o conversación.
- Contenido del mensaje.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la partida o conversación.
- Se consulta el jugador emisor.
- Se crea:
    - Identificador de mensaje.
    - Emisor.
    - Contenido.
    - Fecha y hora.
    - Partida/conversación asociada.
- Se consulta si el jugador tiene un muteo activo.
- Se analiza el contenido del mensaje para detectar palabras o expresiones no permitidas.
- Si existe una infracción, se registra la infracción correspondiente.

Reglas de negocio asociadas:

- El jugador debe estar autorizado para participar en el chat.
- El mensaje no puede estar vacío.
- El mensaje debe quedar asociado al jugador que realmente lo envió.
- Antes de enviar un mensaje, el sistema debe verificar si el jugador tiene un muteo activo.
- El contenido del mensaje debe analizarse conforme a las reglas de moderación.
- El sistema debe analizar el contenido utilizando las reglas de moderación correspondientes a los idiomas soportados.
- El análisis debe realizarse antes de mostrar el mensaje a los demás jugadores.
- Si el contenido contiene una palabra o expresión prohibida, el mensaje no debe enviarse.
- La detección debe registrar una infracción asociada al jugador.
- Las infracciones acumuladas pueden ocasionar una advertencia o un muteo conforme a la política definida por el equipo.
- Un jugador muteado puede continuar jugando, pero no puede enviar mensajes.
- Una infracción en el chat no debe afectar por sí misma el estado de la partida.
- Los mensajes deben mantener la trazabilidad necesaria para aplicar las reglas de moderación.

Resultado esperado de la operación:

- El mensaje queda almacenado y disponible para los participantes autorizados de la conversación.

![Crear mensaje.png](PRIMERA%20ENTREGA%20PROYECTO/Crear_mensaje.png)

Registrar infracción de chat

Actor: 

- Sistema.

Datos de entrada requeridos:

- Identificador del jugador.
- Identificador de la conversación o partida.
- Tipo o motivo de infracción.
- Fecha y hora.
- Regla de moderación que fue incumplida.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta el jugador responsable.
- Se consultan las infracciones anteriores del jugador.
- Se consulta si existe un muteo activo.
- Se crea un nuevo registro de infracción con:
    - Identificador de infracción.
    - Jugador.
    - Fecha y hora.
    - Motivo.
    - Conversación o partida relacionada.
    - Regla detectada.

Reglas de negocio asociadas:

- Solamente debe registrarse una infracción cuando el contenido incumpla una regla definida de moderación.
- La infracción debe registrar el tipo o regla de moderación infringida de manera independiente del texto traducido mostrado al jugador.
- Una misma detección no debe generar registros duplicados.
- La infracción debe quedar asociada al jugador que intentó enviar el mensaje.
- Las infracciones anteriores pueden utilizarse para determinar si corresponde aplicar una sanción.
- Registrar una infracción no debe modificar los movimientos ni el estado de la partida.
- El sistema debe conservar suficiente información para conocer por qué se aplicó posteriormente una sanción.

Resultado esperado de la operación:

- La infracción queda registrada y el sistema puede determinar si debe advertir o mutear al jugador conforme a las reglas de moderación.
- El jugador no puede seguir enviando mensajes durante la partida en curso.

![Registrar infracción de chat.png](PRIMERA%20ENTREGA%20PROYECTO/Registrar_infraccin_de_chat.png)

Leer mensaje

Actor: 

- Jugador.

Datos de entrada requeridos:

- Identificador de la partida o conversación.

Datos que se consultan, crean, modifican o eliminan:

- Mensajes pertenecientes a la conversación.
- Emisor de cada mensaje.
- Contenido.
- Fecha y hora.
- Orden de envío.

Reglas de negocio asociadas:

- El jugador debe tener autorización para consultar esa conversación.
- Los mensajes deben mostrarse en orden cronológico.
- La información del emisor debe corresponder al jugador que realmente creó el mensaje.
- Un jugador no debe poder consultar conversaciones a las que no tenga acceso.

Resultado esperado de la operación:

- El sistema muestra los mensajes correspondientes a la conversación solicitada en el orden adecuado.

![Leer mensaje.png](PRIMERA%20ENTREGA%20PROYECTO/Leer_mensaje.png)

Mostrar solicitudes de amistad

Actor: 

- Jugador.

Datos de entrada: 

- identificador del jugador.

Datos consultados:

- Solicitudes recibidas pendientes.
- Jugador que envió cada solicitud.
- Fecha de solicitud.

Reglas principales:

- Solo se muestran solicitudes dirigidas al jugador autenticado.
- Solo deben mostrarse las que estén pendientes.
- Una solicitud aceptada o rechazada deja de aparecer como pendiente.

Resultado esperado:

- Se muestra la lista de solicitudes de amistad pendientes.

![Mostrar solicitudes de amistad.png](PRIMERA%20ENTREGA%20PROYECTO/Mostrar_solicitudes_de_amistad.png)

Mostrar historial de partidas

Actor: 

- Jugador.

Datos de entrada: 

- identificador del jugador.

Datos consultados:

- Partidas jugadas.
- Oponente.
- Fecha.
- Resultado.
- Puntaje final.
- Motivo de finalización, cuando corresponda.

Reglas principales:

- Solo deben mostrarse partidas en las que participó el jugador.
- Las partidas deben conservarse aunque la cuenta de un antiguo oponente sea desactivada.
- Pueden ordenarse de la más reciente a la más antigua.
- Una partida en curso debe distinguirse de una finalizada.

Resultado esperado:

- Se muestra el historial de partidas del jugador y sus resultados.

![Mostrar historial de partidas.png](PRIMERA%20ENTREGA%20PROYECTO/Mostrar_historial_de_partidas.png)

Finalizar partida / Registrar resultado

Actor: 

- Sistema.

Datos de entrada:

- Identificador de partida.
- Estado final del tablero.
- Cantidad de fichas de cada jugador.
- Motivo de finalización.

Datos modificados:

- Estado de partida.
- Ganador, cuando exista.
- Resultado final.
- Fecha/hora de finalización.
- Motivo de finalización.

Reglas principales:

La partida termina cuando ocurra alguna condición definida por las reglas, por ejemplo:

- Ningún jugador puede realizar movimientos.
- Se cumple la condición normal de finalización de Reversi.
- Un jugador abandona.
- Un jugador pierde por inactividad después de agotar el tiempo en tres turnos consecutivos.

El ganador se determina conforme a las reglas de la partida. Una partida finalizada ya no debe aceptar nuevos movimientos.

Resultado esperado:

La partida queda registrada como finalizada junto con su resultado definitivo.

![Abandonar partida.png](PRIMERA%20ENTREGA%20PROYECTO/Abandonar_partida.png)

Aplicar muteo a jugador

Actor: 

- Sistema

Datos de entrada requeridos:

- Jugador sancionado.
- Motivo del muteo.
- Duración del muteo.
- Fecha y hora de inicio.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta el jugador.
- Se consulta si ya existe un muteo activo.
- Se registra:
    - Jugador sancionado.
    - Motivo.
    - Fecha de inicio.
    - Fecha de finalización.
    - Estado del muteo.

Reglas de negocio asociadas:

- El jugador debe existir.
- No deben crearse muteos activos duplicados para el mismo jugador.
- El muteo debe tener una duración definida según las reglas de moderación.
- Un jugador muteado puede seguir jugando, pero no puede enviar mensajes mientras dure la sanción.
- Antes de crear un mensaje, el sistema debe comprobar si el jugador tiene un muteo activo.
- El muteo no elimina los mensajes enviados anteriormente.
- El motivo de la sanción debe conservarse para trazabilidad.

Resultado esperado de la operación:

- El jugador queda temporalmente impedido de enviar mensajes en el chat.

![Aplicar muteo a jugador.png](PRIMERA%20ENTREGA%20PROYECTO/Aplicar_muteo_a_jugador.png)

Consultar estado de muteo

Actor: 

- Jugador / Sistema.

Datos de entrada requeridos:

- Identificador del jugador.

Datos que se consultan:

- Existencia de un muteo activo.
- Motivo.
- Fecha de inicio.
- Fecha de finalización.
- Tiempo restante, si aplica.

Reglas de negocio asociadas:

- Un muteo cuya fecha de finalización ya haya pasado se considera terminado.
- El sistema debe consultar esta información antes de permitir el envío de mensajes.
- El jugador puede conocer que está muteado y hasta cuándo, pero no necesariamente acceder a información interna de moderación adicional.

Resultado esperado de la operación:

- El sistema determina si el jugador puede utilizar el chat y, si está muteado, muestra la información correspondiente.

![Consultar estado de muteo.png](PRIMERA%20ENTREGA%20PROYECTO/Consultar_estado_de_muteo.png)

Generar código de verificación

Actor: 

- Sistema.

Datos de entrada requeridos:

- Correo electrónico.
- Propósito:
    - Verificación de cuenta.
    - Reinicio de contraseña.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la cuenta asociada al correo cuando corresponda.
- Se crea un código de verificación.
- Se registra:
    - Código o representación segura del código.
    - Correo o jugador asociado.
    - Propósito.
    - Fecha de creación.
    - Fecha de expiración.
    - Estado de utilización.

Reglas de negocio asociadas:

- El código debe tener una vigencia limitada.
- Debe estar relacionado con un propósito concreto.
- Un código para verificar una cuenta no debe servir para reiniciar una contraseña.
- El código debe enviarse únicamente al correo correspondiente.
- Un código utilizado o expirado no debe volver a aceptarse.
- La generación de un nuevo código puede invalidar el anterior para el mismo proceso.

Resultado esperado:

- Se genera un código temporal y se envía al correo correspondiente.

![Generar código de verificación.png](PRIMERA%20ENTREGA%20PROYECTO/Generar_cdigo_de_verificacin.png)

Validar código de verificación

Actor: 

- Jugador / Sistema.

Datos de entrada:

- Correo electrónico.
- Código proporcionado.
- Propósito de la operación.

Datos consultados o modificados:

- Código registrado.
- Fecha de expiración.
- Propósito.
- Estado de utilización.
- Se marca el código como utilizado después de una validación exitosa.

Reglas de negocio:

- El código debe existir.
- Debe pertenecer al correo indicado.
- Debe corresponder al propósito solicitado.
- No debe estar expirado.
- No debe haber sido utilizado previamente.
- Después de utilizarse correctamente debe quedar invalidado.

Resultado esperado:

- El sistema autoriza la continuación del registro o del reinicio de contraseña.

![Validar código de verificación.png](PRIMERA%20ENTREGA%20PROYECTO/Validar_cdigo_de_verificacin.png)

Modificar idioma de interfaz

Actor: 

- Jugador.

Datos de entrada requeridos:

- Idioma seleccionado.

Datos que se consultan, crean, modifican o eliminan:

- Se consulta la configuración del jugador.
- Se modifica el idioma preferido de la interfaz.

Reglas de negocio asociadas:

- El idioma seleccionado debe encontrarse dentro de los idiomas soportados por la aplicación.
- El idioma seleccionado debe conservarse entre sesiones.
- Si el jugador no ha seleccionado un idioma, el sistema utilizará el idioma predeterminado.
- El cambio de idioma afecta los textos generados por el sistema, pero no modifica ni traduce automáticamente el contenido creado por otros jugadores, como mensajes del chat o nombres de usuario.
- Los datos internos del sistema deben conservar valores independientes del idioma utilizado en la interfaz.

Resultado esperado de la operación:

- El idioma seleccionado queda almacenado como preferencia del jugador y la interfaz utiliza los recursos correspondientes a ese idioma.

![Modificar idioma de interfaz.png](PRIMERA%20ENTREGA%20PROYECTO/Modificar_idioma_de_interfaz.png)

---

**Casos de uso detallados**

**SJR** = Sistema de Juego de Reversi

CU-01 Crear cuenta

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** cree una cuenta proporcionando sus datos y verificando que tiene acceso al correo electrónico indicado para poder jugar.

Precondiciones

- El **Jugador** no debe tener una sesión iniciada.
- Debe existir comunicación entre el cliente y el servidor.
- El **Jugador** debe disponer de acceso al correo electrónico que desea registrar.

Disparador: 

El **Jugador** selecciona la opción Crear cuenta en la GUI-LoginScreen.

Flujo normal

1. El **SJR** muestra la GUI-AccountCreation con los campos para el nombre de usuario, correo electrónico, contraseña y confirmación de contraseña.
2. El **Jugador** ingresa su nombre de usuario, correo electrónico, contraseña y confirmación de contraseña.
3. Opcionalmente, el **Jugador** selecciona una foto de perfil.
4. El **SJR** comprueba que la foto de perfil seleccionada sea válida. (**FA-07**)
5. El **Jugador** selecciona la opción de continuar con el registro.
6. El **SJR** verifica que todos los datos obligatorios hayan sido proporcionados. (FA-09)
7. El **SJR** valida el formato del correo electrónico. (FA-08)
8. El **SJR** verifica que el nombre de usuario no se encuentre registrado. (**FA-05**)
9. El **SJR** verifica que el correo electrónico no se encuentre asociado a otra cuenta. (**FA-06)**
10. El **SJR** comprueba que ambas contraseñas coincidan y cumplan la política establecida. (**FA-01) (FA-02)**
11. El **SJR** genera un código de verificación temporal y lo envía al correo electrónico indicado. (**FE-02)**
12. El **SJR** muestra la GUI-ConfirmationCodeRequired con el campo para ingresar el código recibido.
13. El **Jugador** introduce el código en el campo.
14. El **Jugador** selecciona la opción para validar el código recibido.
15. El **SJR** comprueba que el código corresponda al proceso de registro, se encuentre vigente y no haya sido utilizado. (**FA-03) (FA-04)**
16. El **SJR** cierra la GUI-ConfirmationCodeRequired
17. El **SJR** almacena de forma segura la contraseña.
18. El **SJR** crea una **Cuenta** con estado activo en la base de datos a partir de los datos proporcionados e invalida el código de verificación utilizado. (**FE-03)**
19. El **SJR** muestra la GUI-AccountCreationSuccessful informando que la cuenta fue creada correctamente con la opción para cerrar.
20. El **Jugador** selecciona la opción para cerrar la GUI-AccountCreationSuccessful
21. El **SJR** cierra la GUI-AccountCreationSuccessful y muestra la GUI-MainMenu

Flujos alternos

**FA-01. Contraseñas diferentes**

1. El **SJR** muestra la **GUI-PasswordMismatch** informando al jugador que las contraseñas no coinciden y con la opción para cerrarla.
2. El **Jugador** selecciona la opción para cerrar la **GUI-PasswordMismatch**
3. El **SJR** cierra la **GUI-PasswordMismatch**
4. El flujo continúa en el paso 2 del FN.

**FA-02. Contraseña no cumple la política**

1. El **SJR** muestra la **GUI-InvalidPasswordFormat** informando qué requisitos no se cumplen y con la opción para cerrarla.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidPasswordFormat.**
3. El **SJR** cierra la **GUI-InvalidPasswordFormat.**
4. El flujo continúa en el paso 2 del FN.

**FA-03. Código incorrecto**

1. El **SJR** muestra la **GUI-InvalidConfirmationCode** indicando que se rechazó el código porque es incorrecto y con la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidConfirmationCode**.
3. El **SJR** cierra la **GUI-InvalidConfirmationCode**.
4. El flujo continúa en paso 13 del FN.

**FA-04. Código expirado**

1. El **SJR** muestra la **GUI-ExpiredConfirmationCode** indicando que el código perdió vigencia con la opción para enviar uno nuevo.
2. El jugador solicita la opción para enviar un nuevo código.
3. El **SJR** cierra la **GUI-ExpiredConfirmationCode.**
4. El flujo continúa en el paso 11 del FN.

**FA-05. Nombre de usuario ocupado**

1. El **SJR** muestra la **GUI-UsernameAlreadyTaken** indicando que el nombre ya pertenece a otra cuenta y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-UsernameAlreadyTaken.**
3. El **SJR** cierra la **GUI-UsernameAlreadyTaken.**
4. El flujo continúa en el paso 2 del FN.

**FA-06. Correo electrónico ya registrado**

1. El **SJR** muestra la **GUI-EmailAlreadyTaken** indicando que el correo pertenece a otra cuenta y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-EmailAlreadyTaken**.
3. El **SJR** cierra la **GUI-EmailAlreadyTaken.**
4. El flujo continúa en el paso 2 del FN.

**FA-07. Imagen de perfil inválida o no procesable**

1. El **SJR** muestra la **GUI-ImageNotSupported** indicando que la imagen seleccionada de perfil no es válida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ImageNotSupported.**
3. El **SJR** cierra la **GUI-ImageNotSupported.**
4. El flujo continúa en el paso 3 del FN.

**FA-08. Formato de correo inválido**

1. El **SJR** muestra la **GUI-InvalidEmailFormat** indicando que el formato del correo no es válido y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidEmailFormat.**
3. El **SJR** cierra la **GUI-InvalidEmailFormat.**
4. El flujo continúa en el paso 2 del FN.

**FA-09. Datos faltantes**

1. El **SJR** muestra la **GUI-MissingRequiredInformation** indicando que los datos requeridos no están completos y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MissingRequiredInformation.**
3. El **SJR** cierra la **GUI-MissingRequiredInformation.**
4. El flujo continúa en el paso 2 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer conexión con el servidor y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El **SJR** cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. No es posible enviar el correo de verificación**

- El **SJR** muestra la **GUI-CannotSendEmail** indicando que no se pudo enviar el correo de verificación y la opción para cerrar.
- El **Jugador** selecciona la opción para cerrar la **GUI-CannotSendEmail.**
- El **SJR** cierra la **GUI-CannotSendEmail.**
- Termina el caso de uso.

**FE-03. Error al almacenar la cuenta**

- El **SJR** muestra la **GUI-AccountCreationPersistenceError** indicando que no se pudo almacenar la cuenta en la base de datos y la opción para cerrar.
- El **Jugador** selecciona la opción para cerrar la **GUI-AcountCreationPersistenceError.**
- El **SJR** cierra la **GUI-AccountCreationPersistenceError**
- Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- Existe una nueva cuenta activa.
- El nombre y correo son únicos.
- La contraseña queda almacenada de forma segura.
- El correo fue verificado.
- El código utilizado quedó invalidado.

**En caso de fallo:**

- No existe una cuenta utilizable creada parcialmente.

CU-02 Reiniciar contraseña

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** establezca una nueva contraseña mediante la verificación del acceso al correo electrónico asociado a su **Cuenta**.

Precondiciones

- El **Jugador** no debe tener una sesión iniciada.
- Debe existir comunicación entre el cliente y el servidor.
- El **Jugador** debe disponer de acceso al correo electrónico asociado a su **Cuenta**.

Disparador:

El **Jugador** selecciona la opción Reiniciar contraseña en la GUI-LoginScreen.

Flujo normal

1. El **SJR** muestra la **GUI-PasswordReset** con el campo para ingresar el correo electrónico asociado a la **Cuenta**.
2. El **Jugador** ingresa su correo electrónico.
3. El **Jugador** selecciona la opción para continuar.
4. El **SJR** verifica que el correo electrónico haya sido proporcionado. (**FA-01**)
5. El **SJR** valida el formato del correo electrónico. (**FA-02**)
6. El **SJR** verifica que el correo electrónico corresponda a una **Cuenta** registrada y activa. (**FA-03**)
7. El **SJR** genera un código de verificación temporal asociado al proceso de reinicio de contraseña y lo envía al correo electrónico registrado. (**FE-02**)
8. El **SJR** muestra la **GUI-ConfirmationCodeRequired** con el campo para ingresar el código recibido.
9. El **Jugador** introduce el código en el campo.
10. El **Jugador** selecciona la opción para validar el código recibido.
11. El **SJR** comprueba que el código corresponda al correo y al proceso de reinicio, se encuentre vigente y no haya sido utilizado. (**FA-04**) (**FA-05**) (**FA-06**)
12. El **SJR** cierra la **GUI-ConfirmationCodeRequired.**
13. El **SJR** muestra la **GUI-NewPasswordRequired** con los campos para la nueva contraseña y confirmación de contraseña.
14. El **Jugador** ingresa la nueva contraseña y su confirmación.
15. El **Jugador** selecciona la opción para confirmar el cambio.
16. El **SJR** comprueba que ambas contraseñas coincidan. (**FA-07**)
17. El **SJR** comprueba que la nueva contraseña cumpla la política establecida. (**FA-08**)
18. El **SJR** almacena de forma segura la nueva contraseña sustituyendo la contraseña anterior e invalida el código de verificación utilizado. (**FE-03**)
19. El **SJR** cierra la **GUI-NewPasswordRequired.**
20. El **SJR** muestra la **GUI-PasswordResetSuccessful** informando que la contraseña fue actualizada correctamente con la opción para cerrar.
21. El **Jugador** selecciona la opción para cerrar la **GUI-PasswordResetSuccessful.**
22. El **SJR** cierra la **GUI-PasswordResetSuccessful** y muestra la **GUI-LoginScreen.**

Flujos alternos

**FA-01. Correo electrónico no proporcionado**

1. El **SJR** muestra la **GUI-MissingRequiredInformation** indicando que debe proporcionarse el correo electrónico y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MissingRequiredInformation**.
3. El **SJR** cierra la **GUI-MissingRequiredInformation.**
4. El flujo continúa en el paso 2 del FN.

**FA-02. Formato de correo inválido**

1. El **SJR** muestra la **GUI-InvalidEmailFormat** indicando que el formato del correo no es válido y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidEmailFormat**.
3. El **SJR** cierra la **GUI-InvalidEmailFormat**.
4. El flujo continúa en el paso 2 del FN.

**FA-03. Correo electrónico no asociado a una cuenta activa**

1. El **SJR** muestra la **GUI-AccountNotFound** indicando que no existe una **Cuenta** activa asociada al correo proporcionado y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-AccountNotFound**.
3. El **SJR** cierra la **GUI-AccountNotFound**.
4. El flujo continúa en el paso 2 del FN.

**FA-04. Código incorrecto**

1. El **SJR** muestra la **GUI-InvalidConfirmationCode** indicando que el código ingresado es incorrecto y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidConfirmationCode**.
3. El **SJR** cierra la **GUI-InvalidConfirmationCode**.
4. El flujo continúa en el paso 9 del FN.

**FA-05. Código expirado**

1. El **SJR** muestra la **GUI-ExpiredConfirmationCode** indicando que el código perdió vigencia y la opción para solicitar uno nuevo.
2. El **Jugador** selecciona la opción para solicitar un nuevo código.
3. El **SJR** cierra la **GUI-ExpiredConfirmationCode**.
4. El flujo continúa en el paso 7 del FN.

**FA-06. Código utilizado previamente**

1. El **SJR** muestra la **GUI-ConfirmationCodeAlreadyUsed** indicando que el código ya fue utilizado y la opción para solicitar uno nuevo.
2. El **Jugador** selecciona la opción para solicitar un nuevo código.
3. El **SJR** cierra la **GUI-ConfirmationCodeAlreadyUsed**.
4. El flujo continúa en el paso 7 del FN.

**FA-07. Contraseñas diferentes**

1. El **SJR** muestra la **GUI-PasswordMismatch** indicando que las contraseñas no coinciden y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-PasswordMismatch**.
3. El **SJR** cierra la **GUI-PasswordMismatch**.
4. El flujo continúa en el paso 14 del FN.

**FA-08. Contraseña no cumple la política**

1. El **SJR** muestra la **GUI-InvalidPasswordFormat** informando qué requisitos no se cumplen y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidPasswordFormat**.
3. El **SJR** cierra la **GUI-InvalidPasswordFormat**.
4. El flujo continúa en el paso 14 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed**.
3. El **SJR** cierra la **GUI-ServerConnectionFailed**.
4. Termina el caso de uso.

**FE-02. No es posible enviar el correo de verificación**

1. El **SJR** muestra la **GUI-CannotSendEmail** indicando que no fue posible enviar el código de verificación y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-CannotSendEmail**.
3. El **SJR** cierra la **GUI-CannotSendEmail.**
4. Termina el caso de uso.

**FE-03. Error al almacenar la nueva contraseña**

1. El **SJR** conserva la contraseña anterior.
2. El **SJR** muestra la **GUI-PasswordResetPersistenceError** indicando que no fue posible actualizar la contraseña y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-PasswordResetPersistenceError**.
4. El **SJR** cierra la **GUI-PasswordResetPersistenceError.**
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La contraseña anterior ha sido sustituida por la nueva contraseña.
- La nueva contraseña queda almacenada de forma segura.
- El código de verificación utilizado queda invalidado.

**En caso de fallo:**

- La contraseña anterior permanece sin modificaciones.
- No queda ningún cambio parcial utilizable en la **Cuenta**.

CU-03 Mostrar información de cuenta

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte la información autorizada de su propia **Cuenta** o la información pública de la **Cuenta** de otro **Jugador**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Jugador** selecciona la opción para consultar un perfil propio o de otro **Jugador**.

Flujo normal

1. El **Jugador** selecciona el perfil que desea consultar.
2. El **SJR** obtiene el identificador de la **Cuenta** seleccionada.
3. El **SJR** verifica que la **Cuenta** exista y se encuentre activa. (**FA-01**)
4. El **SJR** determina si la **Cuenta** consultada pertenece al **Jugador** autenticado. (**FA-02**)
5. El **SJR** consulta el nombre de usuario, foto de perfil e información pública de la **Cuenta**. (**FA-04**) (**FE-02**)
6. El **SJR** obtiene las estadísticas disponibles del **Jugador**. (**FA-03**)
7. El **SJR** muestra la **GUI-AccountInformation** con la información permitida.
8. El **Jugador** consulta la información mostrada.
9. El **Jugador** selecciona la opción para cerrar la **GUI-AccountInformation.**
10. El **SJR** cierra la **GUI-AccountInformation** y muestra la GUI desde la cual se realizó la consulta.

Flujos alternos

**FA-01. Cuenta inexistente o desactivada**

1. El **SJR** muestra la **GUI-AccountNotAvailable** indicando que la **Cuenta** solicitada no se encuentra disponible y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-AccountNotAvailable**.
3. El **SJR** cierra la **GUI-AccountNotAvailable.**
4. Termina el caso de uso.

**FA-02. Consulta de la cuenta propia**

1. El **SJR** determina que la **Cuenta** pertenece al **Jugador** autenticado.
2. El **SJR** incluye la información privada autorizada, como el correo electrónico.
3. El flujo continúa en el paso 5 del FN.

**FA-03. Jugador sin partidas finalizadas**

1. El **SJR** determina que no existen partidas finalizadas para calcular estadísticas.
2. El **SJR** muestra los valores estadísticos correspondientes en cero.
3. El flujo continúa en el paso 7 del FN.

**FA-04. Cuenta sin foto de perfil personalizada**

1. El **SJR** determina que la **Cuenta** no posee una foto de perfil personalizada.
2. El **SJR** utiliza la imagen de perfil predeterminada.
3. El flujo continúa en el paso 6 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El **SJR** cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar la información de la cuenta**

1. El **SJR** muestra la **GUI-AccountInformationRetrievalError** indicando que no fue posible obtener la información con la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-AccountInformationRetrievalError.**
3. El **SJR** cierra la **GUI-AccountInformationRetrievalError.**
4. Termina el caso de uso.

Postcondiciones

En caso de éxito:

- El Jugador visualiza únicamente la información de la **Cuenta** para la cual posee autorización.

En caso de fallo:

- No se muestra información de ninguna **Cuenta**.

CU-04 Modificar nombre de usuario

Actor principal:

Jugador.

Objetivo

Permitir que un Jugador modifique el nombre de usuario asociado a su propia **Cuenta.**

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- La **Cuenta** debe encontrarse activa.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Jugador** selecciona la opción para modificar su nombre de usuario en la **GUI-AccountInformation**.

Flujo normal

1. El **SJR** muestra la **GUI-UsernameModification** con el nombre de usuario actual y el campo para ingresar uno nuevo.
2. El **Jugador** ingresa el nuevo nombre de usuario.
3. El **Jugador** selecciona la opción para confirmar.
4. El **SJR** verifica que el nuevo nombre haya sido proporcionado. (FA-01)
5. El **SJR** verifica que el nombre cumpla las reglas de formato establecidas. (FA-02)
6. El **SJR** comprueba que el nuevo nombre no se encuentre utilizado por otra **Cuenta**. (FA-03)
7. El **SJR** sustituye el nombre de usuario anterior por el nuevo. (FE-02)
8. El **SJR** cierra la **GUI-UsernameModification.**
9. El **SJR** muestra la **GUI-UsernameModificationSuccessful** indicando que el cambio se realizó efectivamente y con la opción parar cerrar.
10. El **Jugador** selecciona la opción para cerrar la **GUI-UsernameModificationSuccessful.**
11. El **SJR** cierra la **GUI-UsernameModificationSuccessful** y muestra la **GUI-AccountInformation** actualizada.

Flujos alternos

FA-01. Nombre de usuario no proporcionado

1. El SJR muestra la **GUI-MissingRequiredInformation** indicando que faltan datos requeridos y con la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-MissingRequiredInformation**.
3. El SJR cierra la **GUI-MissingRequiredInformation**.
4. El flujo continúa en el paso 2 del FN.

FA-02. Nombre de usuario con formato inválido

1. El SJR muestra la **GUI-InvalidUsernameFormat** indicando que el nombre no sigue el formato permitido y con la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-InvalidUsernameFormat.**
3. El SJR cierra la **GUI-InvalidUsernameFormat**.
4. El flujo continúa en el paso 2 del FN.

FA-03. Nombre de usuario ocupado

1. El SJR muestra la **GUI-UsernameAlreadyTaken** indicando que el nombre ya fue usado en otra cuenta y con la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-UsernameAlreadyTaken.**
3. El SJR cierra la **GUI-UsernameAlreadyTaken.**
4. El flujo continúa en el paso 2 del FN.

FA-04. El jugador cancela la modificación

1. El Jugador selecciona la opción para cancelar.
2. El SJR cierra la **GUI-UsernameModification.**
3. El SJR muestra la **GUI-AccountInformation** sin realizar cambios.
4. Termina el caso de uso.

Flujos de excepción

FE-01. No se puede conectar con el servidor

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

FE-02. Error al actualizar la cuenta

1. El SJR conserva el nombre de usuario anterior.
2. El SJR muestra la **GUI-UsernameModificationPersistenceError** indicando que ocurrió un error al guardar el cambio y con la opción para cerrar.
3. El Jugador selecciona la opción parar cerrar la **GUI-UsernameModificationPersistenceError**.
4. El SJR cierra la **GUI-UsernameModificationPersistenceError.**
5. Termina el caso de uso.

Postcondiciones

En caso de éxito:

- La **Cuenta** conserva el nuevo nombre de usuario.
- El nuevo nombre continúa siendo único dentro del sistema.

En caso de fallo:

- El nombre de usuario anterior permanece sin modificaciones.

CU-05 Modificar contraseña

Actor principal:

Jugador.

Objetivo

Permitir que un Jugador autenticado sustituya la contraseña de su Cuenta después de comprobar su contraseña actual.

Precondiciones

- El Jugador debe tener una sesión iniciada.
- La Cuenta debe encontrarse activa.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El Jugador selecciona la opción para modificar contraseña desde la **GUI-AccountInformation**.

Flujo normal

1. El SJR muestra la **GUI-PasswordModification** con los campos para contraseña actual, nueva contraseña y confirmación de nueva contraseña.
2. El Jugador proporciona los tres valores.
3. El Jugador selecciona la opción para confirmar.
4. El SJR verifica que todos los campos requeridos hayan sido proporcionados. (FA-01)
5. El SJR verifica que la contraseña actual sea correcta. (FA-02)
6. El SJR comprueba que la nueva contraseña y su confirmación coincidan. (FA-03)
7. El SJR comprueba que la nueva contraseña cumpla la política establecida. (FA-04)
8. El SJR almacena de forma segura la nueva contraseña sustituyendo la anterior. (FE-02)
9. El SJR cierra la GUI-PasswordModification.
10. El SJR muestra la GUI-PasswordModificationSuccessful indicando que la modificación de la contraseña se realizó correctamente y la opción para cerrar.
11. El Jugador selecciona la opción para cerrar la GUI-PasswordModificationSuccessful.
12. El SJR cierra la GUI-PasswordModificationSuccessful y muestra la GUI-AccountInformation.

Flujos alternos

FA-01. Datos faltantes

1. El SJR muestra la **GUI-MissingRequiredInformation** indicando que faltan datos requeridos y con la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-MissingRequiredInformation**.
3. El SJR cierra la **GUI-MissingRequiredInformation**.
4. El flujo continúa en el paso 2 del FN.

FA-02. Contraseña actual incorrecta

1. El SJR muestra la **GUI-CurrentPasswordIncorrect** indicando que la contraseña actual ingresada no es correcta y con la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-CurrentPasswordIncorrect.**
3. El SJR cierra la **GUI-CurrentPasswordIncorrect.**
4. El flujo continúa en el paso 2 del FN.

FA-03. Nuevas contraseñas diferentes

1. El SJR muestra la **GUI-PasswordMismatch** indicando que las contraseñas no coinciden y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-PasswordMismatch.**
3. El SJR cierra la **GUI-PasswordMismatch.**
4. El flujo continúa en el paso 2 del FN.

FA-04. Nueva contraseña no cumple la política

1. El **SJR** muestra la **GUI-InvalidPasswordFormat** informando qué requisitos no se cumplen y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidPasswordFormat**.
3. El **SJR** cierra la **GUI-InvalidPasswordFormat**.
4. El flujo continúa en el paso 2 del FN.

FA-05. El jugador cancela la modificación

1. El **Jugador** selecciona la opción para cancelar.
2. El **SJR** cierra la GUI-PasswordModification.
3. Termina el caso de uso.

Flujos de excepción

FE-01. No se puede conectar con el servidor

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

FE-02. Error al almacenar la nueva contraseña

1. El SJR conserva la contraseña anterior.
2. El **SJR** muestra la **GUI-PasswordModificationPersistenceError** indicando que no fue posible actualizar la contraseña y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-PasswordModificationPersistenceError**.
4. El **SJR** cierra la **GUI-PasswordModificationPersistenceError.**
5. Termina el caso de uso.

Postcondiciones

En caso de éxito:

- La contraseña anterior ha sido sustituida.
- La nueva contraseña queda almacenada de forma segura.

En caso de fallo:

- La contraseña anterior permanece válida y sin modificaciones.

CU-06 Guardar foto de perfil

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** seleccione una imagen para utilizarla como foto de perfil asociada a su **Cuenta**.

Precondiciones

-

Disparador:

El **Jugador** selecciona la opción para cambiar su foto de perfil en la GUI-AccountInformation o GUI-AccountCreation.

Flujo normal

1. El **SJR** muestra la GUI-ProfileImageModification.
2. El **Jugador** selecciona la opción para buscar una imagen.
3. El **Jugador** selecciona una imagen almacenada en su equipo. (**FA-02**)
4. El **SJR** comprueba que la imagen posea un formato permitido y pueda ser procesada. (**FA-01**)
5. El **SJR** muestra la **GUI-ProfileImageModificationConfirmation** solicitando confirmación del usuario para utilizar la imagen seleccionada.
6. El **Jugador** confirma que desea utilizar la imagen seleccionada. (**FA-03**)
7. El **SJR** cierra la **GUI-ProfileImageModificationConfirmation.**
8. El **SJR** almacena la imagen en el servidor y la asocia a la **Cuenta** del **Jugador**. (**FE-02**) (**FE-01**)
9. El **SJR** sustituye la foto anterior (si existía una) asociada a la **Cuenta** del **Jugador**.
10. El **SJR** muestra la **GUI-ProfileImageModificationSuccessful** indicando que la modificación se realizó correctamente y la opción para cerrar.
11. El **Jugador** selecciona la opción para cerrar la **GUI-ProfileImageModificationSuccessful**.
12. El **SJR** cierra la **GUI-ProfileImageModificationSuccessful**.
13. El **SJR** muestra la **GUI-AccountInformation** o  **GUI-AccountCreation** con la nueva imagen.

Flujos alternos

**FA-01. Imagen inválida o no procesable**

1. El **SJR** muestra la **GUI-ImageNotSupported** indicando que la imagen seleccionada no es válida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ImageNotSupported**.
3. El **SJR** cierra la **GUI-ImageNotSupported.**
4. El flujo continúa en el paso 2 del FN.

**FA-02. El jugador cancela la selección del archivo**

1. El **Jugador** cierra el selector sin escoger una imagen.
2. El **SJR** conserva la foto anterior.
3. El flujo continúa en el paso 1 del FN.

**FA-03. El jugador cancela antes de guardar**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-ProfileImageModificationConfirmation**.
3. El **SJR** conserva la foto anterior.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al almacenar la imagen**

1. El **SJR** conserva la fotografía anterior.
2. El **SJR** muestra la **GUI-ProfileImagePersistenceError** indicando que no fue posible actualizar la foto de perfil y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-ProfileImagePersistenceError**.
4. El **SJR** cierra la **GUI-ProfileImagePersistenceError.**
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La nueva fotografía queda asociada a la **Cuenta**.

**En caso de fallo:**

- La fotografía anterior permanece asociada a la **Cuenta**.

CU-07 Mostrar estadísticas del jugador

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte las estadísticas obtenidas a partir de las partidas finalizadas de un jugador.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Jugador** selecciona la opción para consultar las estadísticas de un perfil.

Flujo normal

1. El **SJR** identifica al **Jugador** cuyas estadísticas serán consultadas.
2. El **SJR** consulta las partidas finalizadas en las que participó. (**FA-01**)
3. El **SJR** determina el resultado obtenido en cada partida.
4. El **SJR** calcula el total de partidas jugadas, victorias, derrotas y empates.
5. El **SJR** calcula el porcentaje de victorias cuando corresponda.
6. El **SJR** muestra la **GUI-PlayerStatistics** con los valores calculados.
7. El **Jugador** consulta las estadísticas.
8. El **Jugador** selecciona la opción para cerrar.
9. El **SJR** cierra la **GUI-PlayerStatistics**.

Flujos alternos

**FA-01. Jugador sin partidas finalizadas**

1. El **SJR** determina que no existen partidas contabilizables.
2. El **SJR** muestra las estadísticas con valores iniciales en cero.
3. El flujo continúa en el paso 7 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar las partidas**

1. El **SJR** muestra la **GUI-StatisticsRetrievalError** indicando que no fue posible recuperar las estadísticas del jugador y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-StatisticsRetrievalError.**
3. El **SJR** cierra la **GUI-StatisticsRetrievalError.**
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza estadísticas calculadas a partir de las partidas registradas.

**En caso de fallo:**

- No se modifica ninguna información persistente.

CU-08 Guardar fondo del tablero

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** establezca una imagen personalizada como fondo de su tablero sin modificar la visualización del oponente.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.

Disparador:

El **Jugador** selecciona la opción para personalizar el fondo del tablero en la **GUI-Settings**.

Flujo normal

1. El **SJR** muestra la **GUI-BackgroundImageConfiguration** con una selección de archivos del dispositivo del **Jugador**.
2. El **Jugador** selecciona una imagen. (**FA-02)**
3. El **SJR** comprueba que la imagen sea válida. (**FA-01**)
4. El **SJR** cierra la **GUI-BackgroundImageConfiguration.**
5. El **SJR** muestra la **GUI-BackgroundImageConfigurationConfirmation** solicitando la confirmación del **Jugador** para usar la imagen.
6. El **Jugador** confirma el cambio. (**FA-03)**
7. El **SJR** cierra la **GUI-BackgroundImageConfigurationConfirmation.**
8. El **SJR** almacena la imagen seleccionada en el servidor y la asocia a la **Cuenta** del **Jugador**. (**FE-02**)
9. El **SJR** muestra la **GUI-BackgroundImageConfigurationSuccessful** indicando que la imagen se configuró correctamente y la opción para cerrar.
10. El **Jugador** selecciona la opción parar cerrar la **GUI-BackgroundImageConfigurationSuccessful**
11. El **SJR** cierra la **GUI-BackgroundImageConfigurationSuccessful** y muestra la **GUI-Settings**.

Flujos alternos

**FA-01. Imagen inválida**

1. El **SJR** muestra la **GUI-ImageNotSupported** indicando que la imagen seleccionada no es válida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ImageNotSupported**.
3. El **SJR** cierra la **GUI-ImageNotSupported.**
4. El flujo continúa en el paso 2 del FN.

**FA-02. Cancelación de selección**

1. El **Jugador** cancela el selector.
2. El **SJR** cierra la **GUI-BackgroundImageConfiguration.**
3. Termina el caso de uso.

**FA-03. El jugador decide conservar el fondo actual**

1. El **Jugador** cancela la modificación.
2. El **SJR** cierra la **GUI-BackgroundImageConfigurationConfirmation.**
3. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al guardar la configuración**

1. El **SJR** muestra la **GUI-BackgroundImageConfigurationPersistenceError** indicando que no se pudo guardar la imagen y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-BackgroundImageConfigurationPersistenceError.**
3. El **SJR** cierra la **GUI-BackgroundImageConfigurationPersistenceError.**
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El fondo queda almacenado como preferencia del **Jugador**.
- El fondo personalizado únicamente afecta la vista del **Jugador** que lo configuró.

**En caso de fallo:**

- Se conserva la configuración anterior.

CU-09 Modificar idioma de interfaz

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** seleccione uno de los idiomas soportados por el **SJR** para visualizar la interfaz.

Precondiciones

-

Disparador:

El **Jugador** selecciona la opción para modificar el idioma en la **GUI-LoginScreen** o **GUI-MainMenu**.

Flujo normal

1. El **SJR** muestra la **GUI-LanguageSettings** con los idiomas soportados.
2. El **Jugador** selecciona un idioma. (**FA-03) (FA-02)**
3. El **SJR** muestra la **GUI-LanguageChangeConfirmation** solicitando confirmación al **Jugador** sobre el cambio de idioma.
4. El **Jugador** confirma el cambio. (**FA-04)**
5. El **SJR** comprueba que el idioma continúe disponible entre los idiomas soportados. (**FA-01**)
6. El **SJR** cierra la **GUI-LanguageChangeConfirmation.**
7. El **SJR** cierra la **GUI-LanguageSettings.**
8. El **SJR** guarda el idioma como preferencia del **Jugador**. (**FE-01**)
9. El **SJR** carga los recursos correspondientes al idioma seleccionado. (**FE-02**)
10. El **SJR** actualiza los textos de la interfaz.

Flujos alternos

**FA-01. Idioma no disponible**

1. El **SJR** muestra la **GUI-LanguageNotAvailable** indicando que el idioma seleccionado no está disponible y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-LanguageNotAvailable.**
3. El **SJR** cierra la **GUI-LanguageNotAvailable.**
4. El **SJR** conserva el idioma anterior.
5. Termina el caso de uso.

**FA-02. El jugador selecciona el idioma que ya se encuentra activo**

1. El **SJR** detecta que no existe cambio.
2. El **SJR** conserva la configuración actual.
3. Termina el caso de uso.

**FA-03. El jugador cancela**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-LanguageSettings**.
3. Termina el caso de uso.

**FA-04. El jugador no confirma el cambio**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-LanguageChangeConfirmation.**
3. El **SJR** cierra la **GUI-LanguageSettings.**
4. Termina el caso de uso.

Flujos de excepción

**FE-01. Error al almacenar la preferencia**

1. El **SJR** conserva el idioma configurado anteriormente.
2. El **SJR** muestra la **GUI-LanguageSettingsPersistenceError** indicando que ocurrió un error al guardar las preferencias del **Jugador** y la opción cerrar.
3. El **Jugador** selecciona la opción **Cerrar.**
4. El **SJR** cierra la **GUI-LanguageSettingsPersistenceError.**
5. Termina el caso de uso.

**FE-02. Error al cargar los recursos del idioma**

1. El **SJR** conserva el idioma anterior.
2. El **SJR** muestra la **GUI-LanguageResourcesError** indicando que ocurrió un error al cargar los recursos del idioma seleccionado y la opción cerrar.
3. El **Jugador** selecciona la opción cerrar.
4. El **SJR** cierra la **GUI-LanguageResourcesError.**
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El idioma queda asociado a la configuración del **Jugador**.
- Los textos propios del sistema se muestran utilizando el idioma seleccionado.

**En caso de fallo:**

- El idioma previamente configurado permanece activo.

CU-10 Desactivar cuenta

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** desactive su propia **Cuenta** conservando la información histórica necesaria.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- La **Cuenta** debe encontrarse activa.

Disparador:

El **Jugador** selecciona la opción Desactivar cuenta desde la **GUI-AccountInformation** en su propio perfil.

Flujo normal

1. El **SJR** muestra la **GUI-AccountDeactivationConfirmation** solicitando confirmación.
2. El **Jugador** selecciona la opción para desactivar la **Cuenta**.
3. El **SJR** comprueba que el **Jugador** no participe en una partida activa. (**FA-01**)
4. El **SJR** modifica el estado de la **Cuenta** a desactivada sin eliminarla físicamente. (**FE-01)** (**FE-02**)
5. El **SJR** conserva las partidas, movimientos, mensajes y resultados históricos asociados.
6. El **SJR** termina la sesión del **Jugador**.
7. El **SJR** muestra la **GUI-AccountDeactivationSuccessful** indicando que la desactivación se realizó correctamente y la opción cerrar.
8. El **Jugador** selecciona la opción para cerrar la **GUI-AccountDeactivationSuccessful**.
9. El **SJR** cierra la **GUI-AccountDeactivationSuccessful**.
10. El **SJR** muestra la **GUI-LoginScreen**.

Flujos alternos

**FA-01. Existe una partida activa**

1. El **SJR** muestra la **GUI-ActiveMatchPreventsAccountDeactivation** indicando que una cuenta no puede ser desactivada durante una partida activa y la opción cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ActiveMatchPreventsAccountDeactivation**.
3. El **SJR** cierra la **GUI-ActiveMatchPreventsAccountDeactivation.**
4. La **Cuenta** permanece activa.
5. Termina el caso de uso.

**FA-02. El jugador cancela la desactivación**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la GUI-AccountDeactivationConfirmation.
3. La **Cuenta** permanece activa.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al modificar el estado de la cuenta**

1. El **SJR** mantiene la **Cuenta** activa.
2. El **SJR** muestra la **GUI-AccountDeactivationPersistenceError** indicando que ocurrió un error al modificar el estado de la cuenta y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-AccountDeactivationPersistenceError.**
4. El **SJR** cierra la **GUI-AccountDeactivationPersistenceError**.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La **Cuenta** queda desactivada.
- El **Jugador** deja de poder utilizarla.
- La información histórica permanece almacenada.

**En caso de fallo:**

- La **Cuenta** permanece activa.

CU-11 Crear partida

Actor principal:

**Jugador**

Objetivo

Permitir que un **Jugador** cree una nueva **Partida** de Reversi contra otro **Jugador** o contra un oponente controlado por el **SJR**.

Precondiciones

- El **Jugador** debe tener una sesión de **Cuenta** iniciada o el **Invitado** debe tener una sesión temporal activa.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Jugador** selecciona la opción Crear partida desde la **GUI-MainMenu**.

Flujo normal

1. El **SJR** muestra la **GUI-MatchCreation** con la configuración de tiempo disponible por turno.
2. El **Jugador** establece el tiempo por turno.
3. El **Jugador** selecciona la opción para crear la **Partida**. (**FA-04)**
4. El **SJR** comprueba que la configuración proporcionada sea válida. (**FA-02**)
5. El **SJR** crea una nueva **Partida**. (**FE-01**) (**FE-02**)
6. El **SJR** registra al **Jugador** creador como primer participante.
7. El **SJR** genera un **Código de partida** que identifica de forma única a la **Partida** mientras se encuentre disponible.
8. El **SJR** asocia el **Código de partida** con la **Partida** creada.
9. El **SJR** establece la distribución inicial válida del tablero de Reversi.
10. El **SJR** establece la **Partida** en estado Esperando.
11. El **SJR** cierra la **GUI-MatchCreation**.
12. El **SJR** muestra la **GUI-WaitingForOpponent** mostrando el **Código de partida** y la opción para jugar contra el sistema
13. Después de cierta cantidad de tiempo, otro **Jugador** se conecta a la partida. (**FA-03)**
14. El **SJR** asigna la configuración inicial del tablero y el turno inicial.
15. El **SJR** establece la **Partida** en estado En curso.
16. El **SJR** deja de aceptar nuevas conexiones mediante el **Código de partida**.
17. El **SJR** muestra la GUI-Game.

Flujos alternos

**FA-01. Utilizar tiempo predeterminado**

1. El **Jugador** no modifica el tiempo por turno.
2. El **SJR** asigna 40 segundos por turno.
3. El flujo continúa en el paso 4 del FN.

**FA-02. Configuración de tiempo inválida**

1. El **SJR** muestra la **GUI-InvalidTurnTime** indicando que la configuración ingresada de tiempo no es válida y la opción cerrar.
2. El **Jugador** selecciona la opción para cerra la **GUI-InvalidTurnTime**.
3. El **SJR** cierra la **GUI-InvalidTurnTime.**
4. El flujo continúa en el paso 2 del FN.

**FA-03. Crear partida contra el SJR**

1. El **Jugador** selecciona jugar contra un oponente controlado por el **SJR**.
2. El **SJR** crea la **Partida** con ambos participantes.
3. El **SJR** asigna la configuración inicial del tablero y el turno inicial.
4. El **SJR** establece la **Partida** en estado En curso.
5. El **SJR** deja de aceptar conexiones mediante el **Código de partida.**
6. El **SJR** muestra la **GUI-Game**.
7. Termina el caso de uso.

**FA-04. El jugador cancela la creación**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-MatchCreation.**
3. El **SJR** muestra la **GUI-MainMenu.**
4. Termina el caso de uso.

**FA-05. Jugador cancela búsqueda de oponente**

1. El **Jugador** selecciona cancelar.
2. El **SJR** invalida el **Código de partida**.
3. El **SJR** cierra la **GUI-WaitingForOpponent**.
4. El **SJR** muestra la **GUI-MainMenu**.
5. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al crear la partida**

1. El **SJR** evita dejar una **Partida** parcialmente registrada.
2. El **SJR** muestra la **GUI-MatchCreationPersistenceError** indicando que no se pudo crear la partida y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-MatchCreationPersistenceError.**
4. El **SJR** cierra la **GUI-MatchCreationPersistenceError.**
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- Existe una nueva **Partida**.
- El tablero posee la configuración inicial válida.
- El **Jugador** creador se encuentra registrado como participante.
- La **Partida** queda Esperando o En curso según el tipo de oponente.
- La **Partida** posee un **Código de partida** válido que permite a otro participante localizarla directamente.

**En caso de fallo:**

- No queda una **Partida** parcialmente creada.

CU-12 Mostrar información de partida

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte la información y el estado correspondiente a una **Partida**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir comunicación con el servidor.

Disparador:

El **Jugador** selecciona una **Partida** para consultar su información.

Flujo normal

1. El **SJR** obtiene el identificador de la **Partida** seleccionada. (**FE-02)**
2. El **SJR** verifica que la **Partida** exista. (**FA-01**)
3. El **SJR** consulta los participantes.
4. El **SJR** consulta el estado de la **Partida**.
5. El **SJR** consulta la configuración de tiempo.
6. El **SJR** obtiene el último estado válido del tablero.
7. El **SJR** obtiene la cantidad de fichas de cada participante.
8. El **SJR** consulta los movimientos registrados.
9. El **SJR** obtiene el resultado si la **Partida** se encuentra finalizada. (**FA-02**)
10. El **SJR** muestra la **GUI-MatchInformation**.
11. El **Jugador** consulta la información.
12. El **Jugador** selecciona la opción para cerrar.
13. El **SJR** cierra la **GUI-MatchInformation**.

Flujos alternos

**FA-01. Partida inexistente**

1. El **SJR** muestra la **GUI-MatchNotFound** indicando que la partida seleccionada no existe y la opción cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchNotFound**.
3. El **SJR** cierra la **GUI-MatchNotFound.**
4. Termina el caso de uso.

**FA-02. Partida aún no finalizada**

1. El **SJR** determina que todavía no existe un resultado definitivo.
2. El **SJR** muestra el estado actual sin resultado final.
3. El flujo continúa en el paso 10 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al obtener la partida**

1. El **SJR** muestra la **GUI-MatchInformationRetrievalError** indicando que los datos de la partida seleccionada no pudieron ser recuperados y la opción cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchInformationRetrievalError.**
3. El **SJR** cierra la **GUI-MatchInformationRetrievalError.**
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza la información vigente o histórica de la **Partida**.

**En caso de fallo:**

- No se muestra el estado de la **Partida**.

CU-13 Mostrar partidas disponibles

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte las **Partidas** creadas por otros jugadores que aún se encuentran disponibles para unirse.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.

Disparador:

El **Jugador** selecciona la opción Unirse a una partida desde la GUI-MainMenu.

Flujo normal

1. El **SJR** consulta las **Partidas** en estado Esperando.
2. El **SJR** excluye aquellas que ya poseen dos participantes.
3. El **SJR** excluye las partidas contra un oponente controlado por el **SJR**.
4. El **SJR** muestra la GUI-AvailableMatches con las partidas disponibles. (**FA-01**)
5. El **Jugador** consulta la lista.
6. El **Jugador** puede seleccionar una **Partida** para iniciar CU-15 Unirse a partida.

Flujos alternos

**FA-01. No existen partidas disponibles**

1. El **SJR** muestra la GUI-AvailableMatches sin partidas y un mensaje indicando que actualmente no existen partidas disponibles con las opciones para cerrar y actualizar.
2. El **Jugador** puede cerrar la GUI o actualizar la lista.

**FA-02. El jugador solicita actualizar la lista**

1. El **Jugador** selecciona la opción para actualizar.
2. El flujo continúa en el paso 1 del FN.

**FA-03. El jugador cierra la lista**

1. El **Jugador** selecciona la opción para volver.
2. El **SJR** cierra la **GUI-AvailableMatches**.
3. El **SJR** muestra la **GUI-MainMenu**.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar las partidas**

1. El **SJR** muestra la **GUI-AvailableMatchesRetrievalError** indicando que no pudo consultar las partidas disponibles y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-AvailableMatchesRetrievalError.**
3. El **SJR** cierra la **GUI-AvailableMatchesRetrievalError.**
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** dispone de una lista actualizada de partidas a las que puede unirse.

**En caso de fallo:**

- No se muestra la lista de partidas.

CU-14 Modificar partida

Actor principal:

**Jugador creador**.

Objetivo

Permitir que el **Jugador creador** modifique la configuración permitida de una **Partida** antes de que esta haya comenzado.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- El **Jugador** debe ser el creador de la **Partida**.
- La **Partida** debe encontrarse en estado Esperando.

Disparador:

El **Jugador creador** selecciona la opción para modificar la configuración de una **Partida** creada.

Flujo normal

1. El **SJR** muestra la **GUI-MatchSettings** con la configuración actual de tiempo disponible por turno y la opción para guardar.
2. El **Jugador creador** modifica el tiempo disponible por turno.
3. El **Jugador creador** selecciona la opción para guardar. (**FA-04)**
4. El **SJR** verifica que la **Partida** continúe en estado Esperando. (**FA-01**)
5. El **SJR** verifica que el **Jugador** continúe siendo el creador. (**FA-02**)
6. El **SJR** valida la nueva configuración. (**FA-03**)
7. El **SJR** actualiza la configuración de la **Partida**. (**FE-02**)
8. El **SJR** muestra la **GUI-MatchSettingsModificationSuccessful** indicando que la configuración fue modificada exitosamente y la opción para cerrar.
9. El **Jugador** selecciona la opción para cerrar la **GUI-MatchSettingsModificationSuccessful**.
10. El **SJR** cierra la **GUI-MatchSettingsModificationSuccessful.**
11. El **SJR** muestra la **GUI-WaitingForOpponent**.

Flujos alternos

**FA-01. La partida ya comenzó**

1. El **SJR** muestra la **GUI-MatchAlreadyStarted** indicando que la partida ya comenzó y por tanto no puede editarse y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchAlreadyStarted**
3. El **SJR** cierra la **GUI-MatchAlreadyStarted**.
4. Termina el caso de uso.

**FA-02. El jugador no es el creador**

1. El **SJR** muestra la **GUI-UnauthorizedMatchModification** indicando que no tiene autorización para modificar los ajustes de esta partida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-UnauthorizedMatchModification.**
3. El **SJR** cierra la **GUI-UnauthorizedMatchModification.**
4. Termina el caso de uso.

**FA-03. Configuración inválida**

1. El **SJR** muestra la **GUI-InvalidTurnTime** indicando que el valor introducido para el tiempo no es válido y la opción cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InvalidTurnTime**.
3. El **SJR** cierra la **GUI-InvalidTurnTime.**
4. El flujo continúa en el paso 2 del FN.

**FA-04. El jugador cancela**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-MatchSettings**.
3. No se realizan cambios.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al actualizar la partida**

1. El **SJR** conserva la configuración anterior.
2. El **SJR** muestra la **GUI-MatchSettingsPersistenceError** indicando que no fue posible modificar la configuración de la partida y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-MatchSettingsPersistenceError**.
4. El **SJR** cierra la **GUI-MatchSettingsPersistenceError**.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La **Partida** conserva la nueva configuración.
- Su estado y tablero permanecen sin modificaciones.

**En caso de fallo:**

- Se conserva la configuración anterior.

CU-15 Unirse a partida

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** se incorpore como segundo participante de una **Partida** disponible.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir una **Partida** disponible a la cual se desea unir.

Disparador:

El **Jugador** selecciona una **Partida** en la **GUI-AvailableMatches** o selecciona la opción Unirse mediante código.

Flujo normal

1. El jugador no selecciona Unirse mediante código (**FA-04)**
2. El **SJR** obtiene el identificador de la **Partida** seleccionada.
3. El **SJR** vuelve a consultar el estado actual de la **Partida**.
4. El **SJR** verifica que continúe en estado Esperando. (**FA-01**)
5. El **SJR** verifica que solamente tenga un participante. (**FA-01**)
6. El **SJR** comprueba que el **Jugador** no sea el creador de la misma **Partida**. (**FA-02**)
7. El **SJR** registra al **Jugador** como segundo participante. (**FE-02**)
8. El **SJR** asigna los elementos necesarios para iniciar la partida conforme a las reglas del juego.
9. El **SJR** establece el turno inicial.
10. El **SJR** modifica el estado de la **Partida** a En curso.
11. El **SJR** cierra la **GUI-AvailableMatches**.
12. El **SJR** muestra la **GUI-Game** a ambos participantes.

Flujos alternos

**FA-01. La partida dejó de estar disponible**

1. El **SJR** muestra la **GUI-MatchNoLongerAvailable** indicando que la partida ya no se encuentra disponible y la opción de cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchNoLongerAvailable**.
3. El **SJR** cierra la **GUI-MatchNoLongerAvailable.**
4. El flujo continúa en CU-13 Mostrar partidas disponibles.

**FA-02. El jugador intenta unirse a su propia partida**

1. El **SJR** muestra la **GUI-CannotJoinOwnMatch** indicando que no un jugador no puede unirse a su propia partida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-CannotJoinOwnMatch.**
3. El **SJR** cierra la **GUI-CannotJoinOwnMatch**.
4. El flujo continúa en CU-13.

**FA-03. El jugador cancela antes de unirse**

1. El **Jugador** cancela la selección.
2. No se modifica la **Partida**.
3. Termina el caso de uso.

**FA-04. Unirse directamente mediante código de partida**

1. El **Jugador** selecciona la opción Unirse mediante código.
2. El **SJR** muestra la **GUI-JoinMatchByCode** con el campo para ingresar el **Código de partida**.
3. El **Jugador** ingresa el código.
4. El **Jugador** selecciona la opción para unirse.
5. El **SJR** verifica que se haya proporcionado un código y que cumpla el formato establecido. (**FA-05**)
6. El **SJR** busca una **Partida** disponible asociada al código proporcionado. (**FA-06**)
7. El **SJR** obtiene el identificador de la **Partida** correspondiente.
8. El **SJR** cierra la **GUI-JoinMatchByCode**.
9. El flujo continúa en el paso 3 del FN.

**FA-05. Código de partida vacío o con formato inválido**

1. El **SJR** muestra la **GUI-InvalidMatchCodeFormat** indicando que debe proporcionarse un código válido y la opción para cerrar.
2. El **Jugador** o **Invitado** selecciona la opción para cerrar la **GUI-InvalidMatchCodeFormat**.
3. El **SJR** cierra la **GUI-InvalidMatchCodeFormat**.
4. El flujo continúa en el paso 3 del FA-04.

**FA-06. Código inexistente o correspondiente a una partida no disponible**

1. El **SJR** no encuentra una **Partida** disponible asociada al código ingresado.
2. El **SJR** muestra la **GUI-MatchCodeNotAvailable** indicando que el código no corresponde a una partida disponible y la opción para cerrar.
3. El **Jugador** o **Invitado** selecciona la opción para cerrar la **GUI-MatchCodeNotAvailable**.
4. El **SJR** cierra la **GUI-MatchCodeNotAvailable**.
5. El flujo continúa en el paso 3 del FA-04.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al registrar al segundo participante**

1. El **SJR** evita modificar parcialmente la **Partida**.
2. El **SJR** muestra la **GUI-JoinMatchPersistenceError** indicando que no fue posible unirse a la partida y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-JoinMatchPersistenceError.**
4. El **SJR** cierra la **GUI-JoinMatchPersistenceError.**
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La **Partida** posee exactamente dos participantes.
- La **Partida** se encuentra En curso.
- Existe un turno inicial definido.
- El **Código de partida** deja de permitir el ingreso de nuevos participantes.
- Si participa un **Invitado**, este conserva las restricciones propias de su sesión temporal.

**En caso de fallo:**

- El **Jugador** no queda parcialmente asociado a la **Partida**.
- La disponibilidad del código no cambia como consecuencia de un intento fallido.

CU-16 Realizar movimiento

Actor principal:

**Jugador**.

Objetivo

Permitir que el **Jugador** cuyo turno se encuentra activo realice un movimiento válido conforme a las reglas de Reversi y actualizar el estado de la **Partida**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- El **Jugador** debe ser participante de la **Partida**.
- La **Partida** debe encontrarse En curso.
- Debe corresponderle el turno al **Jugador**.

Disparador:

Comienza el turno del **Jugador** en la GUI-Game.

Flujo normal

1. El **SJR** determina las posiciones válidas disponibles para el **Jugador**. (**FA-02**)
2. El **SJR** identifica visualmente las posiciones válidas en el tablero.
3. El **SJR** inicia el temporizador del turno utilizando el tiempo configurado para la **Partida**. (**FA-03**) (**FA-04**)
4. El **Jugador** selecciona una posición del tablero.
5. El **SJR** comprueba que la posición continúe siendo válida. (**FA-01**)
6. El **SJR** determina las fichas que deben cambiar conforme a las reglas de Reversi.
7. El **SJR** crea el registro del **Movimiento**, indicando que fue realizado por el **Jugador**. (**FE-02**)
8. El **SJR** actualiza el estado del tablero.
9. El **SJR** reinicia el contador de agotamientos consecutivos del **Jugador**, debido a que realizó el movimiento dentro del tiempo disponible.
10. El **SJR** actualiza la cantidad de fichas de cada participante.
11. El **SJR** determina las posiciones válidas para el oponente. (**FA-05**) (**FA-06**)
12. El **SJR** cambia el turno al oponente.
13. El **SJR** actualiza la **GUI-Game** mostrando el nuevo estado del tablero, puntuación y turno.
14. Termina el caso de uso.

Flujos alternos

**FA-01. Posición inválida**

1. El **SJR** rechaza la posición seleccionada.
2. El **SJR** mantiene sin cambios el estado del tablero.
3. El **SJR** mantiene activo el turno actual.
4. El flujo continúa en el paso 4 del FN mientras el tiempo no haya terminado.

**FA-02. El jugador no tiene movimientos válidos al comenzar su turno**

1. El **SJR** determina que el **Jugador** no posee posiciones válidas.
2. El **SJR** comprueba si el oponente posee movimientos válidos. (**FA-06**)
3. El **SJR** omite el turno del **Jugador**.
4. El **SJR** asigna el turno al oponente.
5. El **SJR** actualiza la GUI-Game.
6. Termina el caso de uso.

**FA-03. Se agota el tiempo por primera o segunda vez consecutiva**

1. El temporizador llega a cero antes de que el **Jugador** realice un movimiento.
2. El **SJR** incrementa el contador de agotamientos consecutivos del **Jugador**.
3. El **SJR** obtiene las posiciones válidas disponibles.
4. El **SJR** selecciona aleatoriamente una posición válida.
5. El **SJR** determina las fichas que deben cambiar.
6. El **SJR** registra el **Movimiento** indicando que fue realizado automáticamente debido al agotamiento del tiempo.
7. El **SJR** actualiza el tablero y la cantidad de fichas.
8. El **SJR** determina las posiciones válidas del oponente.
9. Si la partida puede continuar, el **SJR** cambia el turno al oponente.
10. El **SJR** actualiza la GUI-Game.
11. Termina el caso de uso.

**FA-04. Se agota el tiempo por tercera vez consecutiva**

1. El temporizador llega a cero.
2. El **SJR** determina que corresponde al tercer turno consecutivo en que el **Jugador** agota su tiempo.
3. El **SJR** finaliza la **Partida** por inactividad.
4. El **SJR** registra al oponente como ganador conforme a las reglas establecidas. (**FE-03**)
5. El **SJR** registra el motivo de finalización como Inactividad.
6. El **SJR** muestra la **GUI-MatchResult**.
7. Termina el caso de uso.

**FA-05. El oponente no tiene movimientos válidos pero el jugador actual sí**

1. El **SJR** determina que el oponente no posee movimientos válidos.
2. El **SJR** comprueba que el **Jugador** que acaba de mover sí posee nuevas posiciones válidas.
3. El **SJR** omite el turno del oponente.
4. El **SJR** vuelve a asignar el turno al **Jugador**.
5. El **SJR** inicia un nuevo temporizador.
6. El **SJR** actualiza la **GUI-Game**.
7. Termina el caso de uso.

**FA-06. Ninguno de los jugadores tiene movimientos válidos**

1. El **SJR** determina que ninguno de los dos participantes posee movimientos válidos.
2. El **SJR** cuenta las fichas correspondientes a cada jugador.
3. El **SJR** determina el resultado conforme a las reglas de Reversi.
4. El **SJR** modifica el estado de la **Partida** a Finalizada. (**FE-03**)
5. El **SJR** registra el resultado y la fecha de finalización.
6. El **SJR** muestra la GUI-MatchResult con el resultado definitivo.
7. Termina el caso de uso.

Flujos de excepción

**FE-01. Se pierde la conexión con el servidor antes de registrar el movimiento**

1. El **SJR** no considera confirmado el movimiento.
2. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
3. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
4. El SJR cierra la **GUI-ServerConnectionFailed.**
5. El estado persistido de la **Partida** permanece en el último movimiento confirmado.
6. Termina el caso de uso.

**FE-02. Error al registrar el movimiento o actualizar el tablero**

1. El **SJR** evita almacenar un **Movimiento** sin su correspondiente actualización de estado.
2. El **SJR** conserva el último estado válido de la **Partida**.
3. El **SJR** muestra la **GUI-MovementPersistenceError** indicando que no se pudo registrar el movimiento ni actualizar el tablero y la opción cerrar.
4. El **Jugador** selecciona la opción para cerrar la **GUI-MovementPersistenceError.**
5. El **SJR** cierra la **GUI-MovementPersistenceError.**
6. Termina el caso de uso.

**FE-03. Error al registrar el resultado de una partida finalizada**

1. El **SJR** muestra la **GUI-MatchResultPersistenceError** indicando que no se pudo guardar el resultado de la partida y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchResultPersistenceError.**
3. El **SJR** cierra la **GUI-MatchResultPersistenceError.**
4. El **SJR** evita aceptar nuevos movimientos hasta resolver el estado persistente de la **Partida**.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito con continuación de la partida:**

- Existe un nuevo **Movimiento** válido registrado.
- El tablero corresponde al movimiento realizado.
- La cantidad de fichas se encuentra actualizada.
- El turno siguiente ha sido determinado correctamente.

**En caso de éxito con finalización:**

- La **Partida** se encuentra Finalizada.
- Existe un resultado definitivo y un motivo de finalización.
- No se permiten movimientos posteriores.

**En caso de fallo:**

- El estado persistente de la **Partida** permanece en el último estado válido confirmado.

CU-17 Abandonar partida

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** abandone una **Partida** en curso, provocando su finalización conforme a las reglas establecidas.

Precondiciones

- El **Jugador** debe participar en una **Partida** En curso.

Disparador:

El **Jugador** selecciona la opción para abandonar la partida en la **GUI-Game**.

Flujo normal

1. El **SJR** muestra la **GUI-AbandonMatchConfirmation** solicitando confirmación al usuario antes de abandonar.
2. El **Jugador** confirma que desea abandonar la **Partida**. (**FA-01**)
3. El **SJR** verifica que la **Partida** continúe En curso. (**FA-02**)
4. El **SJR** registra que el **Jugador** abandonó la **Partida**.
5. El **SJR** establece al otro participante como ganador conforme a las reglas establecidas.
6. El **SJR** modifica el estado de la **Partida** a Finalizada.
7. El **SJR** registra Abandono como motivo de finalización. (**FE-02**)
8. El **SJR** conserva todos los movimientos anteriores.
9. El **SJR** muestra la **GUI-MatchResult** indicando el ganador de la partida y la opción para cerrar.
10. El **Jugador** selecciona la opción para cerrar la **GUI-MatchResult.**
11. El **SJR** cierra la **GUI-MatchResult.**
12. Termina el caso de uso.

Flujos alternos

**FA-01. El jugador cancela el abandono**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-AbandonMatchConfirmation**.
3. El **SJR** muestra nuevamente la **GUI-Game**.
4. La **Partida** continúa.
5. Termina el caso de uso.

**FA-02. La partida ya había finalizado**

1. El **SJR** detecta que la **Partida** ya no se encuentra En curso.
2. El **SJR** no registra un abandono adicional.
3. El **SJR** muestra la **GUI-MatchResult** existente.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** no confirma el abandono.
2. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
3. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
4. El SJR cierra la **GUI-ServerConnectionFailed.**
5. Termina el caso de uso.

**FE-02. Error al registrar la finalización**

1. El **SJR** muestra la **GUI-MatchResultPersistenceError** indicando que ocurrió un error al registrar la finalización de la **Partida** y con la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchResultPersistenceError.**
3. El **SJR** cierra la **GUI-MatchResultPersistenceError.**
4. El **SJR** evita registrar un resultado parcial.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- La **Partida** queda Finalizada.
- Se conserva su historial de movimientos.
- Se registra Abandono como motivo de finalización.

**En caso de fallo o cancelación:**

- No se registra un abandono incompleto.

CU-18 Mostrar historial de partidas

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte las **Partidas** en las que ha participado y sus resultados.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.

Disparador:

El **Jugador** selecciona la opción para el historial de partidas en la **GUI-MainMenu**.

Flujo normal

1. El **SJR** consulta las **Partidas** asociadas al **Jugador**. (**FE-01) (FE-02)**
2. El **SJR** obtiene para cada partida el oponente, fecha, estado, resultado, puntaje final y motivo de finalización cuando corresponda.
3. El **SJR** ordena las partidas de la más reciente a la más antigua.
4. El **SJR** muestra la **GUI-MatchHistory**. (**FA-01**)
5. El **Jugador** consulta el historial.
6. El **Jugador** puede seleccionar una partida para iniciar CU-12 Mostrar información de partida.
7. El **Jugador** cierra la **GUI-MatchHistory**.

Flujos alternos

**FA-01. El jugador no posee partidas**

1. El **SJR** muestra la **GUI-MatchHistory** sin registros y un mensaje informativo indicando que no se tienen partidas registradas y con la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchHistory**.
3. El **SJR** cierra la **GUI-MatchHistory.**
4. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar el historial**

1. El **SJR** muestra la **GUI-MatchHistoryRetrievalError** indicando que no fue posible consultar los datos del historial de partidas y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MatchHistoryRetrievalError.**
3. El **SJR** cierra la **GUI-MatchHistoryRetrievalError**.
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza las partidas registradas en su historial.

**En caso de fallo:**

- No se muestra ninguna **Partida**.

CU-19 Mostrar información de movimiento

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte la información de los **Movimientos** registrados en una **Partida**.

Precondiciones

- El **Jugador** debe tener acceso a la **Partida** consultada.

Disparador:

El **Jugador** selecciona la opción para consultar los movimientos de una **Partida**.

Flujo normal

1. El **SJR** obtiene el identificador de la **Partida**.
2. El **SJR** consulta sus **Movimientos**.
3. El **SJR** ordena los movimientos conforme a su secuencia.
4. El **SJR** obtiene para cada movimiento el jugador, posición, fecha y hora, fichas afectadas y origen manual o automático.
5. El **SJR** muestra la **GUI-MovementHistory** con la información de los **Movimientos** y la opción para cerrar. (**FA-01**)
6. El **Jugador** consulta la información.
7. El **Jugador** selecciona la opción para cerrar la **GUI-MovementHistory**.
8. El **SJR** cierra la **GUI-MovementHistory.**

Flujos alternos

-

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al recuperar los movimientos**

1. El **SJR** muestra la **GUI-MovementHistoryRetrievalError** indicando que ocurrió un error al recuperar el historial de **Movimientos** de la **Partida** y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MovementHistoryRetrievalError.**
3. El **SJR** cierra la **GUI-MovementHistoryRetrievalError**.
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza el historial ordenado de movimientos.

**En caso de fallo:**

- Ningún **Movimiento** es mostrado.

CU-20 Consultar instrucciones

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte las instrucciones y reglas de Reversi disponibles en el idioma configurado.

Precondiciones

- El **SJR** debe disponer de los recursos de instrucciones correspondientes.

Disparador:

El **Jugador** selecciona la opción para consultar las instrucciones.

Flujo normal

1. El **SJR** determina el idioma configurado para el **Jugador**.
2. El **SJR** carga las instrucciones correspondientes a dicho idioma. (**FE-01**)
3. El **SJR** muestra la **GUI-Instructions** con las instrucciones de Reversi en el idioma actualmente configurado y la opción para cerrar**.**
4. El **Jugador** consulta las instrucciones.
5. El **Jugador** selecciona la opción para cerrar la **GUI-Instructions**.
6. El **SJR** cierra la **GUI-Instructions**.
7. El **SJR** muestra la **GUI** desde la cual se realizó la consulta.

Flujos alternos

**FA-01. Consulta desde una partida**

1. El **Jugador** selecciona el botón de instrucciones desde la GUI-Game.
2. El **SJR** muestra la GUI-Instructions sobre el contexto de la partida.
3. Después de cerrarla, el **SJR** vuelve a mostrar la GUI-Game.

**FA-02. Consulta desde una pantalla distinta**

1. El **SJR** conserva la pantalla desde la cual fueron abiertas las instrucciones.
2. Al cerrar la GUI-Instructions, vuelve a mostrarla.

Flujos de excepción

**FE-01. No se pueden cargar las instrucciones**

1. El **SJR** muestra la **GUI-InstructionsLoadingError** indicando que ocurrió un error al cargar las instrucciones en el idioma actualmente seleccionado y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-InstructionsLoadingError**.
3. El **SJR** cierra la **GUI-InstructionsLoadingError**.
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza las instrucciones.
- No se modifica ninguna información de la **Partida** ni de la **Cuenta**.

**En caso de fallo:**

- El estado previo del sistema permanece sin modificaciones.

CU-21 Enviar solicitud de amistad

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** envíe una solicitud de amistad a otro **Jugador**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- La **Cuenta** destinataria debe encontrarse activa.

Disparador:

El **Jugador** selecciona la opción para agregar amigo desde el perfil de otro **Jugador, GUI-AccountInformation**.

El **Jugador** selecciona la opción para enviar solicitud desde la **GUI-AddFriends.**

Flujo normal

1. El **SJR** identifica al **Jugador** solicitante y al destinatario.
2. El **SJR** verifica que no correspondan a la misma **Cuenta**. (**FA-01**)
3. El **SJR** comprueba que no exista una relación de amistad aceptada entre ambos. (**FA-02**)
4. El **SJR** comprueba que no exista una solicitud pendiente equivalente. (**FA-03**)
5. El **SJR** crea una **Solicitud de amistad** en estado Pendiente. (**FE-02**)
6. El **SJR** muestra la **GUI-FriendRequestSent** indicando que se envió la solicitud de amistad al jugador con la opción para cerrar.
7. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRequestSent.**
8. El **SJR** cierra la **GUI-FriendRequestSent**.

Flujos alternos

**FA-01. El jugador intenta agregarse a sí mismo**

1. El **SJR** muestra la **GUI-CannotAddYourself** indicando que no puedes enviarte una solicitud de amistad a tí mismo y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-CannotAddYourself.**
3. El **SJR** cierra la **GUI-CannotAddYourself.**
4. Termina el caso de uso.

**FA-02. Los jugadores ya son amigos**

1. El **SJR** muestra la **GUI-FriendshipAlreadyExists** indicando que los jugadores ya son amigos y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendshipAlreadyExists.**
3. El **SJR** cierra la **GUI-FriendshipAlreadyExists.**
4. Termina el caso de uso.

**FA-03. Ya existe una solicitud pendiente**

1. El **SJR** muestra la **GUI-FriendRequestAlreadyPending** indicando que ya existe una solicitud pendiente y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendshipAlreadyExists.**
3. El **SJR** cierra la **GUI-FriendshipAlreadyExists.**
4. Termina el caso de uso.

**FA-04. La cuenta destinataria dejó de estar disponible**

1. El **SJR** muestra la **GUI-AccountNotAvailable** indicando que la cuenta seleccionada ya no está disponible y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendshipAlreadyExists.**
3. El **SJR** cierra la **GUI-FriendshipAlreadyExists.**
4. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al registrar la solicitud**

1. El **SJR** muestra la **GUI-FriendRequestPersistenceError** indicando que no se pudo registrar la solicitud de amistad y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar **GUI-FriendRequestPersistenceError.**
3. El **SJR** cierra la **GUI-FriendRequestPersistenceError**.
4. No queda una solicitud parcialmente registrada.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- Existe una nueva **Solicitud de amistad** Pendiente entre ambos jugadores.

**En caso de fallo:**

- No se crea una relación o solicitud duplicada.

CU-22 Mostrar solicitudes de amistad

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte las solicitudes de amistad pendientes que ha recibido.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.

Disparador:

El **Jugador** selecciona la opción para ver solicitudes de amistad en la **GUI-MainMenu**.

Flujo normal

1. El **SJR** consulta las solicitudes dirigidas al **Jugador**.
2. El **SJR** selecciona únicamente aquellas en estado Pendiente.
3. El **SJR** obtiene el jugador solicitante y la fecha de cada solicitud.
4. El **SJR** muestra la **GUI-FriendRequests.** (**FA-01**)
5. El **Jugador** consulta las solicitudes.
6. El **Jugador** puede seleccionar una para iniciar CU-23 Responder solicitud de amistad.

Flujos alternos

**FA-01. No existen solicitudes pendientes**

1. El **SJR** muestra la GUI-FriendRequests indicando que no existen solicitudes pendientes.
2. El **Jugador** cierra la GUI.
3. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar las solicitudes**

1. El **SJR** muestra la **GUI-FriendRequestsRetrievalError** indicando que ocurrió un error al consultar las solicitudes y con la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRequestsRetrievalError.**
3. El **SJR** cierra la **GUI-FriendRequestsRetrievalError**.
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza sus solicitudes Pendientes.

**En caso de fallo:**

- Ninguna solicitud es mostrada.

CU-23 Responder solicitud de amistad

Actor principal:

**Jugador**.

Objetivo

Permitir que el **Jugador** destinatario acepte o rechace una solicitud de amistad pendiente.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir una solicitud Pendiente dirigida al **Jugador**.

Disparador:

El **Jugador** selecciona una solicitud desde la **GUI-FriendRequests**.

Flujo normal

1. El **SJR** muestra la información de la solicitud y las opciones Aceptar y Rechazar.
2. El **Jugador** selecciona Aceptar. (**FA-01**)
3. El **SJR** verifica que la solicitud continúe en estado Pendiente. (**FA-02**)
4. El **SJR** verifica que el **Jugador** sea su destinatario. (**FA-03**)
5. El **SJR** cambia el estado de la solicitud a Aceptada. (**FE-02**)
6. El **SJR** establece la relación de amistad entre ambos jugadores.
7. El **SJR** muestra la **GUI-FriendRequestAccepted** indicando que la solicitud de amistad fue aceptada de manera exitosa y la opción para cerrar.
8. El **Jugador** selecciona la opción parar cerrar la **GUI-FriendRequestAccepted**.
9. El **SJR** cierra la **GUI-FriendRequestAccepted.**
10. El **SJR** actualiza la **GUI-FriendRequests**.

Flujos alternos

**FA-01. Rechazar solicitud**

1. El **Jugador** selecciona Rechazar.
2. El **SJR** verifica que la solicitud continúe Pendiente.
3. El **SJR** cambia su estado a Rechazada.
4. El **SJR** muestra la **GUI-FriendRequestRejected** indicando que se rechazó la solicitud correctamente y la opción para cerrar.
5. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRequestRejected**.
6. El **SJR** cierra la **GUI-FriendRequestRejected**.
7. Termina el caso de uso.

**FA-02. La solicitud ya fue resuelta**

1. El **SJR** muestra la **GUI-FriendRequestAlreadyProcessed** indicando que la solicitud seleccionada ya fue procesada anteriormente y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRequestAlreadyProcessed.**
3. El **SJR** cierra la **GUI-FriendRequestAlreadyProcessed.**
4. El **SJR** actualiza la lista de solicitudes.
5. Termina el caso de uso.

**FA-03. El jugador no es el destinatario**

1. El **SJR** rechaza la operación.
2. El **SJR** muestra la **GUI-UnauthorizedFriendRequestModification** indicando que la solicitud no está asociada al **Jugador** y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-UnauthorizedFriendRequestModification.**
4. Termina el caso de uso.

**FA-04. El jugador cancela**

1. El **Jugador** cierra la información de la solicitud.
2. No se modifica su estado.
3. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al actualizar la solicitud**

1. El **SJR** conserva su estado anterior.
2. El **SJR** muestra la **GUI-FriendRequestPersistenceError** indicando que ocurrió un error al actualizar el estado de la solicitud y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRequestPersistenceError.**
4. El **SJR** cierra la **GUI-FriendRequestPersistenceError**.
5. Termina el caso de uso.

Postcondiciones

**En caso de aceptación:**

- La solicitud queda Aceptada.
- Ambos jugadores mantienen una relación de amistad.

**En caso de rechazo:**

- La solicitud queda Rechazada.
- No se crea una relación de amistad.

**En caso de fallo:**

- La solicitud conserva su estado anterior.

CU-24 Mostrar amigos

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** consulte la lista de jugadores con los que mantiene una relación de amistad aceptada.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.

Disparador:

El **Jugador** selecciona la opción Amigos en la **GUI-MainMenu**.

Flujo normal

1. El **SJR** consulta las relaciones de amistad del **Jugador**. (**FE-01)** (**FE-02)**
2. El **SJR** selecciona únicamente las relaciones aceptadas y válidas.
3. El **SJR** obtiene el nombre de usuario, foto de perfil e información pública necesaria de cada amigo.
4. El **SJR** muestra la **GUI-Friends** con la lista de amigos. (**FA-01**)
5. El **Jugador** consulta su lista de amigos.
6. El **Jugador** puede seleccionar un amigo para consultar su perfil o iniciar CU-25 Eliminar amigo.

Flujos alternos

**FA-01. El jugador no tiene amigos**

1. El **SJR** muestra la **GUI-Friends** sin registros y un mensaje informativo indicando que no hay amigos aún y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-Friends.**
3. El **SJR** cierra la **GUI-Friends.**
4. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El SJR muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El Jugador selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El SJR cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al consultar las amistades**

1. El **SJR** muestra la **GUI-FriendsRetrievalError** indicando que ocurrió un error al consultar las amistades del **Jugador** y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FriendsRetrievalError.**
3. El **SJR** cierra la **GUI-FriendsRetrievalError**.
4. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza sus amistades válidas.

**En caso de fallo:**

- No se muestra ninguna relación.

CU-25 Eliminar amigo

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** termine una relación de amistad existente con otro **Jugador**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- Debe existir una relación de amistad aceptada.

Disparador:

El **Jugador** selecciona la opción Eliminar amigo desde la **GUI-Friends**.

Flujo normal

1. El **SJR** muestra la **GUI-RemoveFriendConfirmation** solicitando confirmación al usuario antes de eliminar la amistad.
2. El **Jugador** confirma que desea eliminar la amistad. (**FA-01**)
3. El **SJR** verifica que la relación continúe existiendo. (**FA-02**)
4. El **SJR** verifica que el **Jugador** pertenezca a la relación.
5. El **SJR** desactiva la relación de amistad. (**FE-02**)
6. El **SJR** conserva las partidas y mensajes históricos existentes.
7. El **SJR** muestra la **GUI-FriendRemoved** indicando que la amistad fue eliminada de manera exitosa y la opción para cerrar.
8. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRemoved.**
9. El **SJR** cierra la **GUI-FriendRemoved.**
10. El **SJR** actualiza la **GUI-Friends.**

Flujos alternos

**FA-01. El jugador cancela**

1. El **Jugador** selecciona cancelar.
2. El **SJR** cierra la **GUI-RemoveFriendConfirmation.**
3. La relación permanece activa.
4. Termina el caso de uso.

**FA-02. La relación ya no existe**

1. El **SJR** muestra la **GUI-FriendshipNotFound** con un mensaje informativo indicando que no se encontró la relación de amistad y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-FiendshipNotFound**.
3. El **SJR** cierra la **GUI-FriendshipNotFound**.
4. El **SJR** actualiza la lista de amigos.
5. Termina el caso de uso.

Flujos de excepción

**FE-01. Error de conexión**

1. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El **SJR** cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al eliminar la relación**

1. El **SJR** conserva la amistad.
2. El **SJR** muestra la **GUI-FriendRemovalPersistenceError** indicando que ocurrió un error al eliminar la amistad y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-FriendRemovalPersistenceError.**
4. El **SJR** cierra la **GUI-FriendRemovalPersistenceError**.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- Los jugadores dejan de mantener una relación de amistad activa.
- El historial de partidas y mensajes no es eliminado.

**En caso de fallo:**

- La relación permanece sin modificaciones.

CU-26 Enviar mensaje

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** envíe un mensaje dentro de una conversación autorizada, aplicando las reglas de moderación definidas por el **SJR**.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- El **Jugador** debe tener autorización para participar en la conversación.
- Debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Jugador** escribe un mensaje en la **GUI-Chat** y selecciona la opción para enviarlo.

Flujo normal

1. El **SJR** obtiene el contenido escrito por el **Jugador**.
2. El **SJR** verifica que el contenido no se encuentre vacío. (**FA-01**)
3. El **SJR** verifica que el **Jugador** continúe autorizado para participar en la conversación. (**FA-02**)
4. El **SJR** consulta si el **Jugador** posee un **Muteo** vigente. (**FA-03**)
5. El **SJR** analiza el contenido utilizando las reglas de moderación correspondientes a los idiomas soportados. (**FA-04**)
6. El **SJR** determina que el mensaje no contiene ninguna palabra o expresión prohibida.
7. El **SJR** crea el **Mensaje** asociándolo con el **Jugador**, la conversación y la fecha y hora correspondientes. (**FE-02**)
8. El **SJR** almacena el **Mensaje**.
9. El **SJR** muestra el mensaje en la **GUI-Chat** de los participantes autorizados.
10. El **SJR** limpia el campo utilizado para escribir el mensaje.

Flujos alternos

**FA-01. Mensaje vacío**

1. El **SJR** no crea ningún **Mensaje**.
2. El **SJR** muestra la **GUI-EmptyMessageNotAllowed** indicando que el mensaje debe tener contenido y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-EmptyMessageNotAllowed**.
4. El **SJR** cierra la **GUI-EmptyMessageNotAllowed.**
5. Termina el caso de uso.

**FA-02. Jugador no autorizado para la conversación**

1. El **SJR** rechaza el envío.
2. El **SJR** muestra la **GUI-UnauthorizedChatAccess** indicando que el **Jugador** no está autorizado para hablar en la conversación y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-UnauthorizedChatAccess.**
4. El **SJR** cierra la **GUI-UnauthorizedChatAccess.**
5. Termina el caso de uso.

**FA-03. Jugador con muteo vigente**

1. El **SJR** determina que existe un **Muteo** cuya fecha de finalización todavía no ha pasado.
2. El **SJR** no crea el **Mensaje**.
3. El **SJR** muestra la **GUI-PlayerMuted** indicando que el **Jugador** no puede enviar mensajes y la fecha u hora de finalización del muteo y la opción cerrar.
4. El **Jugador** selecciona la opción para cerrar la **GUI-PlayerMuted**.
5. El **SJR** cierra la **GUI-PlayerMuted**.
6. Termina el caso de uso.

**FA-04. Se detecta contenido prohibido**

1. El **SJR** determina la regla de moderación infringida.
2. El **SJR** impide el envío del **Mensaje**.
3. El **SJR** registra una **Infracción de chat** asociada al **Jugador**, la conversación, la regla detectada y la fecha y hora. (**FE-03**)
4. El **SJR** consulta las infracciones anteriores correspondientes.
5. El **SJR** determina la medida aplicable conforme a la política de moderación. (**FA-05**)
6. El **SJR** muestra la **GUI-MessageBlocked** indicando que el contenido no puede enviarse por contener contenido prohibido y la opción cerrar.
7. El **Jugador** selecciona la opción para cerrar la **GUI-MessageBlocked.**
8. El **SJR** cierra la **GUI-MessageBlocked**.
9. Termina el caso de uso.

**FA-05. La infracción requiere aplicar un muteo**

1. El **SJR** determina, conforme a la política de moderación, que corresponde aplicar un **Muteo**.
2. El **SJR** verifica que no exista otro **Muteo** activo.
3. El **SJR** registra el motivo, fecha de inicio y fecha de finalización del **Muteo**. (**FE-04**)
4. El **SJR** muestra la **GUI-PlayerMuted** informando la sanción y su duración y la opción para cerrar.
5. El **Jugador** selecciona la opción para cerrar la **GUI-PlayerMuted**.
6. El **SJR** cierra la **GUI-PlayerMuted**.
7. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** no considera enviado el **Mensaje**.
2. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
4. El **SJR** cierra la **GUI-ServerConnectionFailed.**
5. Termina el caso de uso.

**FE-02. Error al almacenar el mensaje**

1. El **SJR** no muestra el mensaje como enviado.
2. El **SJR** muestra la **GUI-MessagePersistenceError** indicando que ocurrió un error al almacenar el mensaje y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-MessagePersistenceError.**
4. El **SJR** cierra la **GUI-MessagePersistenceError.**
5. Termina el caso de uso.

**FE-03. Error al registrar la infracción**

1. El **SJR** no permite enviar el contenido detectado como prohibido.
2. El **SJR** muestra la **GUI-ModerationPersistenceError** indicando que ocurrió un error al registrar la infracción y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-ModerationPersistenceError.**
4. El **SJR** cierra la **GUI-ModerationPersistenceError**.
5. Termina el caso de uso.

**FE-04. Error al registrar el muteo**

1. El **SJR** conserva la infracción registrada cuando corresponda.
2. El **SJR** no informa que el muteo fue aplicado si no pudo persistirse correctamente.
3. El **SJR** muestra la **GUI-MutePersistenceError** indicando que ocurrió un error al registrar el muteo del **Jugador** y la opción cerrar**.**
4. El **Jugador** selecciona la opción para cerrar la **GUI-ModerationPersistenceError.**
5. El **SJR** cierra la **GUI-ModerationPersistenceError.**
6. Termina el caso de uso.

Postcondiciones

**En caso de envío exitoso:**

- Existe un nuevo **Mensaje** asociado al **Jugador** y a la conversación.
- El mensaje se encuentra disponible para los participantes autorizados.

**En caso de detección de contenido prohibido:**

- El **Mensaje** no es enviado.
- La infracción queda registrada cuando la operación puede completarse correctamente.
- Puede existir un nuevo **Muteo** cuando la política de moderación lo determine.

**En caso de fallo:**

- No se muestra como enviado un **Mensaje** que no haya sido almacenado correctamente.
- No se considera aplicado un **Muteo** que no haya sido persistido.

CU-27 Leer mensajes

Actor principal:

**Jugador**.

Objetivo

Permitir que un **Jugador** autorizado consulte los mensajes pertenecientes a una conversación.

Precondiciones

- El **Jugador** debe tener una sesión iniciada.
- El **Jugador** debe estar autorizado para consultar la conversación.

Disparador:

El **Jugador** abre la **GUI-Chat** correspondiente a una conversación.

Flujo normal

1. El **SJR** identifica la conversación solicitada.
2. El **SJR** verifica que el **Jugador** tenga autorización para consultarla. (**FA-01**)
3. El **SJR** consulta los **Mensajes** pertenecientes a la conversación.
4. El **SJR** obtiene el emisor, contenido, fecha y hora de cada mensaje.
5. El **SJR** ordena los mensajes cronológicamente.
6. El **SJR** muestra los mensajes en la GUI-Chat. (**FA-02**)
7. El **Jugador** consulta los mensajes.
8. Mientras la GUI-Chat permanezca abierta, el **SJR** incorpora los nuevos mensajes autorizados que sean recibidos. (**FA-03**)

Flujos alternos

**FA-01. Jugador no autorizado**

1. El **SJR** rechaza la consulta.
2. El **SJR** muestra la **GUI-UnauthorizedChatAccess** indicando que el **Jugador** no está autorizado para leer la conversación y la opción cerrar.
3. El **Jugador** selecciona la opción para cerrar la **GUI-UnauthorizedChatAccess.**
4. El **SJR** cierra la **GUI-UnauthorizedChatAccess.**
5. Termina el caso de uso.

**FA-02. Conversación sin mensajes**

1. El **SJR** muestra la **GUI-Chat** sin mensajes históricos.
2. El **Jugador** puede permanecer en la conversación o cerrarla.

**FA-03. Se recibe un nuevo mensaje mientras el chat está abierto**

1. El **SJR** recibe el nuevo **Mensaje**.
2. El **SJR** lo agrega respetando el orden cronológico.
3. El flujo continúa en el paso 7 del FN.

Flujos de excepción

**FE-01. No se puede conectar con el servidor**

1. El **SJR** muestra la **GUI-ServerConnectionFailed** indicando que no se pudo establecer comunicación con el servidor y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-ServerConnectionFailed.**
3. El **SJR** cierra la **GUI-ServerConnectionFailed.**
4. Termina el caso de uso.

**FE-02. Error al obtener los mensajes**

1. El **SJR** muestra la **GUI-MessageRetrievalError** indicando que ocurrió un error al obtener los mensajes de la conversación y la opción para cerrar.
2. El **Jugador** selecciona la opción para cerrar la **GUI-MessageRetrievalError.**
3. El **SJR** cierra la **GUI-MessageRetrievalError**.
4. El **SJR** no presenta información incompleta como si correspondiera al historial completo.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- El **Jugador** visualiza los mensajes autorizados en orden cronológico.

**En caso de fallo:**

- Ningún **Mensaje** es mostrado.

CU-28 Entrar como invitado

Actor principal:

**Invitado**.

Objetivo

Permitir que un **Invitado** acceda a las funcionalidades de juego sin crear una **Cuenta**, utilizando un nombre temporal identificado mediante un prefijo reservado y aceptando las restricciones correspondientes al modo invitado.

Precondiciones

- El **Invitado** no debe tener una sesión de **Cuenta** iniciada.
- El **SJR** debe encontrarse en la **GUI-MainMenu**.
- Para utilizar funcionalidades de juego en línea, debe existir comunicación entre el cliente y el servidor.

Disparador:

El **Invitado** selecciona la opción Jugar como invitado en la **GUI-Login**.

Flujo normal

1. El **SJR** muestra la **GUI-GuestAccess** informando las restricciones correspondientes al modo invitado:
    - Las estadísticas del **Invitado** no serán almacenadas.
    - El **Invitado** no podrá utilizar el chat.
    - El **Invitado** no podrá consultar, enviar, responder ni eliminar relaciones de amistad.
    - La identidad utilizada será temporal y no corresponde a una **Cuenta** registrada.
2. El **SJR** muestra en la **GUI-GuestAccess** un campo para ingresar el nombre que utilizará el **Invitado**.
3. El **Invitado** ingresa el nombre que desea utilizar.
4. El **Invitado** selecciona la opción para continuar aceptando las restricciones informadas. (**FA-04**)
5. El **SJR** verifica que el nombre haya sido proporcionado. (**FA-01**)
6. El **SJR** verifica que el nombre cumpla las reglas de formato establecidas. (**FA-02**)
7. El **SJR** verifica que el nombre temporal resultante no se encuentre utilizado por otra sesión de invitado activa. (**FA-03**)
8. El **SJR** antepone al nombre proporcionado el prefijo reservado `Guest-`.
9. El **SJR** crea una sesión temporal de **Invitado** utilizando el nombre resultante. (**FE-01**)
10. El **SJR** configura la sesión para impedir el acceso a las funcionalidades de estadísticas persistentes, chat y amistades.
11. El **SJR** cierra la **GUI-GuestAccess**.
12. El **SJR** muestra la **GUI-MainMenu** identificando al usuario mediante su nombre temporal y habilitando únicamente las funcionalidades disponibles para el **Invitado**.

Flujos alternos

**FA-01. Nombre de invitado no proporcionado**

1. El **SJR** muestra la **GUI-MissingRequiredInformation** indicando que debe proporcionarse un nombre y la opción para cerrar.
2. El **Invitado** selecciona la opción para cerrar la **GUI-MissingRequiredInformation**.
3. El **SJR** cierra la **GUI-MissingRequiredInformation**.
4. El flujo continúa en el paso 3 del FN.

**FA-02. Nombre de invitado con formato inválido**

1. El **SJR** muestra la **GUI-InvalidGuestUsernameFormat** indicando que el nombre proporcionado no cumple con el formato permitido y la opción para cerrar.
2. El **Invitado** selecciona la opción para cerrar la **GUI-InvalidGuestUsernameFormat**.
3. El **SJR** cierra la **GUI-InvalidGuestUsernameFormat**.
4. El flujo continúa en el paso 3 del FN.

**FA-03. Nombre temporal actualmente utilizado**

1. El **SJR** determina que el nombre resultante después de agregar el prefijo ya se encuentra asociado a otra sesión activa.
2. El **SJR** muestra la **GUI-GuestUsernameAlreadyInUse** indicando que debe seleccionar otro nombre y la opción para cerrar.
3. El **Invitado** selecciona la opción para cerrar la **GUI-GuestUsernameAlreadyInUse**.
4. El **SJR** cierra la **GUI-GuestUsernameAlreadyInUse**.
5. El flujo continúa en el paso 3 del FN.

**FA-04. El invitado no desea continuar con las restricciones**

1. El **Invitado** selecciona la opción para cancelar.
2. El **SJR** cierra la **GUI-GuestAccess**.
3. El **SJR** muestra la **GUI-MainMenu** sin iniciar una sesión de invitado.
4. Termina el caso de uso.

Flujos de excepción

**FE-01. No se puede crear la sesión temporal de invitado**

1. El **SJR** muestra la **GUI-GuestSessionCreationError** indicando que no fue posible iniciar la sesión temporal y la opción para cerrar.
2. El **Invitado** selecciona la opción para cerrar la **GUI-GuestSessionCreationError**.
3. El **SJR** cierra la **GUI-GuestSessionCreationError**.
4. No se considera iniciada ninguna sesión de invitado.
5. Termina el caso de uso.

Postcondiciones

**En caso de éxito:**

- Existe una sesión temporal de **Invitado**.
- El nombre visible del **Invitado** contiene el prefijo reservado `Guest-`.
- No se crea una **Cuenta** en la base de datos.
- No se almacenan estadísticas asociadas al **Invitado**.
- Las funcionalidades de chat y amistades permanecen deshabilitadas para el **Invitado**.
- El **Invitado** puede acceder a las funcionalidades de juego permitidas.

**En caso de fallo o cancelación:**

- No existe una sesión temporal de **Invitado** activa.
- No se crea ningún registro de **Cuenta**.

---

Modelado de la base de datos

Modelo Entidad-Relación.

![ER.drawio.svg](PRIMERA%20ENTREGA%20PROYECTO/ER.drawio.svg)

Modelo relacional.

![Relacional.png](PRIMERA%20ENTREGA%20PROYECTO/Relacional.png)

Evidencia de normalización a 3FN.

La base de datos cumple con **Primera, Segunda y Tercera Forma Normal**, por lo que se considera normalizada hasta **3FN**.

Primera Forma Normal — 1FN

Una relación cumple 1FN cuando sus atributos contienen valores atómicos y no existen grupos repetitivos.

El modelo cumple esta condición.

Por ejemplo, una partida no almacena:

```
players = "Player1, Player2"
```

sino que cada participante posee su propio registro en `MatchParticipant`.

De la misma manera, no se almacena:

```
moves = "D3,C4,E6,F5"
```

sino que cada movimiento se registra individualmente en `GameMove`.

Las amistades, mensajes, infracciones y patrones de moderación también se representan mediante registros individuales.

Por tanto, **todas las relaciones cumplen 1FN**.

---

Segunda Forma Normal — 2FN

Para cumplir 2FN, la relación debe cumplir 1FN y todos los atributos que no pertenecen a la clave deben depender de la clave primaria completa.

Las relaciones con claves primarias simples cumplen esta condición directamente.

Para las relaciones con claves compuestas:

### `MatchParticipant`

PK:

```
(matchId, seatNumber)
```

Atributos como:

```
participantTypeCode
displayNameSnapshot
pieceColorCode
resultCode
finalPieceCount
isCreator
```

describen al participante que ocupa un asiento determinado dentro de una partida concreta, por lo que dependen de la clave completa.

### `GameMove`

PK:

```
(matchId, moveNumber)
```

`rowIndex`, `columnIndex`, `seatNumber`, `moveSourceCode` y `playedAtUtc` describen específicamente al movimiento número `moveNumber` dentro de `matchId`.

### `ChatMessage`

PK:

```
(matchId, messageNumber)
```

El emisor, contenido y fecha corresponden específicamente a ese mensaje dentro de esa partida.

### `Friendship`

PK:

```
(accountIdA, accountIdB)
```

El estado, solicitante y fechas describen la relación existente entre esas dos cuentas.

No existen dependencias parciales de los atributos no clave respecto de solamente una parte de las claves compuestas.

Por tanto, el modelo cumple 2FN.

---

Tercera Forma Normal — 3FN

Para cumplir 3FN, además de cumplir 2FN, ningún atributo no clave debe depender transitivamente de otro atributo no clave.

El diseño separa la información de acuerdo con la entidad a la que realmente pertenece.

Por ejemplo, `GameMove` conserva:

```
matchId
moveNumber
seatNumber
rowIndex
columnIndex
moveSourceCode
playedAtUtc
```

pero no almacena:

```
username
emailAddress
profileImageUri
```

porque estos datos pertenecen a `Account`.

De la misma manera, `ChatViolation` conserva `ruleCode`, pero no duplica:

```
violationThreshold
muteDurationSeconds
```

porque estos atributos pertenecen a `ModerationRule`.

`ModerationPattern` almacena `localeCode`, pero no repite información del locale, ya que esta corresponde a `SupportedLocale`.

`Account` tampoco almacena:

```
wins
losses
draws
winPercentage
```

porque son datos derivados de los resultados almacenados en `GameMatch` y `MatchParticipant`.

Asimismo, los datos de configuración como locale preferido, zona horaria y fondo del tablero se encuentran en `AccountSettings`, evitando mezclar preferencias de presentación con los datos de identidad de `Account`.

El atributo `displayNameSnapshot` de `MatchParticipant` no representa el nombre actual de `Account`; representa el nombre mostrado históricamente durante una participación concreta. Por ello constituye un hecho propio de `MatchParticipant` y no una dependencia transitiva del nombre actual de la cuenta.

En consecuencia, los atributos no clave dependen de la clave, de toda la clave y de nada más que la clave, por lo que el modelo se encuentra en Tercera Forma Normal (3FN).

Evidencia de la implementación en SQL Server

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%201.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%202.png)

Usuario de la BD con SQL Authentication y permisos específicos

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%203.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%204.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%205.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%206.png)

![image.png](PRIMERA%20ENTREGA%20PROYECTO/image%207.png)

Datos de prueba

Se adjunta el script para insertarlos.