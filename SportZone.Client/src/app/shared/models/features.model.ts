import { Product } from "./product.model";

export interface Features {
  id : number;
  name: string;
  desc: string;
  imgUrl: string;
  isBanner: boolean;
  products?: Product[]; 
}

export interface UpdateFeature {
  id : number;
  name: string;
  desc: string;
  imgUrl?: string;
  isBanner: boolean;
  productIds?: number[]; 
}