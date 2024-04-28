namespace MxmChallenge.Models
{
    public class Address
    {
        public int Id { get; set; }

        public string Zipcode { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string Number { get; set; } = null!;
        public string? Complement { get; set; }
        public string Neighborhood { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
