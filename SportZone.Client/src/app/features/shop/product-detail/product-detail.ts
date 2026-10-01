import { Component, computed, effect, inject, Input, signal } from '@angular/core';
import { Product } from '../../../shared/models/product.model';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { ProductService } from '../../../core/services/product-service';
import { ToastService } from '../../../core/services/toast-service';
import { CartService } from '../../../core/services/cart-service';
import { AccountService } from '../../../core/services/account-service';



@Component({
  selector: 'app-product-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css',
})

export class ProductDetail {
  private route = inject(ActivatedRoute);
  private accountService = inject(AccountService)
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private toast = inject(ToastService);
  private router = inject(Router)
  product = signal<Product | null>(null);
  productRefer = signal<Product[] | null>(null);

  productId = toSignal(
    this.route.params.pipe(map(p => +p['id'] || 0)),
    { initialValue: 0 }
  );

  constructor() {
    effect(() => {
      const id = this.productId();
      if (id === 0) return;

      this.productService.getProductsById(id).subscribe({
        next: (res) => {
          this.product.set(res);
          // Call product by category to set ref
          this.productService.getProductsByCategoryId(this.product()?.categoryId).subscribe({
            next: res => this.productRefer.set(res)
          });

          // Tự động gán dung tích mặc định cho sản phẩm
          const defaultSize = res.volume || (res.productSizes && res.productSizes.length > 0 ? res.productSizes[0].sizeName : 'Tiêu chuẩn');
          this.selectedSize.set(defaultSize);
        },
        error: (err) => console.error(err)
      });
    });
  }

  // State quản lý bằng Signal
  quantity = signal(1);
  selectedSize = signal<string | null>(null);
  isDescriptionOpen = signal(true);

  selectSize(sizeLabel: string) {
    this.selectedSize.set(sizeLabel);
    this.quantity.set(1);
  }

  // Tự động tính toán số lượng tồn kho dựa trên size/dung tích được chọn
  stockForSelectedSize = computed(() => {
    const product = this.product();
    if (!product) return 0;
    const sizeName = this.selectedSize();
    const sizeInfo = product.productSizes?.find(s => s.sizeName === sizeName);
    return sizeInfo ? sizeInfo.quantity : ((product.stock ?? 0) > 0 ? product.stock! : 99);
  });

  updateQuantity(amount: number) {
    const currentQty = this.quantity();
    const maxQty = this.stockForSelectedSize() || 99;
    const newQty = currentQty + amount;

    if (newQty < 1) return;

    if (newQty > maxQty) {
      this.toast.error(`Rất tiếc, sản phẩm chỉ còn ${maxQty} trong kho!`);
      return;
    }

    this.quantity.set(newQty);
  }

  addToCart() {
    if (this.accountService.currentUser() === null) {
      this.toast.error('Vui lòng đăng nhập để mua hàng!');
      return;
    }
    const product = this.product();
    if (!product) return;

    const size = this.selectedSize() || product.volume || (product.productSizes?.[0]?.sizeName) || 'Tiêu chuẩn';

    this.cartService.addToCart(product.id, this.quantity(), size).subscribe({
      next: () => {
        this.toast.success(`Đã thêm "${product.name}" vào giỏ hàng!`, 3500, product.imageUrl, '/cart');
      },
      error: (err) => {
        console.error(err);
        this.toast.error('Đã có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng.');
      }
    });
  }

  toggleFavorite() {
    console.log('Toggled favorite');
  }



  toggleDescription() {
    this.isDescriptionOpen.update(v => !v);
  }
}