using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Domain.Entities;
using RepairSystem.Infrastructure.Data;

namespace RepairSystem.Infrastructure.Repositories.Users;

public class UserRepository : IUserRepository
{
    // this keeps database context same so we can access users table and save changes to db
    private readonly RepairDbContext _context;


    public UserRepository(RepairDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(
            User => User.Email == email);
    }

    // add new user and save changes to db
    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);

        await _context.SaveChangesAsync();
    }



}
