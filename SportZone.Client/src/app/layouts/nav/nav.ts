import {
  Component, inject, OnInit, OnDestroy, signal,
  ChangeDetectorRef, HostListener, PLATFORM_ID
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../../core/services/account-service';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { ToastService } from '../../core/services/toast-service';
import { BusyService } from '../../core/services/busy-service';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../core/services/product-service';
import { Product } from '../../shared/models/product.model';
import { UserDrawer } from '../user-layout/user-drawer/user-drawer';
import { ELORIA_CONFIG } from '../../shared/data/eloria-config';

@Component({
  selector: 'app-nav',
  imports: [FormsModule, RouterLink, RouterLinkActive, CommonModule, UserDrawer],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav implements OnInit, OnDestroy {
  private platformId = inject(PLATFORM_ID);

  // ── Services ──────────────────────────────────────────
  protected accountService = inject(AccountService);
  protected productService = inject(ProductService);
  protected busyService = inject(BusyService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  // ── Announcement bar ──────────────────────────────────
  private announcements = ELORIA_CONFIG.announcements;
  private annIdx = 0;
  private annTimer?: ReturnType<typeof setInterval>;
  currentAnnouncement = this.announcements[0];

  // ── Header scroll state ────────────────────────────────
  isScrolled = false;

  @HostListener('window:scroll')
  onWindowScroll() {
    this.isScrolled = window.scrollY > 40;
  }

  // ── Mobile menu ───────────────────────────────────────
  isMobileMenuOpen = false;
  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = this.isMobileMenuOpen ? 'hidden' : '';
    }
  }

  // ── Auth / Login modal ───────────────────────────────
  protected creds: any = {
    step: 1,
    email: '',
    password: '',
    otpCode: '',
    emailError: false,
    old: false,
    condition: false,
    marketing: false,
  };

  protected isLoginModalOpen = false;
  protected isPasswordVisible = false;
  protected isSubmitting = false;

  toggleLoginModal() {
    this.isLoginModalOpen = !this.isLoginModalOpen;
    if (!this.isLoginModalOpen) {
      this.creds.step = 1;
      this.creds.email = '';
      this.creds.password = '';
      this.creds.otpCode = '';
      this.creds.emailError = false;
    }
  }

  closeOnBackdrop(event: MouseEvent) {
    if ((event.target as HTMLElement).classList.contains('eloria-modal-backdrop')) {
      this.toggleLoginModal();
    }
  }

  openFaceBook() {
    this.toast.warning('Tính năng đang được phát triển. Vui lòng sử dụng email để đăng nhập.');
  }

  openGoogle() {
    this.toast.warning('Tính năng đang được phát triển. Vui lòng sử dụng email để đăng nhập.');
  }

  onContinue() {
    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (!emailPattern.test(this.creds.email)) {
      this.creds.emailError = true;
      return;
    }
    this.creds.emailError = false;
    if (!this.creds.old) {
      this.toast.error('Bạn cần xác nhận rằng bạn trên 16 tuổi để tiếp tục.');
      return;
    }
    if (!this.creds.condition) {
      this.toast.error('Bạn cần đồng ý với các điều khoản và điều kiện.');
      return;
    }
    this.creds.step = 2;
  }

  onLogin() {
    if (this.isSubmitting) return;
    this.isSubmitting = true;
    this.cdr.detectChanges();

    if (!this.creds.fullname && this.creds.email) {
      this.creds.fullname = this.creds.email.split('@')[0];
    }

    this.accountService.authenticate(this.creds).subscribe({
      next: (res: any) => {
        if (res.requireOtp) {
          this.toast.success(res.message);
          setTimeout(() => {
            this.creds.step = 3;
            this.isSubmitting = false;
            this.cdr.detectChanges();
          }, 2000);
        } else {
          this.isSubmitting = false;
          this.toast.success(res.message);
          this.toggleLoginModal();
          if (res.user?.roles?.includes('Admin')) {
            this.router.navigateByUrl('/admin');
          } else {
            this.router.navigateByUrl('/');
          }
          this.cdr.detectChanges();
        }
      },
      error: (error: any) => {
        this.isSubmitting = false;
        if (error.status === 400 && error.error?.requireOtp) {
          this.toast.info(error.error.message);
          setTimeout(() => {
            this.creds.step = 3;
            this.cdr.detectChanges();
          }, 2000);
        } else if (error.status === 401) {
          this.toast.error(error.error?.message || 'Email hoặc mật khẩu không chính xác.');
        } else {
          this.toast.error(error.error?.message || 'Đăng nhập không thành công. Vui lòng thử lại.');
        }
        this.cdr.detectChanges();
      },
    });
  }

  onVerifyOtp() {
    if (!this.creds.otpCode || this.creds.otpCode.length < 6) {
      this.toast.error('Vui lòng nhập đủ mã xác thực 6 chữ số.');
      return;
    }
    if (this.isSubmitting) return;
    this.isSubmitting = true;
    this.cdr.detectChanges();

    this.accountService
      .verifyEmail({ email: this.creds.email, otpCode: this.creds.otpCode })
      .subscribe({
        next: (res: any) => {
          this.isSubmitting = false;
          this.toast.success(res.message);
          this.toggleLoginModal();
          if (res.user?.roles?.includes('Admin')) {
            this.router.navigateByUrl('/admin');
          } else {
            this.router.navigateByUrl('/');
          }
          this.cdr.detectChanges();
        },
        error: (err: any) => {
          this.isSubmitting = false;
          this.toast.error(err.error?.message || 'Mã OTP không hợp lệ hoặc đã hết hạn.');
          this.cdr.detectChanges();
        },
      });
  }

  logout() {
    this.accountService.logout();
    this.router.navigate([]);
  }

  togglePassword() {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  onCart() {
    this.router.navigate(['/cart']);
  }

  onAssist() {
    alert('Vui lòng liên hệ email: ' + ELORIA_CONFIG.contact.email + ' để được hỗ trợ.');
  }

  // ── Search ────────────────────────────────────────────
  allProducts = this.productService.products;
  filteredProducts = signal<Product[]>([]);
  protected isSearching = false;

  constructor() {
    this.filteredProducts.set([]);
  }

  onSearch(value: string) {
    const query = value.toLowerCase().trim();
    if (!query) {
      this.isSearching = false;
      this.filteredProducts.set([]);
      return;
    }
    this.isSearching = true;
    const result = this.allProducts()
      .filter(p => p.name.toLowerCase().includes(query))
      .slice(0, 6);
    this.filteredProducts.set(result);
  }

  goToProduct(productId: number | string) {
    this.isSearching = false;
    this.router.navigate(['/product-detail', productId]);
  }

  // ── Lifecycle ─────────────────────────────────────────
  ngOnInit(): void {
    // Rotate announcement bar mỗi 4 giây
    this.annTimer = setInterval(() => {
      this.annIdx = (this.annIdx + 1) % this.announcements.length;
      this.currentAnnouncement = this.announcements[this.annIdx];
      this.cdr.detectChanges();
    }, 4000);
  }

  ngOnDestroy(): void {
    if (this.annTimer) clearInterval(this.annTimer);
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = '';
    }
  }
}