using FacturArtisan.Api.Application.DTOs.Clients;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class UpdateClientValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Nom)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Telephone)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Type)
            .NotEmpty().MaximumLength(50);
    }
}
