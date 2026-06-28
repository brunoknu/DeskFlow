namespace DeskFlow.Domain.ValueObjects;

public sealed record TicketNumber(string Value)
{
    public static TicketNumber Create(int year, long sequence) =>
        new($"HD-{year}-{sequence:D6}");

    public override string ToString() => Value;
}
