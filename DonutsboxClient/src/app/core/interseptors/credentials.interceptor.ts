import { HttpInterceptorFn } from '@angular/common/http';

export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.withCredentials) {
    return next(req);
  }

  const authorizedRequest = req.clone({ withCredentials: true });
  return next(authorizedRequest);
};

