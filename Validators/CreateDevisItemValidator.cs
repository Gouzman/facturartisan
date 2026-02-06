using FacturArtisan.Api.Application.DTOs.Devis;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class CreateDevisItemValidator : AbstractValidator<CreateDevisItemRequest>
{
    public CreateDevisItemValidator()
    {
        RuleFor(x => x.ServiceItemId)
            .NotEmpty();

        RuleFor(x => x.Quantite)
            .GreaterThan(0);

        RuleFor(x => x.PrixUnitaire)
            .GreaterThanOrEqualTo(0);
    }
}
