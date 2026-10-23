// Snapshot of the working tree on 2026-10-22, including uncommitted changes.
// Describes source wiring, not a verified end-to-end deployment.
workspace "Reversi Game System" "Current implementation inferred from source code" {
    model {
        player = person "Jugador" "Utiliza el cliente de escritorio."
        mail = softwareSystem "Proveedor de correo" "Entrega los codigos de verificacion enviados por el servidor."

        reversi = softwareSystem "Reversi Game System" "Aplicacion de Reversi con servicios WCF y funciones locales en transicion." {
            client = container "Cliente WPF" "Proceso de escritorio; combina sesiones remotas con servicios locales y prototipos." "C# / WPF / .NET Framework 4.7.2" {
                ui = component "Vistas y navegacion" "Views, MainWindow y servicios de presentacion." "WPF / XAML"
                sessionClient = component "Cliente de sesiones WCF" "SessionClient y proxy generado; inicio con codigo, invitado y cierre de sesion." "WCF / SessionEndpoint"
                clientState = component "Estado de sesion y preferencias" "SessionState, AuthenticatedAccountSession y UserPreferencesState; estado del proceso cliente." "C# / memoria"
                localRuntime = component "LocalRuntime" "LocalRuntimeFactory, LocalAccountService y LocalPlayerService; biblioteca dentro del cliente." "C# / ReversiGameSystem.LocalRuntime"
                localUseCases = component "Casos de uso locales" "CreateAccountHandler, LoginHandler, GetPlayerProfileHandler y GetLeaderboardHandler; instancias locales de Server.Application." "C# / Server.Application"
                localData = component "Persistencia local" "Stores de cuentas, perfiles y clasificacion; DbContext e imagenes. Instancias locales de Server.Persistence." "Entity Framework 6 / archivos"
                localRules = component "Politicas de cuenta locales" "Politicas de Domain.Accounts utilizadas por los casos de uso locales." "C# / Domain"
                localGame = component "Juego y prototipos locales" "ReversiBoard, GameMoveService, PrototypeMatchStore, ChatStore y FriendshipPreviewState; sin transporte remoto observado." "C# / memoria / temporizadores"
            }

            server = container "Servidor WCF" "Proceso Server.Host; publica AccountAccessService y SessionService." "C# / WCF / .NET Framework 4.7.2" {
                host = component "Composicion y host" "Program crea dependencias y abre dos ServiceHost; configuracion en App.config." "Server.Host"
                accountApi = component "API de registro" "AccountAccessService implementa IAccountAccessService. Endpoint publicado; la UI actual registra mediante LocalRuntime." "WCF / Server.ServiceApi"
                sessionApi = component "API de sesiones" "SessionService implementa ISessionService; traduce resultados y fallos." "WCF / Server.ServiceApi"
                registration = component "Registro de cuentas" "CreateAccountHandler valida datos, genera hash y coordina cuenta e imagen." "Server.Application"
                sessions = component "Autenticacion y sesiones" "SessionHandler, VerifiedLoginService, SessionRegistry e InMemoryAuthenticatedSessionStore; estado propio del servidor." "Server.Application / memoria"
                verification = component "Verificacion por codigo" "VerificationCodeService, VerificationCodeIssuer y VerificationCodeProtector." "Server.Application"
                data = component "Repositorios y almacenamiento" "EntityFrameworkAccountRegistrationStore, EntityFrameworkAccountLoginStore, VerificationCodeRepository y FileSystemProfileImageStore." "Server.Persistence / EF6 / SQL / archivos"
                security = component "Hash de contrasenas" "Pbkdf2PasswordHasher implementa IPasswordHasher." "Server.Persistence / PBKDF2"
                emailSender = component "Envio de correo" "VerificationEmailSender; SMTP protegido y configuracion mediante MailSettings." "Server.Persistence / MailKit"
                rules = component "Politicas de cuenta" "UsernamePolicy, EmailPolicy, PasswordPolicy y ProfileImagePolicy; biblioteca de dominio." "C# / Domain"
            }

            database = container "Base de datos" "Esquema relacional usado por los repositorios; incluye cuentas, perfiles y codigos de verificacion." "SQL Server" {
                tags "Database"
            }
            clientImages = container "Imagenes del cliente" "Archivos bajo LocalApplicationData/ReversiGameSystem/profile-images." "Sistema de archivos" {
                tags "FileStore"
            }
            serverImages = container "Imagenes del servidor" "Archivos bajo LocalApplicationData/ReversiGameSystem.Server/profile-images." "Sistema de archivos" {
                tags "FileStore"
            }
        }

        player -> ui "Interactua con"
        ui -> sessionClient "Solicita codigo, completa login y abre o cierra sesiones"
        ui -> clientState "Consulta y actualiza"
        ui -> localRuntime "Registra cuentas y consulta perfiles y clasificacion"
        ui -> localGame "Ejecuta partidas y funciones de prototipo"
        localRuntime -> localUseCases "Construye e invoca" "Llamadas en el mismo proceso"
        localUseCases -> localRules "Valida entradas"
        localUseCases -> localData "Usa puertos implementados por stores locales"
        localData -> database "Consulta y persiste" "Entity Framework / SQL"
        localData -> clientImages "Guarda imagenes" "Acceso a archivos"
        localRuntime -> clientImages "Expone la ruta para cargar imagenes" "Acceso a archivos"
        sessionClient -> sessionApi "Invoca operaciones de sesion" "WCF / net.tcp / puerto 8091"

        host -> accountApi "Construye y publica" "ServiceHost / puerto 8090"
        host -> sessionApi "Construye y publica" "ServiceHost / puerto 8091"
        accountApi -> registration "Delega RegisterAccountAsync"
        sessionApi -> sessions "Delega operaciones de sesion"
        registration -> rules "Valida datos de cuenta"
        registration -> security "Genera hash"
        registration -> data "Registra la cuenta y guarda su imagen"
        sessions -> rules "Valida nombres de invitado"
        sessions -> security "Verifica contrasenas"
        sessions -> data "Consulta cuentas"
        sessions -> verification "Solicita y consume codigos"
        verification -> data "Persiste y consume desafios"
        verification -> emailSender "Solicita envio del codigo"
        emailSender -> mail "Entrega mensajes" "SMTP / TLS"
        data -> database "Consulta y persiste" "Entity Framework / SQL"
        data -> serverImages "Guarda imagenes" "Acceso a archivos"
    }

    views {
        systemContext reversi "Context" {
            title "C1 - Contexto actual de Reversi"
            include *
            autoLayout lr
        }

        container reversi "Containers" {
            title "C2 - Aplicaciones y almacenes actuales"
            include *
            autoLayout lr
        }

        component client "ClientComponents" {
            title "C3 - Componentes dentro del proceso WPF"
            include *
            autoLayout lr
        }

        component server "ServerComponents" {
            title "C3 - Componentes dentro del proceso servidor WCF"
            include *
            autoLayout lr
        }

        styles {
            element "Element" {
                color #ffffff
                background #438dd5
            }
            element "Person" {
                shape Person
                background #08427b
            }
            element "Software System" {
                background #1168bd
            }
            element "Component" {
                background #85bbf0
                color #102a43
            }
            element "Database" {
                shape Cylinder
            }
            element "FileStore" {
                shape Folder
            }
        }
    }
}
