import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { ProductService } from '../../../core/services/product-service';
import { CategoryService } from '../../../core/services/category-service';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, lastValueFrom, map } from 'rxjs';
import { CommonModule } from '@angular/common';
import { Category } from '../../../shared/models/category.model';
import { Product } from '../../../shared/models/product.model';
import { Location } from '@angular/common';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService); // Inject thêm service category

  private readonly PRODUCTS_SLUG = 'products';
  private readonly ALL_SLUG = 'all';
  private readonly NEW_SLUG = 'new';
  private readonly SALE_SLUG = 'sale';
  private readonly COMBOS_SLUG = 'combos';
  private readonly HAIRCARE_SLUG = 'hair-care';

  isLoading = signal<boolean>(false);

  // Lấy ID từ URL
  routeParam = toSignal(
    this.route.paramMap.pipe(map(params => params.get('id'))), // Lấy chuỗi raw
    { initialValue: null }
  );

  products = signal<Product[]>([]);
  private allMergedProducts = signal<any[]>([]);
  subCategories = signal<Category[]>([]);
  activeFilterId = signal<number>(0);

  categoryName = signal<string>('Tất cả sản phẩm');

  constructor() {
    effect(() => {
      const param = this.routeParam();
      if (!param) return;

      // Reset state
      this.products.set([]);
      this.allMergedProducts.set([]);
      this.subCategories.set([]);
      this.activeFilterId.set(0);

      if (param === this.PRODUCTS_SLUG || param === this.ALL_SLUG) {
        this.categoryName.set('TẤT CẢ SẢN PHẨM');
        this.loadAllProducts();
      }
      else if (param === this.NEW_SLUG) {
        this.categoryName.set('SẢN PHẨM MỚI');
        this.loadAllProducts('new');
      }
      else if (param === this.SALE_SLUG) {
        this.categoryName.set('ƯU ĐÃI ĐẶC BIỆT');
        this.loadAllProducts('sale');
      }
      else if (param === this.COMBOS_SLUG) {
        this.categoryName.set('COMBO TIẾT KIỆM');
        this.loadAllProducts();
      }
      else if (param === this.HAIRCARE_SLUG) {
        this.categoryName.set('CHĂM SÓC TÓC');
        this.loadAllProducts();
      }
      else {
        const id = Number(param);
        if (!isNaN(id) && id > 0) {
          this.categoryService.getCategoryById(id).subscribe({
            next: cate => this.categoryName.set(cate?.categoryName || 'Sản phẩm'),
            error: () => this.categoryName.set('Sản phẩm')
          });
          this.productService.getProductsByCategoryId(id).subscribe({
            next: data => {
              this.products.set(data);
              this.allMergedProducts.set(data);
            },
            error: () => this.loadAllProducts()
          });
        } else {
          this.categoryName.set('SẢN PHẨM');
          this.loadAllProducts();
        }
      }
    }, { allowSignalWrites: true });
  }

  loadAllProducts(filterMode: 'none' | 'new' | 'sale' = 'none') {
    this.isLoading.set(true);
    this.productService.getProducts();
    const all = this.productService.products();
    let result = all;
    if (filterMode === 'new') {
      result = all.filter(p => p.isNew);
    } else if (filterMode === 'sale') {
      result = all.filter(p => p.discount && p.discount > 0);
    }
    this.products.set(result);
    this.allMergedProducts.set(result);
    this.isLoading.set(false);
  }


  // --- LOGIC XỬ LÝ GỘP ---

  // Đổi tham số thành filterMode
  async loadMergedData(ids: number[], filterMode: 'none' | 'new' | 'sale' = 'none') {
    try {
      this.isLoading.set(true);

      const categoryRequests = ids.map(id => this.categoryService.getCategoryById(id));
      const productRequests = ids.map(id => this.productService.getProductsByCategoryId(id));

      const [categories, productsLists] = await lastValueFrom(
        forkJoin([
          forkJoin(categoryRequests),
          forkJoin(productRequests)
        ])
      );

      this.subCategories.set(categories);

      let merged: any[] = [];
      productsLists.forEach((list, index) => {
        if (list && list.length > 0) {

          let filteredList = list;

          // XỬ LÝ LỌC THEO FILTER MODE TẠI ĐÂY
          if (filterMode === 'new') {
            // Lọc hàng mới
            filteredList = filteredList.filter((item: Product) => item.isNew === true);
          } else if (filterMode === 'sale') {
            // Lọc hàng giảm giá (Giả sử model Product của bạn có trường discount > 0)
            filteredList = filteredList.filter((item: Product) => item.discount && item.discount > 0);
          }

          const categoryIdOfThisList = ids[index];
          const mappedList = filteredList.map((item: Product) => ({
            ...item,
            localCategoryId: categoryIdOfThisList
          }));
          merged = merged.concat(mappedList);
        }
      });

      this.allMergedProducts.set(merged);
      this.products.set(merged);

    } catch (error) {
      console.error('Lỗi tải dữ liệu gộp:', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  // --- HÀM LỌC (Gắn vào nút bấm html) ---
  filterBySubCategory(subId: number) {
    this.activeFilterId.set(subId);

    // Nếu id = 0 thì hiện tất cả (lấy từ kho gốc)
    if (subId === 0) {
      this.products.set(this.allMergedProducts());
    } else {
      // Lọc từ kho gốc theo ID hoặc localCategoryId ta đã gắn ở trên
      const filtered = this.allMergedProducts().filter(p =>
        (p.categoryId === subId) || (p.localCategoryId === subId)
      );
      this.products.set(filtered);
    }

  }

  //RETURN TO PREVIOUS ACTION
  private location = inject(Location);
  goBack(): void {
    this.location.back(); // Quay lại hành động/trang trước đó trong lịch sử trình duyệt
  }

  // 1. Thêm Signal lưu trạng thái bộ lọc đang chọn
  selectedSort = signal<string>('newest');

  // 2. Hàm bắt sự kiện khi người dùng đổi select box
  onSortChange(event: Event) {
    const selectElement = event.target as HTMLSelectElement;
    this.selectedSort.set(selectElement.value);
  }

  // 3. Tạo một Computed Signal để render ra HTML
  sortedProducts = computed(() => {
    // Tạo một bản sao của mảng để không làm biến đổi (mutate) mảng gốc
    const currentProducts = [...this.products()];
    const sortType = this.selectedSort();

    // Hàm tính giá thực tế sau khi trừ % discount
    const getFinalPrice = (p: Product) => p.price * (1 - (p.discount ?? 0) / 100);

    switch (sortType) {
      case 'priceAsc':
        // Giá tăng dần
        return currentProducts.sort((a, b) => getFinalPrice(a) - getFinalPrice(b));
      case 'priceDesc':
        // Giá giảm dần
        return currentProducts.sort((a, b) => getFinalPrice(b) - getFinalPrice(a));
      case 'mostSale':
        // Sale nhiều nhất (% discount cao nhất xếp trước)
        return currentProducts.sort((a, b) => (b.discount ?? 0) - (a.discount ?? 0));
      case 'oldest':
        // Cũ nhất (ID nhỏ xếp trước)
        return currentProducts.sort((a, b) => a.id - b.id);
      case 'newest':
      default:
        // Mới nhất (ID lớn xếp trước - Mặc định)
        return currentProducts.sort((a, b) => b.id - a.id);
    }
  });

}