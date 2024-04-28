using System.ComponentModel.DataAnnotations;

namespace MXMChallenge.DTOs
{
    public class UserDetailsDTO: BaseUserDTO
    {
        [Key]
        public Guid Id { get; set; }
    }
}
