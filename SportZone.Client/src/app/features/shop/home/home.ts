import {
  Component, computed, inject, OnInit, OnDestroy, signal, effect, AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

import { CategoryService } from '../../../core/services/category-service';
import { ProductService } from '../../../core/services/product-service';
import { OrderService } from '../../../core/services/order-service';
import { AccountService } from '../../../core/services/account-service';

import { ELORIA_PRODUCTS } from '../../../shared/data/eloria-products';
import { ELORIA_COMBOS, ComboWithCalc } from '../../../shared/data/eloria-combos';
import { PRODUCT_MAP } from '../../../shared/data/eloria-products';
import { formatPrice } from '../../../shared/utils/format-vnd';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
  styleUrls: ['./home.css'],
})
export class Home implements OnInit, OnDestroy, AfterViewInit {
  protected categoryService = inject(CategoryService);
  protected productService = inject(ProductService);
  protected orderService = inject(OrderService);
  protected accountService = inject(AccountService);
  private router = inject(Router);

  // ── Data ──────────────────────────────────────────────
  private normalizeName(name: string): string {
    return name.normalize('NFD').replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/gi, 'd').toLowerCase().replace(/\beloria\b/g, '')
      .replace(/\s+/g, ' ').trim();
  }
  readonly eloriaProducts = computed(() => this.products().filter(product =>
    !product.isDelete && ELORIA_PRODUCTS.some(template =>
      [template.name, template.nameShort].some(name => this.normalizeName(name) === this.normalizeName(product.name)))));
  readonly realCombos = computed(() => this.products().filter(product => !product.isDelete).flatMap(product => {
    const template = ELORIA_COMBOS.find(combo => this.normalizeName(combo.name) === this.normalizeName(product.name));
    return template ? [{ ...product, slotId: template.id, isFlagship: !!template.isFlagship }] : [];
  }).sort((a, b) => Number(b.isFlagship) - Number(a.isFlagship)));
  readonly availableNeedChips = computed(() => this.needChips.filter(chip =>
    this.realCombos().some(combo => combo.slotId === chip.comboId)));

  // ── API products (cho search nav) ─────────────────────
  protected products = this.productService.products;
  protected myOrder = this.orderService.myOrder;

  // ── Đơn hàng chờ thanh toán ───────────────────────────
  pendingOrdersCount = computed(() =>
    this.myOrder().filter(o => o.status === 'Pending' || o.status === 'Placed').length
  );

  // ── Combo flagship ────────────────────────────────────
  // Match the existing DB product without inventing a database ID.
  flagshipCombo = computed(() => this.products().find(product =>
    !product.isDelete &&
    product.name.normalize('NFC').trim().replace(/\s+/g, ' ').toLocaleLowerCase('vi-VN') ===
      'combo chăm sóc toàn diện'
  ));
  otherCombos = ELORIA_COMBOS.filter(c => !c.isFlagship);

  // ── FAQ accordion ─────────────────────────────────────
  faqItems = [
    {
      q: 'Thời gian giao hàng là bao lâu?',
      a: 'ELORIA giao hàng toàn quốc trong 2–5 ngày làm việc. Đơn nội thành TP.HCM và Hà Nội có thể giao trong ngày hoặc hỏa tốc 2h. Đơn từ 500.000đ được miễn phí giao hàng.',
      open: false,
    },
    {
      q: 'Chính sách đổi trả như thế nào?',
      a: 'Bạn có thể đổi trả sản phẩm trong vòng 30 ngày kể từ ngày nhận hàng nếu sản phẩm còn nguyên vẹn, chưa qua sử dụng. ELORIA hoàn tiền 100% hoặc đổi sản phẩm tương đương.',
      open: false,
    },
    {
      q: 'Sản phẩm ELORIA có chứa paraben và sulfate không?',
      a: 'Không. ELORIA cam kết sử dụng thành phần chiết xuất thiên nhiên, không chứa paraben, sulfate (SLS/SLES), silicon nặng hay hương liệu tổng hợp gắt. Phù hợp với cả da đầu nhạy cảm.',
      open: false,
    },
    {
      q: 'Tôi nên dùng sản phẩm theo thứ tự nào?',
      a: 'Để hiệu quả tối ưu: (1) Dùng Tẩy tế bào chết da đầu 1–2 lần/tuần trước khi gội. (2) Gội đầu với Dầu gội, xoa bóp nhẹ nhàng 2–3 phút. (3) Dùng Dầu xả từ giữa đến đuôi tóc, để 3–5 phút rồi xả. (4) 1–2 lần/tuần thay Dầu xả bằng Ủ tóc. (5) Xịt Dưỡng tóc lên tóc còn ẩm trước khi sấy/tạo kiểu.',
      open: false,
    },
    {
      q: 'Tôi có thể đặt combo không? Combo tiết kiệm bao nhiêu?',
      a: 'Bạn có thể xem thành phần, giá và ưu đãi hiện tại trên trang chi tiết combo trước khi đặt mua.',
      open: false,
    },
  ];

  toggleFaq(idx: number) {
    this.faqItems[idx].open = !this.faqItems[idx].open;
  }

  // ── Chip lọc combo theo nhu cầu ───────────────────────
  needChips = [
    { label: 'Tóc khô, xơ', comboId: 'combo-phuc-hoi' },
    { label: 'Dân văn phòng', comboId: 'combo-bong-muot' },
    { label: 'Da đầu dầu, gàu', comboId: 'combo-da-dau-khoe' },
    { label: 'Muốn trọn bộ', comboId: 'combo-toan-dien' },
  ];

  activeChip = signal<string | null>(null);

  selectChip(comboId: string) {
    this.activeChip.set(this.activeChip() === comboId ? null : comboId);
    // Cuộn đến section combo
    const el = document.getElementById('combo-' + comboId);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  // ── Formatters ────────────────────────────────────────
  fmt = formatPrice;

  getProductName(id: string): string {
    return PRODUCT_MAP.get(id)?.nameShort ?? id;
  }

  getColorClass(color: string): string {
    return `product-color-${color}`;
  }

  // ── Màu bg cho bục sản phẩm ───────────────────────────
  COLOR_PALETTE: Record<string, string> = {
    cream:    '#E8D9C0',
    sage:     '#C5D1BA',
    blush:    '#E3B7AE',
    amber:    '#D9A441',
    lavender: '#B9A9CF',
  };

  getProductBg(color: string): string {
    return this.COLOR_PALETTE[color] ?? '#EFE4D6';
  }

  // ── Navigation ────────────────────────────────────────
  goToProduct(id: string | number) {
    this.router.navigate(['/product-detail', id]);
  }

  goToProducts() {
    this.router.navigate(['/category/products']);
  }

  goToCombos() {
    const el = document.getElementById('combos-section');
    if (el) el.scrollIntoView({ behavior: 'smooth' });
    else this.router.navigate(['/combos']);
  }

  // ── IntersectionObserver (reveal animation) ───────────
  private observer?: IntersectionObserver;

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(e => {
          if (e.isIntersecting) {
            e.target.classList.add('visible');
            this.observer?.unobserve(e.target);
          }
        });
      },
      { threshold: 0.1 }
    );

    document.querySelectorAll('.reveal').forEach(el => this.observer?.observe(el));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  // ── Lifecycle ─────────────────────────────────────────
  ngOnInit(): void {
    this.productService.getProducts();
    if (this.accountService.currentUser() !== null) {
      this.orderService.getUserOrders();
    }
  }
}
