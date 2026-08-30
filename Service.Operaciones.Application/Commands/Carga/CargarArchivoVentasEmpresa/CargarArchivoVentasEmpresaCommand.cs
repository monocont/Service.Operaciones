using MediatR;
using Service.Operaciones.Application.DTOs.Carga;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoVentasEmpresa;

public class CargarArchivoVentasEmpresaCommand : IRequest<CargarArchivoSunatDTO>
{
    public required Stream ArchivoStream { get; set; }
    public required string NombreArchivo { get; set; }
    public required string EmpresaRuc { get; set; }
    public required string Periodo { get; set; }
    public required string Usuario { get; set; }
}
