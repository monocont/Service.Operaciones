using FluentValidation;

namespace Service.Operaciones.Application.Commands.Carga.CargarArchivoVentas;

public class CargarArchivoVentasValidator : AbstractValidator<CargarArchivoVentasCommand>
{
    public CargarArchivoVentasValidator()
    {
        RuleFor(x => x.EmpresaRuc)
            .NotEmpty().WithMessage("El RUC de la empresa es obligatorio")
            .Length(11).WithMessage("El RUC debe tener 11 dígitos")
            .Matches(@"^\d{11}$").WithMessage("El RUC solo debe contener números");

        RuleFor(x => x.Periodo)
            .NotEmpty().WithMessage("El periodo es obligatorio")
            .Length(6).WithMessage("El periodo debe tener 6 dígitos (YYYYMM)")
            .Matches(@"^\d{6}$").WithMessage("El periodo solo debe contener números");

        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El usuario es obligatorio");

        RuleFor(x => x.NombreArchivo)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio")
            .Must(nombre => Path.GetExtension(nombre).ToLower() is ".txt" or ".csv")
                .WithMessage("La extensión del archivo debe ser .txt o .csv");

        RuleFor(x => x.ArchivoStream)
            .NotNull().WithMessage("El stream del archivo es obligatorio");
    }
}
