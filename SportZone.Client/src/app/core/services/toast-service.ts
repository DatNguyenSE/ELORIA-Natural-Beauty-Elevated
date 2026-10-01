import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private router = inject(Router);

  constructor() {
    this.createToastContainer();
    this.injectToastStyles();
  }

  // ── Container gốc ──
  private createToastContainer() {
    if (typeof document === 'undefined') return;
    if (!document.getElementById('toast-container')) {
      const container = document.createElement('div');
      container.id = 'toast-container';
      container.className = 'el-toast-container';
      document.body.appendChild(container);
    }
  }

  // ── Inject CSS hiệu ứng toast chuẩn phong cách ELORIA ──
  private injectToastStyles() {
    if (typeof document === 'undefined') return;
    if (document.getElementById('el-toast-styles')) return;

    const style = document.createElement('style');
    style.id = 'el-toast-styles';
    style.innerHTML = `
      .el-toast-container {
        position: fixed;
        top: 24px;
        right: 24px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        z-index: 999999;
        pointer-events: none;
        max-width: calc(100vw - 32px);
        font-family: var(--font-sans, 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, sans-serif);
      }

      @media (max-width: 640px) {
        .el-toast-container {
          top: 16px;
          right: 16px;
          left: 16px;
          max-width: none;
        }
      }

      @keyframes eloriaToastIn {
        0% {
          opacity: 0;
          transform: translateY(-12px) translateX(24px) scale(0.95);
        }
        100% {
          opacity: 1;
          transform: translateY(0) translateX(0) scale(1);
        }
      }

      @keyframes eloriaToastOut {
        0% {
          opacity: 1;
          transform: translateY(0) translateX(0) scale(1);
        }
        100% {
          opacity: 0;
          transform: translateY(-4px) translateX(30px) scale(0.94);
        }
      }

      @keyframes eloriaProgress {
        from { width: 100%; }
        to { width: 0%; }
      }

      .el-toast-in {
        animation: eloriaToastIn 0.35s cubic-bezier(0.16, 1, 0.3, 1) forwards;
      }

      .el-toast-out {
        animation: eloriaToastOut 0.3s cubic-bezier(0.4, 0, 1, 1) forwards;
      }

      .el-toast-item {
        position: relative;
        overflow: hidden;
        display: flex;
        align-items: flex-start;
        gap: 12px;
        padding: 14px 16px;
        border-radius: 14px;
        background: rgba(251, 247, 241, 0.98);
        backdrop-filter: blur(16px);
        -webkit-backdrop-filter: blur(16px);
        border: 1px solid rgba(47, 42, 36, 0.12);
        box-shadow: 0 12px 36px -4px rgba(47, 42, 36, 0.12), 0 2px 8px rgba(47, 42, 36, 0.04);
        min-width: 310px;
        max-width: 420px;
        color: var(--ink, #2F2A24);
        pointer-events: auto;
        transition: transform 0.2s ease, box-shadow 0.2s ease;
      }

      .el-toast-item:hover {
        transform: translateY(-2px);
        box-shadow: 0 16px 40px -4px rgba(47, 42, 36, 0.18);
      }

      .el-toast-item:hover .el-toast-progress-bar {
        animation-play-state: paused;
      }

      .el-toast-item.is-clickable {
        cursor: pointer;
      }

      /* ── Biểu tượng Icon Wrapper ── */
      .el-toast-icon-wrap {
        width: 32px;
        height: 32px;
        border-radius: 9px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        margin-top: 1px;
      }

      .el-toast-avatar {
        width: 38px;
        height: 38px;
        border-radius: 8px;
        object-fit: cover;
        flex-shrink: 0;
        border: 1px solid rgba(47, 42, 36, 0.1);
        background: #F6EFE6;
      }

      /* ── Thân Toast ── */
      .el-toast-body {
        flex: 1;
        min-width: 0;
      }

      .el-toast-title {
        display: block;
        font-size: 10px;
        font-weight: 700;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        margin-bottom: 2px;
      }

      .el-toast-msg {
        font-size: 13px;
        line-height: 1.45;
        font-weight: 500;
        color: var(--ink, #2F2A24);
        word-break: break-word;
        margin: 0;
      }

      /* ── Nút Đóng ── */
      .el-toast-close {
        width: 22px;
        height: 22px;
        border-radius: 999px;
        background: transparent;
        border: none;
        color: #8C8275;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: all 0.15s ease;
        margin-left: 4px;
        flex-shrink: 0;
        padding: 0;
      }

      .el-toast-close:hover {
        background: rgba(47, 42, 36, 0.08);
        color: #2F2A24;
      }

      /* ── Thanh Tiến Trình Progress Bar ── */
      .el-toast-progress-bar {
        position: absolute;
        bottom: 0;
        left: 0;
        height: 2.5px;
        width: 100%;
        animation-name: eloriaProgress;
        animation-timing-function: linear;
        animation-fill-mode: forwards;
      }

      /* ── Các biến thể trạng thái chuẩn ELORIA ── */
      /* SUCCESS: Xanh Xô Thơm Thảo Mộc */
      .el-toast-success {
        border-left: 4px solid #8A9A7B;
      }
      .el-toast-success .el-toast-icon-wrap {
        background: rgba(138, 154, 123, 0.18);
        color: #5F6F52;
      }
      .el-toast-success .el-toast-title {
        color: #5F6F52;
      }
      .el-toast-success .el-toast-progress-bar {
        background: #8A9A7B;
      }

      /* ERROR: Đất Nung Thảo Mộc / Terracotta */
      .el-toast-error {
        border-left: 4px solid #C0392B;
      }
      .el-toast-error .el-toast-icon-wrap {
        background: rgba(192, 57, 43, 0.12);
        color: #C0392B;
      }
      .el-toast-error .el-toast-title {
        color: #C0392B;
      }
      .el-toast-error .el-toast-progress-bar {
        background: #C0392B;
      }

      /* WARNING: Hổ Phách / Amber */
      .el-toast-warning {
        border-left: 4px solid #D9A441;
      }
      .el-toast-warning .el-toast-icon-wrap {
        background: rgba(217, 164, 65, 0.16);
        color: #9E6E17;
      }
      .el-toast-warning .el-toast-title {
        color: #9E6E17;
      }
      .el-toast-warning .el-toast-progress-bar {
        background: #D9A441;
      }

      /* INFO: Tím Lavender Tinh Khiết */
      .el-toast-info {
        border-left: 4px solid #B9A9CF;
      }
      .el-toast-info .el-toast-icon-wrap {
        background: rgba(185, 169, 207, 0.25);
        color: #64507C;
      }
      .el-toast-info .el-toast-title {
        color: #64507C;
      }
      .el-toast-info .el-toast-progress-bar {
        background: #B9A9CF;
      }
    `;
    document.head.appendChild(style);
  }

  // ── Khởi tạo phần tử Toast ──
  private createToastElement(
    message: string,
    type: ToastType,
    duration = 4000,
    avatar?: string,
    route?: string
  ) {
    if (typeof document === 'undefined') return;
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `el-toast-item el-toast-${type} el-toast-in ${route ? 'is-clickable' : ''}`;

    if (route) {
      toast.addEventListener('click', (e) => {
        // Không navigate nếu nhấn nút close
        if ((e.target as HTMLElement).closest('.el-toast-close')) return;
        this.router.navigateByUrl(route);
      });
    }

    const titleMap: Record<ToastType, string> = {
      success: 'ELORIA · Thành Công',
      error: 'ELORIA · Thông Báo',
      warning: 'ELORIA · Cảnh Báo',
      info: 'ELORIA · Cập Nhật'
    };

    const iconMap: Record<ToastType, string> = {
      success: `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="20 6 9 17 4 12"></polyline>
        </svg>
      `,
      error: `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" y1="8" x2="12" y2="12"></line>
          <line x1="12" y1="16" x2="12.01" y2="16"></line>
        </svg>
      `,
      warning: `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
          <line x1="12" y1="9" x2="12" y2="13"></line>
          <line x1="12" y1="17" x2="12.01" y2="17"></line>
        </svg>
      `,
      info: `
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" y1="16" x2="12" y2="12"></line>
          <line x1="12" y1="8" x2="12.01" y2="8"></line>
        </svg>
      `
    };

    const mediaHtml = avatar
      ? `<img src="${avatar}" class="el-toast-avatar" alt="Sản phẩm"/>`
      : `<div class="el-toast-icon-wrap">${iconMap[type]}</div>`;

    toast.innerHTML = `
      ${mediaHtml}
      <div class="el-toast-body">
        <span class="el-toast-title">${titleMap[type]}</span>
        <p class="el-toast-msg">${this.escapeHtml(message)}</p>
      </div>
      <button type="button" class="el-toast-close" title="Đóng thông báo" aria-label="Đóng">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </button>
      <div class="el-toast-progress-bar" style="animation-duration: ${duration}ms;"></div>
    `;

    // Đóng thủ công
    toast.querySelector('.el-toast-close')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.removeToast(toast);
    });

    container.appendChild(toast);

    // Tự động tắt sau khoảng thời gian duration
    const timeoutId = setTimeout(() => this.removeToast(toast), duration);

    // Lưu timeoutId vào dataset để có thể clear nếu đóng thủ công
    (toast as any).__timeoutId = timeoutId;
  }

  private removeToast(toast: HTMLElement) {
    if ((toast as any).__timeoutId) {
      clearTimeout((toast as any).__timeoutId);
    }
    toast.classList.remove('el-toast-in');
    toast.classList.add('el-toast-out');
    setTimeout(() => {
      toast.remove();
    }, 300);
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  // ── Public API ──
  success(msg: string, dur = 3500, avatar?: string, route?: string) {
    this.createToastElement(msg, 'success', dur, avatar, route);
  }

  error(msg: string, dur = 4500, avatar?: string, route?: string) {
    this.createToastElement(msg, 'error', dur, avatar, route);
  }

  warning(msg: string, dur = 4000, avatar?: string, route?: string) {
    this.createToastElement(msg, 'warning', dur, avatar, route);
  }

  info(msg: string, dur = 3500, avatar?: string, route?: string) {
    this.createToastElement(msg, 'info', dur, avatar, route);
  }
}
