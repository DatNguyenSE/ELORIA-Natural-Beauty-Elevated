import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ProductService } from '../../../core/services/product-service';
import { Product } from '../../../shared/models/product.model';
import { filterCatalog, finalPrice, originalPrice, salePercent } from '../../../shared/utils/catalog';

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
  private location = inject(Location);
  private catalog = signal<Product[]>([]);
  private reload = signal(0);
  routeParam = toSignal(this.route.paramMap.pipe(map(params => params.get('id') ?? 'products')), { initialValue: 'products' });
  isLoading = signal(true);
  error = signal('');
  activeFilterId = signal(0);
  selectedSort = signal('newest');
  finalPrice = finalPrice;
  originalPrice = originalPrice;
  salePercent = salePercent;
  private matchingProducts = computed(() => filterCatalog(this.catalog(), this.routeParam()));
  categoryName = computed(() => {
    const names: Record<string, string> = {
      products: 'TẤT CẢ SẢN PHẨM', all: 'TẤT CẢ SẢN PHẨM', new: 'SẢN PHẨM MỚI',
      combos: 'COMBO TIẾT KIỆM', 'hair-care': 'CHĂM SÓC TÓC', sale: 'ƯU ĐÃI ĐẶC BIỆT',
    };
    return names[this.routeParam()] ?? this.matchingProducts()[0]?.categoryName ?? 'SẢN PHẨM';
  });
  subCategories = computed(() => {
    const categories = new Map<number, { id: number; categoryName: string }>();
    for (const product of this.matchingProducts()) {
      if (product.categoryName) categories.set(product.categoryId, { id: product.categoryId, categoryName: product.categoryName });
    }
    return [...categories.values()];
  });
  products = computed(() => this.matchingProducts().filter(product =>
    this.activeFilterId() === 0 || product.categoryId === this.activeFilterId()));
  sortedProducts = computed(() => [...this.products()].sort((a, b) => {
    switch (this.selectedSort()) {
      case 'priceAsc': return finalPrice(a) - finalPrice(b);
      case 'priceDesc': return finalPrice(b) - finalPrice(a);
      case 'mostSale': return salePercent(b) - salePercent(a);
      case 'oldest': return a.id - b.id;
      default: return b.id - a.id;
    }
  }));
  constructor() {
    effect(onCleanup => {
      this.routeParam();
      this.reload();
      this.activeFilterId.set(0);
      this.catalog.set([]);
      this.error.set('');
      this.isLoading.set(true);
      const request = this.productService.fetchProducts().subscribe({
        next: products => {
          this.catalog.set(products);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('Chưa thể tải sản phẩm. Bạn vui lòng thử lại.');
          this.isLoading.set(false);
        },
      });
      // Prevent stale responses from a previous category replacing the current results.
      onCleanup(() => request.unsubscribe());
    });
  }
  retry() { this.reload.update(value => value + 1); }
  filterBySubCategory(id: number) { this.activeFilterId.set(id); }
  goBack() { this.location.back(); }
  onSortChange(event: Event) { this.selectedSort.set((event.target as HTMLSelectElement).value); }
}
