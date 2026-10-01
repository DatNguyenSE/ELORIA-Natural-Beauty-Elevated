import { Product, ProductSize } from "./product.model";

export type Cart = {
    id: number;
    userId: string;
    items?: CartItem[];
}

export type CartItem = {
    productId: number;
    quantity: number;
    product?: Product;
    productSizeId?: number;
    sizeName : string;
}

