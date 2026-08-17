using FluentValidation;

namespace Service.Operaciones.Application.Queries.Compra.ListarCompras;

public class ListarComprasValidator : AbstractValidator<ListarComprasQuery>
{
    public ListarComprasValidator()
    {
        RuleFor(x => x.IdCarga).NotEmpty().WithMessage("El IdCarga es obligatorio");
    }
}
