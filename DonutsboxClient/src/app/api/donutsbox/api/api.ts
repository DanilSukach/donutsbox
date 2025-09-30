export * from './user.service';
import { UserService } from './user.service';
export * from './userAuth.service';
import { UserAuthService } from './userAuth.service';
export * from './userType.service';
import { UserTypeService } from './userType.service';
export const APIS = [UserService, UserAuthService, UserTypeService];
