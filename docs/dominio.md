# Dominio

## Cuentas

Hay un solo tipo de usuario. Se registra con nombre, email y contraseña. Con esa cuenta reserva en cualquier negocio. Cuando crea un negocio, queda como dueño de ese negocio y puede tener varios. Los empleados no tienen usuario.

## Negocio

Tiene slug público, nombre y zona horaria IANA (`America/Argentina/Buenos_Aires` por defecto). La URL de reserva es `/{slug}`.

Al crear el negocio se crea un empleado con el mismo nombre y un horario de lunes a sábado, de 10:00 a 18:00. Ese horario se edita después.

## Catálogo

Un servicio tiene duración en minutos y precio, y pertenece a un negocio. Un empleado ofrece solo los servicios que la empresa le asigna. Un servicio nuevo queda ofrecido por los empleados activos que ya existen. Un empleado nuevo no ofrece nada hasta que se lo asigna.

## Agenda

La disponibilidad es semanal por empleado: día de la semana (0 domingo a 6 sábado) y una franja `horaInicio`–`horaFin`. Puede haber varias franjas el mismo día, sin solaparse. Un bloqueo tapa un intervalo concreto.

Los turnos se guardan en UTC. El día calendario y los huecos se calculan en la zona del negocio. El paso de la grilla es la duración del servicio. Un hueco entra solo si el intervalo completo cabe en una franja y no pisa un turno confirmado ni un bloqueo. No se ofrecen horarios que ya pasaron.

## Turno

Estados: `Confirmado` y `Cancelado`. Reprogramar actualiza el mismo turno y lo deja confirmado. Solo el cliente dueño del turno puede reprogramarlo. Cancelar puede el cliente o el dueño del negocio.

Reglas al confirmar o mover:

- El servicio y el empleado pertenecen al negocio, están activos y el empleado ofrece ese servicio.
- El inicio coincide con un hueco calculado para esa fecha.
- El empleado no tiene otro confirmado que se solape.
- El cliente no tiene otro confirmado ese día en ese negocio. Al mover, el turno actual no cuenta.

## Mail

Cada alta, movimiento y cancelación deja un correo en `server/local-mail/`. No hay SMTP en este entorno.
