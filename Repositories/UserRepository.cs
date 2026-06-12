using Microsoft.EntityFrameworkCore;
using MxmChallenge.Data;
using MxmChallenge.Models;
using MxmChallenge.Repositories.Interfaces;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;

namespace MxmChallenge.Repositories
{
    public class UserRepository(ApplicationDbContext context, IHashService hashService) : IUserRepository
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHashService _hashService = hashService;

        public async Task<UserDetailsDTO> CreateUser(CreateUserDTO createUserDTO)
        {
            var existingEmail = await _context.Users.AnyAsync(u => u.Email == createUserDTO.Email);
            if (existingEmail)
            {
                throw new ArgumentException("O email já está em uso.");
            }

            var existingCPF = await _context.Users.AnyAsync(u => u.cpf_cnpj == createUserDTO.cpf_cnpj);
            if (existingCPF)
            {
                throw new ArgumentException("O CPF já está em uso.");
            }

            var user = MapToUser(createUserDTO);
            user.Password = _hashService.HashPassword(createUserDTO.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToDetails(user);
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserDetailsDTO> GetUserByCPF(string cpf_cnpj)
        {
            var user = await _context
                .Users.Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.cpf_cnpj == cpf_cnpj);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return MapToDetails(user);
        }

        public async Task<UserDetailsDTO> GetUserByEmail(string email)
        {
            var user = await _context
                .Users.Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return MapToDetails(user);
        }

        public async Task<UserDetailsDTO> GetUserById(Guid id)
        {
            var user = await _context
                .Users.Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return MapToDetails(user);
        }

        public async Task<List<UserDetailsDTO>> GetUsers()
        {
            var users = await _context
                .Users.Include(u => u.Address)
                .ToListAsync();

            return users.Select(MapToDetails).ToList();
        }

        public async Task<UserDetailsDTO> UpdateUser(Guid id, UpdateUserDTO updateUserDTO)
        {
            var existingUser = await _context
                .Users.Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (
                existingUser.Email != updateUserDTO.Email
                && await _context.Users.AnyAsync(u => u.Email == updateUserDTO.Email)
            )
            {
                throw new ArgumentException("Email already in use by another user");
            }

            if (
                existingUser.cpf_cnpj != updateUserDTO.cpf_cnpj
                && await _context.Users.AnyAsync(u => u.cpf_cnpj == updateUserDTO.cpf_cnpj)
            )
            {
                throw new ArgumentException("CPF already in use by another user");
            }

            ApplyUpdate(existingUser, updateUserDTO);
            existingUser.Password = _hashService.HashPassword(updateUserDTO.Password);
            existingUser.UpdatedAt = DateTime.Now;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            return MapToDetails(existingUser);
        }

        private static User MapToUser(CreateUserDTO dto)
        {
            return new User
            {
                Fullname = dto.Fullname,
                Email = dto.Email,
                DDD = dto.DDD,
                PhoneNumber = dto.PhoneNumber,
                cpf_cnpj = dto.cpf_cnpj,
                Address = new Address
                {
                    Zipcode = dto.Zipcode,
                    Street = dto.Street,
                    Number = dto.Number,
                    Complement = dto.Complement,
                    Neighborhood = dto.Neighborhood,
                    City = dto.City,
                    State = dto.State
                }
            };
        }

        private static void ApplyUpdate(User user, UpdateUserDTO dto)
        {
            user.Fullname = dto.Fullname;
            user.Email = dto.Email;
            user.DDD = dto.DDD;
            user.PhoneNumber = dto.PhoneNumber;
            user.cpf_cnpj = dto.cpf_cnpj;
            user.Address.Zipcode = dto.Zipcode;
            user.Address.Street = dto.Street;
            user.Address.Number = dto.Number;
            user.Address.Complement = dto.Complement;
            user.Address.Neighborhood = dto.Neighborhood;
            user.Address.City = dto.City;
            user.Address.State = dto.State;
        }

        private static UserDetailsDTO MapToDetails(User user)
        {
            return new UserDetailsDTO
            {
                Id = user.Id,
                Fullname = user.Fullname,
                Email = user.Email,
                DDD = user.DDD,
                PhoneNumber = user.PhoneNumber,
                cpf_cnpj = user.cpf_cnpj,
                Zipcode = user.Address.Zipcode,
                Street = user.Address.Street,
                Number = user.Address.Number,
                Complement = user.Address.Complement,
                Neighborhood = user.Address.Neighborhood,
                City = user.Address.City,
                State = user.Address.State
            };
        }
    }
}
