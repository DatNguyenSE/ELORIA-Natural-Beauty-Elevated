import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Order } from '../../../shared/models/order.model';
import { OrderService } from '../../../core/services/order-service';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PaymentInput } from '../../../shared/models/payment.model';
import { AccountService } from '../../../core/services/account-service';
import { PaymentService } from '../../../core/services/payment-service';
import { ToastService } from '../../../core/services/toast-service';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css',
})
export class OrderDetail implements OnInit {
  private orderService = inject(OrderService);
  private accountService = inject(AccountService);
  private paymentService = inject(PaymentService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  protected order = signal<Order | null>(null);
  protected isLoading = signal<boolean>(true);
  protected isProcessingPayment = signal<boolean>(false);

  orderId = toSignal(
    this.route.params.pipe(map(p => +p['orderId'] || 0)),
    { initialValue: 0 }
  );

  ngOnInit(): void {
    this.getOrder();
  }

  getOrder(): void {
    const id = this.orderId();
    if (!id) {
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.orderService.getOrderDetail(id).subscribe({
      next: (res) => {
        this.order.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Không tìm thấy thông tin đơn hàng:', err);
        this.isLoading.set(false);
      }
    });
  }

  handleCancel(): void {
    const id = this.orderId();
    if (!id) return;

    if (confirm(`Quý khách có chắc chắn muốn hủy đơn hàng #${id} không?`)) {
      this.orderService.cancelOrder(id).subscribe({
        next: () => {
          this.toast.success(`Đã hủy thành công đơn hàng #${id}.`);
          this.getOrder();
        },
        error: (err) => {
          this.toast.error('Không thể hủy đơn hàng này hoặc đơn đã được xử lý.');
          console.error('Lỗi khi hủy đơn:', err);
        }
      });
    }
  }

  handlePayment(orderId: number): void {
    if (this.isProcessingPayment()) return;
    this.isProcessingPayment.set(true);

    const currentUser = this.accountService.currentUser();
    const paymentPayload: PaymentInput = {
      orderId: orderId,
      description: `ELORIA - Don hang #${orderId} - User ${currentUser?.id || ''}`
    };

    this.paymentService.processPayment(paymentPayload).subscribe({
      next: (res) => {
        this.isProcessingPayment.set(false);
        if (res?.url) {
          window.location.href = res.url;
        } else {
          this.toast.error('Không thể tạo liên kết thanh toán. Vui lòng thử lại sau.');
        }
      },
      error: (err) => {
        this.isProcessingPayment.set(false);
        this.toast.error('Hệ thống thanh toán đang gặp sự cố. Vui lòng liên hệ hỗ trợ.');
        console.error('Payment Error:', err);
      }
    });
  }
}