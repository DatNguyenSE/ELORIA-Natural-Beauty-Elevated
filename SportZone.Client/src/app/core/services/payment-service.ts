import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { PaymentInput } from '../../shared/models/payment.model';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  processPayment(paymenInput: PaymentInput) {
    return this.http.post<{ url: string }>(`${this.apiUrl}payments/create-payment-url`, paymenInput);
  }
}
