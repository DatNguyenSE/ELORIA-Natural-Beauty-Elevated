import { Component, inject, OnInit, signal } from '@angular/core';
import { AccountService } from '../../../core/services/account-service';
import { UserProfile } from '../../../shared/models/user';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order-service';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Order } from '../../../shared/models/order.model';
import { CartService } from '../../../core/services/cart-service';
import { CartItem } from '../../../shared/models/cart.model';
import { ToastService } from '../../../core/services/toast-service';
import { PaymentService } from '../../../core/services/payment-service';
import { PaymentInput } from '../../../shared/models/payment.model';

@Component({
  selector: 'app-order-processing',
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './order-processing.html',
  styleUrl: './order-processing.css',
})
export class OrderProcessing implements OnInit {
  protected accountService = inject(AccountService);
  protected cartService = inject(CartService);
  protected orderService = inject(OrderService);
  protected paymentService = inject(PaymentService)
  private router = inject(Router);
  private toast = inject(ToastService);






  protected profile = signal<UserProfile | null>(null);
  protected cartItems = signal<CartItem[]>([]);
  protected orderId: number = 0;

  isPaymentVisible: boolean = false;
  selectedPaymentMethod: number = 0;

  discount = this.router.getCurrentNavigation()?.extras.state?.['discount'] || '';
  couponCode = this.router.getCurrentNavigation()?.extras.state?.['couponCode'] || '';
  // taxAmount = this.router.getCurrentNavigation()?.extras.state?.['taxAmount'] || '';
  subTotal = this.router.getCurrentNavigation()?.extras.state?.['subTotal'] || '';
  finalTotal = this.router.getCurrentNavigation()?.extras.state?.['finalTotal'] || '';
  totalItems = this.router.getCurrentNavigation()?.extras.state?.['totalItems'] || '';



  ngOnInit(): void {
    this.getProfile();
    this.getCartItems();
    if (this.totalItems === '') {
      this.router.navigate(['/cart'])
    }
    // this.getItemOrder();
    if (this.profile()?.address === null || this.profile()?.phoneNumber === null || this.profile()?.fullName === null) {
      this.router.navigate(['/my-account/profile']);
      this.toast.error("Hãy cập nhật thông tin nhận hàng.")
    }

  }


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

  getProfile() {
    this.accountService.getProfile().subscribe({
      next: (response) => {
        console.log('User Profile:', response);
        this.profile.set(response);
      },
      error: (err) => console.error('Lỗi gọi API:', err)
    });
  }

  // getItemOrder() {
  //   return this.orderService.getOrderDetail(this.id).subscribe({
  //     next: (response) => {
  //       this.orderDetail.set(response);
  //       console.log('Order Detail:', response);
  //     },
  //     error: (err) => console.error('Lỗi gọi API:', err)
  //   });
  // }

  showPaymentSection() {
    this.isPaymentVisible = true;
    setTimeout(() => {
      document.getElementById('payment-section')?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    }, 150);
  }



  placeOrder() {
    const code = this.couponCode.trim();
    const paymentMethod = this.selectedPaymentMethod;

    if (!paymentMethod) {
      this.toast.warning("Vui lòng chọn phương thức thanh toán.");
      return;
    }

    //Tạo đơn hàng trước
    this.orderService.addOrder(code, paymentMethod).subscribe({
      next: (order) => {
        console.log('Đơn hàng đã được tạo:', order);
        this.orderId = order.id;


        if (paymentMethod === 2) {
          this.handleOnlinePayment(order.id);
        } else if (paymentMethod === 3) {
          this.toast.success("Đặt hàng thành công! Đang xử lý thanh toán thẻ...");
          this.router.navigate(['/checkout-success', order.id]);
        } else {
          this.toast.success("Đặt hàng thành công! Phương thức COD.");
          this.router.navigate(['/checkout-success', order.id]);
        }
      },
      error: (err) => {
        this.toast.error("Không thể tạo đơn hàng. Vui lòng thử lại.");
        console.error(err);
      }
    });
  }

  private handleOnlinePayment(orderId: number) {
    // Tạo payload chuẩn
    const paymentPayload: PaymentInput = {
      orderId: orderId,
      description: `ELORIA - Don hang #${orderId} - User ${this.profile()?.id}`
    };

    this.paymentService.processPayment(paymentPayload).subscribe({
      next: res => {
        if (res.url) {
          console.log("link laf" + res.url)
          window.location.href = res.url;
        }
      },
      error: (err) => {
        this.toast.error("Hệ thống thanh toán đang gặp sự cố.");
        console.error("Payment Error:", err);
      }
    });
  }
}
