import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Promotion } from '../../shared/models/promotion.model';

@Injectable({
  providedIn: 'root',
})
export class PromotionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  promotions = signal<Promotion[]>([]);

  getPromotions() {
    return this.http.get<Promotion[]>(`${this.baseUrl}promotions/active-promotions`).subscribe({
      next: (promos) => {
        this.promotions.set(promos);
      },
      error: (err) => {
        console.error('Lỗi khi lấy khuyến mãi:', err);
      }
    });
  }

  getPromotionByCode(code: string) {
    return this.http.get<Promotion>(`${this.baseUrl}promotions/code/${code}`);
  }
  validateCoupon(code: string, orderValue: number) {
    return this.http.get<number>(`${this.baseUrl}promotions/validate/${code}?orderValue=${orderValue}`);
  }
}

