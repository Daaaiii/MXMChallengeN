using System.ComponentModel.DataAnnotations;

namespace MXMChallenge.DTOs
{
    public class BaseUserDTO
    {
        [Required(ErrorMessage = "Nome não informado.")]
        [MinLength(3, ErrorMessage = "Nome deve ter pelo menos 3 caracteres.")]
        [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
        [RegularExpression(
            @"^[a-zA-Zçãõáéíóúàèìòùâêîôûäëïöüñ\s]*$",
            ErrorMessage = "Name só pode conter letras"
        )]
        

        public string Fullname { get; set; } = null!;

        [Required(ErrorMessage = "Email não informado.")]
        [EmailAddress(ErrorMessage = "O endereço de Email precisa ser um email válido.")]

        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "DDD não informado.")]
        [Range(11, 99, ErrorMessage = "DDD Precisa ter 2 digítos.")]
        public int DDD { get; set; }

        [Required(ErrorMessage = "Número de telefone não informado.")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Número de telefone deve possuir apenas números.")]
        public int PhoneNumber { get; set; }


        [Required(ErrorMessage = "CPF ou CNPJ não informados.")]
        [StringLength(14, ErrorMessage = "CPF deve possuir 11 números")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "CPF deve possuir apenas números.")]
        public string cpf_cnpj { get; set; } = null!;


        [Required(ErrorMessage = "Cep não informado.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Formato inválido de CEP. Use apenas dígitos.")]
        public string Zipcode { get; set; } = null!;

        [Required(ErrorMessage = "Rua não informado.")]
        public string Street { get; set; } = null!;

        [Required(ErrorMessage = "Número de endereço não informado.")]
        public string Number { get; set; } = null!;

        public string? Complement { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bairro não informado.")]
        public string Neighborhood { get; set; } = null!;

        [Required(ErrorMessage = "Cidade não informado.")]
        [RegularExpression(@"^[a-zA-Zçãõáéíóúàèìòùâêîôûäëïöüñ\s]*$", ErrorMessage = "Campo cidade só pode ter letras.")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Rua não informado.")]
        [StringLength(2, ErrorMessage = "Campo rua deve possuir ao menos 2 letras.")]
        [RegularExpression(@"^[a-zA-Z]*$", ErrorMessage = "Campo Rua só pode ter letras.")]
        public string State { get; set; } = null!;
    }

}
