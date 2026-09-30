# DemoTurnos

Repositorio fullstack. El asistente de reserva sigue en Angular y la API vive en una solución .NET en capas.

```text
client/     Angular
server/     Appointments.slnx
  src/Appointments.Domain
  src/Appointments.Application
  src/Appointments.Infrastructure
  src/Appointments.Api
  seed/db.json
```

El cliente todavía no llama a esta API. Sigue leyendo servicios y horarios desde `http://localhost:3000`. `server/seed/db.json` guarda esos datos para cargarlos cuando exista el CRUD.

## Client

```bash
cd client
npm install
npm start
```

La app queda en http://localhost:4200/.

## Server

Hace falta el SDK de .NET 10.

```bash
dotnet run --project server/src/Appointments.Api --launch-profile http
```

Swagger queda en http://localhost:5080/swagger. La API todavía no expone turnos: las cuatro capas están creadas y referenciadas para el siguiente paso.
