import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppPermission {
    public id: string = "";
    public description: string = "";
}

export class AppPermissionApi extends ApiWrapper<AppPermission> {
    private static _instance: AppPermissionApi = new AppPermissionApi();
    public static get(): AppPermissionApi { return AppPermissionApi._instance; };

    public static parse(elem: any): AppPermission {
        return {
            ...elem,
        };
    }

    public static async getAll(): Promise<Loading<AppPermission[]>> {
        return AppPermissionApi.get().readList(`/api/permission/`, AppPermissionApi.parse);
    }

}
