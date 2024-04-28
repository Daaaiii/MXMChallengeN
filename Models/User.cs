using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MxmChallenge.Models
{
    public class User
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Fullname { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;


        public int DDD { get; set; }
        public int PhoneNumber { get; set; }

        [Required]
        public string cpf_cnpj { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public virtual Address Address { get; set; } = new Address();

    }
}
