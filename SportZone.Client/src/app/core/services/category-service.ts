import { inject, Injectable, signal } from '@angular/core';
import { Category } from '../../shared/models/category.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  categories = signal<Category[]>([]); 


  getCategories() {
    this.http.get<Category[]>(`${this.baseUrl}categories`).subscribe({
      next: (res) => {
        this.categories.set(res);
      },
      error: (err) => {
        console.error('Lỗi lấy danh mục:', err);
      }
    });
  }
  
  getCategoryById(categoryId: number) {
    return this.http.get<Category>(`${this.baseUrl}categories/` + categoryId);
  }

  createCategory(category: { categoryName: string; description?: string; imageUrl?: string }) {
    return this.http.post<Category>(`${this.baseUrl}categories`, category).pipe(
      tap((newCat) => {
        if (newCat && newCat.id) {
          this.categories.update((current) => [...current, newCat]);
        } else {
          this.getCategories();
        }
      })
    );
  }

  deleteCategory(categoryId: number) {
    return this.http.delete(`${this.baseUrl}categories/${categoryId}`).pipe(
      tap(() => {
        this.categories.update((current) => current.filter((c) => c.id !== categoryId));
      })
    );
  }
}
