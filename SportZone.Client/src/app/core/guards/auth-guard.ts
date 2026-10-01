import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account-service';
import { ToastService } from '../services/toast-service';

export const authGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const toast = inject(ToastService);
  const routers = inject(Router);
  if (accountService.currentUser()) return true;
  else {
    toast.error('Hãy đăng nhập trước.');
    routers.navigateByUrl('/');
    return false;
  }
};
