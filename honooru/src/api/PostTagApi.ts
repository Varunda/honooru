import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { ExtendedTag, TagApi } from "./TagApi";

export class PostTag {
    public postID: number = 0;
    public tagID: number = 0;
}

export class PostTagApi extends ApiWrapper<PostTag> {
    private static _instance: PostTagApi = new PostTagApi();
    public static get(): PostTagApi { return PostTagApi._instance }

    public static parse(elem: any): PostTag {
        return {
            ...elem
        };
    }

    public static async getByPostID(postID: number): Promise<Loading<ExtendedTag[]>> {
        return PostTagApi.get().readList(`/api/post-tag/post/${postID}`, TagApi.parseExtendedTag);
    }


}