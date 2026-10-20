# Internacionalización de Aplicaciones WPF con Acceso a Base de Datos

Fecha de creación: 27 de septiembre de 2026 12:17
Clase: tecnologias

## Descripción breve del juego

Reversi Game System es una aplicación de escritorio desarrollada en C# y WPF que permite jugar Reversi, un juego de estrategia para dos participantes cuyo objetivo es finalizar la partida con la mayor cantidad de fichas propias en el tablero. Durante cada turno, el jugador coloca una ficha para encerrar y convertir una o más fichas del oponente.
El sistema incluye creación de cuentas, inicio de sesión, perfiles con fotografía, estadísticas de partidas y una clasificación de jugadores. También contempla partidas como usuario registrado o invitado, configuración de partidas, historial de movimientos, solicitudes de amistad, chat e internacionalización en español e inglés. Actualmente utiliza Entity Framework para acceder a SQL Server y una ejecución local desacoplada que permitirá incorporar posteriormente un servidor sin modificar los casos de uso principales.

## Descripción de la base de datos utilizada

La aplicación utiliza una base de datos relacional en Microsoft SQL Server, diseñada para almacenar la información persistente del sistema. Incluye datos de cuentas de usuario, configuraciones personales, partidas, participantes, movimientos, amistades, mensajes de chat, sanciones y códigos de verificación. Las relaciones entre estas tablas permiten conservar la integridad de la información mediante llaves primarias, llaves foráneas y restricciones de unicidad.

El acceso a los datos se realiza mediante Entity Framework 6 con un enfoque Database First. El modelo EDMX representa la estructura existente de la base de datos y genera las entidades utilizadas por la aplicación. Las consultas, inserciones y actualizaciones se expresan mediante LINQ y Entity Framework las traduce a instrucciones SQL. La cadena de conexión se configura localmente mediante una variable de entorno o un archivo `.env`, evitando almacenar información específica o sensible de cada equipo dentro del repositorio compartido.

## Explicación de la integración con Entity Framework

La integración con Entity Framework 6 se realizó mediante el enfoque Database First, utilizando la estructura existente de la base de datos en SQL Server. A partir de esta estructura se creó un modelo EDMX que representa las tablas, columnas, llaves y relaciones, y genera automáticamente las entidades de C# y el contexto `ReversiGameSystemEntities`.

El contexto funciona como intermediario entre la aplicación y la base de datos. Sus propiedades `DbSet` representan las tablas y permiten realizar consultas mediante LINQ. Entity Framework traduce estas expresiones a instrucciones SQL, ejecuta las operaciones y convierte los resultados en objetos de C#. Para insertar información se agregan entidades al contexto y se utiliza `SaveChangesAsync`, mientras que las consultas se ejecutan con métodos como `AnyAsync`, `FirstOrDefaultAsync` y `ToListAsync`.

El acceso a Entity Framework está encapsulado en componentes de persistencia que implementan las interfaces definidas por la capa de aplicación. De esta manera, los casos de uso no dependen directamente de Entity Framework ni de SQL Server. Actualmente esta integración permite crear cuentas, validar credenciales, consultar perfiles, calcular estadísticas y mostrar la clasificación de jugadores.

La conexión se construye a partir de la variable `REVERSI_DB_CONNECTION_STRING`, obtenida desde el entorno del sistema o desde un archivo local `.env`. Esto evita incluir datos particulares de cada computadora en el código fuente y permite que cada integrante configure su propia instancia de SQL Server sin modificar el proyecto compartido.

## Evidencia de las actividades de encuadre en Eminus.

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image.png)

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%201.png)

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%202.png)

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%203.png)

## Capturas de la GUI de inicio de sesión y evidencia de cambio de cultura

**Pantalla de Inicio de Sesión**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/Captura_de_pantalla_2026-09-19_164503.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/Captura_de_pantalla_2026-09-19_164546.png)

Inglés

Pantalla de inicio de sesión con datos llenos

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%204.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%205.png)

Inglés

**Pantalla de datos faltantes**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%206.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%207.png)

Inglés

**Pantalla de correo electrónico no valido**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%208.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%209.png)

Inglés

Pantalla de usuario o contraseña incorrectos

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2010.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2011.png)

Inglés

Pantallas al ingresar correo y contraseña correcta.

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/Captura_de_pantalla_2026-09-27_123548.png)

Español

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2012.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2013.png)

Inglés

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2014.png)

Inglés

## Capturas de la GUI de creación de usuarios y evidencia de cambio de cultura

**Pantalla de Registro de Cuenta**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2015.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2016.png)

Inglés

**Pantalla de Datos Faltantes**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2017.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2018.png)

Inglés

**Ventana de Correo Electrónico no Válido**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2019.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2020.png)

Inglés

**Ventana de Contraseñas Diferentes**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2021.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2022.png)

Inglés

**Ventana de Contraseña Menor de 8 Caracteres**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2023.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2024.png)

Inglés

**Ventana de Contraseña Sin Letra Mayúscula**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2025.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2026.png)

Inglés

**Ventana de Contraseña Sin Números**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2027.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2028.png)

Inglés

**Ventana de Contraseña Sin Símbolo**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2029.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2030.png)

Inglés

**Ventana de Contraseña Sin Letra Minúscula**

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2031.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2032.png)

Inglés

Ventana de nombre de usuario en uso

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2033.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2034.png)

Inglés

Ventana de correo electrónico en uso

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2035.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2036.png)

Inglés

Ventana de registro de usuario con exito

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2037.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2038.png)

Inglés

## Capturas de la GUI de consulta de puntuaciones y evidencia de cambio de cultura

Pantalla de Menú principal con la tabla de Top ganadores

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2039.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2040.png)

Inglés

Pantalla de perfil del usuario con sus estadísticas

![Español](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2041.png)

Español

![Inglés](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2042.png)

Inglés

## Evidencia de datos consultados desde la base de datos.

**Inicio de sesión**

En la tabla Account se observa el registro del usuario con sus credenciales almacenadas de forma segura (identificador, nombre de usuario, correo electrónico, hash de la contraseña y estado de cuenta activo/inactivo).

![Estado anterior de la base de datos](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2043.png)

Estado anterior de la base de datos

![Nuevo estado de la tabla en la base de datos evidenciando la creación de la cuenta nueva](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2044.png)

Nuevo estado de la tabla en la base de datos evidenciando la creación de la cuenta nueva

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2045.png)

En la pantalla de inicio de sesión de la aplicación, el usuario introduce su identificador (correo o nombre de usuario) y su contraseña. Al pulsar **"Iniciar sesión"**, la capa de persistencia (mediante Entity Framework) realiza la consulta asíncrona hacia la base de datos:

```cpp
Account? account = await context.Account.AsNoTracking().FirstOrDefaultAsync(
candidate => candidate.username == login || candidate.emailAddress == login);
```

Esta instrucción busca una coincidencia exacta contra el campo username o emailAddress.

Una vez recuperada la entidad con sus campos clave (passwordHash, statusCode, accountId), la lógica del servidor valida la contraseña contra el hash almacenado y confirma que la cuenta se encuentre activa.

Al resultar válida la autenticación, el sistema concede el acceso desplegando el **Menú principal**.

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2046.png)

**Consulta de estadísticas de partidas**

Como se observa en la tabla, el usuario p3p0 (accountId: 7) tiene registradas 5 participaciones en partidas finalizadas (matchId: 8, 9, 10, 11 y 12):   
• Victorias (WIN): 3 partidas (Matches 8, 9 y 10).   
• Derrotas (LOSS): 1 partida (Match 11).   
• Empates (DRAW): 1 partida (Match 12).  

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2047.png)

Al solicitar la información del perfil del jugador, el método `GetAsync(accountId)` ejecuta la consulta agrupada sobre la entidad `MatchParticipant` para contabilizar los resultados válidos del usuario: 

```cpp
context.MatchParticipant
    .AsNoTracking()
    .Where(participant => participant.accountId == accountId
        && participant.GameMatch1.statusCode == FinishedMatchStatusCode
        && participant.GameMatch1.countsTowardStatistics
        && participant.resultCode != null)
    .GroupBy(participant => participant.resultCode)
    .Select(group => new ResultCount
    {
        ResultCode = group.Key,
        Count = group.Count()
    })
    .ToListAsync();
```

Esta instrucción filtra únicamente las partidas finalizadas para estadística y agrupa por el código de resultado (`WIN`, `LOSS`, `DRAW`). 

En la pantalla **Mi perfil / Estadísticas**, la aplicación recibe los conteos agregados y muestra las estadísticas.

![image.png](Internacionalizaci%C3%B3n%20de%20Aplicaciones%20WPF%20con%20Acces/image%2048.png)

## Declaración de Uso de IA

Durante la realización de esta actividad se usó la inteligencia artificial ChatGPT de OpenAI para guiar el desarrollo, escribir código, validar opciones disponibles y explicar de manera clara el funcionamiento del sistema. Todo el código implementado fue diseñado, revisado y validado por ambos miembros del equipo de desarrollo en cada momento de la implementación. El diseño arquitectónico del sistema es autoría del equipo.

## Referencias consultadas

Microsoft. (s. f.). *Create a basic data application with WPF and Entity Framework 6*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/data-tools/create-a-simple-data-application-with-wpf-and-entity-framework-6](https://learn.microsoft.com/en-us/visualstudio/data-tools/create-a-simple-data-application-with-wpf-and-entity-framework-6)

Microsoft. (s. f.). *Databinding with WPF - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/fundamentals/databinding/wpf](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/databinding/wpf)

Microsoft. (s. f.). *Entity Framework documentation hub*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/](https://learn.microsoft.com/en-us/ef/)

Microsoft. (2020, 14 de octubre). *Database First - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/workflows/database-first](https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/workflows/database-first)

Microsoft. (2020, 14 de octubre). *No-tracking queries - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking](https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking)

Microsoft. (2022, 9 de marzo). *Async query and save - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/fundamentals/async](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/async)

Microsoft. (s. f.). *Connection strings and models - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/fundamentals/configuring/connection-strings](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/configuring/connection-strings)

Microsoft. (2025, 1 de diciembre). *Standard query operators overview*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/linq/standard-query-operators/](https://learn.microsoft.com/en-us/dotnet/csharp/linq/standard-query-operators/)

Microsoft. (2026, 20 de julio). *Primary and foreign key constraints*. Microsoft Learn. [https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints](https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints)

Microsoft. (s. f.). *Rfc2898DeriveBytes class*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes)

Microsoft. (2020, 14 de octubre). *Relationships - EF Designer - EF6*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/relationships](https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/relationships)