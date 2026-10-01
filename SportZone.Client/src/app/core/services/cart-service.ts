import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Cart } from '../../shared/models/cart.model';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private http = inject(HttpClient);
    private baseUrl = environment.apiUrl;
  
  

  addToCart(productId: number, quantity: number = 1, sizeName: string) {
    return this.http.post(`${this.baseUrl}cart/add`, { productId, quantity, sizeName });
  }

  getCartItems() {
    return this.http.get<Cart>(`${this.baseUrl}cart`);
  }

  removeFromCart(productId: number, sizeName: string) {
    return this.http.delete(`${this.baseUrl}cart/remove/${productId}?sizeName=${sizeName}`);
  }

  clearCart() {
    return this.http.delete(`${this.baseUrl}cart/clear`);
  }

  updateCartItem(productId: number, quantity: number, sizeName: string) : Observable<any> {
    return this.http.post(`${this.baseUrl}cart/update-item`, { productId, quantity, sizeName });
  }
  
}