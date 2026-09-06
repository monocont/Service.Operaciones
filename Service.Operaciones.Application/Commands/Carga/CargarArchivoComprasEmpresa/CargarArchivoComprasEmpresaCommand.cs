using MediatR;
using Service.Operaciones.Application.DTOs.Carga;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoComprasEmpresa;

public class CargarArchivoComprasEmpresaCommand : IRequest<CargarArchivoSunatDTO>
{
    public required Stream ArchivoStream { get; set; }
    public required string NombreArchivo { get; set; }
    public required string EmpresaRuc { get; set; }
    public required string Periodo { get; set; }
    public required string Usuario { get; set; }
}
