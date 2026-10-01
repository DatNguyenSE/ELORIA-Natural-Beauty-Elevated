import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-payment-fail',
  standalone: true,
  imports: [RouterLink], // Import RouterLink để dùng trong HTML
  templateUrl: './payment-fail.html',
  styleUrl: './payment-fail.css',
})
export class PaymentFail implements OnInit {
  private route = inject(ActivatedRoute);
  
  // Signal lưu trữ orderId lấy từ URL
  orderId = signal<string | null>(null);

  ngOnInit(): void {
    // Lấy orderId từ query parameters (?orderId=...)
    const id = this.route.snapshot.queryParamMap.get('orderId');
    this.orderId.set(id);
  }
}