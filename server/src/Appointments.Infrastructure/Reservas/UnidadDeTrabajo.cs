using System.Data;
using Appointments.Application.Reservas;
using Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Appointments.Infrastructure.Reservas;

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly AppDbContext _db;

    public UnidadDeTrabajo(AppDbContext db) => _db = db;

    public async Task<T> EjecutarAsync<T>(Func<CancellationToken, Task<T>> accion, CancellationToken cancellationToken)
    {
        const int maximo = 3;
        for (var intento = 1; ; intento++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var resultado = await accion(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return resultado;
            }
            catch (Exception ex) when (intento < maximo && EsInterbloqueo(ex))
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    public async Task BloquearReservaAsync(Guid clienteId, Guid empleadoId, CancellationToken cancellationToken)
    {
        await BloquearFila("SELECT `Id` FROM `AspNetUsers` WHERE `Id` = @id FOR UPDATE", clienteId, cancellationToken);
        await BloquearFila("SELECT `Id` FROM `Empleados` WHERE `Id` = @id FOR UPDATE", empleadoId, cancellationToken);
    }

    private static bool EsInterbloqueo(Exception ex)
    {
        for (var actual = ex; actual is not null; actual = actual.InnerException)
        {
            if (actual is MySqlConnector.MySqlException mysql && mysql.Number is 1213 or 1205)
                return true;
        }

        return false;
    }

    private async Task BloquearFila(string sql, Guid id, CancellationToken cancellationToken)
    {
        var conexion = _db.Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
            await conexion.OpenAsync(cancellationToken);

        await using var comando = conexion.CreateCommand();
        comando.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
        comando.CommandText = sql;
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@id";
        parametro.Value = id.ToString();
        comando.Parameters.Add(parametro);
        await comando.ExecuteScalarAsync(cancellationToken);
    }
}
