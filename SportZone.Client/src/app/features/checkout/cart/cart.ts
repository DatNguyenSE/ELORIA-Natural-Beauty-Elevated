import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../../core/services/cart-service';
import { CartItem } from '../../../shared/models/cart.model';
import { debounceTime, of, Subject, Subscription, switchMap } from 'rxjs';
import { PromotionService } from '../../../core/services/promotion-service';
import { Promotion } from '../../../shared/models/promotion.model';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast-service';
import { OrderService } from '../../../core/services/order-service';


@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cart.html',
  styleUrls: ['./cart.css'],
})
export class Cart implements OnInit, OnDestroy {
  /** Expose Math cho template (tính % tiến độ freeship) */
  readonly Math = Math;
  /** Phương thức thanh toán hiển thị trong tóm tắt đơn hàng */
  readonly paymentMethods = ['VISA', 'MoMo', 'VNPay', 'COD'];

  private cartService = inject(CartService);
  private promotionService = inject(PromotionService);
  private toast = inject(ToastService);
  private router = inject(Router);


  private updateQuantitySubject = new Subject<{ productId: number, quantity: number, sizeName: string }>();
  private updateSubscription!: Subscription;

  protected cartItems = signal<CartItem[]>([]);
  protected promotions = this.promotionService.promotions


  discount = signal<number>(0); 


  // --- COMPUTED SIGNALS (Tự động tính toán khi cartItems thay đổi) ---
  totalItems = computed(() =>
    this.cartItems().reduce((sum, item) => sum + item.quantity, 0
    )
  );


  // 2. Tổng tiền hàng trước thuế và phí vận chuyển
  subTotal = computed(() =>
  this.cartItems().reduce((total, item) => {

    const price = item.product?.price || 0;
    const discountPercent = item.product?.discount || 0;

    // Giá gốc * (1 - %giảm/100)
    const discountedUnitPrice = price * (1 - discountPercent / 100);

    //  Cộng dồn vào tổng (Giá đã giảm * Số lượng)
    return total + (discountedUnitPrice * item.quantity);
  }, 0)
);

  // 3. Thuế (Giả sử 8% trên subTotal - discount)
  // taxRate = 0.03; // 3% thuế VAT
  // taxAmount = computed(() => {
  //   const amount = (this.subTotal() - this.discount()) * this.taxRae;
  //   return amount > 0 ? amount : 0;
  // });


  // 4. Tổng thanh toán cuối cùng
  shippingFee = 0; // Miễn phí

  finalTotal = computed(() =>
    this.subTotal() + this.shippingFee - this.discount() //+ this.taxAmount()
  );


  getCartItems() {
    this.cartService.getCartItems().subscribe({
      next: (res) => {
        // Kiểm tra an toàn null/undefined
        this.cartItems.set(res.items || []);
      },
      error: (err) => {
        console.error('Lỗi khi lấy giỏ hàng:', err);
      }
    });
  }


  ngOnInit() {
    this.getCartItems();
    this.promotionService.getPromotions();
    this.setupDebounceUpdate();
  }

  ngOnDestroy() {
    // Hủy đăng ký khi component bị đóng để tránh rò rỉ bộ nhớ
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
    }
  }


  // 1. Quản lý trạng thái đóng/mở danh sách khuyến mãi
  showAllPromotions = signal<boolean>(false);

  // 2. Lấy cái đầu tiên (Featured)
  firstPromotion = computed(() => {
    const apps = this.promotions().filter(p => p.isVisible);
    return apps.length > 0 ? apps[0] : null;
  });

  // 3. Lấy danh sách còn lại (từ phần tử thứ 2 trở đi)
  remainingPromotions = computed(() => {
    const apps = this.promotions().filter(p => p.isVisible);
    return apps.slice(1);
  });

  // Hàm toggle trạng thái
  togglePromotions() {
    this.showAllPromotions.update(val => !val);
  }


  // Lưu trữ mã giảm giá người dùng nhập
  couponCodeInput = signal<string>('');
  applyCoupon() {
    const code = this.couponCodeInput().trim();
    const finalTotal = this.finalTotal();
    if (code) {
      console.log('Đang áp dụng mã:', code);
      this.promotionService.validateCoupon(code, finalTotal).subscribe({
        next: (res) => {
          console.log(`Mã hợp lệ! Giảm ${res} VND`);
          console.log(`Order value is ${finalTotal} VND`);
          if (res === null || res <= 0) {
            this.toast.error('Mã giảm giá không hợp lệ hoặc không áp dụng được.');
            this.discount.set(0);
            return;
          }
          this.discount.set(res || 0);

        },
        error: (err) => {
          console.error('Mã không hợp lệ hoặc lỗi:', err);
          alert('Mã giảm giá không hợp lệ hoặc đã hết hạn.');
        }
      });
    }


  }


  // --- CẤU HÌNH DEBOUNCE ---
  setupDebounceUpdate() {
    this.updateSubscription = this.updateQuantitySubject.pipe(
      debounceTime(800),
      switchMap(data => {
        // Gọi API cập nhật
        console.log(`update cho SP ${data.productId} với SL ${data.quantity} và size ${data.sizeName}`);
        return this.cartService.updateCartItem(data.productId, data.quantity, data.sizeName);
      })
    ).subscribe({
      next: (res) => {
        console.log(' Cập nhật thành công trên server');
      },
      error: (err) => {
        console.error('Lỗi cập nhật:', err);
        this.getCartItems();
        alert('Có lỗi xảy ra khi cập nhật số lượng!');
      }
    });
  }

  // --- HÀM UPDATE QUANTITY ---
  updateQuantity(item: CartItem, value: any) {
    let newQty = Number(value);
    if (!newQty || newQty < 1) newQty = 1;

    this.cartItems.update(items =>
      items.map(i => i.productId === item.productId ? { ...i, quantity: newQty } : i)
    );

    // 2. Đẩy yêu cầu vào "dòng suối" để chờ xử lý (Debounce)
    this.updateQuantitySubject.next({
      productId: item.productId,
      quantity: newQty,
      sizeName: item.sizeName
    });
  }

  // Xóa sản phẩm
  removeItem(productId: number, sizeName: string) {

    this.cartService.removeFromCart(productId, sizeName).subscribe({
      next: () => {
        console.log(` Đã xóa SP ${productId} thành công trên server`);
      },
      error: (err) => {
        console.error(' Lỗi xóa sản phẩm:', err);
        alert('Có lỗi xảy ra khi xóa sản phẩm!');
        return;
      }
    });

    this.cartItems.update(items => items.filter(i => i.productId !== productId));


  }


 onContinueInfoOrder() {
  if (this.cartItems().length === 0) {
    this.toast.error("Giỏ hàng hiện đang trống.");
    return;
  }

  const code = this.couponCodeInput().trim();
  // SỬA Ở ĐÂY: Dùng subTotal thay vì finalTotal
  const baseTotal = this.subTotal(); 

  const couponCheck$ = code
    ? this.promotionService.validateCoupon(code, baseTotal)
    : of(0);

  couponCheck$.subscribe({
    next: (res) => {
      let finalDiscount = 0;

      if (code) {
        // Kiểm tra res khác null và > 0
        if (res !== null && res > 0) {
          finalDiscount = res;
          this.discount.set(res);
          this.toast.success(`Đã áp dụng mã giảm giá: ${res.toLocaleString()} VND`);
        } else {
          // Nếu res là null, set discount về 0
          this.toast.warning("Mã giảm giá không còn thỏa mãn điều kiện.");
          this.discount.set(0);
          finalDiscount = 0;
        }
      }
      

      const calculatedFinalTotal = baseTotal  - finalDiscount //+ this.shippingFee;
      this.navigateToProcess(code, finalDiscount, calculatedFinalTotal);
    },
    error: err => {
      this.toast.error("Mã không hợp lệ hoặc đã hết hạn.");
      this.discount.set(0);
      this.navigateToProcess('', 0, baseTotal + this.shippingFee);
    }
  });
}

// Cập nhật lại hàm navigate để nhận giá trị chính xác
private navigateToProcess(code: string, discountAmount: number, calculatedFinal: number) {
  this.router.navigate(['/order-processing'], {
    state: {
      discount: discountAmount,
      couponCode: code,
      subTotal: this.subTotal(),
      finalTotal: calculatedFinal, // Dùng giá trị đã tính toán để tránh lệch Signal
      totalItems: this.totalItems()
    }
  });
}

}