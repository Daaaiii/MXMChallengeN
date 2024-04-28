using System.ComponentModel.DataAnnotations;

namespace MXMChallenge.DTOs
{
    public class UserRequestDTO
    {
        [Required(ErrorMessage = "O email deverá ser informado para a realização do Login.")]
        public string email { get; set; } 

        [Required(ErrorMessage = "A senha deverá ser informada para a realização do login")]
        public string password { get; set; }
    }
}
