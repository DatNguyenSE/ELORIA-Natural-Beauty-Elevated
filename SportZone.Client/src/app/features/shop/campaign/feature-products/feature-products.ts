import { Component, inject, OnInit, signal } from '@angular/core';
import { ProductService } from '../../../../core/services/product-service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Features } from '../../../../shared/models/features.model';
import { Product } from '../../../../shared/models/product.model';
import { ToastService } from '../../../../core/services/toast-service';
import { FeatureService } from '../../../../core/services/feature-service';
@Component({
  selector: 'app-feature-products',
  standalone: true, // Thêm nếu bạn dùng standalone
  imports: [CommonModule, RouterLink],
  templateUrl: './feature-products.html',
  styleUrl: './feature-products.css',
})
export class FeatureProducts implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private featureService = inject(FeatureService);
  private router = inject(Router);
  private toast = inject(ToastService);

  featureProducts = signal<Product[]>([]);
  featureDetail = signal<Features | null>(null);

  ngOnInit(): void {
    // const navigation = this.router.getCurrentNavigation();
    // const state = navigation?.extras.state as { featureIds: number[] };

    const featureId = Number(this.route.snapshot.paramMap.get('id'));
    if (featureId) {
      this.getProductByfeatures(featureId);
      this.getFeatureById(featureId);

    }

  }

  getFeatureById(featureId: number) {
    this.featureService.getFeatureById(featureId).subscribe({
      next: (res) => {
        this.featureDetail.set(res);
      },
      error: (err) => {
        console.error(err);
      }
    })
  }
  getProductByfeatures(featureId: number) {
    this.productService.getProductsByFeatureId(featureId).subscribe({
      next: (res: Product[]) => {
        this.featureProducts.set(res);

      }
      , error: (err) => {
        this.toast.error("Máy chủ đang gặp sự cố, không thể lấy sản phẩm theo đặc tính. Vui lòng thử lại sau.");
        console.error(err);
      }
    })
  }
}