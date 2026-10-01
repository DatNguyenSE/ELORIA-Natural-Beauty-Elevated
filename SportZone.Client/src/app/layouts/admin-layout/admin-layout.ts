import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AccountService } from '../../core/services/account-service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css',
})
export class AdminLayout {
  public accountService = inject(AccountService);
  private router = inject(Router);
  
  isSidebarCollapsed = signal(false);

  menuItems = [
    // { label: 'Thống kê', icon: 'dashboard', link: '/admin' },
    { label: 'Sản phẩm', icon: 'inventory_2', link: '/admin/product-management' },
    { label: 'Thể loại', icon: 'category', link: '/admin/category-management' },
    { label: 'Thành viên', icon: 'group', link: '/admin/members' },
    { label: 'Banner', icon: 'view_carousel', link: '/admin/feature-banner-management' },
  ];

  toggleSidebar() {
    this.isSidebarCollapsed.update(v => !v);
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/login');
  }
}