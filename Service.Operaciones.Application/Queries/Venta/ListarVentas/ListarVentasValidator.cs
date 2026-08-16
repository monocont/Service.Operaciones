using FluentValidation;

namespace Service.Operaciones.Application.Queries.Venta.ListarVentas;

public class ListarVentasValidator : AbstractValidator<ListarVentasQuery>
{
    public ListarVentasValidator()
    {
        RuleFor(x => x.IdCarga).NotEmpty().WithMessage("El IdCarga es obligatorio");
    }
}
