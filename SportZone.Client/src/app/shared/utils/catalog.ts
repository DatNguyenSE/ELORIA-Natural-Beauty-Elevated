import { Product } from '../models/product.model';

export function isCombo(product: Product): boolean {
  return [product.name, product.categoryName].some(name => /^combo(?:\s|$)/i.test(name?.trim() ?? ''));
}

export function isBestSeller(product: Product): boolean {
  return !product.isDelete && !product.isDeleted
    && product.label?.normalize('NFC').trim().toLocaleLowerCase('vi-VN') === 'bán chạy nhất';
}
export function finalPrice(product: Product): number {
  return product.price * (1 - Math.min(100, Math.max(0, product.discount ?? 0)) / 100);
}
export function originalPrice(product: Product): number {
  return Math.max(product.price, product.compareAtPrice ?? 0);
}
export function salePercent(product: Product): number {
  const original = originalPrice(product);
  return original > 0 ? Math.round((1 - finalPrice(product) / original) * 100) : 0;
}
export function filterCatalog(products: Product[], slug: string): Product[] {
  return products.filter(product => {
    if (product.isDeleted || product.isDelete) return false;
    switch (slug) {
      case 'products':
      case 'all': return true;
      case 'combos': return isCombo(product);
      case 'hair-care': return !isCombo(product);
      case 'sale': return finalPrice(product) < originalPrice(product);
      case 'new': return !!product.isNew;
      default: return /^\d+$/.test(slug) && product.categoryId === Number(slug);
    }
  });
}
