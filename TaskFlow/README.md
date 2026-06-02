# Documentación Backend - TaskFlow

## Descripción General

El backend de **TaskFlow** está desarrollado con:

* ASP.NET Core Web API (.NET 8)
* Entity Framework Core
* MySQL (proveedor Pomelo)
* Autenticación JWT

Se trata de una API tipo **Kanban (estilo Trello)** que permite:

* Registro y login de usuarios
* Gestión de tableros
* Gestión de columnas
* Gestión de tareas
* Sistema de roles por tablero

---

# Autenticación

La autenticación se realiza mediante **JWT (JSON Web Token)**.

Todos los endpoints protegidos requieren:

```
Authorization: Bearer <token>
```

---

## Endpoints de Autenticación

### POST `/api/auth/register`

Crea un nuevo usuario y automáticamente:

* Crea un tablero personal
* Añade al usuario como Owner
* Crea columnas por defecto (To Do, In Progress, Done)

### Ejemplo de petición

```json
{
  "username": "Joel",
  "email": "joel@test.com",
  "password": "123456"
}
```

---

### POST `/api/auth/login`

Devuelve un token JWT si las credenciales son correctas.

### Ejemplo de petición

```json
{
  "email": "joel@test.com",
  "password": "123456"
}
```

### Respuesta

```json
{
  "token": "JWT_TOKEN_AQUI"
}
```

---

# Usuarios

## Modelo User

* Id
* Username
* Email
* PasswordHash
* CreatedAt
* IsActive

Las contraseñas se almacenan hasheadas con SHA256.

---

# Tableros (Boards)

## Modelo Board

* Id
* Name
* JoinCode
* OwnerId
* CreatedAt
* IsDeleted

Cada tablero tiene un **Owner** y puede tener múltiples miembros.

---

## Roles por tablero

Enum `BoardRole`:

* Owner
* Admin
* Member

### Permisos

| Acción           | Owner | Admin | Member |
| ---------------- | ----- | ----- | ------ |
| Crear columna    | Sí    | Sí    | No     |
| Eliminar columna | Sí    | Sí    | No     |
| Crear tarea      | Sí    | Sí    | Sí     |
| Mover tarea      | Sí    | Sí    | Sí     |
| Eliminar tablero | Sí    | No    | No     |

---

## Endpoints de Tablero

### GET `/api/board/my`

Devuelve los tableros donde el usuario es miembro.

### GET `/api/board/{id}`

Devuelve el tablero completo con:

* Columnas ordenadas por posición
* Tareas ordenadas por posición dentro de cada columna
* Un indicador de si el usuario es el propietario (IsOwner)

### POST `/api/board`

Crea un nuevo tablero.

### POST `/api/board/join`

Permite unirse a un tablero mediante JoinCode.

---

# Columnas

## Modelo TaskColumn

* Id
* Name
* Position
* BoardId

Cada columna pertenece a un tablero.

---

## Endpoints de Columnas

### POST `/api/column`

Crea una columna (solo Owner/Admin).

La posición se asigna automáticamente al final.

### PUT `/api/column/{id}`

Permite modificar el nombre.

### PUT `/api/column/{id}/move`

Permite cambiar el orden de la columna.

Se reajustan automáticamente las posiciones.

### DELETE `/api/column/{id}`

Elimina la columna y reajusta las posiciones restantes.

---

# Tareas

## Modelo TaskItem

* Id
* Title
* Description
* ColumnId
* AssignedTo (opcional)
* Position
* DueDate
* CreatedAt
* IsDeleted

Cada tarea pertenece a una columna.

---

## Endpoints de Tareas

### POST `/api/task`

Crea una tarea.

La posición se asigna automáticamente al final.

### PUT `/api/task/{id}`

Permite modificar título y descripción.

### PUT `/api/task/{id}/move`

Permite mover la tarea entre columnas.

Incluye:

* Reordenamiento automático
* Compactación de posiciones
* Prevención de duplicados

### DELETE `/api/task/{id}`

Elimina una tarea.

---

# Base de Datos

## Tablas

* Users
* Boards
* BoardMembers
* Columns
* Tasks

## Relaciones

* Usuario -> Tableros (Owner)
* Usuario <-> Tableros (BoardMembers)
* Tablero -> Columnas
* Columna -> Tareas
* Tarea -> Usuario asignado (opcional)

Se utilizan claves foráneas con reglas de borrado en cascada.

---

# Conexión a la Base de Datos

La aplicación utiliza **MySQL** como sistema gestor de base de datos y se conecta
mediante **Entity Framework Core** con el proveedor Pomelo.

## 1. Cadena de conexión

En el archivo `appsettings.json` se define la cadena de conexión:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3306;database=TaskFlowDB;user=root;password=TU_PASSWORD;"
}
```

Parámetros principales:

* `server`: dirección del servidor MySQL
* `port`: puerto (por defecto 3306)
* `database`: nombre de la base de datos
* `user`: usuario de MySQL
* `password`: contraseña

---

## 2. Configuración en Program.cs

En `Program.cs` se registra el DbContext:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));
```

Esto permite que Entity Framework:

* Se conecte automáticamente a MySQL
* Detecte la versión del servidor
* Gestione las migraciones

---

## 3. DbContext

El archivo `AppDbContext.cs` define:

* Las tablas (DbSet)
* Las relaciones entre entidades
* Las claves compuestas
* Las reglas de borrado

Ejemplo:

```csharp
public DbSet<User> Users { get; set; }
public DbSet<Board> Boards { get; set; }
public DbSet<BoardMember> BoardMembers { get; set; }
public DbSet<TaskColumn> Columns { get; set; }
public DbSet<TaskItem> Tasks { get; set; }
```

---

## 4. Migraciones

Para crear o actualizar la base de datos se utilizan migraciones.

### Crear migración:

```
Add-Migration InitialCreate
```

### Aplicar migración a la base de datos:

```
Update-Database
```

Esto genera automáticamente las tablas según los modelos definidos.

---

## 5. Flujo de conexión

1. La API arranca.
2. Se registra el DbContext con la cadena de conexión.
3. Entity Framework abre conexión cuando es necesario.
4. Se ejecutan consultas LINQ.
5. EF traduce a SQL y ejecuta contra MySQL.

---

# Configuración JWT

En `appsettings.json`:

```json
"JwtSettings": {
  "Key": "CLAVE_SECRETA",
  "Issuer": "TaskFlowAPI",
  "Audience": "TaskFlowClient",
  "ExpiresInMinutes": 720
}
```

El token incluye los siguientes claims:

* NameIdentifier (Id del usuario)
* Email
* Username

---

# Seguridad

* Todos los endpoints de negocio requieren autenticación.
* Se valida la pertenencia al tablero en cada operación.
* Se aplican permisos según el rol.
* El sistema de posiciones mantiene la integridad del Kanban.

---

# Proyecto

Desarrollado como Proyecto Final de DAW.
