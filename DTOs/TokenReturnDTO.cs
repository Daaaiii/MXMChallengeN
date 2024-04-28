namespace MXMChallenge.DTOs
{
    public class TokenReturnDTO
    {
        public string token { get; set; }
        public string userName { get; set; }
        

        public TokenReturnDTO(string token, string userName)
        {
            this.token = token;
            this.userName = userName;
            
        }

        public TokenReturnDTO()
        {
        }
    }
}
