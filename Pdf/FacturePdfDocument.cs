using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FacturArtisan.Api.Models;

namespace FacturArtisan.Api.Pdf;

public class FacturePdfDocument : IDocument
{
    private readonly Facture _facture;

    public FacturePdfDocument(Facture facture)
    {
        _facture = facture;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);

            page.Header().Text("FacturArtisan")
                .FontSize(20)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            page.Content().Column(col =>
            {
                col.Spacing(10);

                col.Item().Text($"Facture N° : {_facture.Numero}").Bold();
                col.Item().Text($"Date : {_facture.CreatedAt:dd/MM/yyyy}");
                col.Item().Text($"Client : {_facture.Devis.Client.Nom}");
                col.Item().LineHorizontal(1);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Service").Bold();
                        header.Cell().Text("Qté").Bold();
                        header.Cell().Text("Prix").Bold();
                        header.Cell().Text("Total").Bold();
                    });

                    foreach (var item in _facture.Devis.Items)
                    {
                        table.Cell().Text(item.ServiceItem.Nom);
                        table.Cell().Text(item.Quantite.ToString());
                        table.Cell().Text(item.PrixUnitaire.ToString("N0"));
                        table.Cell().Text(item.Total.ToString("N0"));
                    }
                });

                col.Item().LineHorizontal(1);
                col.Item().AlignRight().Text($"TOTAL : {_facture.Total:N0} FCFA")
                    .FontSize(14).Bold();

                col.Item().Text("Merci pour votre confiance !");
            });

            page.Footer().AlignCenter().Text("FacturArtisan - Facturation simple pour artisans");
        });
    }
}
