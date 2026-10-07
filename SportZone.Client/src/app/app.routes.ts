import { Routes } from '@angular/router';
// SỬA DÒNG NÀY:
import { Home } from './features/shop/home/home';
import { Nav } from './layouts/nav/nav';
import { authGuard } from './core/guards/auth-guard';
import { AccountProfile } from './features/user/account-profile/account-profile';
import { ProductList } from './features/shop/product-list/product-list';
import { ProductManagement } from './features/admin/product-management/product-management';
import { ProductDetail } from './features/shop/product-detail/product-detail';
import { adminGuard } from './core/guards/admin-guard';
import { Cart } from './features/checkout/cart/cart';
import { OrderProcessing } from './features/checkout/order-processing/order-processing';
import { OrderUser } from './features/user/order-user/order-user';
import { PaymentFail } from './features/checkout/payment-fail/payment-fail';
import { OrderDetail } from './features/user/order-detail/order-detail';
import { CheckoutSuccess } from './features/checkout/checkout-success/checkout-success';
import { FeatureProducts } from './features/shop/campaign/feature-products/feature-products';
import { FeatureBannerManagement } from './features/admin/feature-banner-management/feature-banner-management';
import { AdminLayout } from './layouts/admin-layout/admin-layout';
import { UserLayout } from './layouts/user-layout/user-layout';
import { MemberList } from './features/admin/member/member-list/member-list';
import { CategoryManagement } from './features/admin/category-management/category-management';

export const routes: Routes = [
    {
        path: '',
        component: UserLayout, 
        children: [
            { path: '', component: Home },
            { path: 'category/:id', component: ProductList },
            { path: 'combos', redirectTo: 'category/combos', pathMatch: 'full' },
            { path: 'story', loadComponent: () => import('./features/shop/story/story').then(m => m.Story), title: 'Câu chuyện ELORIA | Một chút dịu dàng, mỗi ngày' },
            { path: 'hair-care', redirectTo: 'category/hair-care', pathMatch: 'full' },
            { path: 'product-detail/:id', component: ProductDetail },
            { path: 'payment-fail/:orderId', component: PaymentFail },
            { path: 'checkout-success/:orderId', component: CheckoutSuccess },
            { path: 'feature-products/:id', component: FeatureProducts },
        ]
    },

    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authGuard],
        children: [
            { path: 'my-account/profile', component: AccountProfile },
            { path: 'cart', component: Cart },
            { path: 'order-processing', component: OrderProcessing },
            { path: 'order-user', component: OrderUser },
            { path: 'order-detail/:orderId', component: OrderDetail }
        ]
    },
    {
        path: 'admin',
        component: AdminLayout,
        canActivate: [adminGuard], // Bảo vệ chỉ Admin mới vào được
        children: [
            { path: '', redirectTo: 'product-management', pathMatch: 'full' },
            { path: 'product-management', component: ProductManagement, canActivate: [adminGuard] },
            { path: 'category-management', component: CategoryManagement, canActivate: [adminGuard] },
            { path: 'feature-banner-management', component: FeatureBannerManagement, canActivate: [adminGuard] },
            { path: 'members', component: MemberList, canActivate: [adminGuard] },

        ]
    }
    ,
    { path: '**', redirectTo: '' }
];
