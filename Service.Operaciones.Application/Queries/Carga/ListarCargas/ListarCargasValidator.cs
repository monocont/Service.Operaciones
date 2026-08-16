using FluentValidation;

namespace Service.Operaciones.Application.Queries.Carga.ListarCargas;

public class ListarCargasValidator : AbstractValidator<ListarCargasQuery>
{
    public ListarCargasValidator()
    {
        RuleFor(x => x.EmpresaRuc)
            .NotEmpty().WithMessage("El RUC de la empresa es obligatorio")
            .Length(11).WithMessage("El RUC debe tener 11 dígitos");

        RuleFor(x => x.TipoArchivo)
            .IsInEnum().WithMessage("El tipo de archivo es obligatorio y debe ser válido (Ventas o Compras)");

        RuleFor(x => x.PageNumber)
            .NotNull().WithMessage("PageNumber es obligatorio")
            .GreaterThan(0).WithMessage("PageNumber debe ser mayor que 0");

        RuleFor(x => x.PageSize)
            .NotNull().WithMessage("PageSize es obligatorio")
            .InclusiveBetween(1, 100).WithMessage("PageSize debe estar entre 1 y 100");
    }
}
