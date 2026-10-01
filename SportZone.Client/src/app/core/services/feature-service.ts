import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Features, UpdateFeature } from '../../shared/models/features.model';
import { Observable } from 'rxjs';
import { Product } from '../../shared/models/product.model';

@Injectable({
  providedIn: 'root',
})
export class FeatureService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl
  featuresBanner = signal<Features[]>([]);
  allfeatures = signal<Features[]>([]);

  getFeatures() {
    return this.http.get(`${this.baseUrl}features`).subscribe({
      next: (res) => {
        this.allfeatures.set(res as Features[]);
      },
      error: (err) => {
        console.error('Lỗi lấy features:', err);
      }
    });
  }

  getFeaturesWithProducts() {
    return this.http.get(`${this.baseUrl}features/with-products`).subscribe({
      next: (res) => {
        this.allfeatures.set(res as Features[]);
      },
      error: (err) => {
        console.error('Lỗi lấy features:', err);
      }
    });
  }

  getFeatureById(featureId: number) {
    return this.http.get<Features>(`${this.baseUrl}features/` + featureId);
  }

  getProductByListFeatureIds(featureIds: number[]): Observable<Product[]> {
    let params = new HttpParams();

    if (featureIds && featureIds.length > 0) {
      featureIds.forEach(id => {
        params = params.append('featureIds', id.toString());
      });
    }

    return this.http.get<Product[]>(`${this.baseUrl}features/feature-items`, { params });
  }



  getBannerFeatures() {
    return this.http.get(`${this.baseUrl}features/feature-banners`).subscribe({
      next: (res) => {
        this.featuresBanner.set(res as Features[]);
      },
      error: (err) => {
        console.error('Lỗi lấy features:', err);
      }
    });
  }

 
updateFeature(id: number, data: UpdateFeature): Observable<any> {
  return this.http.put(`${this.baseUrl}features/${id}/update`, data);
}
}
