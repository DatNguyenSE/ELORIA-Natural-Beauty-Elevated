import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ProductService } from '../../../core/services/product-service';
import { CategoryService } from '../../../core/services/category-service';
import { ToastService } from '../../../core/services/toast-service';
import { Product } from '../../../shared/models/product.model';

@Component({
  selector: 'app-product-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-management.html',
  styleUrl: './product-management.css',
})
export class ProductManagement implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private toast = inject(ToastService);

  products = this.productService.products;
  categories = this.categoryService.categories;

  searchKeyword = signal<string>('');
  selectedCategoryFilter = signal<number | 'all'>('all');

  isEditing: boolean = false;
  isLoading: boolean = false;
  selectedFile: File | null = null;
  imagePreview: string | ArrayBuffer | null = null;

  newProduct = this.getEmptyProduct();

  getEmptyProduct() {
    return {
      id: 0,
      name: '',
      volume: '400ml',
      description: '',
      brand: 'ELORIA',
      price: 239000,
      stock: 100,
      categoryId: 1,
      discount: 0,
      isNew: true,
      label: 'Bán chạy nhất',
      imageUrl: '',
    };
  }

  // Danh sách sản phẩm sau lọc
  filteredProducts = computed(() => {
    const kw = this.searchKeyword().toLowerCase().trim();
    const catId = this.selectedCategoryFilter();
    let list = this.products();

    if (catId !== 'all') {
      list = list.filter((p) => p.categoryId === Number(catId));
    }

    if (kw) {
      list = list.filter(
        (p) =>
          p.name?.toLowerCase().includes(kw) ||
          p.description?.toLowerCase().includes(kw) ||
          p.brand?.toLowerCase().includes(kw)
      );
    }
    return list;
  });

  ngOnInit(): void {
    this.loadProducts();
    this.categoryService.getCategories();
  }

  loadProducts() {
    this.productService.getProducts();
  }

  // Lấy dung tích hiển thị từ sản phẩm
  getProductVolume(product: Product): string {
    if (product.productSizes && product.productSizes.length > 0) {
      return product.productSizes[0].sizeName || 'Tiêu chuẩn';
    }
    return '400ml';
  }

  // Lấy tồn kho của sản phẩm
  getProductStock(product: Product): number {
    if (product.productSizes && product.productSizes.length > 0) {
      return product.productSizes.reduce((sum, s) => sum + (s.quantity || 0), 0);
    }
    return product.quantity || 0;
  }

  // Giá sau giảm giá
  getCalculatedPrice(): number {
    const price = Number(this.newProduct.price) || 0;
    const discount = Number(this.newProduct.discount) || 0;
    if (discount <= 0) return price;
    return price * (1 - discount / 100);
  }

  onEdit(product: Product) {
    this.isEditing = true;
    const stock = this.getProductStock(product);
    const volume = this.getProductVolume(product);

    this.newProduct = {
      id: product.id,
      name: product.name,
      volume: volume,
      description: product.description || '',
      brand: product.brand || 'ELORIA',
      price: product.price,
      stock: stock,
      categoryId: product.categoryId || 1,
      discount: product.discount || 0,
      isNew: product.isNew ?? true,
      label: product.label || '',
      imageUrl: product.imageUrl || '',
    };

    this.imagePreview = product.imageUrl || null;
    this.selectedFile = null;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onCancelEdit() {
    this.isEditing = false;
    this.resetForm();
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = (e) => (this.imagePreview = reader.result);
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    // 1. Kiểm tra trường bắt buộc
    if (!this.newProduct.name || !this.newProduct.name.trim()) {
      this.toast.error('Vui lòng nhập tên sản phẩm.');
      return;
    }

    if (!this.newProduct.price || this.newProduct.price <= 0) {
      this.toast.error('Vui lòng nhập giá bán hợp lệ (> 0 VNĐ).');
      return;
    }

    if (this.newProduct.stock === null || this.newProduct.stock === undefined || this.newProduct.stock < 0) {
      this.toast.error('Vui lòng nhập số lượng tồn kho (≥ 0).');
      return;
    }

    // Tự động chuẩn hóa dung tích và số lượng thành productSizes để tương thích với backend hiện tại
    const volumeLabel = this.newProduct.volume?.trim() || '400ml';
    const stockQty = Number(this.newProduct.stock) || 0;

    const payload: any = {
      id: this.newProduct.id,
      name: this.newProduct.name.trim(),
      brand: this.newProduct.brand || 'ELORIA',
      price: Number(this.newProduct.price),
      categoryId: Number(this.newProduct.categoryId) || 1,
      description: this.newProduct.description?.trim() || '',
      discount: Number(this.newProduct.discount) || 0,
      isNew: !!this.newProduct.isNew,
      label: this.newProduct.label?.trim() || null,
      productSizes: [
        {
          sizeName: volumeLabel,
          quantity: stockQty,
        },
      ],
    };

    this.isLoading = true;

    if (this.isEditing) {
      this.productService
        .updateProduct(this.newProduct.id, payload, this.selectedFile)
        .pipe(finalize(() => (this.isLoading = false)))
        .subscribe({
          next: () => {
            this.toast.success(`Đã cập nhật sản phẩm "${this.newProduct.name}" thành công!`);
            this.loadProducts();
            this.onCancelEdit();
          },
          error: (err) => {
            console.error(err);
            this.toast.error('Lỗi cập nhật sản phẩm. Vui lòng thử lại.');
          },
        });
    } else {
      this.productService
        .addProduct(payload, this.selectedFile)
        .pipe(finalize(() => (this.isLoading = false)))
        .subscribe({
          next: () => {
            this.toast.success(`Đã thêm sản phẩm "${this.newProduct.name}" thành công!`);
            this.loadProducts();
            this.resetForm();
          },
          error: (err) => {
            console.error(err);
            this.toast.error('Lỗi khi thêm sản phẩm. Vui lòng kiểm tra lại thông tin.');
          },
        });
    }
  }

  onDelete(id: number, name: string) {
    if (!confirm(`Bạn có chắc chắn muốn xóa sản phẩm "${name}" khỏi hệ thống?`)) {
      return;
    }

    this.isLoading = true;
    this.productService
      .deleteProduct(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toast.success(`Đã xóa sản phẩm "${name}".`);
          this.loadProducts();
        },
        error: (err) => {
          console.error(err);
          this.toast.error('Không thể xóa sản phẩm này.');
        },
      });
  }

  resetForm() {
    this.newProduct = this.getEmptyProduct();
    this.selectedFile = null;
    this.imagePreview = null;
    this.isEditing = false;
  }
}