# Turnos

Reservas para negocios con varios locales por dueño. El cliente también tiene cuenta; los empleados no entran al sistema, los carga la empresa.

- `client/`: Angular, con el asistente de reserva, "mis turnos" y el panel del dueño.
- `server/`: API en .NET 10 con MySQL, dividida en Domain, Application, Infrastructure y Api.
- `docs/`: dominio, contrato de la API, arquitectura y operación local.

## Arranque rápido

Hace falta el SDK de .NET 10, Node y MySQL en `localhost:3306`.

```bash
# desde server/src/Appointments.Api: secretos locales (una sola vez)
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=appointments;User=root;Password=TU_PASSWORD;"
dotnet user-secrets set "Jwt:SigningKey" "una-clave-local-de-al-menos-32-caracteres"

# API en http://localhost:5080 (Swagger en /swagger)
dotnet run --project server/src/Appointments.Api --launch-profile http

# cliente en http://localhost:4200
cd client && npm install && npm start
```

En Development la API aplica las migraciones y carga un negocio de prueba: http://localhost:4200/estudio-norte. Las cuentas locales, los correos de desarrollo y los tests están en [docs/desarrollo-local.md](docs/desarrollo-local.md).

## Tests

```bash
cd server && dotnet test
```

Los tests de reservas usan MySQL real con la base `appointments_test`, sin tocar la de desarrollo.

## Documentación

- [Dominio](docs/dominio.md): cuentas, negocios, agenda, turnos y sus reglas.
- [API](docs/api.md): contrato HTTP.
- [Arquitectura](docs/arquitectura.md): capas y dependencias.
- [Desarrollo local](docs/desarrollo-local.md): puesta en marcha y configuración.
