# Desarrollo local

Hace falta el SDK de .NET 10, Node, y MySQL en `localhost:3306`.

La cadena y la clave JWT no van al repositorio. Desde `server/src/Appointments.Api`:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=appointments;User=root;Password=TU_PASSWORD;"
dotnet user-secrets set "Jwt:SigningKey" "una-clave-local-de-al-menos-32-caracteres"
```

En Development la API aplica migraciones al arrancar y, si la base está vacía, carga un negocio de prueba.

- Dueño: `owner@local.test` / `Local-owner-123`
- Cliente: `cliente@local.test` / `Local-cliente-123`
- Reserva: http://localhost:4200/estudio-norte

```bash
dotnet run --project server/src/Appointments.Api --launch-profile http
```

Swagger: http://localhost:5080/swagger

```bash
cd client
npm install
npm start
```

Los correos quedan en `server/local-mail/`.

Los tests de reservas usan la misma cadena y una base distinta, `appointments_test`. No tocan la base de desarrollo.

La herramienta de migraciones está en `server/dotnet-tools.json`. Desde `server/`:

```bash
dotnet tool restore
```
