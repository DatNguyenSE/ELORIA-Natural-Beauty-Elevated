import { Features } from "./features.model";

export interface Product {
  id: number;
  name: string;
  brand : string;
  categoryId: number;
  categoryName?: string;
  description?: string;
  quantity: number;
  price: number;
  imageUrl?: string;
  isDelete?: boolean;
  isDeleted?: boolean;

  discount?: number; 
  isNew?: boolean;
  productSizes: ProductSize[];
  label?: string;

  volume?: string;
  stock?: number;
  productType?: string;
  compareAtPrice?: number;
  benefits?: string;
  usage?: string;
  ingredients?: string;
  accentColor?: string;
  comboItems?: ComboItemModel[];

  features?: Features[];
}

export interface ComboItemModel {
  id: number;
  comboProductId: number;
  componentProductId: number;
  componentProductName?: string;
  componentProductImageUrl?: string;
  componentProductVolume?: string;
  componentProductPrice: number;
  quantity: number;
}

export interface ProductSize {
  id: number;
  sizeName: string;
  quantity: number;
}

