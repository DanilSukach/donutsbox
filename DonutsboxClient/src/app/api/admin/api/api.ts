export * from './adminContent.service';
import { AdminContentService } from './adminContent.service';
export * from './adminUser.service';
import { AdminUserService } from './adminUser.service';
export const APIS = [AdminContentService, AdminUserService];
