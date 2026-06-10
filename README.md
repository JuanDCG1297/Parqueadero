# 🅿️ Parqueadero API

**Prueba Técnica — Semi Senior**

API REST para gestión de parqueadero (entrada/salida de vehículos, cálculo de tarifas, notificaciones por email).

---

## 📋 Requisitos Previos

| Herramienta | Versión | Para qué |
|-------------|---------|----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0**+ | Compilar y ejecutar la API |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Cualquiera | Base de datos MySQL |
| [Git](https://git-scm.com/) | Cualquiera | Clonar el repositorio |
| Postman / insomnia / curl | Cualquiera | Probar los endpoints |

### Verificar instalaciones

```powershell
dotnet --version          # → 10.0.x
docker --version          # → Docker version xx.x
docker compose version    # → Docker Compose version v2.x
```

---

## 🚀 Inicio Rápido

### 1. Clonar el repositorio

```powershell
git clone <url-del-repo>
cd Parqueadero
```

### 2. Levantar MySQL con Docker

```powershell
docker compose up -d
```

Esto crea un contenedor `parking-mysql` con MySQL 8.0 y ejecuta automáticamente el script de inicialización (`script-mysql.sql`) que crea la base de datos y la tabla `VehicleEntries`.

**Verificar que MySQL esté listo:**

```powershell
docker compose ps
# → parking-mysql   Up (healthy)  0.0.0.0:3306->3306/tcp
```

### 3. Ejecutar la API

```powershell
dotnet run --project Web.Api
```

La API arranca en `http://localhost:5156` y el Swagger en:
→ **http://localhost:5156/swagger**

---

## 🐳 Explicación del Docker Compose

```yaml
services:
  mysql:
    image: mysql:8.0
    container_name: parking-mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: root           # Contraseña del usuario root
      MYSQL_DATABASE: parqueadero         # Base de datos creada al iniciar
      MYSQL_USER: parking_user            # Usuario adicional (opcional)
      MYSQL_PASSWORD: parking_pass        # Contraseña del usuario adicional
    ports:
      - "3306:3306"                       # Puerto local:puerto contenedor
    volumes:
      - mysql_data:/var/lib/mysql         # Datos persistentes (no se borran al apagar)
      - ./script-mysql.sql:/docker-entrypoint-initdb.d/init.sql  # Script inicial automático
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  mysql_data:                             # Volumen nombrado para persistencia
```

### ¿Qué hace cada cosa?

- **`image: mysql:8.0`** — Usa MySQL versión 8.0 oficial
- **`MYSQL_ROOT_PASSWORD`** — Define la contraseña del usuario root
- **`MYSQL_DATABASE`** — Crea la base de datos automáticamente al primer inicio
- **`ports: "3306:3306"`** — Expone MySQL en el puerto 3306 de tu máquina
- **`volumes: mysql_data`** — Los datos se guardan aunque apagues el contenedor
- **`healthcheck`** — Docker espera a que MySQL esté listo antes de marcar el contenedor como "healthy"
- **`init.sql`** — Script que se ejecuta automáticamente la primera vez que arranca el contenedor


## 🗄️ Comandos para revisar la base de datos

### Conectar desde la terminal

```powershell
docker exec -it parking-mysql mysql -uroot -proot
```

Una vez dentro de MySQL:

```sql
USE parqueadero;
SELECT * FROM VehicleEntries\G
```

### Consultas útiles

```powershell
# Todos los registros
docker exec -it parking-mysql mysql -uroot -proot -e "
  USE parqueadero;
  SELECT Id, Plate, VehicleType, EntryTime, ExitTime,
         TotalMinutes, Fee, EmailSent
  FROM VehicleEntries\G
"

# Solo vehículos estacionados (sin salir)
docker exec -it parking-mysql mysql -uroot -proot -e "
  USE parqueadero;
  SELECT Id, Plate, VehicleType, EntryTime
  FROM VehicleEntries
  WHERE ExitTime IS NULL;
"

# Vehículos que ya salieron
docker exec -it parking-mysql mysql -uroot -proot -e "
  USE parqueadero;
  SELECT Plate, EntryTime, ExitTime, TotalMinutes, Fee
  FROM VehicleEntries
  WHERE ExitTime IS NOT NULL;
"

# Conteo
docker exec -it parking-mysql mysql -uroot -proot -e "
  USE parqueadero;
  SELECT COUNT(*) AS Total FROM VehicleEntries;
  SELECT COUNT(*) AS Estacionados FROM VehicleEntries WHERE ExitTime IS NULL;
"
```

### Batch interactivo (db-queries.bat)

También incluí un script `db-queries.bat` con menú interactivo para consultas rápidas. Solo ejecutalo desde PowerShell:

```powershell
.\db-queries.bat
```

---

## 📐 Estructura del Proyecto (Clean Architecture)

```
Parqueadero/
├── Domain/                        # Capa más interna — entidades y reglas de negocio
│   └── Entities/
│       └── VehicleEntry.cs        # Entidad principal (entrada/salida de vehículo)
│
├── Application/                   # Casos de uso de la aplicación
│   ├── DTOs/                      # Objetos de transferencia de datos
│   ├── Interfaces/                # Puertos (contratos) que implementa Infrastructure
│   ├── Services/
│   │   └── ParkingService.cs      # Lógica de negocio: registrar entrada/salida, calcular tarifas
│   └── Validators/                # Validación con FluentValidation
│
├── Infrastructure/                # Implementaciones concretas (DB, Email, etc.)
│   ├── Data/
│   │   ├── AppDbContext.cs        # Contexto de Entity Framework Core
│   │   └── VehicleRepository.cs   # Repositorio de vehículos
│   └── Email/
│       ├── EmailClient.cs         # Cliente HTTP tipado para la API de email
│       ├── EmailDelegatingHandler.cs  # Interceptor que obtiene e inyecta el token JWT
│       ├── EmailOptions.cs        # Configuración (appsettings.json)
│       ├── EmailPolicies.cs       # Polly: retry con exponential backoff
│       └── EmailService.cs        # Servicio que construye el payload y envía
│
├── Web.Api/                       # Punto de entrada — API REST
│   ├── Controllers/
│   │   └── VehiclesController.cs  # Endpoints de entrada/salida
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs # Manejo global de errores
│   ├── Migrations/                # Migraciones de EF Core
│   ├── Program.cs                 # Configuración de servicios, DI, pipeline
│   └── appsettings.json           # Configuración (DB, Email)
│
└── tests/
    └── Parqueadero.Tests/         # Tests unitarios y de integración
```

### Principios aplicados

| Principio | Cómo se aplica |
|-----------|----------------|
| **Clean Architecture** | Dependencias hacia adentro: Domain → Application → Infrastructure → Web.Api |
| **SOLID** | Interfaces segregadas, inyección de dependencias, responsabilidad única |
| **Repository Pattern** | `IVehicleRepository` abstrae el acceso a datos |
| **TDD** | Tests escritos primero, 40/40 tests pasando |
| **FluentValidation** | Validación declarativa con mensajes en español |
| **Middleware** | ExceptionMiddleware centraliza el manejo de errores |
| **Polly** | Retry policy para el envío de emails |
| **Typed HttpClient** | EmailClient con DelegatingHandler para autenticación |

---

## 🔌 Endpoints de la API

### Registrar entrada de vehículo

```http
POST /api/vehicles/entry
Content-Type: application/json

{
  "plate": "ABC123",
  "vehicleType": "Carro"
}
```

**Respuesta:**
```json
{
  "id": "guid",
  "plate": "ABC123",
  "vehicleType": "Carro",
  "entryTime": "2026-06-10T10:30:00",
  "message": "Ingreso registrado exitosamente"
}
```

### Registrar salida de vehículo

```http
POST /api/vehicles/exit
Content-Type: application/json

{
  "plate": "ABC123"
}
```

**Respuesta:**
```json
{
  "id": "guid",
  "plate": "ABC123",
  "entryTime": "2026-06-10T10:30:00",
  "exitTime": "2026-06-10T12:45:00",
  "totalMinutes": 135,
  "fee": 5400.00,
  "message": "Salida registrada exitosamente"
}
```

### Tipos de vehículo y tarifas

| Tipo | Tarifa por hora |
|------|----------------|
| `Carro` | $2.400 COP |
| `Moto` | $1.200 COP |
| `Bicicleta` | $600 COP |

El cálculo es **por fracción de hora** (si estacionó 1h10m, paga 2 horas).

### Códigos de error

| Código | Significado |
|--------|-------------|
| `200` | OK |
| `400` | Error de validación (placa vacía, tipo inválido, vehículo ya registrado) |
| `404` | Vehículo no encontrado (al registrar salida) |
| `500` | Error interno del servidor |

---

## ⚙️ Configuración por Entorno

La app detecta automáticamente qué base de datos usar según el connection string:

**MySQL (producción)** — cuando el string empieza con `"Server="`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=parqueadero;User=root;Password=root;"
  }
}
```

**SQLite (desarrollo)** — cuando el string NO empieza con `"Server="`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=parqueadero.db"
  }
}
```

Para cambiar entre MySQL y SQLite solo hay que modificar `appsettings.json`.

---

## 📧 Notificaciones por Email

Al registrar la salida de un vehículo, la API intenta enviar un email de notificación a través de un servicio externo.

**Configuración en `appsettings.json`:**
```json
{
  "Email": {
    "BaseUrl": "https://dev-sites.similtech.co/api-email/",
    "Username": "proceso_pruebas",
    "Password": "***"
  }
}
```

**Nota:** El envío de email es **no bloqueante** — si falla, la salida se registra igual pero con `EmailSent = false`. Revisá los logs de la aplicación para ver el estado.

---

## ✅ Tests

```powershell
dotnet test
```

Todos los tests son **deterministas**: no dependen de la base de datos ni de servicios externos. Usan mocks y datos en memoria.

---

## 🧰 Solución de Problemas

### Puerto 3306 ocupado

Si ya tenés MySQL instalado localmente, detenelo primero:

```powershell
net stop MySQL
# o desde Services.msc
```

O cambiá el puerto en `docker-compose.yml`:
```yaml
ports:
  - "3307:3306"    # MySQL local en 3306, Docker en 3307
```

Y actualizá `appsettings.json`:
```json
"DefaultConnection": "Server=localhost;Port=3307;..."
```

### DNS no resuelve dev-sites.similtech.co (notificaciones email)

Si el navegador abre `https://dev-sites.similtech.co/api-email/swagger/` pero la API no puede resolver el hostname, agregá esta línea al archivo `C:\Windows\System32\drivers\etc\hosts` como Administrador:

```
3.136.214.85 dev-sites.similtech.co
```
