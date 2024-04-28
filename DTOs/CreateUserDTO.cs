using System.ComponentModel.DataAnnotations;

namespace MXMChallenge.DTOs
{
    public class CreateUserDTO: BaseUserDTO
    {
        [Required(ErrorMessage = "Senha n�o informada.")]
        [MinLength(8, ErrorMessage = "Senha precisa ter 8 caracteres.")]
        [MaxLength(32, ErrorMessage = "Senha pode ter no máximo 32 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$", ErrorMessage = "Senha deve possuir pelo menos um número, um símbolo, uma letra maiúscula e uma minúscula.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirmação de senha não informada.")]
        [Compare("Password", ErrorMessage = "Senhas não conferem.")]
        public string ConfirmPassword { get; set; } = null!;

    }
}
