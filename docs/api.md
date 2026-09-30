# API

Base local: `http://localhost:5080`. El cliente Angular llama a `/api` y el proxy de desarrollo reenvía ahí.

El access token dura 15 minutos y viaja en `Authorization: Bearer`. El refresh es una cookie `HttpOnly` `turnos_refresh`, path `/api/auth`. Login, registro, alta de turno y reprogramación tienen rate limit por IP.

Si el negocio no pertenece al usuario autenticado, la respuesta es 404.

## Auth

- `POST /api/auth/registro` `{ nombre, email, password, telefono? }`
- `POST /api/auth/login` `{ email, password }` → `{ accessToken, expiraUtc, usuario }`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/yo`

## Panel

Requieren sesión.

- `GET/POST /api/negocios`, `GET/PUT /api/negocios/{id}`
- `GET/POST /api/negocios/{id}/servicios`, `PUT/DELETE .../servicios/{servicioId}`
- `GET/POST /api/negocios/{id}/empleados`, `PUT/DELETE .../empleados/{empleadoId}`
- `PUT /api/negocios/{id}/empleados/{empleadoId}/servicios` `{ servicioIds }`
- `GET/PUT /api/negocios/{id}/empleados/{empleadoId}/disponibilidad` `{ franjas: [{ diaSemana, horaInicio, horaFin }] }`
- `GET/POST /api/negocios/{id}/empleados/{empleadoId}/bloqueos`, `DELETE .../bloqueos/{bloqueoId}`
- `GET /api/negocios/{id}/turnos`

`DELETE` de servicio o empleado los desactiva. No borra turnos históricos.

## Reserva

Público:

- `GET /api/publico/negocios/{slug}`
- `GET /api/publico/negocios/{slug}/servicios`
- `GET /api/publico/negocios/{slug}/servicios/{servicioId}/empleados`
- `GET /api/publico/negocios/{slug}/huecos?servicioId&empleadoId&fecha=yyyy-MM-dd`

Con sesión:

- `POST /api/turnos` `{ negocioId, servicioId, empleadoId, inicioUtc }`
- `GET /api/turnos/mios`
- `POST /api/turnos/{id}/reprogramar` `{ inicioUtc }`
- `POST /api/turnos/{id}/cancelar`
