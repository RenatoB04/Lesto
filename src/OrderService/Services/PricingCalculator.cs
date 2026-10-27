namespace OrderService.Services;

// Preço da encomenda = preço base + km x taxa + kg x taxa
public static class PricingCalculator
{
    // Chaves das definições (tabela Settings)
    public const string BaseKey = "PrecoBaseEur";
    public const string PerKmKey = "PrecoPorKmEur";
    public const string PerKgKey = "PrecoPorKgEur";

    // Valores por omissão, caso a definição não exista
    public const decimal DefaultBase = 2.50m;
    public const decimal DefaultPerKm = 0.40m;
    public const decimal DefaultPerKg = 0.20m;

    // O Stripe não aceita pagamentos abaixo de 0,50 €
    public const decimal MinimumPrice = 0.50m;

    public static decimal Calculate(double distanceKm, double weightKg, decimal baseEur, decimal perKmEur, decimal perKgEur)
    {
        if (distanceKm < 0) distanceKm = 0;
        if (weightKg < 0) weightKg = 0;

        var price = baseEur
            + (decimal)distanceKm * perKmEur
            + (decimal)weightKg * perKgEur;

        price = Math.Round(price, 2, MidpointRounding.AwayFromZero);
        return Math.Max(price, MinimumPrice);
    }
}
