import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Order } from '../../shared/models/order.model';

@Injectable({
  providedIn: 'root',
})
export class OrderService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  myOrder = signal<Order[]>([]);

  addOrder(CouponCode: string, paymentMethod: number) {
    // Sử dụng HttpParams để đưa dữ liệu lên URL
    const params = new HttpParams()
      .set('CouponCode', CouponCode || '')
      .set('paymentMethod', paymentMethod.toString());

    return this.http.post<Order>(`${this.apiUrl}order/add`, null, { params });
  }

  getOrderDetail(orderId: number) {
    return this.http.get<Order>(`${this.apiUrl}order/${orderId}/details`);
  }

  getUserOrders() {
    return this.http.get<Order[]>(`${this.apiUrl}order/user-orders`).subscribe({
      next: (res) => {
        this.myOrder.set(res);
      },
      error: (err) => {
        console.error('Lỗi lấy orders:', err);
      }
    });
  }

  cancelOrder(orderId: number) {
    return this.http.delete(`${this.apiUrl}order/${orderId}/cancel`);
  }
}