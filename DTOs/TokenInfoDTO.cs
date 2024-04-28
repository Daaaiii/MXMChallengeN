namespace MXMChallenge.DTOs
{
    public class TokenInfoDTO
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }

        public string Fullname { get; set; }
        public object Token { get; internal set; }

        public TokenInfoDTO() { }
    }
}
