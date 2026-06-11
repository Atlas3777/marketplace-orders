namespace Marketplace.Products.Api.Protos;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);

        return new DecimalValue { Units = units, Nanos = nanos };
    }

    public static implicit operator decimal(DecimalValue? grpcDecimal)
    {
        if (grpcDecimal == null)
        {
            return 0m;
        }

        return grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;
    }
}