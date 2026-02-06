using FacturArtisan.Api.Application.DTOs.Devis;
using FluentValidation;

namespace FacturArtisan.Api.Validators;

public class CreateDevisValidator : AbstractValidator<CreateDevisRequest>
{
    public CreateDevisValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotNull()
            .Must(items => items.Count > 0)
            .WithMessage("Au moins une ligne est requise");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateDevisItemValidator());
    }
}
