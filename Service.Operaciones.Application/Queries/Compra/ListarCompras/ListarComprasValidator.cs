using FluentValidation;

namespace Service.Operaciones.Application.Queries.Compra.ListarCompras;

public class ListarComprasValidator : AbstractValidator<ListarComprasQuery>
{
    public ListarComprasValidator()
    {
        RuleFor(x => x.IdCarga).NotEmpty().WithMessage("El IdCarga es obligatorio");
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("PageNumber debe ser mayor que 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500).WithMessage("PageSize debe estar entre 1 y 500");
    }
}
