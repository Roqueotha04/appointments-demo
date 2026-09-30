---
name: turnos
description: >-
  Trabaja el producto de turnos (reservas, negocios, empleados, disponibilidad,
  panel admin, JWT y mail local). Usar al cambiar la API, el dominio, el panel,
  el asistente de reserva, la autenticación o la agenda.
---

# Turnos

Leé `AGENTS.md` y `docs/dominio.md` antes de editar. El contrato HTTP está en `docs/api.md`.

No muevas reglas de agenda al cliente. No filtres negocios solo con el id que manda el browser: el dueño es el usuario del token. No commitees connection strings ni claves JWT. El correo de un turno pasa por `IConfirmacionCorreo`.
