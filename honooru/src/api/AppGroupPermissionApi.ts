import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppGroupPermission {
    public id: number = 0;
    public groupID: number = 0;
    public permission: string = "";
    public timestamp: Date = new Date();
    public grantedByID: number = 0;
}

export class AppGroupPermissionApi extends ApiWrapper<AppGroupPermission> {

    private static _instance: AppGroupPermissionApi = new AppGroupPermissionApi();
    public static get(): AppGroupPermissionApi { return AppGroupPermissionApi._instance; };

    public static parse(elem: any): AppGroupPermission {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

    public static async getByAccountID(accountID: number): Promise<Loading<AppGroupPermission[]>> {
        return AppGroupPermissionApi.get().readList(`/api/group-permission/account/${accountID}`, AppGroupPermissionApi.parse);
    }

    public static async getByGroupID(groupID: number): Promise<Loading<AppGroupPermission[]>> {
        return AppGroupPermissionApi.get().readList(`/api/group-permission/${groupID}`, AppGroupPermissionApi.parse);
    }

    public static async insert(groupID: number, perm: string): Promise<Loading<number>> {
        return AppGroupPermissionApi.get().postReply(`/api/group-permission/${groupID}?permission=${perm}`, (iter: any) => iter);
    }

    public static async delete(accPermID: number): Promise<Loading<void>> {
        return AppGroupPermissionApi.get().delete(`/api/group-permission/${accPermID}`);
    }

}
