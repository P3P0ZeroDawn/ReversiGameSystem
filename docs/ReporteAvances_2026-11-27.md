# Reporte de Avances del Proyecto — ReversiGameSystem

**Fecha de corte:** 27 de noviembre de 2026  
**Próximos hitos:** Pre-revisión (viernes 4 de diciembre) · Revisión ordinario (viernes 11 de diciembre)  
**Integrantes:** Pedro Enrique Sánchez Rodríguez (`p3p0`) y César Daniel Ortega Castillejos (`Cesar-DOC`)  
**Criterio de medición:** El avance y la contribución se evalúan **exclusivamente sobre funcionalidades, módulos y responsabilidades implementadas**, sin contar commits ni líneas de código como métrica de contribución.

---

## 1. Porcentaje de Avance (Por Funcionalidades y Módulos)

| Bloque funcional / técnico | Peso | Avance | Aporte | Estado actual |
| :--- | :---: | :---: | :---: | :--- |
| **Arquitectura de soluciones y contratos WCF** | 10% | **95%** | 9.5% | Dos soluciones separadas ([ReversiGameSystem.Client.slnx](../ReversiGameSystem.Client.slnx) y [ReversiGameSystem.Server.slnx](../ReversiGameSystem.Server.slnx)), 3 niveles lógicos, sin motor compartido, contratos sin `Async`, callbacks de 2–3 métodos y `JoinMatch` unificado. |
| **Cuentas, sesiones, autenticación y correo** | 15% | **95%** | 14.3% | Registro con verificación, entrada de código mejorada, login con warmup EF6/WCF, cambio de contraseña, desactivación, plantillas HTML localizadas y auditoría sanitizada. |
| **Partidas, lobby, gameplay, rival automático, invitados y espectador** | 25% | **95%** | 23.8% | Partidas PvP, contra el sistema y como invitado; observación, historial y repeticiones con avatares; `ForfeitMatch`, cancelación de partidas interrumpidas, reconexión de 30s y abandono al vencer el plazo. |
| **Chat en partida y moderación** | 8% | **95%** | 7.6% | Mensajería en tiempo real, límite de 2 mensajes/s, palabras prohibidas en BD, muteos y bloqueo de chat para invitados. |
| **Amistades, presencia, perfiles, clasificación y preferencias** | 15% | **90%** | 13.5% | Amistades completas, perfiles y clasificación migrados a MVVM con estado confirmado, corrección de elegibilidad para estadísticas, preferencias persistentes, idioma e imágenes personalizadas. |
| **Observabilidad (logs), rendimiento y seguridad** | 10% | **65%** | 6.5% | Auditoría sanitizada y warmup de login listos; faltan compresión automática y fallback a `EventLog` en bitácoras, logs de reconexión en cliente, cifrado/protección de secretos en BD y prueba de carga. |
| **Pruebas automatizadas, operativas y SonarCloud** | 17% | **75%** | 12.8% | **1,273 pruebas MSTest** (904 servidor + 369 cliente con host WCF en memoria). Faltan SonarCloud en verde, limpieza de `Legacy`/comentarios AAA y pruebas operativas manuales. |
| **Total de Implementación** | **100%** | **~88%** | **88.0%** | Funcionalidad prácticamente cerrada; el ~12% restante es estabilización, calidad estática (SonarCloud), bitácoras y validaciones operativas. |

---

## 2. Avances de la Semana (21 – 27 de noviembre de 2026)

### Cliente e integración funcional (Pedro)
- **Gameplay y MVVM:** Refactorización de gameplay a MVVM y división de comunicación en `Lobby`, `Chat` y `Gameplay`.
- **Modos de juego e historial:** Soporte completo de **invitados** en lobby y gameplay (con chat deshabilitado), **modo espectador** autenticado, **historial y repeticiones** con fotos de perfil y cancelación automática de partidas interrumpidas al arranque.
- **Perfiles, clasificación y preferencias:** Migración de **perfil y clasificación a MVVM** con estado confirmado por el servidor, corrección de las partidas que cuentan para estadísticas ([20261127_MatchStatisticsEligibility.sql](../database/migrations/20261127_MatchStatisticsEligibility.sql)), preferencias persistentes (idioma y sesión) y normalización segura de imágenes personalizadas.
- **Cuentas, amistades y UX:** Flujos completos de registro, cambio de contraseña y desactivación; integración completa de amistades y mejoras de interfaz (panel de amigos, legibilidad del tablero y entrada del código mientras se solicita al servidor).

### Servidor, pruebas automatizadas, rendimiento y auditoría (César)
- **Refactorización del servidor:** Unificación de capas (cuentas, sesiones, correo, chat y partidas), manejo unificado de `FaultException`, plantillas HTML localizadas de correo y limpieza de documentación XML solo en fronteras WCF.
- **Migración completa a MSTest:** Eliminación del runner legado y construcción de la batería de **904 pruebas del servidor** (unitarias de todos los módulos, integración SQL/WCF/SMTP y pruebas E2E con `SystemTestHost`) más la corrección de pruebas de integración del cliente usando un host WCF en memoria ([InMemoryClientWcfHost.cs](../tests/ReversiGameSystem.Client.Tests/InMemoryClientWcfHost.cs)).
- **Rendimiento y auditoría:** Warmup de consulta EF6 y canal WCF para eliminar el retraso inicial de login; incorporación de **auditoría sanitizada** para sesiones, lobby/gameplay y ciclo de vida de cuentas.

---

## 3. Lo que han trabajado para esta entrega (Acumulado de la entrega)

- **Integración completa del gameplay del cliente y servidor:** Partidas entre jugadores, contra el sistema (selección aleatoria legal) y como invitado; modo espectador, historial y repeticiones, `ForfeitMatch` y cambio de duración de turno en lobby.
- **Resiliencia ante desconexiones:** Reconexión automática durante 30 segundos en lobby y partida (sin polling en partidas activas), detección de cortes silenciosos, recuperación de estado confirmado y abandono automático al vencer el plazo.
- **Chat y social:** Chat en partida con límite de 2 mensajes/s, palabras prohibidas en BD y muteos; sistema completo de amistades y presencia.
- **Perfiles, clasificación y preferencias:** Migración a MVVM con estado confirmado por el servidor, corrección de partidas elegibles para estadísticas, preferencias persistentes, idioma e imágenes personalizadas normalizadas.
- **Cuentas, sesiones y contratos WCF:** Contratos sin `Async`, callbacks reducidos a 2–3 métodos, `JoinMatch` unificado, flujos completos de cuenta, mejora en entrada de código de verificación, warmup de login, plantillas HTML de correo y auditoría sanitizada.
- **Verificación automatizada:** Suite MSTest consolidada con **1,273 pruebas** (904 en servidor y 369 en cliente) y revisión contra el estándar de codificación.

---

## 4. Lo que les falta

### Bloqueantes técnicos y de calidad (prioridad para pre-revisión del viernes 4)
1. **Corregir SonarCloud y generar el reporte final en verde:**
   - Actualizar [sonarqube-cloud.yml](../.github/workflows/sonarqube-cloud.yml) (aún usa `dotnet run` del runner legado en lugar de `dotnet test`) y resolver cualquier violación bloqueante (0 violaciones al estándar, 0 complejidad excesiva, 0 duplicación, 0 clases sobrantes).
2. **Limpieza y uniformidad del código y pruebas:**
   - Completar la limpieza y uniformidad de los módulos del cliente.
   - Revisar duplicación de código y constantes entre capas.
   - Retirar o migrar los **8 archivos `Legacy`** que siguen en `tests/ReversiGameSystem.Client.Tests/Legacy/` y eliminar los **393 comentarios redundantes** (`// Arrange`, `// Act`, `// Assert`) en `tests/`.
3. **Aplicar migración en la base compartida:**
   - Ejecutar [20261127_MatchStatisticsEligibility.sql](../database/migrations/20261127_MatchStatisticsEligibility.sql) en la base de datos compartida (y verificar que el esquema quede sincronizado con [20261127_GuestMatchParticipants.sql](../database/migrations/20261127_GuestMatchParticipants.sql)).
4. **Completar requisitos de bitácoras (logs):**
   - Implementar compresión automática de archivos de log al superar límites y fallback al Visor de Eventos del SO (`EventLog`) en [ClientFileLoggerProvider.cs](../src/ReversiGameSystem.Client.Presentation/Services/ClientFileLoggerProvider.cs) / [EmergencyFailureLog.cs](../src/ReversiGameSystem.Client.Communication/Services/EmergencyFailureLog.cs).
   - Confirmar registro de pérdida de conexión, intentos de recuperación, reconexión y vencimiento de los 30s en el cliente.

### Verificaciones operativas, seguridad y cierre técnico
5. **Pruebas y verificaciones manuales pendientes:**
   - **Prueba obligatoria con base de datos en cero.**
   - Prueba de concurrencia real (**8 jugadores / 4 partidas simultáneas**) y revisión de consumo de `MatchStateSnapshot`.
   - Comprobación en red con **Hamachi**, recuperación manual del espectador ante corte y escenarios de fallo (BD, red, SO, permisos, IPs, apagado de servicios).
6. **Seguridad y justificaciones técnicas:**
   - Revisar protección/cifrado de secretos en BD, permisos restringidos y validaciones de truncamiento.
   - Justificar los temporizadores (`TimeSpan` y `Timer` de cliente) frente a la configuración efectiva de WCF.
7. **Preparación para defensa individual (sin IA):**
   - Dominio cruzado de cliente y servidor por ambos integrantes y práctica de depuración en vivo (*debugger*).

---

## 5. Qué está trabajando cada quien

| Integrante | Responsabilidades principales | Foco actual |
| :--- | :--- | :--- |
| **Pedro (`p3p0`)** | Cliente WPF/MVVM, integración cliente–servidor y pruebas funcionales de cliente. | Cierre de migración de perfiles/clasificación a MVVM, estadísticas, limpieza y uniformidad de módulos del cliente, revisión de duplicación/constantes y cumplimiento del estándar. |
| **César (`Cesar-DOC`)** | Mantenimiento y migración del servidor, arquitectura de pruebas automatizadas (servidor e integración WCF/SQL/E2E), rendimiento, auditoría y preparación del reporte. | Suite MSTest (servidor + host WCF en memoria para cliente), auditoría sanitizada, warmup de login, ajuste de SonarCloud y consolidación del reporte de avances. |
| **Conjunto** | Decisiones técnicas, revisión de contratos e integración extremo a extremo. | Pruebas manuales (BD en cero, Hamachi, concurrencia), migración en BD compartida y validación final de SonarCloud. |

---

## 6. Porcentaje de Contribución al Proyecto (Por Responsabilidades y Funcionalidades)

Evaluado sobre el reparto real de módulos, funcionalidades y responsabilidades técnicas del sistema (sin basarse en conteo crudo de commits o líneas):

| Dimensión de responsabilidad | Pedro | César |
| :--- | :---: | :---: |
| **Cliente WPF (MVVM, vistas, comunicación, resiliencia y UX)** | Principal (~85%) | Apoyo (~15%) |
| **Servidor (capas, contratos WCF, persistencia, rendimiento y auditoría)** | Apoyo (~30%) | Principal (~70%) |
| **Flujos funcionales end-to-end (gameplay, social, cuentas, preferencias)** | Principal (~65%) | Apoyo (~35%) |
| **Pruebas automatizadas (MSTest unitarias, integración SQL/WCF y E2E)** | Apoyo (~30%) | Principal (~70%) |
| **Contribución global estimada al proyecto** | **~50%** | **~50%** |

> **Nota:** El proyecto refleja una división **equitativa (~50% / ~50%)** y complementaria por especialidad: Pedro sostiene el frente de cliente, integración funcional y experiencia de usuario, mientras César sostiene la arquitectura interna del servidor, rendimiento, auditoría y la infraestructura de pruebas automatizadas.
