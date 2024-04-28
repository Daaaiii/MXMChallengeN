using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MxmChallenge.Data;
using MxmChallenge.Models;
using MxmChallenge.Repositories.Interfaces;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;

namespace MxmChallenge.Repositories
{
    public class UserRepository(ApplicationDbContext context, IMapper mapper, IHashService hashService) : IUserRepository
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IHashService _hashService = hashService;




        public async Task<UserDetailsDTO> CreateUser(CreateUserDTO createUserDTO)
        {

            var existingEmail = await _context.Users.AnyAsync(
                 u => u.Email == createUserDTO.Email
             );
            if (existingEmail)
            {
                throw new ArgumentException("O email já está em uso.");
            }

            var existingCPF = await _context.Users.AnyAsync(u => u.cpf_cnpj == createUserDTO.cpf_cnpj);
            if (existingCPF)
            {
                throw new ArgumentException("O CPF já está em uso.");
            }
            var user = _mapper.Map<User>(createUserDTO);
            string hashedPassword = _hashService.HashPassword(createUserDTO.Password);

            user.Password = hashedPassword;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _mapper.Map<UserDetailsDTO>(user);
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }


            _context.Users.Remove(user);
            await context.SaveChangesAsync();

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
            return _mapper.Map<UserDetailsDTO>(user);
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
            return _mapper.Map<UserDetailsDTO>(user);
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
            return _mapper.Map<UserDetailsDTO>(user);
        }

        public async Task<List<UserDetailsDTO>> GetUsers()
        {
            var users = await _context
                .Users.Include(u => u.Address)
                .ToListAsync();

            return _mapper.Map<List<UserDetailsDTO>>(users);
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

            _mapper.Map(updateUserDTO, existingUser);
            existingUser.Password = _hashService.HashPassword(updateUserDTO.Password);
            existingUser.UpdatedAt = DateTime.Now;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            var userDetailsDTO = _mapper.Map<UserDetailsDTO>(existingUser);

            return userDetailsDTO;
        }
    }
}
