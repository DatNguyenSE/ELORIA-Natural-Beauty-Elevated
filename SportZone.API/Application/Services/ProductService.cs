using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using SportZone.Domain.Entities;

namespace SportZone.Application.Services
{
    public class ProductService(IUnitOfWork uow, IMapper mapper, IPhotoService photoService) : IProductService
    {
        public async Task<ProductDto> AddAsync(CreateProductDto createDto)
        {
            var errors = new List<string>();

            var isDuplicate = await uow.ProductRepository.AnyAsync(p => p.Name == createDto.Name);
            if (isDuplicate) errors.Add("Product name already exists.");

            if (createDto.Price < 0) errors.Add("Price must be greater than or equal to 0.");

            if (errors.Count > 0)
            {
                throw new BadRequestException("Validation failed.", errors.ToArray());
            }

            var entity = mapper.Map<Product>(createDto);

            await uow.ProductRepository.AddAsync(entity);
            await uow.Complete();
            return mapper.Map<ProductDto>(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var result = await uow.ProductRepository.ChangeStatusProduct(id); // soft delete
            if (!result)
            {
                throw new NotFoundException($"Product with id {id} not found.");
            }
            await uow.Complete();
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await uow.ProductRepository.GetAllProductsDetailAsync();
            return mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var entity = await uow.ProductRepository.GetProductByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Product with id {id} not found.");
            }
            return mapper.Map<ProductDto>(entity);
        }

        public async Task<IEnumerable<ProductDto>> GetListByCategoryIdAsync(int categoryId)
        {
            // Kiểm tra Category có tồn tại không trước (Optional - Good UX)
            // var categoryExists = await uow.CategoryRepository.ExistsAsync(categoryId);
            // if (!categoryExists) throw new NotFoundException($"Category {categoryId} not found.");
            var entities = await uow.ProductRepository.GetListByCategoryIdAsync(categoryId);
            return mapper.Map<IEnumerable<ProductDto>>(entities);
        }

        public async Task<IEnumerable<ProductDto>> GetListByFeatureIdAsync(int featureId)
        {
            var products = await uow.ProductRepository.GetListByFeatureIdAsync(featureId);
            if (products == null)
            {
                throw new BadRequestException($"No products found with the specified feature.");
            }
            return mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<IEnumerable<ProductDto>> GetListByLabelAsync(string label)
        {
            var products = await uow.ProductRepository.GetListByLabelAsync(label);
            if (products == null)
            {
                throw new BadRequestException($"Product with label: {label} not found");
            }
            return mapper.Map<IEnumerable<ProductDto>>(products);

        }



        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto productDto, IFormFile? file)
        {
            var product = await uow.ProductRepository.GetProductByIdAsync(id);
            if (product == null) throw new NotFoundException($"Product with id {id} not found.");

            mapper.Map(productDto, product);

            // 3. Xử lý ảnh (Giữ nguyên logic của bạn)
            if (file != null)
            {
                if (!string.IsNullOrEmpty(product.PublicId)) await photoService.DeletePhotoAsync(product.PublicId);
                var uploadResult = await photoService.AddPhotoAsync(file);
                if (uploadResult.Error != null) throw new Exception(uploadResult.Error);
                product.ImageUrl = uploadResult.Url;
                product.PublicId = uploadResult.PublicId;
            }

            // 4. Xử lý logic ProductSizes và CartItems
            if (productDto.ProductSizes != null) //dto có thong tin size
            {
                var existingSizes = product.ProductSizes.ToList(); //Danh sách Size trong DB
                var sizeIdsToCleanCart = new List<int>(); // danh sach SizeID cần chỉnh trong user cart


                var SizeIdsInDto = productDto.ProductSizes.Where(s => s.Id != 0).Select(s => s.Id).ToList(); //danh sách SizeID có trong DTO (cả update và thêm mới)
                var cartItems = await uow.CartRepository.GetCartItemsBySizeIdsAsync(SizeIdsInDto); // lấy hết Size mà client gửi để tìm tới giỏ hàng cần chỉnh sửa số lượng
                int quantityInAllCart = cartItems.Sum(ci => ci.Quantity); // tổng số lượng của Size đó trong tất cả giỏ hàng

                // A. Cập nhật hoặc Thêm mới
                foreach (var dtoSize in productDto.ProductSizes) // duyet hết size trong DTO 
                {
                    
                    var sizeInDb = existingSizes.FirstOrDefault(s =>
                        (dtoSize.Id != 0 && s.Id == dtoSize.Id) ||
                        ( s.SizeName == dtoSize.SizeName));

                    if (sizeInDb != null)// nếu tìm thấy -> cập nhật
                    {
                        sizeInDb.Quantity = dtoSize.Quantity;
                        sizeInDb.IsActive = true;

                        // nếu số lượng size sau khi cập nhật <=0 hoặc tổng số lượng size đó trong tất cả giỏ hàng lớn hơn số lượng mới cập nhật -> Thêm vào danh sách dọn dẹp giỏ hàng
                        if (sizeInDb.Quantity <= 0 || quantityInAllCart > dtoSize.Quantity) sizeIdsToCleanCart.Add(sizeInDb.Id);
                    }
                    else
                    {
                        var newSize = mapper.Map<ProductSize>(dtoSize);
                        newSize.ProductId = id;
                        newSize.IsActive = true;
                        product.ProductSizes.Add(newSize);
                    }
                }

                // B. Xử lý Soft Delete (Ẩn size)
                var dtoSizeIds = productDto.ProductSizes.Select(s => s.Id).ToList(); //lấy danh sách SizeID từ DTO 
                var sizesToDisable = existingSizes.Where(s => s.Id != 0 && !dtoSizeIds.Contains(s.Id)).ToList();
                // so sanh với Db xem sizeId nào không có trong dto mà có trong Db thì chuyển Size trong Db IsActive = false 

                foreach (var size in sizesToDisable)
                {
                    size.IsActive = false;
                    size.Quantity = 0;

                    sizeIdsToCleanCart.Add(size.Id); // Thêm vào danh sách dọn dẹp giỏ hàng
                }

                // C. THỰC HIỆN XÓA KHỎI GIỎ HÀNG
                if (sizeIdsToCleanCart.Any())
                {
                    // Tìm tất cả CartItems liên quan đến các SizeId này
                    var cartItemsToRemove = await uow.CartRepository
                        .GetCartItemsBySizeIdsAsync(sizeIdsToCleanCart);

                    foreach (var cartItem in cartItemsToRemove)
                    {
                        uow.CartRepository.RemoveCartItem(cartItem);
                    }
                }
            }

            // 5. Lưu tất cả thay đổi trong 1 Transaction duy nhất
            uow.ProductRepository.Update(product);
            await uow.Complete();

            return mapper.Map<ProductDto>(product);
        }
    }
}
