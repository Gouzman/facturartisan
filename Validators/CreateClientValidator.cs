using FacturArtisan.Api.Application.DTOs.Clients;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class CreateClientValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Nom)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Telephone)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Type)
            .NotEmpty().MaximumLength(50);
    }
}
