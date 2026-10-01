import { Component, EventEmitter, inject, Input, OnInit, Output, signal, HostListener } from '@angular/core';
import { AccountService } from '../../../core/services/account-service';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PromotionService } from '../../../core/services/promotion-service';
import { ToastService } from '../../../core/services/toast-service';
import { MemberService } from '../../../core/services/member-service';

@Component({
  selector: 'app-user-drawer',
  imports: [CommonModule, RouterModule],
  templateUrl: './user-drawer.html',
  styleUrl: './user-drawer.css',
})
export class UserDrawer implements OnInit {
  private promotionSerivce = inject(PromotionService);
  protected accountService = inject(AccountService);
  private memberService = inject(MemberService);

  private router = inject(Router);
  private toast = inject(ToastService);
  promotions = this.promotionSerivce.promotions;

  ngOnInit(): void {
    this.promotionSerivce.getPromotions();
  }
 
  // Nhận trạng thái mở/đóng từ cha
  @Input() isOpen: boolean = false;

  // Gửi sự kiện lên cha khi cần đóng Drawer hoặc Đăng xuất
  @Output() closeDrawer = new EventEmitter<void>();
  @Output() logoutUser = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.isOpen) {
      this.onClose();
    }
  }

  onClose() {
    this.closeDrawer.emit();
  }

  onLogout() {
    this.logoutUser.emit();
  }

  copyCode(code: string) {
    navigator.clipboard.writeText(code).then(() => {
      // Bạn có thể thay thế bằng Toast/Snackbar thông báo ở đây
      alert(`Đã sao chép mã: ${code}`); 
    }).catch(err => {
      console.error('Không thể sao chép mã', err);
    });
  }
  
  // Quản lý trạng thái hiện danh sách đổi điểm
showExchangeList = signal<boolean>(false);

toggleExchange() {
  this.showExchangeList.update(val => !val);
}

  exchange(points: number) {
    // 1. Gọi API trừ điểm ở Backend
    this.memberService.redeemReward(points).subscribe({
      next: () => {
        // 2. Thông báo thành công
        this.toast.success(`Đổi quà thành công! -${points} điểm`);
        
       
      },
      error: () => this.toast.error("Không đủ điểm để đổi quà.")
    });
  } 
}