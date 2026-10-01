export interface Promotion {
    id: number;
    code: string;
    discountType: 'PERCENT' | 'FIXED';
    discountValue: number;
    discountAmount: number;
    minOrderValue: number;
    maxDiscountAmount: number;
    startDate: string;
    endDate: string;
    isActive: boolean;
    title: string;
    description: string;
    icon: string;
    isVisible: boolean
}
