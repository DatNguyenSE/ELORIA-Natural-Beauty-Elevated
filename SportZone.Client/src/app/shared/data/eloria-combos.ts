/**
 * ELORIA — Dữ liệu Combo (nguồn duy nhất)
 * Giá lẻ = tổng giá thành phần; tiết kiệm = giá lẻ − giá combo
 * KHÔNG hard-code giá lẻ — tính tự động từ ELORIA_PRODUCTS
 */
import { ELORIA_PRODUCTS, getProductPrice } from './eloria-products';

export interface EloriaCombo {
  id: string;
  name: string;
  productIds: string[];      // ID các sản phẩm thành phần
  comboPrice: number;        // giá combo thực sự
  badge?: string;            // VD: "BÁN CHẠY NHẤT", "FLAGSHIP"
  note: string;              // ghi chú "phù hợp cho..."
  isFlagship?: boolean;      // combo nổi bật nhất
}

// ── CÁC COMBO ──────────────────────────────────────────────────
export const ELORIA_COMBOS_RAW: EloriaCombo[] = [
  {
    id: 'combo-khoi-dau',
    name: 'Combo Khởi Đầu',
    productIds: ['dau-goi', 'dau-xa'],
    comboPrice: 429_000,
    badge: 'BÁN CHẠY NHẤT',
    note: 'Bộ đôi cơ bản, phù hợp cho người mới bắt đầu chăm sóc tóc.',
  },
  {
    id: 'combo-phuc-hoi',
    name: 'Combo Phục Hồi Mềm Mượt',
    productIds: ['dau-goi', 'dau-xa', 'u-toc'],
    comboPrice: 659_000,
    note: 'Dành cho tóc khô, xơ, hư tổn cần được nuôi dưỡng chuyên sâu.',
  },
  {
    id: 'combo-bong-muot',
    name: 'Combo Bóng Mượt Hằng Ngày',
    productIds: ['dau-goi', 'dau-xa', 'xit-duong-toc'],
    comboPrice: 619_000,
    note: 'Phù hợp dân văn phòng, sinh viên cần tóc gọn đẹp mỗi ngày.',
  },
  {
    id: 'combo-da-dau-khoe',
    name: 'Combo Da Đầu Khỏe',
    productIds: ['dau-goi', 'tay-te-bao-chet'],
    comboPrice: 419_000,
    note: 'Dành cho người hay bị gàu, da đầu dầu, cần làm sạch sâu.',
  },
  {
    id: 'combo-toan-dien',
    name: 'Combo Chăm Sóc Toàn Diện',
    productIds: ['dau-goi', 'dau-xa', 'u-toc', 'xit-duong-toc', 'tay-te-bao-chet'],
    comboPrice: 999_000,
    badge: 'FLAGSHIP',
    note: 'Trọn bộ 5 sản phẩm — chăm sóc tóc và da đầu toàn diện từ A đến Z.',
    isFlagship: true,
  },
];

/** Tính giá lẻ (tổng giá thành phần) và tiết kiệm, trả ComboWithCalc */
export interface ComboWithCalc extends EloriaCombo {
  retailPrice: number;   // tổng giá lẻ
  saving: number;        // tiết kiệm
  savingPercent: number; // % tiết kiệm (làm tròn)
}

export function enrichCombos(combos: EloriaCombo[]): ComboWithCalc[] {
  return combos.map(combo => {
    const retailPrice = combo.productIds.reduce(
      (sum, id) => sum + getProductPrice(id),
      0
    );
    const saving = retailPrice - combo.comboPrice;
    const savingPercent = Math.round((saving / retailPrice) * 100);
    return { ...combo, retailPrice, saving, savingPercent };
  });
}

export const ELORIA_COMBOS: ComboWithCalc[] = enrichCombos(ELORIA_COMBOS_RAW);

// ── KIỂM TRA SỐ LIỆU (chạy khi dev, bắt lỗi sớm) ─────────────
const EXPECTED: Record<string, { retail: number; combo: number; saving: number }> = {
  'combo-khoi-dau':    { retail: 478_000, combo: 429_000, saving: 49_000 },
  'combo-phuc-hoi':    { retail: 747_000, combo: 659_000, saving: 88_000 },
  'combo-bong-muot':   { retail: 697_000, combo: 619_000, saving: 78_000 },
  'combo-da-dau-khoe': { retail: 468_000, combo: 419_000, saving: 49_000 },
  'combo-toan-dien':   { retail: 1_195_000, combo: 999_000, saving: 196_000 },
};

if (typeof window === 'undefined' || (window as any).__ELORIA_COMBO_VALIDATED__ !== true) {
  for (const combo of ELORIA_COMBOS) {
    const exp = EXPECTED[combo.id];
    if (!exp) continue;
    if (combo.retailPrice !== exp.retail) {
      console.error(`[ELORIA] Combo "${combo.name}" giá lẻ sai: ${combo.retailPrice} ≠ ${exp.retail}`);
    }
    if (combo.comboPrice !== exp.combo) {
      console.error(`[ELORIA] Combo "${combo.name}" giá combo sai: ${combo.comboPrice} ≠ ${exp.combo}`);
    }
    if (combo.saving !== exp.saving) {
      console.error(`[ELORIA] Combo "${combo.name}" tiết kiệm sai: ${combo.saving} ≠ ${exp.saving}`);
    }
  }
  if (typeof window !== 'undefined') (window as any).__ELORIA_COMBO_VALIDATED__ = true;
}
