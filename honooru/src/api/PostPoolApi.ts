import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class PostPool {
    public id: number = 0;
    public name: string = "";
    public createdByID: number = 0;
    public timestamp: Date = new Date();
}

export class PostPoolApi extends ApiWrapper<PostPool> {
    private static _instance: PostPoolApi = new PostPoolApi();
    public static get(): PostPoolApi { return PostPoolApi._instance; }

    public static parse(elem: any): PostPool {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        }
    }

    public static getAll(): Promise<Loading<PostPool[]>> {
        return PostPoolApi.get().readList(`/api/post-pool`, PostPoolApi.parse);
    }

    public static getByID(poolID: number): Promise<Loading<PostPool>> {
        return PostPoolApi.get().readSingle(`/api/post-pool/${poolID}`, PostPoolApi.parse);
    }

    public static create(name: string): Promise<Loading<PostPool>> {
        return PostPoolApi.get().postReply(`/api/post-pool/?name=${name}`, PostPoolApi.parse);
    }

    public static delete(poolID: number): Promise<Loading<void>> {
        return PostPoolApi.get().delete(`/api/post-pool/${poolID}`);
    }

}
