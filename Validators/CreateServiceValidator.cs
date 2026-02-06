using FacturArtisan.Api.Application.DTOs.Services;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Nom)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Prix)
            .GreaterThanOrEqualTo(0);
    }
}
