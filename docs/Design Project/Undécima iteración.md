# Undécima iteración

Objetivo: Incorporar las preferencias de idioma al diseño del cliente y del servidor para soportar CU-09 Modificar idioma de interfaz.

### Paso 2: Drivers para establecer el objetivo de la iteración

- **CU-09 Modificar idioma de interfaz:** STK-01 (Jugador) confirma un idioma soportado y el sistema conserva su preferencia.
- **STK-02 (Equipo de desarrollo):** Mantener separados los recursos visuales, la comunicación y el almacenamiento de preferencias.
- **ASR-05:** Consultar y actualizar las preferencias de la cuenta mediante contratos remotos.

### Paso 3: Elementos a refinar

Se refina Perfil y configuración en presentación y se amplían los módulos de comunicación, servicios, negocio y persistencia definidos en IT6 e IT7. El alcance de esta iteración comprende el idioma de la interfaz.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

- **Preferencia de cuenta almacenada en el servidor.** Permite recuperar el idioma asociado a la cuenta. Requiere sesión válida y comunicación para guardar el cambio.
- **Preferencia almacenada únicamente en el equipo cliente.** Permite cambiar sin consultar al servidor, pero la configuración queda ligada a ese equipo.
- **Incluir la preferencia dentro del flujo de modificación del perfil.** Reduce la cantidad de fronteras, pero mezcla la configuración de uso con los datos públicos del jugador.

### Paso 5: Decisión y elementos resultantes

Se incorpora un Servicio de preferencias de jugador que delega en un Módulo de preferencias. Este define un Contrato de persistencia de preferencias, implementado por el Módulo de datos de preferencias. El Cliente de preferencias encapsula las solicitudes desde presentación.

STK-01 selecciona y confirma un idioma. Para una cuenta, el cliente solicita guardar el idioma y espera la confirmación antes de aplicarlo a la interfaz. Si se rechaza la solicitud o falla su almacenamiento, conserva el idioma visual anterior. La cancelación y la selección del idioma ya activo conservan la configuración.

Si el guardado se confirma y después falla la carga local de recursos, la interfaz conserva el idioma anterior e informa el fallo de carga. El guardado remoto ya pudo completarse. La confirmación del guardado y la aplicación visual constituyen resultados distintos.

El invitado aplica el cambio durante su sesión. La preferencia persistente se asocia a una cuenta.

**Costo aceptado.** Guardar un cambio de idioma de cuenta depende del servidor. Un fallo posterior de recursos puede dejar temporalmente el idioma guardado y el mostrado con valores distintos.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

| Elemento | Capa | Responsabilidad | Dependencias |
| --- | --- | --- | --- |
| Perfil y configuración | Presentación del cliente | Coordina la confirmación y aplicación del idioma. | Cliente de preferencias y recursos de localización. |
| Cliente de preferencias | Datos del cliente | Solicita consultar o guardar el idioma. | Contrato remoto de preferencias de jugador. |
| Servicio de preferencias de jugador | Servicios del servidor | Expone la consulta y actualización de preferencias. | Módulo de preferencias. |
| Módulo de preferencias | Negocio del servidor | Comprueba la sesión y el idioma y coordina el almacenamiento. | Sesiones y Contrato de persistencia de preferencias. |
| Módulo de datos de preferencias | Persistencia del servidor | Consulta y guarda las preferencias de la cuenta. | Contrato de persistencia de preferencias y base de datos relacional. |

### Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| CU-09 | | | X | Se define el guardado de la preferencia, su aplicación visual y los resultados de cancelación y fallo. |
| STK-02 | | | X | Cada capa conserva una responsabilidad identificada en el recorrido. |
| ASR-05 | | | X | El cliente consulta y actualiza preferencias mediante un contrato remoto. |
