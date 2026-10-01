import { Component, signal, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Product } from '../../../shared/models/product.model';
import { FeatureService } from '../../../core/services/feature-service';
import { ProductService } from '../../../core/services/product-service';
import { Features, UpdateFeature } from '../../../shared/models/features.model';

@Component({
  selector: 'app-feature-banner-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './feature-banner-management.html',
  styleUrl: './feature-banner-management.css',
})
export class FeatureBannerManagement implements OnInit {
  private featureService = inject(FeatureService);
  private productService = inject(ProductService);

  // Lấy dữ liệu từ Signals trong Service
  allFeatures = this.featureService.allfeatures; 
  allProducts = this.productService.products;    

  searchQuery = signal<string>('');
  isLoading = false;
  isEditing = false;
  imagePreview: string | ArrayBuffer | null = null;
  selectedProductIds: number[] = [];

  // Form Model đồng bộ với Entity Backend
  currentFeature: UpdateFeature = {
    id: 0,
    name: '',
    desc: '',
    isBanner: false,
    imgUrl: '',
    productIds: []
  };

  filteredProducts = computed(() => {
    const query = this.searchQuery().toLowerCase();
    return this.allProducts().filter(p => p.name.toLowerCase().includes(query));
  });

  ngOnInit() {
    this.featureService.getFeaturesWithProducts();
    this.productService.getProducts();
  }

  // --- Logic Xử Lý ---

  onSearchChange(event: any) {
    this.searchQuery.set(event.target.value);
  }

  toggleProductSelection(productId: number) {
    const index = this.selectedProductIds.indexOf(productId);
    if (index > -1) {
      this.selectedProductIds.splice(index, 1);
    } else {
      this.selectedProductIds.push(productId);
    }
  }

  isProductSelected(productId: number): boolean {
    return this.selectedProductIds.includes(productId);
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreview = reader.result;
        this.currentFeature.imgUrl = reader.result as string; // Lưu base64 hoặc xử lý upload tùy API
      };
      reader.readAsDataURL(file);
    }
  }

  onEdit(feature: UpdateFeature) {
    this.isEditing = true;
    this.currentFeature = { ...feature };
    // Lấy mảng ID từ danh sách Products hiện có của Feature
    this.selectedProductIds = feature.productIds || [];
    this.imagePreview = feature.imgUrl as string;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onCancelEdit() {
    this.isEditing = false;
    this.resetForm();
  }

  resetForm() {
    this.currentFeature = { id: 0, name: '', desc: '', isBanner: false, imgUrl: '' };
    this.selectedProductIds = [];
    this.imagePreview = null;
    this.searchQuery.set('');
    this.isLoading = false;
  }

  onSubmit() {
    if (!this.currentFeature.name) return;
    this.isLoading = true;
    const updateId = this.currentFeature.id;
    // Payload gửi về Backend (gồm IDs sản phẩm để xử lý quan hệ N-N)
    const payload : UpdateFeature = {
      ...this.currentFeature,
      productIds: this.selectedProductIds // Gửi dưới dạng list object nếu API yêu cầu
    };

    if (this.isEditing) {
      if (!this.currentFeature.id || this.currentFeature.id === 0) {
      console.error("ID không hợp lệ!");
      return;
    }
      this.featureService.updateFeature(updateId, payload).subscribe({
        next: () => {
          this.featureService.getFeaturesWithProducts();
          this.onCancelEdit();
          console.log('Cập nhật feature thành công với id = ' + updateId + ` và payload: `, payload);
          alert('Cập nhật thành công!');
        },
        error: (err) => {
          console.error(err);
          console.error('Lỗi cập nhật feature, với id = ' + updateId);
          this.isLoading = false;
        }
      });
    } else {
      // Logic Thêm mới (Create)
      // this.featureService.createFeature(payload)...
    }
  }

  onDelete(id: number) {
    if (confirm('Bạn có chắc chắn muốn xóa banner này?')) {
      // this.featureService.deleteFeature(id).subscribe(...)
    }
  }
}