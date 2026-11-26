# Guía de Estándares y Estructura de Pruebas (Servidor Reversi)

Este documento define las reglas, estructura y buenas prácticas para diseñar, implementar y auditar pruebas en el servidor de **Reversi** (.NET Framework 4.7, WCF, Entity Framework y SQL Server) utilizando **MSTest** y dobles de prueba manuales (**Stubs** y **Fakes**).

## 1. Estructura del Proyecto de Pruebas

Todas las pruebas del servidor conviven en un **único proyecto de MSTest**, separadas por tipo desde la raíz y etiquetadas mediante `[TestCategory]` para permitir su ejecución independiente en el *Test Explorer* de Visual Studio.

```
ReversiGameSystem.Server.Tests/
├── App.config                             # Cadena de conexión exclusiva a la BD de pruebas (ej. Reversi_TestDB)
├── TestDoubles/                           # Stubs y Fakes compartidos entre unitarias e integración
│   └── <InterfazOServicio>Stub.cs         # Un doble configurable por interfaz compartida
├── UnitTests/                             # [TestCategory("Unit")] - Milisegundos, sin BD ni red
│   ├── Domain/                            # Reglas puras del juego y validaciones
│   │   └── <EntidadOValidador>/
│   │       └── <MetodoPublico>Tests.cs
│   ├── Application/                       # Casos de uso y servicios de aplicación
│   │   └── <NombreDelServicio>/
│   │       ├── <NombreDelServicio>TestBase.cs   # Configuración compartida y stubs locales del servicio
│   │       ├── <MetodoPublico1>Tests.cs         # Un archivo por cada método público
│   │       └── <MetodoPublico2>Tests.cs
│   └── Services/                          # (Opcional) Mapeo de DTOs y traducción a FaultException
│       └── <NombreDelServicioWcf>/
│           └── <MetodoPublico>Tests.cs
└── IntegrationTests/                      # [TestCategory("Integration")] - Usa BD real o WCF local
    ├── Repository/                        # Pruebas de Entity Framework contra SQL Server
    │   └── <NombreDelRepositorio>Tests.cs
    ├── Services/                          # Pruebas de contratos WCF con ServiceHost local
    │   └── <NombreDelServicioWcf>Tests.cs
    └── System/                            # [TestCategory("System")] - Flujos E2E y concurrencia entre clientes
        └── <FlujoCritico>Tests.cs

```

### Regla de organización por método público

* **Una carpeta por clase/servicio** que se está probando.

* **Un archivo de prueba por cada método público** (por ejemplo, `GetTests.cs`, `MakeMoveTests.cs`, `SurrenderTests.cs`). Esto evita archivos extensos y facilita localizar exactamente qué pruebas fallan o necesitan revisión.

* Si un servicio tiene varios métodos públicos que comparten inicialización o datos base, centralizar esa configuración en una clase abstracta `<Servicio>TestBase.cs` con `[TestInitialize]`. Los stubs que **solo** utiliza ese servicio viven en su `TestBase` (o dentro de su carpeta); los stubs reutilizados por varios módulos o entre `UnitTests/` e `IntegrationTests/` se colocan en `TestDoubles/`.

## 2. Alcance: Qué Probar y Qué NO Probar

### Qué SÍ probar

* **Métodos públicos únicamente:** Toda la lógica interna debe evaluarse a través de la interfaz pública de la clase.

* **Capa `Domain` (Unitarias puras):** Reglas del juego, cálculos, validadores y casos borde.

* **Capa `Application` (Unitarias con Stubs/Fakes):** Flujos de negocio, decisiones (`if/else`), validaciones de entrada, manejo de excepciones y verificación de interacciones con repositorios.

* **Capa `Repository` (Integración):** Consultas LINQ, operaciones CRUD, relaciones (`Include`/`Join`) y restricciones de SQL Server.

* **Capa `Services / API` (Integración WCF):** Serialización de `[DataContract]` / `[DataMember]` y conversión de excepciones a `FaultException<T>` sin romper el canal.

### Qué NO probar

* **Métodos privados y *helpers* internos:** Nunca deben probarse directamente ni cambiarse a `public` solo para probarlos. Quedan cubiertos indirectamente al probar los métodos públicos que los invocan. *(Si un helper tiene lógica compleja y reutilizable, debe extraerse a `Domain` como clase propia, pero debe revisarse).*

* **Clases sin lógica:** DTOs, modelos o entidades que solo contienen propiedades `get; set;`.

* **Proyecto `Host` (`Program.cs`):** Solo inicializa dependencias y levanta el servidor; se valida al ejecutar el sistema.

* **Capa `Repository` con Stubs:** Simular Entity Framework no valida consultas reales ni restricciones de SQL.

## 3. Política de Dobles de Prueba (Stubs y Fakes)

**Regla general:** Sustituye mediante **Stubs** o **Fakes** manuales (nombrados con sufijo `Stub` o `Fake`) todo lo que salga de la memoria del proceso (base de datos, red, servicios externos) y utiliza instancias **reales** para toda la lógica interna. Para evitar explosión de clases, define **un único stub configurable por interfaz** (mediante propiedades, callbacks o flags de error).

| Componente | ¿Usar Stub/Fake? | Motivo | 
| ----- | ----- | ----- | 
| Interfaces de `Repository` | **Sí** | Aísla las pruebas unitarias de SQL Server. | 
| Callbacks / Clientes WCF | **Sí** | Permite verificar notificaciones sin abrir puertos de red. | 
| Servicios externos (correo, archivos, reloj) | **Sí** | Permite simular fallos o estados controlados. | 
| Clases de `Domain` | **No** | No tienen dependencias externas y garantizan integración real con `Application`. | 
| DTOs y modelos de datos | **No** | Son estructuras simples en memoria; instanciar directamente con `new`. | 
| Clase bajo prueba (SUT) | **No** | Simular la clase evaluada invalida la prueba. | 

## 4. Diseño de Pruebas Unitarias (Buenas Prácticas)

### A. Convención de nombres

Todo método de prueba debe ser autoexplicativo siguiendo el formato: `MetodoPublico_EscenarioEvaluado_ResultadoEsperado (revisar en el estándar si esta es la estructura)`

* *Ejemplo correcto:* `Execute_EmailYaRegistrado_LanzaDuplicateEmailException`

* *Ejemplo incorrecto:* `TestRegister1`

### B. Patrón AAA (Arrange - Act - Assert)

Cada método de prueba debe estructurarse visualmente en tres bloques claros:

1. **Arrange:** Preparar datos de entrada y configurar el comportamiento de los *stubs*.

2. **Act:** Invocar **un solo** método público bajo prueba.

3. **Assert:** Verificar el resultado esperado, la excepción lanzada o el estado/llamadas registradas en un *stub*.

### C. Un solo comportamiento por prueba (Evitar exceso de `Assert`)

* **Prohibido** encadenar múltiples escenarios distintos en un mismo `[TestMethod]` con 8 o 12 `Assert`. Si falla uno al inicio, oculta el estado del resto.

* Para pruebas donde es necesario utilizar múltiples assert, revisar con el equipo de desarrollo.

### D. Pruebas parametrizadas con `[DataRow]`

Para validar múltiples entradas inválidas o casos límite que producen el mismo resultado esperado, consolidar en un solo método usando `[DataTestMethod]` y `[DataRow]` en lugar de duplicar métodos casi idénticos.

### E. Categorización obligatoria

Toda clase de prueba debe incluir el atributo de categoría correspondiente:

* `[TestCategory("Unit")]`

* `[TestCategory("Integration")]`

* `[TestCategory("System")]`

## 5. Reglas para Pruebas de Integración

### Base de datos (`Repository` + Entity Framework)

* Apuntar siempre a una base de datos exclusiva de pruebas configurada en `App.config`.

* Envolver cada prueba en un `TransactionScope` inicializado en `[TestInitialize]` y liberado en `[TestCleanup]` **sin llamar a `.Complete()`**. Esto garantiza un *rollback* automático y mantiene la base de datos limpia e independiente entre ejecuciones.

* Ninguna prueba debe depender de datos creados por otra prueba anterior.

### Comunicación (`Services` + WCF)

* Levantar un `ServiceHost` temporal en un puerto local durante `[ClassInitialize]` y cerrarlo en `[ClassCleanup]`.

* Conectarse mediante `ChannelFactory<T>` real para validar serialización y contratos.

* Al probar errores de negocio, verificar explícitamente que se recibe `FaultException<T>` y que el estado del canal sigue siendo `CommunicationState.Opened` (no `Faulted`).

## 6. Lista de Verificación para Revisar Pruebas (Auditoría)

Antes de aceptar una prueba (propia o generada por un agente de IA), verificar:

1. **¿Prueba lógica real y no el Stub?** Si se modifica o invierte una condición (`if`) en el código de producción, la prueba **debe fallar**.

2. **¿Es independiente?** Puede ejecutarse sola o en cualquier orden y siempre da el mismo resultado.

3. **¿Respeta el aislamiento?** Ninguna prueba en `UnitTests/` toca SQL Server, archivos locales ni red.

4. **¿Es legible?** Su nombre y cuerpo permiten entender en menos de 10 segundos qué regla del negocio está protegiendo.