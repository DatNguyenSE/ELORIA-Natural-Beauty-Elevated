using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Repositories;

public class MembersRepository(AppDbContext _context) : GenericRepository<AppUser>(_context), IMembersRepository
{
    private const string AdminRoleId = "admin-id"; // Thay bằng ID thực tế của Role Admin
    public async Task<IEnumerable<AppUser>> GetAllMembersAsync()
    {
        // Lấy danh sách UserId của những người có Role là Admin
        var adminUserIds = await _context.UserRoles
            .Where(ur => ur.RoleId == AdminRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        // Trả về những User không nằm trong danh sách adminUserIds
        return await _context.Users
            .Where(u => !adminUserIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AppUser?> GetMemberWithOrdersAsync(string id)
    {
        // Kiểm tra xem ID này có phải Admin không
        var isAdmin = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == id && ur.RoleId == AdminRoleId);

        if (isAdmin) return null; // Nếu là admin thì không trả về dữ liệu

        return await _context.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Product)
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.ProductSize)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<AppUser?> GetByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddPointsAsync(string id, int points) // vi gửi lên cho payment nên làm thêm add cho gọn
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user != null)
        {
            user.Points = (user.Points ?? 0) + points;
        }
    }
    public async Task<bool> SubtractPointsAsync(string id, int pointsToSubtract)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return false;

   
        int currentPoints = user.Points ?? 0;
        if (currentPoints < pointsToSubtract) return false;

        // Thực hiện trừ điểm
        user.Points = currentPoints - pointsToSubtract;
        return true;
    }


}