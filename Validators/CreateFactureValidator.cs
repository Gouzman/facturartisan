using FacturArtisan.Api.Application.DTOs.Factures;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class CreateFactureValidator : AbstractValidator<CreateFactureRequest>
{
    public CreateFactureValidator()
    {
        RuleFor(x => x.DevisId)
            .NotEmpty();
    }
}
