import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class UserSetting {
    public accountID: number = 0;
    public name: string = "";
    public type: string = "";
    public value: string = "";
}

export class UserSettingApi extends ApiWrapper<UserSetting> {
    private static _instance: UserSettingApi = new UserSettingApi();
    public static get(): UserSettingApi { return UserSettingApi._instance; }

    public static parse(elem: any): UserSetting {
        return {
            ...elem
        };
    }

    public static getByCurrentUser(): Promise<Loading<UserSetting[]>> {
        return UserSettingApi.get().readList(`/api/user-setting/`, UserSettingApi.parse);
    }

    public static update(name: string, value: string): Promise<Loading<void>> {
        return UserSettingApi.get().post(`/api/user-setting/${name}?value=${value}`);
    }

}
