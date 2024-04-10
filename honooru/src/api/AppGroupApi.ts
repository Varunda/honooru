import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppGroup {
    public id: number = 0;
    public name: string = "";
    public hexColor: string = "";
    public implies: number[] = [];
}

export class AppGroupApi extends ApiWrapper<AppGroup> {
    private static _instance: AppGroupApi = new AppGroupApi();
    public static get(): AppGroupApi { return AppGroupApi._instance; }

    public static parse(elem: any): AppGroup {
        return {
            ...elem
        }
    }

    public static getAll(): Promise<Loading<AppGroup[]>> {
        return AppGroupApi.get().readList(`/api/group`, AppGroupApi.parse);
    }

    public static create(name: string): Promise<Loading<void>> {
        return AppGroupApi.get().post(`/api/group?name=${name}&hex=ff0000`);
    }

}
