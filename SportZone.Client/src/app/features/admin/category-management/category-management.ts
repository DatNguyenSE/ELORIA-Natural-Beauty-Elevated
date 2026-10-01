import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../../core/services/category-service';
import { ToastService } from '../../../core/services/toast-service';
import { Category } from '../../../shared/models/category.model';

@Component({
  selector: 'app-category-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-management.html',
  styleUrl: './category-management.css',
})
export class CategoryManagement implements OnInit {
  private categoryService = inject(CategoryService);
  private toast = inject(ToastService);

  categories = this.categoryService.categories;
  searchKeyword = signal<string>('');
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);

  newCategory: { categoryName: string; description: string; imageUrl: string } = {
    categoryName: '',
    description: '',
    imageUrl: '',
  };

  filteredCategories = computed(() => {
    const kw = this.searchKeyword().toLowerCase().trim();
    const list = this.categories();
    if (!kw) return list;
    return list.filter(
      (c) =>
        c.categoryName?.toLowerCase().includes(kw) ||
        c.description?.toLowerCase().includes(kw)
    );
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories() {
    this.isLoading.set(true);
    this.categoryService.getCategories();
    this.isLoading.set(false);
  }

  onSubmit() {
    if (!this.newCategory.categoryName.trim()) {
      this.toast.error('Vui lòng nhập tên thể loại.');
      return;
    }

    this.isSubmitting.set(true);
    this.categoryService
      .createCategory({
        categoryName: this.newCategory.categoryName.trim(),
        description: this.newCategory.description.trim() || undefined,
        imageUrl: this.newCategory.imageUrl.trim() || undefined,
      })
      .subscribe({
        next: (created) => {
          this.toast.success(`Đã thêm thể loại "${this.newCategory.categoryName}" thành công!`);
          this.newCategory = { categoryName: '', description: '', imageUrl: '' };
          this.isSubmitting.set(false);
          this.categoryService.getCategories();
        },
        error: (err) => {
          console.error(err);
          this.toast.error('Không thể thêm thể loại. Vui lòng thử lại.');
          this.isSubmitting.set(false);
        },
      });
  }

  onDelete(cat: Category) {
    if (!confirm(`Bạn có chắc chắn muốn xóa thể loại "${cat.categoryName}"?`)) {
      return;
    }

    this.categoryService.deleteCategory(cat.id).subscribe({
      next: () => {
        this.toast.success(`Đã xóa thể loại "${cat.categoryName}".`);
      },
      error: () => {
        this.toast.error('Không thể xóa thể loại này.');
      },
    });
  }
}
