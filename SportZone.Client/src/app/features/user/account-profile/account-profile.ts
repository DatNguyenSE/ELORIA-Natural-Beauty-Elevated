import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserProfile } from '../../../shared/models/user';
import { AccountService } from '../../../core/services/account-service';
import { ToastService } from '../../../core/services/toast-service';

@Component({
  selector: 'app-account-profile',
  standalone: true, // Đảm bảo đã có nếu dùng Angular mới
  imports: [CommonModule, FormsModule],
  templateUrl: './account-profile.html',
  styleUrl: './account-profile.css',
})
export class AccountProfile implements OnInit {
  protected accountService = inject(AccountService);
  private toast = inject(ToastService);
  user = signal<UserProfile | null>(this.getSavedUser());

  ngOnInit(): void {
    this.accountService.getProfile().subscribe({
      next: (response) => {
        this.user.set(response);
        this.saveToLocal(response); 
      },
      error: (err) => console.error('Lỗi gọi API:', err)
    });
  }

  // Hàm cập nhật từng trường khi người dùng nhập liệu (Tránh lỗi ngtsc)
  updateField(field: keyof UserProfile, value: any) {
    const current = this.user();
    if (current) {
      this.user.set({ ...current, [field]: value });
    }
  }

  onSave(): void {
    const data = this.user();
    if (!data) return;
        console.log('Cập nhật thành công:', data);
    
    this.accountService.updateProfile(data).subscribe({
      next: (res: any) => {
        // res.user là dữ liệu mới nhất từ Backend (đã qua xử lý UTC)
        this.user.set(res.user);
        this.saveToLocal(res.user);
        this.toast.success('Cập nhật hồ sơ thành công!');
      },
      error: (err) => {
        console.error('Lỗi khi lưu:', err);
        this.toast.error('Lỗi! Kiểm tra định dạng ngày sinh dd/mm/yyyy');
      }
    });
  }

  // Helper: Lưu vào bộ nhớ máy trình duyệt
  private saveToLocal(data: UserProfile): void {
    localStorage.setItem('user_profile', JSON.stringify(data));
  }

  // Helper: Lấy dữ liệu từ bộ nhớ máy
  private getSavedUser(): UserProfile | null {
    const saved = localStorage.getItem('user_profile');
    return saved ? JSON.parse(saved) : null;
  }

  onSelectAvatar(): void {
    alert('Chức năng upload ảnh sẽ được tích hợp sau');
  }

  isEditingAddress = signal<boolean>(false);

  toggleEditAddress() {
    this.isEditingAddress.set(!this.isEditingAddress());
  }
}