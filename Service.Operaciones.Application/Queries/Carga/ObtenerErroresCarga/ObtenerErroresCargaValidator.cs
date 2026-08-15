using FluentValidation;

namespace Service.Operaciones.Application.Queries.Carga.ObtenerErroresCarga;

public class ObtenerErroresCargaValidator : AbstractValidator<ObtenerErroresCargaQuery>
{
    public ObtenerErroresCargaValidator()
    {
        RuleFor(x => x.IdCarga).NotEmpty().WithMessage("El IdCarga es obligatorio");
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("PageNumber debe ser mayor que 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500).WithMessage("PageSize debe estar entre 1 y 500");
    }
}
