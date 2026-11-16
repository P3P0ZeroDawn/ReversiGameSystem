// Workspace C4 derivado de la documentacion de arquitectura de software (SAD v0.2 e IT1 a IT10).
// Representa el diseno arquitectonico objetivo hasta el nivel C3 (Componentes).

workspace "Reversi Game System" "Arquitectura C4 derivada del diseno (SAD v0.2 / IT1-IT10)" {

    !impliedRelationships false

    model {
        player = person "Jugador" "Juega partidas en linea y gestiona su cuenta y social."
        mail = softwareSystem "Proveedor externo de correo (DEP-01)" "Entrega codigos temporales e invitaciones por correo." {
            tags "External"
        }

        reversi = softwareSystem "Sistema Reversi" "Videojuego de estrategia por turnos en linea para dos jugadores." {
            client = container "Cliente de escritorio" "Interfaz grafica, validacion local y consumo remoto." "C# / WPF" {
                group "Capa de presentacion (C-01)" {
                    c16 = component "C-16 Acceso y registro" "Inicio de sesion, invitado y registro." "WPF / MVVM"
                    c17 = component "C-17 Menu principal" "Clasificacion, amigos, acceso a partidas y cierre." "WPF / MVVM"
                    c18 = component "C-18 Perfil y configuracion" "Perfil, idioma y solicitudes de amistad." "WPF / MVVM"
                    c19 = component "C-19 Amistades" "Busqueda de jugadores y solicitudes." "WPF / MVVM"
                    c20 = component "C-20 Sala de partida" "Creacion, busqueda, espera y juego contra sistema." "WPF / MVVM"
                    c21 = component "C-21 Partida en curso" "Tablero, posiciones validas, jugadas y chat." "WPF / MVVM"
                    c22 = component "C-22 Historial de partidas" "Consulta y detalle de partidas pasadas." "WPF / MVVM"
                    c23 = component "C-23 Apoyo de presentacion" "Dialogos, idioma, sesion local y clientes." "WPF / MVVM"
                }

                group "Capa de negocio del cliente" {
                    c02 = component "C-02 Validador de interfaz" "Prevalida jugadas locales y politicas de entrada." "C#"
                }

                group "Capa de datos (C-03)" {
                    c24 = component "C-24 Cliente de sesiones" "Proxy de sesiones e invitado." "WCF Proxy"
                    c25 = component "C-25 Cliente de cuentas" "Proxy de registro y cuenta." "WCF Proxy"
                    c26 = component "C-26 Cliente de perfil" "Proxy de perfil e imagen." "WCF Proxy"
                    c27 = component "C-27 Cliente de clasificacion" "Proxy de tabla de posiciones." "WCF Proxy"
                    c28 = component "C-28 Cliente de amistades" "Proxy duplex de amigos y presencia." "WCF Duplex"
                    c29 = component "C-29 Cliente de partidas" "Proxy duplex de sala, partida y chat." "WCF Duplex"
                }
            }

            server = container "Aplicacion de servicio" "Servicios remotos, reglas autoritativas, estado activo y persistencia." "C# / WCF" {
                group "Capa de arranque" {
                    c15 = component "C-15 Raiz de composicion" "Construye e inyecta dependencias en el arranque." "C#"
                }

                group "Capa de servicios (C-04 y C-11)" {
                    c30 = component "C-30 Servicio de cuentas" "Expone registro de cuentas." "WCF"
                    c31 = component "C-31 Servicio de sesiones" "Expone sesiones e invitado." "WCF"
                    c32 = component "C-32 Servicio de perfil" "Expone perfil e imagen." "WCF"
                    c33 = component "C-33 Servicio de clasificacion" "Expone tabla de posiciones." "WCF"
                    c34 = component "C-34 Servicio de amistades" "Expone amistades y presencia." "WCF Duplex"
                    c35 = component "C-35 Servicio de sala" "Expone creacion, union e inicio de sala." "WCF Duplex"
                    c36 = component "C-36 Servicio de partida" "Expone jugadas, estado completo e historial." "WCF Duplex"
                    c37 = component "C-37 Servicio de chat" "Expone chat e historial de mensajes." "WCF Duplex"
                    c11 = component "C-11 Despachador de correo" "Envia correos con limite de 5 s." "SMTP"
                }

                group "Capa de negocio" {
                    c05 = component "C-05 Modulo de juego" "Coordina salas, turnos, version y bot." "C#"
                    c06 = component "C-06 Motor de juego" "Valida reglas, flanqueos y victoria." "C#"
                    c07 = component "C-07 Estado de juego" "Estado activo, version y reconexion en memoria." "Memoria"
                    c08 = component "C-08 Modulo de jugadores" "Cuentas, autenticacion, perfiles y sesiones." "C#"
                    c09 = component "C-09 Modulo de amistades" "Contactos y solicitudes de amistad." "C#"
                    c10 = component "C-10 Modulo de chat" "Moderacion y envio de mensajes." "C#"
                }

                group "Capa de persistencia (C-12)" {
                    c38 = component "C-38 PartidaDatos" "Persistencia de partidas." "Entity Framework"
                    c39 = component "C-39 ChatDatos" "Persistencia de chat." "Entity Framework"
                    c40 = component "C-40 AmistadesDatos" "Persistencia de amistades." "Entity Framework"
                    c41 = component "C-41 JugadoresDatos" "Persistencia de jugadores." "Entity Framework"
                }

                group "Capa transversal" {
                    c13 = component "C-13 Registro de eventos" "Registra eventos y fallos en disco." "Logging"
                }
            }

            database = container "C-14 Base de datos relacional" "Almacena cuentas, perfiles, amistades, partidas y chat." "SQL Server" {
                tags "Database"
            }
        }

        // --- Nivel C1: Contexto (V-01) ---
        player -> reversi "Juega partidas, gestiona cuenta y chat"
        reversi -> mail "Envia codigos e invitaciones" "SMTP (max 5 s)"

        // --- Nivel C2: Contenedores ---
        player -> client "Usa" "WPF"
        client -> server "Invoca servicios y recibe estado" "WCF / Net.TCP"
        server -> database "Lee y persiste datos" "EF / SQL"
        server -> mail "Envia correos" "SMTP (max 5 s)"

        // --- Nivel C3: Cliente ---
        player -> c16 "Usa"
        player -> c17 "Usa"
        player -> c18 "Usa"
        player -> c19 "Usa"
        player -> c20 "Usa"
        player -> c21 "Usa"
        player -> c22 "Usa"

        c16 -> c24 "Sesiones"
        c16 -> c25 "Cuentas"
        c16 -> c02 "Politicas"
        c16 -> c23 "Apoyo"

        c17 -> c27 "Clasificacion"
        c17 -> c28 "Amigos"
        c17 -> c24 "Cierre"
        c17 -> c23 "Apoyo"

        c18 -> c26 "Perfil"
        c18 -> c25 "Cuenta"
        c18 -> c23 "Apoyo"

        c19 -> c28 "Amistades"
        c19 -> c23 "Apoyo"

        c20 -> c29 "Sala e inicio"
        c20 -> c02 "Valida entrada"
        c20 -> c23 "Apoyo"

        c21 -> c29 "Jugadas y chat"
        c21 -> c02 "Posiciones validas"
        c21 -> c23 "Apoyo"

        c22 -> c29 "Historial"

        c23 -> c24 "Usa"
        c23 -> c25 "Usa"
        c23 -> c26 "Usa"
        c23 -> c27 "Usa"
        c23 -> c28 "Usa"
        c23 -> c29 "Usa"

        // --- Frontera remota WCF ---
        c24 -> c31 "Sesiones" "WCF"
        c25 -> c30 "Cuentas" "WCF"
        c26 -> c32 "Perfil" "WCF"
        c27 -> c33 "Clasificacion" "WCF"
        c28 -> c34 "Amistades" "WCF Duplex"
        c29 -> c35 "Sala" "WCF Duplex"
        c29 -> c36 "Partida" "WCF Duplex"
        c29 -> c37 "Chat" "WCF Duplex"

        // --- Nivel C3: Servidor ---
        c30 -> c08 "Delega"
        c31 -> c08 "Delega"
        c32 -> c08 "Delega"
        c33 -> c08 "Delega"
        c34 -> c09 "Delega"
        c35 -> c05 "Delega"
        c36 -> c05 "Delega"
        c37 -> c10 "Delega"

        c30 -> c13 "Registra"
        c31 -> c13 "Registra"
        c32 -> c13 "Registra"
        c33 -> c13 "Registra"
        c34 -> c13 "Registra"
        c35 -> c13 "Registra"
        c36 -> c13 "Registra"
        c37 -> c13 "Registra"

        c05 -> c06 "Valida reglas"
        c05 -> c07 "Actualiza estado"
        c09 -> c08 "Consulta"
        c10 -> c05 "Verifica sala"

        // Salidas por contrato
        c05 -> c38 "Contrato partidas" "DIP"
        c05 -> c11 "Contrato correo" "DIP"
        c08 -> c41 "Contrato jugadores" "DIP"
        c08 -> c11 "Contrato correo" "DIP"
        c09 -> c40 "Contrato amistades" "DIP"
        c10 -> c39 "Contrato chat" "DIP"

        // Persistencia e integracion
        c38 -> database "SQL"
        c39 -> database "SQL"
        c40 -> database "SQL"
        c41 -> database "SQL"
        c11 -> mail "SMTP (max 5 s)"

        // Composicion
        c15 -> c30 "Inyecta" "" "Composition"
        c15 -> c31 "Inyecta" "" "Composition"
        c15 -> c32 "Inyecta" "" "Composition"
        c15 -> c33 "Inyecta" "" "Composition"
        c15 -> c34 "Inyecta" "" "Composition"
        c15 -> c35 "Inyecta" "" "Composition"
        c15 -> c36 "Inyecta" "" "Composition"
        c15 -> c37 "Inyecta" "" "Composition"
        c15 -> c05 "Inyecta" "" "Composition"
        c15 -> c08 "Inyecta" "" "Composition"
        c15 -> c09 "Inyecta" "" "Composition"
        c15 -> c10 "Inyecta" "" "Composition"
        c15 -> c11 "Construye" "" "Composition"
        c15 -> c38 "Construye" "" "Composition"
        c15 -> c39 "Construye" "" "Composition"
        c15 -> c40 "Construye" "" "Composition"
        c15 -> c41 "Construye" "" "Composition"
        c15 -> c13 "Inyecta" "" "Composition"
    }

    views {
        systemContext reversi "Context" {
            title "C1 - Contexto del Sistema Reversi (V-01)"
            include *
            autoLayout lr
        }

        container reversi "Containers" {
            title "C2 - Contenedores del Sistema Reversi"
            include *
            autoLayout lr
        }

        component client "ClientComponents" {
            title "C3 - Modulos del Cliente de escritorio"
            include *
            autoLayout lr
        }

        component server "ServerComponents" {
            title "C3 - Modulos de la Aplicacion de servicio"
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
            element "External" {
                background #999999
                color #ffffff
            }
            element "Component" {
                background #85bbf0
                color #102a43
            }
            element "Database" {
                shape Cylinder
            }
            relationship "Composition" {
                dashed true
                color #94a3b8
                thickness 1
            }
        }
    }
}
