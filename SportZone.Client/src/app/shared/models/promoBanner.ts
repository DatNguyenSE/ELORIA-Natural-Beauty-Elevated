interface PromoBanner {
  isVisible: boolean;    
  title: string;          
  description: string;    
  icon: string;            
  type: 'info' | 'warning' | 'holiday'; // Loại banner để chỉnh màu (tùy chọn)
  couponCode: string;        
}