import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { Post, PostApi } from "api/PostApi";
import { PostPool, PostPoolApi } from "api/PostPoolApi";

export class PostPoolEntry {
    public poolID: number = 0;
    public postID: number = 0;
}

export class PostPoolEntryApi extends ApiWrapper<PostPoolEntry> {

    private static _instance: PostPoolEntryApi = new PostPoolEntryApi();
    public static get(): PostPoolEntryApi { return PostPoolEntryApi._instance; }

    public static parse(elem: any): PostPoolEntry {
        return {
            ...elem
        };
    }

    public static getPoolsOfPost(postID: number): Promise<Loading<PostPool[]>> {
        return PostPoolEntryApi.get().readList(`/api/post-pool-entry/pool/${postID}`, PostPoolApi.parse);
    }

    public static getPostsOfPool(poolID: number): Promise<Loading<Post[]>> {
        return PostPoolEntryApi.get().readList(`/api/post-pool-entry/post/${poolID}`, PostApi.parse);
    }

    public static addToPool(poolID: number, postID: number): Promise<Loading<void>> {
        return PostPoolEntryApi.get().post(`/api/post-pool-entry/add?poolID=${poolID}&postID=${postID}`);
    }

    public static removeFromPool(poolID: number, postID: number): Promise<Loading<void>> {
        return PostPoolEntryApi.get().delete(`/api/post-pool-entry/${poolID}/${postID}`);
    }

}
