using FluentValidation;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerCargaPorId;

public class ObtenerCargaPorIdValidator : AbstractValidator<ObtenerCargaPorIdQuery>
{
    public ObtenerCargaPorIdValidator()
    {
        RuleFor(x => x.IdCarga).NotEmpty().WithMessage("El IdCarga es obligatorio");
    }
}
