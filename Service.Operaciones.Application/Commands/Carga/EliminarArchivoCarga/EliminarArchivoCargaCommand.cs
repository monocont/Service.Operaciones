using MediatR;

namespace Service.Operaciones.Application.Commands.Carga.EliminarArchivoCarga;

public record EliminarArchivoCargaCommand(Guid IdCarga) : IRequest<bool>;
