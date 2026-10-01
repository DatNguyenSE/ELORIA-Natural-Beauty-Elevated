import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Product } from '../../shared/models/product.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl
  products = signal<Product[]>([]);


  getProducts() {
    this.http.get<Product[]>(`${this.baseUrl}products`).subscribe({
      next: (res) => {
        this.products.set(res);
      },
      error: (err) => {
        console.error('Lỗi lấy products:', err);
      }
    });

  }
  getProductsByCategoryId(categoryId: number | undefined) {
    return this.http.get<Product[]>(`${this.baseUrl}products/category/` + categoryId)
  }

  getProductsByLabel(label: string) {
    return this.http.get<Product[]>(`${this.baseUrl}products/label-items/` + label);
  }

  getProductsByFeatureId(featureIds: number) {
    return this.http.get<Product[]>(`${this.baseUrl}products/feature-items/` + featureIds);
  }

  getProductsById(productId: number) {
    return this.http.get<Product>(`${this.baseUrl}products/` + productId)
  }


  addProduct(productData: Product, file: File | null) {
    const formData = new FormData();

    // 1. Các trường cơ bản (String & Number)
    formData.append('Name', productData.name);
    formData.append('Price', productData.price.toString());
    formData.append('Brand', productData.brand || '');
    formData.append('Description', productData.description || '');
    formData.append('CategoryId', productData.categoryId.toString());

    // 2. Các trường mới thêm (Discount, IsNew, Featured)
    formData.append('Discount', (productData.discount || 0).toString());
    formData.append('IsNew', (productData.isNew ?? true).toString());// FormData sẽ gửi "true" hoặc "false" dưới dạng string
    if (productData.label) {
      formData.append('Label', productData.label);
    }

    // 3. Xử lý gửi DANH SÁCH SIZE (List<ProductSizeDto>)
    // Backend ASP.NET Core nhận mảng qua FormData theo cú pháp: ProductSizes[0].SizeName
    if (productData.productSizes && productData.productSizes.length > 0) {
      productData.productSizes.forEach((size, index) => {
        formData.append(`ProductSizes[${index}].SizeName`, size.sizeName);
        formData.append(`ProductSizes[${index}].Quantity`, size.quantity.toString());
      });
    }

    // 4. File ảnh
    if (file) {
      formData.append('file', file);
    }

    return this.http.post<Product>(this.baseUrl + 'products/add_image', formData);
  }


  updateProduct(id: number, productData: Product, file: File | null) {
    const formData = new FormData();

    // 1. Thông tin cơ bản (Nên viết hoa chữ cái đầu để khớp hoàn toàn với C# DTO)
    formData.append('Name', productData.name);
    formData.append('Price', (productData.price ?? 0).toString());
    formData.append('Brand', productData.brand || '');
    formData.append('Description', productData.description || '');
    formData.append('CategoryId', (productData.categoryId ?? 0).toString());

    // 2. Các trường bổ sung mới
    formData.append('Discount', (productData.discount ?? 0).toString());
    formData.append('IsNew', (productData.isNew ?? false).toString());
    if (productData.label) {
      formData.append('Label', productData.label);
    }

    // 3. QUAN TRỌNG: Gửi danh sách Sizes
    if (productData.productSizes && productData.productSizes.length > 0) {
      productData.productSizes.forEach((size, index) => {
        // Gửi kèm ID của size (nếu có) để Backend biết là update size cũ hay thêm size mới
        if (size.id) {
          formData.append(`ProductSizes[${index}].Id`, size.id.toString());
          console.log(`Appending existing size with ID: ${size.id}`);
        } else {
          console.log(`Lỗi không lấy được ID của size tại index ${index}, có thể là size mới chưa có ID. Sẽ gửi như thêm mới.`);
        }

        formData.append(`ProductSizes[${index}].SizeName`, size.sizeName);
        formData.append(`ProductSizes[${index}].Quantity`, size.quantity.toString());
      });
    }

    // 4. File ảnh mới (nếu có)
    if (file) {
      formData.append('file', file);
    }

    return this.http.put<Product>(`${this.baseUrl}products/update/${id}`, formData);
  }

  deleteProduct(id: number) {
    return this.http.delete(this.baseUrl + 'products/delete/' + id);
  }
}