import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-checkout-success',
  standalone: true,
  imports: [RouterLink], // Thêm RouterLink để các nút bấm hoạt động
  templateUrl: './checkout-success.html',
  styleUrl: './checkout-success.css',
})
export class CheckoutSuccess implements OnInit {
  private route = inject(ActivatedRoute);
  
  // Signal lưu trữ mã đơn hàng
  orderId = signal<string>('');

  ngOnInit(): void {
    // Lấy 'id' từ Path Parameter (/checkout-success/:id)
    const idFromUrl = this.route.snapshot.paramMap.get('orderId') || this.route.snapshot.queryParamMap.get('orderId');
    
    if (idFromUrl) {
      this.orderId.set(idFromUrl);
    } else {
      this.orderId.set('ELO' + Math.floor(Math.random() * 1000000));
    }
  }
}