/**
 * Định dạng số tiền theo chuẩn Việt Nam: 239.000đ
 * Dùng chung cho toàn bộ dự án ELORIA.
 */
export function formatVND(amount: number): string {
  return amount.toLocaleString('vi-VN').replace(/\./g, '.') + 'đ';
}

/**
 * Pipe-friendly: trả về chuỗi dạng "239.000đ"
 */
export function formatPrice(amount: number): string {
  return new Intl.NumberFormat('vi-VN').format(amount) + 'đ';
}
