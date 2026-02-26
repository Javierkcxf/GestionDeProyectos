Sistema web de gestión de proyectos con Blazor Server y API REST ASP.NET Core, acceso controlado por roles y persistencia en SQL Server mediante procedimientos almacenados.

Características
Autenticación mediante JWT con almacenamiento en sessionStorage
Autorización por ruta y rol (PaginaAutenticada base)
Módulos: Configuración, Datos Maestros, Gestión Operativa, Financiero, Estratégico y RRHH 
CRUD genérico vía procedimientos almacenados (api/procedimientos/ejecutarsp)
Gestión de archivos (multipart upload)

FrontBlazor: Componentes .razor con herencia desde PaginaAutenticada
Backend API: Controlador genérico EntidadesController con endpoints CRUD y ejecución de SPs 
Base de datos: Esquema y datos de ejemplo en bdfacturas_sqlserver.sql bdfacturas_sqlserver.sql

Ejecución
Iniciar el backend API.
Iniciar el frontend Blazor.
Abrir el navegador y acceder a la URL del frontend.
Iniciar sesión con credenciales de prueba.
Uso
Tras el login, el dashboard (/inicio) muestra mosaicos por área funcional según permisos 
Cada página de gestión sigue el patrón CRUD con existeEntity y textoBotonGuardar 
Los administradores pueden gestionar usuarios, roles, rutas y permisos en la sección Configuración 

GestionDeProyectos/  
├── FrontBlazor/  
│   ├── Components/  
│   │   ├── Layout/  
│   │   │   ├── MainLayout.razor  
│   │   │   └── NavMenu.razor  
│   │   └── Pages/  
│   │       ├── Login.razor  
│   │       ├── Inicio.razor  
│   │       └── ...  
├── webapicsharp/  
│   ├── Controllers/  
│   │   └── EntidadesController.cs  
│   └── Modelos/  
│       └── bdfacturas_sqlserver.sql  
└── README.md  
