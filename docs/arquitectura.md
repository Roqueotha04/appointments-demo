# Arquitectura

`client` habla con `Appointments.Api`. La API depende de Application e Infrastructure. Application depende de Domain. Infrastructure depende de Application y Domain. Domain no conoce HTTP ni EF.

Application tiene los casos de uso. `ReservasService` decide las reglas de reserva (servicio y empleado válidos, hueco calculado, un turno por día, quién puede mover o cancelar) y habla con el exterior solo por puertos:

- `IAgenda`: lecturas y escrituras de la agenda. Lo implementa `Agenda` en Infrastructure con EF.
- `IUnidadDeTrabajo`: la transacción serializable, el reintento ante interbloqueos y el bloqueo de las filas del cliente y del empleado. Lo implementa `UnidadDeTrabajo`, que es la parte atada a MySQL.
- `IConfirmacionCorreo`: el mail.

Catálogo (`CatalogoService`) y auth (`AuthService`) todavía viven en Infrastructure y usan `AppDbContext` y `UserManager` directamente; Application solo define sus contratos. Se moverán igual que reservas si hace falta.

Identity y el hash del refresh token viven en Infrastructure. Los controladores traducen HTTP y llaman a un servicio de aplicación. Las reglas de solape, hueco y "un turno por día" se deciden antes de guardar, dentro de una transacción.

El mail es un puerto (`IConfirmacionCorreo`). Hoy lo implementa un escritor de archivos.
