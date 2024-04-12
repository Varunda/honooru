import { AppGroupPermission } from "api/AppGroupPermissionApi";
import { UserSetting } from "api/UserSettingApi";


export class AppCurrentAccount {
    public ID: number = 0;
    public name: string = "";
    public permissions: AppGroupPermission[] = [];
    public settings: UserSetting[] = [];
}

export default class AccountUtil {

    private static _settings: Map<string, UserSetting> | null = null;

    public static get(): AppCurrentAccount {
        return (window as any).appCurrentAccount;
    }

    public static getAccountName(): string {
        return AccountUtil.get().name;
    }

    public static getSetting(name: string): UserSetting | undefined {
        if (AccountUtil._settings == null) {
            AccountUtil._settings = new Map();
            for (const setting of AccountUtil.get().settings) {
                AccountUtil._settings.set(setting.name, setting);
            }
        }

        return AccountUtil._settings.get(name);
    }

    public static hasPermission(name: string): boolean {
        return AccountUtil.get().permissions.find(iter => iter.permission.toLowerCase() == name.toLowerCase()) != undefined;
    }

}