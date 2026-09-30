# Arquitectura

`client` habla con `Appointments.Api`. La API depende de Application e Infrastructure. Application depende de Domain. Infrastructure depende de Application y Domain. Domain no conoce HTTP ni EF.

Identity y el hash del refresh token viven en Infrastructure. Los controladores traducen HTTP y llaman a un servicio de aplicación. Las reglas de solape, hueco y "un turno por día" se deciden antes de guardar, dentro de una transacción.

El mail es un puerto (`IConfirmacionCorreo`). Hoy lo implementa un escritor de archivos.
