using MediatR;
using Service.Operaciones.Application.DTOs.VentaEmpresa;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentasEmpresa;

public class ListarVentasEmpresaQuery : IRequest<List<VentaEmpresaDTO>>
{
    public Guid IdCarga { get; set; }
}
