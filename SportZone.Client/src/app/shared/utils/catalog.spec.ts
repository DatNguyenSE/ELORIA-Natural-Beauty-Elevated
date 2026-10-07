import { filterCatalog, finalPrice, originalPrice, isBestSeller } from './catalog';
import { Product } from '../models/product.model';

const product = (id: number, overrides: Partial<Product> = {}): Product => ({
  id, name: 'Dầu gội', brand: 'ELORIA', categoryId: 1, quantity: 10,
  price: 100000, productSizes: [], productType: 'single', ...overrides,
});

describe('Category catalog', () => {
  const data = [
    product(1), product(2, { name: 'Combo Toàn Diện', productType: 'single', categoryId: 6 }),
    product(3, { discount: 10 }), product(4, { compareAtPrice: 120000 }),
    product(5, { isDeleted: true }), product(6, { isDelete: true }),
  ];
  it('shows all visible products including combos', () => {
    expect(filterCatalog(data, 'products').map(p => p.id)).toEqual([1, 2, 3, 4]);
  });
  it('separates combos from individual hair care', () => {
    expect(filterCatalog(data, 'combos').map(p => p.id)).toEqual([2]);
    expect(filterCatalog(data, 'hair-care').map(p => p.id)).toEqual([1, 3, 4]);
  });
  it('includes percentage and compare-at offers but excludes full-price products', () => {
    expect(filterCatalog(data, 'sale').map(p => p.id)).toEqual([3, 4]);
    expect(finalPrice(data[2])).toBe(90000);
    expect(originalPrice(data[3])).toBe(120000);
  });
  it('matches a leading Combo word in product or category names regardless of productType', () => {
    expect(filterCatalog([product(7, { productType: undefined, name: 'Combo Khởi Đầu' })], 'combos').length).toBe(1);
    expect(filterCatalog([product(8, { productType: 'single', name: 'Combo thử' })], 'combos').length).toBe(1);
  });
  it('uses category prefixes and excludes unrelated combo metadata', () => {
    expect(filterCatalog([
      product(1, { categoryName: ' Combo Toàn Diện' }),
      product(2, { name: '  COMBO Phục Hồi' }),
      product(3, { name: 'Dầu gội', productType: 'combo' }),
      product(4, { name: 'Sản phẩm trong combo' }),
    ], 'combos').map(p => p.id)).toEqual([1, 2]);
  });
  it('shows only visible products explicitly labelled Bán chạy nhất', () => {
    const items = [
      product(1, { label: 'Bán chạy nhất' }),
      product(2, { label: ' BÁN CHẠY NHẤT ', name: 'Sản phẩm mới bất kỳ' }),
      product(3, { label: 'Flagship' }), product(4),
      product(5, { label: 'Bán chạy nhất', isDeleted: true }),
      product(6, { label: 'Bán chạy nhất', isDelete: true }),
      product(7, { label: 'Không phải Bán chạy nhất' }),
    ];
    expect(items.filter(isBestSeller).map(p => p.id)).toEqual([1, 2]);
  });
  it('keeps numeric and unknown routes scoped', () => {
    expect(filterCatalog(data, '6').map(p => p.id)).toEqual([2]);
    expect(filterCatalog(data, 'invalid')).toEqual([]);
    expect(filterCatalog(data, '999')).toEqual([]);
  });
});

