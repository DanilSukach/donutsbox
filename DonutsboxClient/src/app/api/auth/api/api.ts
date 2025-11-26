export * from './admin.service';
import { AdminService } from './admin.service';
export * from './auth.service';
import { AuthService } from './auth.service';
export * from './userProfile.service';
import { UserProfileService } from './userProfile.service';
export const APIS = [AdminService, AuthService, UserProfileService];
