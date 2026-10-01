using System;
using SportZone.Application.Interfaces.IRepositories;
using SportZone.Domain.Enums;
using SportZone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;

namespace SportZone.Infrastructure.Repositories;

public class OrderRepository(AppDbContext _context) : GenericRepository<Order>(_context), IOrderRepository
{
    public async Task<Order?> GetOrderWithDetailsAsync(int id, string userId)
    {
        return await _context.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.ProductSizes)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
    }

    public async Task<IEnumerable<Order?>> GetListOrderWithPaymentAsync(string userId, PaymentStatus paymentStatus)
    {
        return await _context.Orders
        .AsNoTracking()
        .Include(o => o.Payment)
        .Where(o => o.UserId == userId && o.Payment != null && o.Payment.PaymentStatus == paymentStatus)
        .ToListAsync();
    }

    public async Task<Order?> GetOrderWithPaymentAsync(int orderId)
    {
        return await _context.Orders
        .Include(o => o.Payment).FirstOrDefaultAsync(x => x.Id == orderId);

    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Order?> GetOrderForBackgroundCancelAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items) // BẮT BUỘC phải có dòng này
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

}
