# SEGUNDA ENTREGA PROYECTO

Fecha de creación: 17 de septiembre de 2026 9:59
Clase: Tecnologías para la Construcción de Software

El proyecto consiste en el desarrollo de un **Sistema de Juego de Reversi (SJR)** con arquitectura cliente-servidor, orientado a permitir que los usuarios jueguen partidas de Reversi contra otros jugadores, invitados o un oponente controlado por el sistema. La aplicación contempla la creación y administración de cuentas, personalización de preferencias, creación y unión a partidas mediante códigos, registro de movimientos, historial y estadísticas, gestión de amistades y comunicación mediante chat con mecanismos de moderación y sanciones temporales.

El sistema también considera aspectos de **internacionalización y localización**, permitiendo manejar distintos idiomas y configuraciones regionales sin depender de textos específicos almacenados en la base de datos. La persistencia de la información se implementa en **SQL Server**, utilizando un modelo relacional normalizado, restricciones de integridad y mecanismos de seguridad basados en autenticación y permisos específicos para el acceso a la base de datos.

## **Culturas Objetivo Definidas**

Para el alcance de este avance se han formalizado y verificado dos culturas lingüísticas específicas:

| **Identificador BCP 47** | **Cultura Lingüística** | **Rol y Definición Arquitectónica** |
| --- | --- | --- |
| **en-US** | Inglés (Estados Unidos) | Cultura Base del sistema. Todos los textos, gramática y mensajes de retroalimentación se originan en este recurso (Resources.resx). |
| **es-MX** | Español (México) | Cultura Adicional provista como localización de soporte internacional (Resources.es-MX.resx). Proporciona traducciones concisas para la terminología y controles del sistema. |

## **Explicación del proceso de internacionalización**

El modelo de internacionalización en ReversiGameSystem se compone de los siguientes elementos de software:

- Catálogos de Recursos (.resx): Archivos XML gestionados donde residen las cadenas de texto.
- DynamicResource Binding en XAML: Los controles visuales como botones, etiquetas, marcas de agua y títulos emplean enlaces de recursos dinámicos ({DynamicResource ClaveRecurso}). A diferencia de StaticResource, este enlace reacciona instantáneamente cuando el diccionario de recursos de la aplicación es reemplazado en memoria.
- Selector de Idioma (languageComboBox): Control de tipo ComboBox ubicado uniformemente en la esquina superior derecha de las ventanas. Cada elemento contiene un atributo Tag="es-MX" o Tag="en-US". Al seleccionarse un elemento, la aplicación emite una consulta de confirmación mediante DialogService y actualiza CultureInfo.CurrentUICulture y el ResourceDictionary activo.
- Cobertura de Diálogos Modales: Los cuadros de texto auxiliares (TextInputDialogWindow) y advertencias (ConfirmationDialogWindow) heredan o referencian directamente los recursos de la ventana propietaria, manteniendo total consistencia lingüística.

## **Evidencias de Interfaz por Vista**

A continuación se presenta el registro visual de cada una de las pantallas y cuadros modales de la aplicación. Por cada componente se ilustra el despliegue bajo la Cultura Base (en-US) y la Cultura Adicional (es-MX), validando la ausencia de truncamientos y la correcta asignación de cadenas internacionalizadas.

**3.1. Pantalla de Inicio de Sesión**

*Propósito Funcional: Punto de entrada principal al sistema. Ofrece autenticación con usuario y contraseña, opción de recordar credenciales, acceso directo para registro de nuevas cuentas e ingreso en modalidad de invitado.*

**Cultura Adicional (es-MX)**

![Captura de pantalla 2026-09-19 164503.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_164503.png)

**Cultura Base (en-US)**

![Captura de pantalla 2026-09-19 164546.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_164546.png)

**3.1.1 Pantalla de datos faltantes**

*Propósito Funcional: Muestra un mensaje cuando no se ingresaron el correo electrónico y la contraseña*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image.png)

**Cultura Base(en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%201.png)

**3.1.2 Pantalla de correo electrónico no valido**

*Propósito Funcional: Muestra un mensaje cuando el correo no es valido*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%202.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%203.png)

**3.2 Pantalla de Verificación de inicio de sesión**

*Propósito Funcional: Solicita el código de verificación que se envío al correo electrónico para poder iniciar sesión.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%204.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%205.png)

**3.3. Pantalla de Registro de Cuenta**

*Propósito Funcional: Formulario de registro para nuevos usuarios que incluye captura de nombre, apellidos, correo electrónico, nombre de usuario y contraseña con confirmación.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%206.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%207.png)

**3.3.1 Pantalla de Datos Faltantes**

*Propósito Funcional: Muestra un mensaje de datos faltantes para completar el registro de la cuenta.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%208.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%209.png)

**3.3.2 Ventana de Correo Electrónico no Válido**

*Propósito Funcional: Muestra un mensaje especificando la estructura de un correo válido.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2010.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2011.png)

**3.3.3 Ventana de Contraseñas Diferentes**

*Propósito Funcional: Muestra un mensaje de que la contraseña escrita no es la misma que en la confirmación de la contraseña.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2012.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2013.png)

**3.3.4 Ventana de Contraseña Menor de 8 Caracteres**

*Propósito Funcional: Muestra un mensaje indicando que la contraseña debe contener al menos 8 caracteres*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2014.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2015.png)

**3.3.5 Ventana de Contraseña Sin Letra Mayúscula**

*Propósito Funcional: Muestra un mensaje indicando que la contraseña debe contener al menos una letra mayúscula*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2016.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2017.png)

**3.3.6 Ventana de Contraseña Sin Números**

*Propósito Funcional: Muestra un mensaje indicando que la contraseña debe contener al menos un número decimal*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2018.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2019.png)

**3.3.7 Ventana de Contraseña Sin Simbolo**

*Propósito Funcional: Muestra un mensaje indicando que la contraseña debe contener al menos un símbolo.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2020.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2021.png)

**3.3.8 Ventana de Contraseña Sin Letra Minúscula**

*Propósito Funcional: Muestra un mensaje indicando que la contraseña debe contener al menos una letra minúscula.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2022.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2023.png)

**3.3.9 Ventana de Verificación de Email**

*Propósito Funcional: Solicita el código de verificación que se envío al correo electrónico para poder crear la cuenta.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2024.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2025.png)

**3.4. Ventana de Reinicio de Contraseña**

*Propósito Funcional: Solicita el correo electrónico de la cuenta, para enviar el código de verificación.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2026.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2027.png)

**3.4.1 Ventana de Revisa tu Correo**

*Propósito Funcional: Muestra un mensaje indicando que se envió el código de verificación al correo electrónico.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2028.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2029.png)

**3.5. Ventana de Entrar como Invitado**

*Propósito Funcional: Diálogo emergente modal empleado para solicitar el apodo o nombre de usuario temporal para la sesión de invitado.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2030.png)

**Cultura Base (en-US)**

![Captura de pantalla 2026-09-19 164716.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_164716.png)

**3.6. Pantalla de Menú Principal**

*Propósito Funcional: Menú principal del usuario autenticado . Da acceso directo a la creación de partidas personalizadas, unirse a partidas públicas, consultar historial de partidas, consultar el perfil, opciones, consultar las instrucciones, consultar amigos, agregar amigos, ver solicitudes de amistad y cerrar sesión.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2031.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2032.png)

**3.6.1 Pantalla de Menú Principal (Invitado)**

*Propósito Funcional: Menú principal del usuario invitado Da acceso directo a la creación de partidas personalizadas, unirse a partidas públicas, consulta las instrucciones y cerrar sesión.*

**Cultura Adicional (es-MX)**

![Captura de pantalla 2026-09-19 175459.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_175459.png)

**Cultura Base (en-US)**

![Captura de pantalla 2026-09-19 175422.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_175422.png)

**3.7. Pantalla de Creación de Partida**

*Propósito Funcional: Configuración de parámetros para partidas nuevas: selección del temporizador por turno.*

**Cultura Adicional (es-MX)**

![Captura de pantalla 2026-09-19 175730.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_175730.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2033.png)

**3.7.1 Pantalla de Sala de Espera de Oponente**

*Propósito Funcional: Sala de espera sincronizada donde se muestra el código único de sala para invitar a otros jugadores y la opción para jugar contra el sistema.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2034.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2035.png)

**3.8. Pantalla de Tablero de Juego**

*Propósito Funcional: Área principal de interacción lúdica con la cuadrícula de 8x8 casillas, panel de marcadores de fichas blancas y negras, chat, temporizador en tiempo real y botón de abandono.*

**Cultura Adicional (es-MX)**

![Captura de pantalla 2026-09-19 231659.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_231659.png)

**Cultura Base (en-US)**

![Captura de pantalla 2026-09-19 231821.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_231821.png)

**3.8.1 Pantalla de Tablero de Juego (Invitado)**

*Propósito Funcional: Área principal de interacción lúdica con la cuadrícula de 8x8 casillas, panel de marcadores de fichas blancas y negras, temporizador en tiempo real y botón de abandono.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2036.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2037.png)

**3.9. Ventana de Confirmación de Abandono de Partida**

*Propósito Funcional: Diálogo crítico de advertencia que previene el abandono accidental de una partida en curso informando al jugador sobre las consecuencias en su puntuación.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2038.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2039.png)

**3.10. Pantalla de Partidas Disponibles**

*Propósito Funcional: Explorador de salas públicas activas en el servidor con información del anfitrión, modalidad de juego, temporizador configurado y acción para unirse como retador.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2040.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2041.png)

**3.10.1 Pantalla de Ingresar a Partida con Código**

*Propósito Funcional: Ventana que solicita un código de partida para unirse*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2042.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2043.png)

**3.10.2 Pantalla de Ingresar a Partida**

*Propósito Funcional: Ventana que solicita confirmación para entrar a una partida*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2044.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2045.png)

**3.11. Pantalla de Espectar Partida**

*Propósito Funcional: Ventana que muestra una partida, con la información de esta, sus jugadores, el tablero en el estado actual de la partida, y la lista de movimientos.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2046.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2047.png)

**3.11.1 Pantalla de Detalles de Movimientos**

*Propósito Funcional: Ventana que muestra detalles  de los movimientos de la partida, como número de movimiento, jugador, posición, fichas afectadas.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2048.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2049.png)

**3.12. Pantalla de Partidas Anteriores**

*Propósito Funcional: Ventana que muestra el historial de partida e información de ellas, como resultado (Victoria/Derrota), contrincante, fecha.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2050.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2051.png)

**3.12.1 Pantalla de Partida Anterior**

*Propósito Funcional: Ventana que muestra una partida, con la información de esta, sus jugadores, el tablero en el estado actual de la partida, y la lista de movimientos, junto con las opciones de ver movimiento anterior, siguiente y reproducir la partida.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2052.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2053.png)

**3.13. Pantalla de Perfil de jugador**

*Propósito Funcional: Ventana que muestra información del jugador con la sesión actual, como su nombre de usuario, correo y sus estadísticas, también se muestran las opciones para cambiar nombre de usuario, cambiar contraseña, ver estadisticas y desactivar cuenta.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2054.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2055.png)

**3.13.1 Pantalla de Perfil de Otro Jugador**

*Propósito Funcional: Ventana que muestra información de un jugador distinto de la sesión actual, como su nombre de usuario y sus estadísticas, también muestra la opción de ver estadísticas.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2056.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2057.png)

**3.13.2 Pantalla de Cambiar Nombre de Usuario**

*Propósito Funcional: Ventana que solicita el nuevo nombre de usuario.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2058.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2059.png)

**3.13.3 Pantalla de Cambio de Contraseña**

*Propósito Funcional: Ventana que solicita la contraseña actual, la nueva contraseña y la confirmación de la nueva contraseña.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2060.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2061.png)

**3.14. Pantalla de Opciones**

*Propósito Funcional: Ventana que muestra la configuración del juego actual y permite modificarla.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2062.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2063.png)

**3.15. Pantalla de Reglas del juego**

*Propósito Funcional: Ventana que muestra las reglas del juego y explica como jugar.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2064.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2065.png)

**3.16. Pantalla de Agregar Amigos**

*Propósito Funcional: Ventana que muestra la lista de jugadores, un buscador, y las opciones de enviar solicitud de amistad.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2066.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2067.png)

**3.17. Pantalla de Solicitudes de Amistad**

*Propósito Funcional: Ventana que muestra la lista de solicitudes de amistad actuales.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2068.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2069.png)

**3.17.1 Pantalla de Responder Solicitud de Amistad**

*Propósito Funcional: Ventana que muestra información de la solicitud de amistad, y las opciones para aceptar o rechazar.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2070.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2071.png)

**3.17.2 Pantalla de Solicitud de Amistad Aceptada**

*Propósito Funcional: Ventana que muestra una confirmación que la solicitud se acepto con exito.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2072.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2073.png)

**3.18. Pantalla de Eliminar Amigo**

*Propósito Funcional: Ventana que solicita una confirmación para eliminar la amistad.*

**Cultura Adicional (es-MX)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2074.png)

**Cultura Base (en-US)**

![image.png](SEGUNDA%20ENTREGA%20PROYECTO/image%2075.png)

## 4. Evidencia de Pruebas de Internacionalización

4.1 Enfoque y Criterios de Aceptación

Las pruebas visuales y funcionales se ejecutaron sobre las vistas desarrolladas para el avance del 30% del sistema ReversiGameSystem. Se validaron los siguientes criterios:

1. **Consistencia Lingüística:** Todos los controles (`Label`, `Button`, `TextBlock`, `Title`) deben obtener sus cadenas desde los archivos de recursos (`.resx`) sin textos fijos ("hardcoded").
2. **Cambio de Cultura en Tiempo de Ejecución:** La selección del selector `languageComboBox` debe alternar de forma inmediata entre la cultura base (`es-MX`) y la secundaria (`en-US`) mediante el `LocalizationManager`.
3. **Integridad del Layout (Diseño Responsivo):** Ningún contenedor debe recortar textos o desbordar componentes que fuercen barras de desplazamiento (`ScrollBar`) innecesarias al ejecutarse en resoluciones estándar, a menos que la cantidad de contenido interna lo justifique.

4.2 Casos de Prueba Ejecutados

| **ID Caso** | **Vista / Módulo** | **Propósito de la Prueba** | **Resultado Esperado** | **Estado** |
| --- | --- | --- | --- | --- |
| **CP-INT-01** | `LoginScreenPage.xaml` | Cambio dinámico ES / EN en formulario de acceso. | Todos los campos, enlaces y botones alternan idioma instantáneamente sin desbordar la tarjeta. | **Aprobado** |
| **CP-INT-02** | `GuestDialog` (Modal) | Ajuste responsivo de la ventana modal y textos informativos. | El texto descriptivo realiza salto de línea (`TextWrapping`), se elimina el scroll horizontal y se visualizan los botones de acción. | **Aprobado** |
| **CP-INT-03** | `AccountCreationPage.xaml` | Adaptabilidad de formularios con restricciones y etiquetas extensas. | Los campos conservan alineación y las instrucciones de contraseña no se truncan en ninguna cultura. | **Aprobado** |
| **CP-INT-04** | `ProfilePage.xaml` | Internacionalización de datos estáticos y estados de estadísticas. | Títulos, etiquetas de resumen y botones de acción ("Desactivar cuenta" / "Deactivate account") mantienen el margen simétrico. | **Aprobado** |
| **CP-INT-05** | `OptionsPage.xaml` | Alternancia de etiquetas de configuración y switchers. | Las descripciones de personalización se despliegan completas y alineadas con los controles interactivos. | **Aprobado** |
| **CP-INT-06** | `GameView.xaml` | Corrección de layout del tablero y visualización de turno/puntuación. | El tablero 8x8 escala uniformemente con `Viewbox`; desaparecen las barras de scroll y la información de turno y abandono queda 100% visible. | **Aprobado** |

4.3 Evidencias Visuales de Validación y Corrección

**Caso Especial: Corrección de Layout y Responsividad en Tablero (`GameView.xaml`)**

- **Problema Detectado:** En resoluciones estándar, la vista original del juego calculaba dimensiones fijas, provocando la aparición de una barra de scroll vertical que ocultaba el indicador de turno y el tiempo restante.
- **Solución Implementada:** Se refactorizó la jerarquía visual envolviendo el tablero y los paneles en un contenedor escalable (`Viewbox Stretch="Uniform"`), eliminando los scrolls indebidos.

Antes:

![Captura de pantalla 2026-09-19 092958.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_092958.png)

Corrección

![Captura de pantalla 2026-09-19 204450.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204450.png)

Caso Especial: Corrección de Diálogo Modal de Invitado (`GuestDialog`)

- **Problema Detectado:** Al invocar la ventana modal de acceso de invitado, el contenedor excedía el límite visual provocando scroll horizontal y vertical, recortando el texto explicativo y ocultando los botones "Cancelar" y "Entrar como invitado".
- **Solución Implementada:** Se configuró dimensionamiento automático (`Auto`), márgenes relativos y ajuste de texto, permitiendo desplegar el formulario completo centrado en pantalla.

Antes:

![Captura de pantalla 2026-09-19 093116.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_093116.png)

Correción

![Captura de pantalla 2026-09-19 204338.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204338.png)

**Catálogo de Vistas Internacionalizadas (Español vs. Inglés)**

1. Pantalla de Creación de Cuenta (`AccountCreationPage.xaml`)

- **Cultura Base (`es-MX`):** Se muestran títulos, placeholders de usuario, correo, contraseña y advertencias en español.
- **Cultura Adicional (`en-US`):** Los campos actualizan a "Username", "Password", "Confirm password" y "Create account" sin alterar las proporciones del panel.

Español

![Captura de pantalla 2026-09-19 204515.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204515.png)

Inglés

![Captura de pantalla 2026-09-19 204537.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204537.png)

2. Pantalla de Perfil de Usuario (`ProfilePage.xaml`)

- **Cultura Base (`es-MX`):** Los encabezados "Mi perfil" y "Estadísticas", junto con los contadores ("Victorias: No disponible", "Volver al menú") se visualizan en español.
- **Cultura Adicional (`en-US`):** El panel actualiza automáticamente a "My profile", "Statistics", "Wins: Not available" y "Back to menu" respetando la grilla.

Español

![Captura de pantalla 2026-09-19 204622.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204622.png)

Inglés

![Captura de pantalla 2026-09-19 204637.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204637.png)

3. Pantalla de Configuración y Opciones (`OptionsPage.xaml`)

- **Cultura Base (`es-MX`):** Los conmutadores ("Usar fotos de perfil en las fichas", "Usar un fondo de tablero personalizado") y el botón "Cambiar fondo del tablero" se despliegan en español.
- **Cultura Adicional (`en-US`):** Los textos cambian a "Use profile pictures on game pieces", "Use a custom board background" y "Change board background" sin desfases de línea.

Español

![Captura de pantalla 2026-09-19 204702.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204702.png)

Inglés

![Captura de pantalla 2026-09-19 204713.png](SEGUNDA%20ENTREGA%20PROYECTO/Captura_de_pantalla_2026-09-19_204713.png)

## Referencias

> Microsoft. (s. f.). *Globalization and localization overview - WPF*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-globalization-and-localization-overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-globalization-and-localization-overview?utm_source=gemini)
> 

> Microsoft. (s. f.). *How to: Use resources in localizable applications - WPF*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-use-resources-in-localizable-applications](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-use-resources-in-localizable-applications?utm_source=gemini)
> 

> Microsoft. (s. f.). *Layout - WPF*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout?utm_source=gemini)
> 

> Microsoft. (s. f.). *Localize an app - WPF*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-localize-an-application](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-localize-an-application?utm_source=gemini)
> 

> Microsoft. (s. f.). *Panels overview*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/panels-overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/panels-overview?utm_source=gemini)
> 

> Microsoft. (s. f.). *ResourceManager class*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-resources-resourcemanager](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-resources-resourcemanager?utm_source=gemini)
> 

> Microsoft. (s. f.). *Thread.CurrentCulture property*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.currentculture](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.currentculture?utm_source=gemini)
>