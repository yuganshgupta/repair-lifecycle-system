using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Contracts.Users;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;
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

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.Users
            .SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users
            .SingleOrDefaultAsync(user => user.UserId == userId);
    }

    public async Task UpdateAsync(User user)
    {
        await _context.SaveChangesAsync();
    }

    public Task<bool> IsActiveInRoleAsync(int userId, UserRole role, CancellationToken ct)
    {
        // Query the database, not the tracked entity cache or JWT role alone.
        return _context.Users.AnyAsync(
            user => user.UserId == userId && user.IsActive && user.Role == role, ct);
    }

    public Task<bool> HasOpenComplaintsAsync(int userId, UserRole role)
    {
        var open = _context.Complaints.Where(c =>
            c.Status != ComplaintStatus.Completed && c.Status != ComplaintStatus.Cancelled);

        return role switch
        {
            UserRole.Customer => open.AnyAsync(c => c.CustomerId == userId),
            UserRole.Technician => open.AnyAsync(c => c.AssignedTechnicianId == userId),
            _ => Task.FromResult(false)
        };
    }

    public async Task<T> ExecuteAccountChangeAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch (SqlException ex) when (ex.Number is 1205 or 2601 or 2627)
        {
            throw new AccountChangeConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
            { Number: 1205 or 2601 or 2627 })
        {
            throw new AccountChangeConflictException(ex);
        }
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        return await _context.Users.CountAsync(user =>
            user.Role == UserRole.Admin && user.IsActive);
    }

}
