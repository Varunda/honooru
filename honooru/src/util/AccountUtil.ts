import { AppPermission } from "../api/AppPermissionApi";
import { UserSetting } from "../api/UserSettingApi";


export class AppCurrentAccount {
    public ID: number = 0;
    public name: string = "";
    public permissions: AppPermission[] = [];
    public settings: UserSetting[] = [];
}

export default class AccountUtil {

    public static get(): AppCurrentAccount {
        return (window as any).appCurrentAccount;
    }

    public static getAccountName(): string {
        return AccountUtil.get().name;
    }

    public static getSetting(name: string): UserSetting | undefined {
        return AccountUtil.get().settings.find(iter => iter.name == name);
    }

    public static hasPermission(name: string): boolean {
        return AccountUtil.get().permissions.find(iter => iter.id.toLowerCase() == name.toLowerCase()) != undefined;
    }

}