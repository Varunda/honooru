import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppAccountPermission {
    public id: number = 0;
    public accountID: number = 0;
    public permission: string = "";
    public timestamp: Date = new Date();
    public grantedByID: number = 0;
}

export class AppAccountPermissionApi extends ApiWrapper<AppAccountPermission> {

    private static _instance: AppAccountPermissionApi = new AppAccountPermissionApi();
    public static get(): AppAccountPermissionApi { return AppAccountPermissionApi._instance; };

    public static parse(elem: any): AppAccountPermission {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

    public static async getByAccountID(accountID: number): Promise<Loading<AppAccountPermission[]>> {
        return AppAccountPermissionApi.get().readList(`/api/account-permission/${accountID}`, AppAccountPermissionApi.parse);
    }

    public static async insert(accountID: number, perm: string): Promise<Loading<number>> {
        return AppAccountPermissionApi.get().postReply(`/api/account-permission/${accountID}?permission=${perm}`, (iter: any) => iter);
    }

    public static async delete(accPermID: number): Promise<Loading<void>> {
        return AppAccountPermissionApi.get().delete(`/api/account-permission/${accPermID}`);
    }

}
