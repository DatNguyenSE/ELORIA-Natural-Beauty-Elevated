/**
 * ELORIA — Dữ liệu sản phẩm (nguồn duy nhất)
 * Mỗi sản phẩm có: id, name, volume, price, color, description, benefits, svgPath
 */

export type ProductColor = 'cream' | 'sage' | 'blush' | 'amber' | 'lavender';

export interface EloriaProduct {
  id: string;
  name: string;
  nameShort: string;
  volume: string;       // ví dụ "400ml"
  price: number;        // giá gốc (VNĐ)
  color: ProductColor;  // màu nhận diện
  description: string;  // mô tả ngắn, giọng nhẹ nhàng
  benefits: ProductBenefit[]; // 3–4 lợi ích icon
  image: string;        // đường dẫn ảnh (thay bằng ảnh thật)
  alt: string;          // alt text tiếng Việt
  svgType: 'bottle-pump' | 'bottle-spray' | 'tube' | 'jar-round' | 'jar-scrub';
}

export interface ProductBenefit {
  icon: string;         // SVG path hoặc emoji/tên icon
  label: string;        // nhãn tiếng Việt
}

// ── DỮ LIỆU SẢN PHẨM ──────────────────────────────────────────
export const ELORIA_PRODUCTS: EloriaProduct[] = [
  {
    id: 'dau-goi',
    name: 'Dầu Gội Thiên Nhiên ELORIA',
    nameShort: 'Dầu Gội',
    volume: '400ml',
    price: 239_000,
    color: 'cream',
    description: 'Làm sạch dịu nhẹ, cho cảm giác tóc và da đầu sạch thoáng sau mỗi lần gội.',
    benefits: [
      { icon: 'leaf',      label: 'Thành phần thiên nhiên' },
      { icon: 'drop',      label: 'Dịu nhẹ cho da đầu' },
      { icon: 'sparkle',   label: 'Hương thơm tinh tế' },
      { icon: 'shield',    label: 'Không chứa paraben' },
    ],
    image: '/images/products/dau-goi.png',
    alt: 'Chai dầu gội thiên nhiên ELORIA 400ml',
    svgType: 'bottle-pump',
  },
  {
    id: 'dau-xa',
    name: 'Dầu Xả Mềm Mượt ELORIA',
    nameShort: 'Dầu Xả',
    volume: '400ml',
    price: 239_000,
    color: 'sage',
    description: 'Giúp tóc mềm, dễ chải, giảm cảm giác xơ rối và dễ tạo kiểu hơn.',
    benefits: [
      { icon: 'leaf',     label: 'Chiết xuất thiên nhiên' },
      { icon: 'silk',     label: 'Tóc mượt dễ chải' },
      { icon: 'heart',    label: 'Phù hợp mọi loại tóc' },
      { icon: 'shield',   label: 'Không chứa sulfate' },
    ],
    image: '/images/products/dau-xa.png',
    alt: 'Chai dầu xả mềm mượt ELORIA 400ml',
    svgType: 'bottle-pump',
  },
  {
    id: 'u-toc',
    name: 'Ủ Tóc Phục Hồi ELORIA',
    nameShort: 'Ủ Tóc',
    volume: '300ml',
    price: 269_000,
    color: 'blush',
    description: 'Nuôi dưỡng sâu, hỗ trợ tóc khô xơ trở nên mềm mượt và rạng rỡ hơn.',
    benefits: [
      { icon: 'leaf',     label: 'Dưỡng chất từ thiên nhiên' },
      { icon: 'drop',     label: 'Dưỡng ẩm chuyên sâu' },
      { icon: 'sparkle',  label: 'Tóc mềm, bóng mượt' },
      { icon: 'heart',    label: 'Dành cho tóc hư tổn' },
    ],
    image: '/images/products/u-toc.png',
    alt: 'Hũ ủ tóc phục hồi ELORIA 300ml',
    svgType: 'jar-round',
  },
  {
    id: 'xit-duong-toc',
    name: 'Xịt Dưỡng Tóc ELORIA',
    nameShort: 'Xịt Dưỡng Tóc',
    volume: '150ml',
    price: 219_000,
    color: 'amber',
    description: 'Bảo vệ và tăng độ bóng, giúp tóc ống mượt tự nhiên, rạng rỡ suốt cả ngày.',
    benefits: [
      { icon: 'sparkle',  label: 'Tăng độ bóng tự nhiên' },
      { icon: 'shield',   label: 'Bảo vệ khỏi nhiệt' },
      { icon: 'leaf',     label: 'Chiết xuất thực vật' },
      { icon: 'silk',     label: 'Không bết, không nặng' },
    ],
    image: '/images/products/xit-duong-toc.png',
    alt: 'Chai xịt dưỡng tóc ELORIA 150ml',
    svgType: 'bottle-spray',
  },
  {
    id: 'tay-te-bao-chet',
    name: 'Tẩy Tế Bào Chết Da Đầu ELORIA',
    nameShort: 'Tẩy Da Đầu',
    volume: '200ml',
    price: 229_000,
    color: 'lavender',
    description: 'Làm sạch sâu lớp tế bào chết, hỗ trợ da đầu thông thoáng và khỏe mạnh hơn.',
    benefits: [
      { icon: 'sparkle',  label: 'Làm sạch sâu da đầu' },
      { icon: 'leaf',     label: 'Hạt scrub tự nhiên' },
      { icon: 'drop',     label: 'Cân bằng độ ẩm' },
      { icon: 'shield',   label: 'Dịu nhẹ, không kích ứng' },
    ],
    image: '/images/products/tay-te-bao-chet.png',
    alt: 'Tuýp tẩy tế bào chết da đầu ELORIA 200ml',
    svgType: 'tube',
  },
];

/** Map id → product để tra nhanh */
export const PRODUCT_MAP = new Map(ELORIA_PRODUCTS.map(p => [p.id, p]));

/** Lấy giá theo id sản phẩm */
export function getProductPrice(id: string): number {
  return PRODUCT_MAP.get(id)?.price ?? 0;
}
