import { Payment } from "./payment.model";

export interface Order {
  id: number;
  createdAt: Date;
  status: string;
  totalAmount: number;
  orderDate: Date;
  discountAmount: number;
  subTotal: number;
  couponCode?: string;
  items: OrderItem[];
  payment: Payment;
}

export interface OrderItem {
  productId: number;
  quantity: number;
  unitPrice: number;
  imageUrl: string;
  payment: Payment;
  name: string;
  sizeName: string;
}



