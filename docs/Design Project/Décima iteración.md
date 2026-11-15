# Décima iteración

Objetivo: Evaluar el costo de enviar el estado completo después de cada jugada y decidir cómo mantener actualizado el tablero de ambos jugadores.

### Paso 2: Drivers para establecer el objetivo de la iteración

- **QAS-Rendimiento-1 (ASR-03):** Cuando STK-01 (Jugador en turno) realiza una jugada válida, el estado actualizado queda disponible para ambos jugadores en menos de 1 segundo en el 95 % de las jugadas, con hasta 8 jugadores en 4 partidas.
- **QAS-Rendimiento-2:** Cuando se agota el turno de STK-01 (Jugador cuyo turno está activo), se realiza exactamente 1 jugada automática válida y, al tercer turno consecutivo agotado, la partida termina con derrota del jugador inactivo.
- **ASR-05:** La aplicación conserva la comunicación remota entre el cliente WPF y el servidor WCF.

### Paso 3: Elementos a refinar

Se refina el intercambio entre el Servicio de partida en curso y el Cliente de partidas definidos en IT6 e IT7. El Módulo de juego conserva la autoridad sobre la partida y usa el control de versión de IT8. El cliente entrega el estado confirmado al submódulo Partida en curso para actualizar el tablero y sus opciones de interacción según IT9.

### Paso 4: Conceptos de diseño candidatos, alternativas y costos

**Estado completo por jugada.** A partir de la separación cliente-servidor de IT5 y los contratos de IT6 e IT7, el Servicio de partida en curso devuelve al solicitante el estado confirmado y lo notifica a los jugadores suscritos mediante un canal dúplex WCF. Con ambos jugadores suscritos se presupuestan 3 envíos por jugada: una respuesta y dos notificaciones. El Cliente de partidas usa la versión definida en IT8 para reconocer copias repetidas y entregar a presentación el estado más reciente, que IT9 usa para actualizar el tablero.

**Cambios por jugada.** Como alternativa, el servicio puede enviar la versión base, la posición colocada y las posiciones que cambian, además de la versión resultante y los datos comunes de partida. Para aplicar el cambio, el cliente necesita tener la versión base. Si falta, debe consultar un estado completo.

#### Base de los tamaños estimados

- El tablero de 8 × 8 tiene 64 celdas. Cada celda tiene tres valores posibles, por lo que bastan 2 bits por celda en una representación compactada (16 bytes total). Para una estimación sencilla se presupuesta 1 byte por celda (64 bytes), sin exigir esa compactación. Una posición entre 0 y 63 también cabe en un byte. [Tipos enteros de C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types)
- Para los cambios se presupuesta un entero de 64 bits para la versión base (8 bytes), un byte para la posición y otro para el color. Se añade un byte por ficha volteada. La versión resultante ya se incluye en los datos comunes. Con 8 fichas volteadas, el costo específico del cambio es de 18 bytes. Para estresar la comparación se usan 62 posiciones volteadas como cota de espacio: de las 64 celdas se excluyen la ficha colocada y al menos una ficha propia que cierre el flanqueo. Esa cota no supone que exista una jugada de Reversi que voltee 62 fichas. El cambio presupuestado en ese caso ocupa 72 bytes. [Tipos enteros de C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types)
- WCF con Net.TCP usa transporte TCP y codificación binaria de mensajes. Los tamaños anteriores describen los datos de juego en el modelo de cálculo. El mensaje en el enlace también incluye los datos comunes de partida, la envoltura, el encuadre y las cabeceras de transporte. Una cabecera TCP y una IPv4 sin opciones suman 40 bytes por segmento. [NetTcpBinding](https://learn.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/wcf/nettcpbinding), [RFC 6691](https://www.rfc-editor.org/rfc/rfc6691.html)

Para el caso favorable se reservan **448 bytes comunes** por envío, incluidos los metadatos y la sobrecarga. Para el extremo se reservan **4 032 bytes comunes**, de modo que el estado completo se presupuesta en **4 KiB** por envío. Ambas reservas son supuestos de sensibilidad y no tamaños publicados de WCF. El tamaño efectivo depende de la codificación, las cabeceras y la segmentación. La comparación mantiene la misma reserva común para ambas opciones en cada caso.

#### Carga base y proyección de crecimiento

Los escenarios suponen dos jugadores suscritos por partida y 3 envíos por jugada. El caso favorable usa 1 jugada válida por segundo por partida y 1 Mbit/s disponible para este tráfico. El caso extremo usa 2 jugadas válidas por segundo por partida. Se repite esa carga con las 4 partidas del objetivo y se proyecta a 16 partidas, con 10 Mbit/s disponibles como condición de crecimiento. La ráfaga representa una jugada simultánea por partida.

La salida sostenida es **bytes por envío × 8 × 3 × partidas × jugadas por segundo por partida**. El tiempo de transmisión de la ráfaga es **bytes por envío × 8 × 3 × partidas ÷ bits por segundo disponibles**.

| Escenario | Envío | Bytes por envío | Salida sostenida | Transmisión de una ráfaga |
| --- | --- | ---: | ---: | ---: |
| Favorable, 8 jugadores en 4 partidas a 1 Mbit/s | Estado completo | 512 | 49,152 kbit/s | 49,2 ms |
| Favorable, 8 jugadores en 4 partidas a 1 Mbit/s | Cambios | 466 | 44,736 kbit/s | 44,7 ms |
| Extremo, 8 jugadores en 4 partidas a 1 Mbit/s | Estado completo | 4 096 | 786,432 kbit/s | 393,2 ms |
| Extremo, 8 jugadores en 4 partidas a 1 Mbit/s | Cambios | 4 104 | 787,968 kbit/s | 394,0 ms |
| Crecimiento, 32 jugadores en 16 partidas a 10 Mbit/s | Estado completo | 4 096 | 3,146 Mbit/s | 157,3 ms |
| Crecimiento, 32 jugadores en 16 partidas a 10 Mbit/s | Cambios | 4 104 | 3,152 Mbit/s | 157,6 ms |

Con estado completo, la ráfaga extrema de 4 partidas deja unos 607 ms del presupuesto de un segundo para la entrada, procesamiento, colas, latencia de red y entrega en el cliente. En la proyección de 16 partidas, 1 Mbit/s sería insuficiente: la demanda sostenida supera el enlace y una sola ráfaga tardaría 1,57 s. Con 10 Mbit/s, la transmisión ocupa cerca de 157 ms y deja unos 843 ms para el resto del recorrido. Estos márgenes sustentan la factibilidad técnica bajo los supuestos de red. El percentil 95 se verifica con una prueba de extremo a extremo.

### Paso 5: Decisión y elementos resultantes

Se conserva el envío del estado completo. En el caso favorable, el cambio ahorra cerca de 4,4 ms por ráfaga. En el extremo de espacio presupuestado, el cambio puede ocupar tanto como el estado completo. El estado completo permite al Cliente de partidas actualizar el tablero aunque no tenga la versión anterior.

**Costo aceptado.** Se vuelven a transmitir las 64 celdas tras cada jugada y, si el solicitante tiene abierto el canal de notificaciones, su estado puede llegar por ambas vías. El Cliente de partidas descarta la copia repetida por versión. Si las mediciones muestran que la salida del servidor limita el tiempo de respuesta, se puede evaluar primero si conviene suprimir el envío duplicado al solicitante.

El Módulo de juego también controla los vencimientos de turno. Comprueba bajo acceso exclusivo que el turno siga vigente y haya expirado antes de resolverlo. En los dos primeros vencimientos consecutivos, aplica una sola jugada automática válida cuando hay una posición legal. Al tercer vencimiento declara la derrota por inactividad y termina la partida en esa misma transición. El servicio publica el estado confirmado.

### Paso 6: Bocetar vistas y registrar decisiones de diseño

![Propagación del estado confirmado después de una jugada](Décima%20iteración/Propagación%20del%20estado%20confirmado%20después%20de%20una%20jugada.png)

| Elemento | Capa | Responsabilidad en esta iteración | Dependencias |
| --- | --- | --- | --- |
| Servicio de partida en curso | Servicios | Devuelve el estado completo al solicitante y distribuye el estado confirmado a los suscriptores por el canal dúplex. | Módulo de juego y suscripciones de la partida. |
| Módulo de juego | Negocio | Resuelve la jugada o el vencimiento del turno y produce el estado confirmado con su versión. | Módulo de estado de juego y Módulo motor de juego. |
| Cliente de partidas | Datos | Recibe el estado por respuesta o notificación, conserva la versión más reciente y entrega el estado a presentación. | Contrato remoto de partida en curso. |
| Partida en curso | Presentación | Actualiza el tablero y las posiciones habilitadas a partir del nuevo estado confirmado. | Cliente de partidas y Módulo validador de interfaz. |

Para verificar ASR-03, registrar desde el envío de cada jugada válida hasta que el estado de esa versión esté disponible para ambos jugadores. Para cada jugada se toma el tiempo del último de los dos clientes en disponer del estado. La prueba se plantea con 8 jugadores en 4 partidas y debe registrar el percentil 95, el tamaño observado de las respuestas y notificaciones, y las condiciones del enlace.

### Paso 7: Análisis del diseño actual

| Driver | No abordado | Parcialmente abordado | Completamente abordado | Decisiones de diseño tomadas y justificación |
| --- | --- | --- | --- | --- |
| **QAS-Rendimiento-1 (ASR-03)** | | **X** | | El estado completo es viable bajo los supuestos de tráfico. El criterio de menos de 1 segundo en el 95 % de las jugadas se debe comprobar de extremo a extremo. |
| **QAS-Rendimiento-2** | | | **X** | El servidor resuelve cada vencimiento bajo acceso exclusivo. Aplica una sola jugada automática cuando corresponde y concluye por inactividad al tercer turno consecutivo agotado. |
| **ASR-05** | | | **X** | La respuesta y las notificaciones conservan el contrato remoto de partida en curso. |
