using System.ComponentModel.DataAnnotations;

namespace MXMChallenge.DTOs
{
    public class UpdateUserDTO: BaseUserDTO
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Senha n�o informada.")]
        [MinLength(6, ErrorMessage = "Senha precisa ter 6 dig�tos.")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Senha deve possuir apenas n�meros.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirmação de senha não informada.")]
        [Compare("Password", ErrorMessage = "Senhas não conferem.")]
        public string ConfirmPassword { get; set; } = null!;

    }
}
