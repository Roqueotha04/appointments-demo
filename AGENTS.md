# Turnos

Producto de reservas para negocios con varios locales por dueño. El cliente también tiene cuenta. Los empleados no entran al sistema: los carga la empresa.

Leé `docs/dominio.md` antes de tocar reservas, horarios o permisos.

## Dónde está cada cosa

- `client/`: Angular. La API se consume por el proxy `/api` hacia `http://localhost:5080`.
- `server/src/Appointments.Domain`: entidades y cálculo de huecos.
- `server/src/Appointments.Application`: casos de uso. No referencia EF ni HTTP.
- `server/src/Appointments.Infrastructure`: MySQL, Identity, JWT y el mail local.
- `server/src/Appointments.Api`: controladores finos.
- `server/local-mail/`: correos de desarrollo. No se commitea.
- `docs/`: operación y contrato.

## Reglas que no se negocian

- Un dueño solo lee y escribe sus negocios. Si el id no es suyo, la respuesta es 404.
- Sacar, mover o cancelar un turno exige usuario autenticado. El cliente mueve el propio turno si el hueco existe.
- Un cliente no puede tener dos turnos confirmados el mismo día en el mismo negocio. El día es el de la zona horaria del negocio.
- No se persisten contraseñas, connection strings ni signing keys. Van en user secrets.
- El mail sale por `IConfirmacionCorreo`. En local escribe un archivo. No vuelvas a mandar el mail desde el browser.
- Los huecos los calcula `CalculadoraDeHuecos`. No armes la grilla en el cliente.
