namespace RevalW.App.Model
{
    public class AddressPoint
    {
        public string Address { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string? Label { get; init; }
    }
}
