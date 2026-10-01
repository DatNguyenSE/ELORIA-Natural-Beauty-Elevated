using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportZone.Application.Dtos;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Data;

public class Seed
{
    // Helper to find file safely in different execution environments
    private static string? FindDataFile(string filename)
    {
        var baseDir = AppContext.BaseDirectory;
        var curDir = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(baseDir, filename),
            Path.Combine(baseDir, "Infrastructure", "Data", filename),
            Path.Combine(baseDir, "Data", filename),
            Path.Combine(curDir, "Infrastructure", "Data", filename),
            Path.Combine(curDir, "..", "Infrastructure", "Data", filename),
            Path.Combine(curDir, "SportZone.API", "Infrastructure", "Data", filename)
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    // ── 1. SEED THỂ LOẠI (CATEGORIES) ──
    public static async Task SeedCategories(AppDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var eloriaCategories = new List<Category>
        {
            new() { Id = 1, CategoryName = "Dầu Gội", Description = "Làm sạch dịu nhẹ da đầu, giảm gãy rụng và nuôi dưỡng chân tóc chắc khỏe từ thảo mộc thiên nhiên." },
            new() { Id = 2, CategoryName = "Dầu Xả", Description = "Cấp ẩm chuyên sâu, phục hồi độ suôn mượt tự nhiên và bảo vệ ngọn tóc khỏi khô xơ chẻ ngọn." },
            new() { Id = 3, CategoryName = "Tẩy Tế Bào Chết Da Đầu", Description = "Thanh lọc nang tóc, loại bỏ bã nhờn, gàu ngứa tích tụ lâu ngày và kích thích tuần hoàn máu." },
            new() { Id = 4, CategoryName = "Ủ Tóc Chuyên Sâu", Description = "Mặt nạ phục hồi liên kết tóc cấp tốc cho tóc uốn, nhuộm, tẩy và hư tổn nặng." },
            new() { Id = 5, CategoryName = "Xịt Dưỡng Tóc", Description = "Tinh chất nuôi dưỡng chân tóc, kích thích mọc tóc con và ngăn ngừa rụng tóc hiệu quả." },
            new() { Id = 6, CategoryName = "Combo Khởi Đầu", Description = "Bộ đôi gội xả tinh khiết cơ bản dành cho người mới bắt đầu chu trình dưỡng tóc tự nhiên." },
            new() { Id = 7, CategoryName = "Combo Phục Hồi Mềm Mượt", Description = "Liệu trình phục hồi chuyên sâu 3 bước (Gội + Xả + Ủ) cho tóc khô xơ, hư tổn." },
            new() { Id = 8, CategoryName = "Combo Bóng Mượt Hằng Ngày", Description = "Bộ chăm sóc nuôi dưỡng suôn mượt và thơm mát cả ngày (Gội + Xả + Xịt dưỡng)." },
            new() { Id = 9, CategoryName = "Combo Da Đầu Khỏe", Description = "Liệu trình làm sạch sâu nang tóc, kiềm dầu và kích mọc tóc (Scrub + Gội + Xịt dưỡng)." },
            new() { Id = 10, CategoryName = "Combo Chăm Sóc Toàn Diện", Description = "Chu trình nuôi dưỡng 5 bước cao cấp chuẩn salon tại nhà của ELORIA." }
        };

        // Thử đọc từ JSON nếu có file tùy chỉnh, nếu không dùng danh sách mẫu
        var filePath = FindDataFile("CategorySeedData.json");
        if (filePath != null)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var loaded = JsonSerializer.Deserialize<List<Category>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (loaded != null && loaded.Count > 0) eloriaCategories = loaded;
            }
            catch { /* fallback to in-memory */ }
        }

        foreach (var cat in eloriaCategories)
        {
            context.Categories.Add(cat);
        }
        await context.SaveChangesAsync();
    }

    // ── 2. SEED SẢN PHẨM (PRODUCTS: 5 ĐƠN + 5 COMBOS) ──
    public static async Task SeedProducts(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var eloriaProducts = new List<Product>
        {
            // === 5 SẢN PHẨM ĐƠN ===
            new()
            {
                Id = 1,
                Name = "Dầu Gội Phục Hồi Sinh Học ELORIA",
                Brand = "ELORIA",
                Price = 260000,
                CompareAtPrice = 290000,
                Volume = "400ml",
                Stock = 120,
                ProductType = "single",
                CategoryId = 1,
                ImageUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?q=80&w=800&auto=format&fit=crop",
                Description = "Dầu gội phục hồi từ tinh chất bưởi rừng, gừng tươi và bồ kết thuần khiết. Làm sạch dịu nhẹ da đầu, giảm rụng tóc và nuôi dưỡng chân tóc chắc khỏe.",
                Benefits = "Sạch sâu dịu nhẹ, giảm gãy rụng, kích thích mọc tóc tự nhiên",
                Usage = "Làm ướt tóc, lấy lượng vừa đủ massage nhẹ nhàng da đầu 2-3 phút rồi xả sạch với nước.",
                Ingredients = "Chiết xuất vỏ bưởi da xanh, bồ kết nướng, hà thủ ô đỏ, tinh dầu tràm trà, vitamin B5.",
                AccentColor = "#8A9A7B",
                Label = "Flagship",
                IsNew = true,
                Discount = 10
            },
            new()
            {
                Id = 2,
                Name = "Dầu Xả Dưỡng Ẩm Thảo Mộc ELORIA",
                Brand = "ELORIA",
                Price = 240000,
                CompareAtPrice = 270000,
                Volume = "300ml",
                Stock = 100,
                ProductType = "single",
                CategoryId = 2,
                ImageUrl = "https://images.unsplash.com/photo-1608248597359-2ff903328eb9?q=80&w=800&auto=format&fit=crop",
                Description = "Dầu xả dưỡng ẩm sâu từ dầu argan và bơ hạt mỡ tự nhiên, giúp gỡ rối tức thì, cho mái tóc mềm mại suôn mượt bồng bềnh mà không gây bết dính.",
                Benefits = "Cấp ẩm chuyên sâu, suôn mượt diệu kỳ, bảo vệ ngọn tóc khỏi khô xơ",
                Usage = "Sau khi gội, thoa đều dầu xả từ thân đến ngọn tóc (cách da đầu 3cm), để 2-3 phút rồi xả sạch.",
                Ingredients = "Dầu argan ép lạnh, bơ hạt mỡ hữu cơ, chiết xuất nha đam, protein lúa mì thủy phân.",
                AccentColor = "#E8D9C0",
                Label = "Bán chạy nhất",
                IsNew = false,
                Discount = 0
            },
            new()
            {
                Id = 3,
                Name = "Tẩy Tế Bào Chết Da Đầu Muối Hồng ELORIA",
                Brand = "ELORIA",
                Price = 280000,
                CompareAtPrice = 310000,
                Volume = "250g",
                Stock = 80,
                ProductType = "single",
                CategoryId = 3,
                ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?q=80&w=800&auto=format&fit=crop",
                Description = "Muối khoáng hồng Himalaya kết hợp tinh dầu hương thảo, thanh lọc bụi bẩn, bã nhờn và tế bào chết tích tụ lâu ngày, mang lại da đầu thông thoáng nhẹ tênh.",
                Benefits = "Thanh lọc nang tóc, loại bỏ gàu ngứa bã nhờn, kích thích tuần hoàn máu dưới da đầu",
                Usage = "Làm ẩm tóc, chia từng đường chân tóc thoa nhẹ scrub, massage tròn 3 phút rồi xả thật sạch trước khi dùng dầu gội.",
                Ingredients = "Muối hồng Himalaya tinh khiết, chiết xuất hương thảo, tràm trà, dầu jojoba, AHA tự nhiên.",
                AccentColor = "#B9A9CF",
                Label = "Độc quyền",
                IsNew = true,
                Discount = 0
            },
            new()
            {
                Id = 4,
                Name = "Kem Ủ Tóc Phục Hồi Chuyên Sâu ELORIA",
                Brand = "ELORIA",
                Price = 310000,
                CompareAtPrice = 350000,
                Volume = "200ml",
                Stock = 90,
                ProductType = "single",
                CategoryId = 4,
                ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?q=80&w=800&auto=format&fit=crop",
                Description = "Mặt nạ ủ tóc phục hồi cấp tốc cho tóc nhuộm, uốn và hư tổn nặng. Bổ sung keratin thực vật và collagen nuôi dưỡng từng sợi tóc bóng khỏe đàn hồi.",
                Benefits = "Tái tạo liên kết tóc hư tổn, phục hồi độ đàn hồi, ngăn ngừa chẻ ngọn",
                Usage = "Sau khi gội, vắt ráo bớt nước, thoa đều kem ủ từ thân đến ngọn tóc. Quấn khăn ủ 10-15 phút rồi xả sạch với nước mát.",
                Ingredients = "Keratin thực vật thủy phân, collagen thực vật, chiết xuất hạt chia, dầu hạt chanh dây.",
                AccentColor = "#E3B7AE",
                Label = "Mới ra mắt",
                IsNew = true,
                Discount = 0
            },
            new()
            {
                Id = 5,
                Name = "Xịt Dưỡng Kích Mọc & Dày Tóc ELORIA",
                Brand = "ELORIA",
                Price = 220000,
                CompareAtPrice = 250000,
                Volume = "150ml",
                Stock = 150,
                ProductType = "single",
                CategoryId = 5,
                ImageUrl = "https://images.unsplash.com/photo-1608248597359-2ff903328eb9?q=80&w=800&auto=format&fit=crop",
                Description = "Tinh chất xịt dưỡng chân tóc từ tinh dầu bưởi cô đặc và rễ hoàng cầm, nuôi dưỡng nang tóc, giảm rụng tóc rõ rệt sau 2 tuần sử dụng.",
                Benefits = "Kích thích mọc tóc con, tăng mật độ tóc dày dặn, không gây bết dính chân tóc",
                Usage = "Xịt trực tiếp vào da đầu khi tóc khô hoặc sau khi sấy se se, dùng đầu ngón tay massage nhẹ nhàng để dưỡng chất thẩm thấu.",
                Ingredients = "Tinh dầu vỏ bưởi chưng cất, chiết xuất rễ hoàng cầm, nhân sâm núi, peptide sinh học, kẽm PCA.",
                AccentColor = "#D9A441",
                Label = "Yêu thích",
                IsNew = false,
                Discount = 0
            },

            // === 5 SẢN PHẨM COMBOS ===
            new()
            {
                Id = 6,
                Name = "Combo Khởi Đầu Tinh Khiết (Gội + Xả)",
                Brand = "ELORIA",
                Price = 450000,
                CompareAtPrice = 500000,
                Volume = "Bộ 2 chai (400ml + 300ml)",
                Stock = 50,
                ProductType = "combo",
                CategoryId = 6,
                ImageUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?q=80&w=800&auto=format&fit=crop",
                Description = "Bộ đôi gội xả bưởi & argan nuôi dưỡng cơ bản, làm sạch dịu nhẹ và giữ nếp suôn mượt cho mái tóc mỗi ngày. Tiết kiệm 50.000₫ so với mua lẻ.",
                Benefits = "Sạch sâu da đầu, suôn mượt tự nhiên, giảm gãy rụng",
                Usage = "Bước 1: Làm sạch cùng Dầu Gội Sinh Học. Bước 2: Dưỡng ẩm mềm mượt cùng Dầu Xả Thảo Mộc.",
                Ingredients = "Chiết xuất vỏ bưởi, bồ kết, dầu argan, bơ hạt mỡ, vitamin B5.",
                AccentColor = "#8A9A7B",
                Label = "Tiết kiệm 50K",
                IsNew = true,
                Discount = 10
            },
            new()
            {
                Id = 7,
                Name = "Combo Phục Hồi Mềm Mượt (Gội + Xả + Ủ Tóc)",
                Brand = "ELORIA",
                Price = 710000,
                CompareAtPrice = 810000,
                Volume = "Bộ 3 sản phẩm chuyên sâu",
                Stock = 40,
                ProductType = "combo",
                CategoryId = 7,
                ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?q=80&w=800&auto=format&fit=crop",
                Description = "Liệu pháp phục hồi cấp tốc 3 bước chuyên sâu cho mái tóc khô xơ, chẻ ngọn và hư tổn do hóa chất. Tiết kiệm 100.000₫ so với mua lẻ.",
                Benefits = "Tái tạo cấu trúc tóc hư tổn, cấp ẩm sâu, tóc chắc khỏe mềm mại",
                Usage = "Gội sạch -> Dưỡng xả hàng ngày -> Ủ tóc chuyên sâu 1-2 lần/tuần.",
                Ingredients = "Keratin thực vật, collagen, bồ kết nướng, dầu argan ép lạnh.",
                AccentColor = "#E3B7AE",
                Label = "Tiết kiệm 100K",
                IsNew = true,
                Discount = 12
            },
            new()
            {
                Id = 8,
                Name = "Combo Bóng Mượt Hằng Ngày (Gội + Xả + Xịt Dưỡng)",
                Brand = "ELORIA",
                Price = 630000,
                CompareAtPrice = 720000,
                Volume = "Bộ 3 sản phẩm nuôi dưỡng",
                Stock = 45,
                ProductType = "combo",
                CategoryId = 8,
                ImageUrl = "https://images.unsplash.com/photo-1608248597359-2ff903328eb9?q=80&w=800&auto=format&fit=crop",
                Description = "Bộ ba nuôi dưỡng nang tóc và tạo hiệu ứng bóng mượt rạng rỡ từ gốc đến ngọn suốt 24h. Tiết kiệm 90.000₫ so với mua lẻ.",
                Benefits = "Dưỡng tóc suôn mượt, lưu hương bưởi tự nhiên, kích thích mọc tóc",
                Usage = "Gội xả sạch sẽ -> Thấm ráo tóc -> Xịt dưỡng trực tiếp vào chân tóc và massage nhẹ.",
                Ingredients = "Vỏ bưởi chưng cất, dầu argan, hoàng cầm, protein lúa mì.",
                AccentColor = "#D9A441",
                Label = "Tiết kiệm 90K",
                IsNew = false,
                Discount = 12
            },
            new()
            {
                Id = 9,
                Name = "Combo Da Đầu Khỏe (Scrub + Gội + Xịt Dưỡng)",
                Brand = "ELORIA",
                Price = 660000,
                CompareAtPrice = 760000,
                Volume = "Bộ 3 sản phẩm thanh lọc da đầu",
                Stock = 35,
                ProductType = "combo",
                CategoryId = 9,
                ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?q=80&w=800&auto=format&fit=crop",
                Description = "Giải pháp thanh lọc nang tóc, kiềm dầu, sạch gàu ngứa và đánh thức nang tóc ngủ quên. Tiết kiệm 100.000₫ so với mua lẻ.",
                Benefits = "Làm sạch sâu da đầu, loại bỏ bã nhờn tích tụ, kích mọc tóc con dày mượt",
                Usage = "Tẩy tế bào chết da đầu (1-2 lần/tuần) -> Gội sạch -> Xịt dưỡng chân tóc hàng ngày.",
                Ingredients = "Muối hồng Himalaya, tràm trà, vỏ bưởi rừng, gừng tươi, kẽm PCA.",
                AccentColor = "#B9A9CF",
                Label = "Tiết kiệm 100K",
                IsNew = true,
                Discount = 13
            },
            new()
            {
                Id = 10,
                Name = "Combo Chăm Sóc Toàn Diện (Full 5 Món)",
                Brand = "ELORIA",
                Price = 1090000,
                CompareAtPrice = 1310000,
                Volume = "Trọn bộ 5 sản phẩm cao cấp",
                Stock = 30,
                ProductType = "combo",
                CategoryId = 10,
                ImageUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?q=80&w=800&auto=format&fit=crop",
                Description = "Bộ sản phẩm toàn diện nhất của ELORIA: Làm sạch, tẩy tế bào chết, xả mượt, ủ chuyên sâu và kích mọc tóc. Tiết kiệm 220.000₫ so với mua lẻ.",
                Benefits = "Liệu trình chăm sóc tóc & da đầu trọn vẹn chuẩn Salon thảo mộc tại nhà",
                Usage = "Thực hiện theo chu trình 5 bước toàn diện của chuyên gia ELORIA.",
                Ingredients = "Bồ kết nướng, vỏ bưởi da xanh, muối hồng, keratin, argan, hoàng cầm.",
                AccentColor = "#8A9A7B",
                Label = "Tiết kiệm 220K",
                IsNew = true,
                Discount = 17
            }
        };

        foreach (var p in eloriaProducts)
        {
            context.Products.Add(p);
        }
        await context.SaveChangesAsync();
    }

    // ── 3. SEED PRODUCT SIZES (DUNG TÍCH & TỒN KHO TƯƠNG THÍCH) ──
    public static async Task SeedProductSizes(AppDbContext context)
    {
        if (await context.ProductSizes.AnyAsync()) return;

        var products = await context.Products.ToListAsync();
        if (products.Count == 0) return;

        foreach (var p in products)
        {
            context.ProductSizes.Add(new ProductSize
            {
                ProductId = p.Id,
                SizeName = p.Volume ?? "Tiêu chuẩn",
                Quantity = p.Stock > 0 ? p.Stock : 100,
                IsActive = true
            });
        }
        await context.SaveChangesAsync();
    }

    // ── 4. SEED COMBO ITEMS (LIÊN KẾT SẢN PHẨM THÀNH PHẦN CHO 5 COMBOS) ──
    public static async Task SeedComboItems(AppDbContext context)
    {
        if (await context.ComboItems.AnyAsync()) return;

        var comboItems = new List<ComboItem>
        {
            // C1 (Id 6): Gội (1) + Xả (2)
            new() { ComboProductId = 6, ComponentProductId = 1, Quantity = 1 },
            new() { ComboProductId = 6, ComponentProductId = 2, Quantity = 1 },

            // C2 (Id 7): Gội (1) + Xả (2) + Ủ tóc (4)
            new() { ComboProductId = 7, ComponentProductId = 1, Quantity = 1 },
            new() { ComboProductId = 7, ComponentProductId = 2, Quantity = 1 },
            new() { ComboProductId = 7, ComponentProductId = 4, Quantity = 1 },

            // C3 (Id 8): Gội (1) + Xả (2) + Xịt dưỡng (5)
            new() { ComboProductId = 8, ComponentProductId = 1, Quantity = 1 },
            new() { ComboProductId = 8, ComponentProductId = 2, Quantity = 1 },
            new() { ComboProductId = 8, ComponentProductId = 5, Quantity = 1 },

            // C4 (Id 9): Tẩy da chết (3) + Gội (1) + Xịt dưỡng (5)
            new() { ComboProductId = 9, ComponentProductId = 3, Quantity = 1 },
            new() { ComboProductId = 9, ComponentProductId = 1, Quantity = 1 },
            new() { ComboProductId = 9, ComponentProductId = 5, Quantity = 1 },

            // C5 (Id 10): Gội (1) + Xả (2) + Tẩy da chết (3) + Ủ tóc (4) + Xịt dưỡng (5)
            new() { ComboProductId = 10, ComponentProductId = 1, Quantity = 1 },
            new() { ComboProductId = 10, ComponentProductId = 2, Quantity = 1 },
            new() { ComboProductId = 10, ComponentProductId = 3, Quantity = 1 },
            new() { ComboProductId = 10, ComponentProductId = 4, Quantity = 1 },
            new() { ComboProductId = 10, ComponentProductId = 5, Quantity = 1 }
        };

        foreach (var item in comboItems)
        {
            context.ComboItems.Add(item);
        }
        await context.SaveChangesAsync();
    }
}
