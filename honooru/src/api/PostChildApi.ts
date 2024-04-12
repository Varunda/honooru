import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { Post, PostApi } from "api/PostApi";

export class PostChild {
    public parentPostID: number = 0;
    public childPostID: number = 0;
}

export class ExtendedPostChild {

    public postChild: PostChild = new PostChild();
    public parent: Post | null = null;
    public child: Post | null = null;
}

export class PostChildApi extends ApiWrapper<PostChild> {
    private static _instance: PostChildApi = new PostChildApi();
    public static get(): PostChildApi { return PostChildApi._instance; }

    public static parse(elem: any): PostChild {
        return {
            ...elem
        };
    }

    public static parseExtended(elem: any): ExtendedPostChild {
        return {
            postChild: PostChildApi.parse(elem.postChild),
            parent: (elem.parent == null) ? null : PostApi.parse(elem.parent),
            child: (elem.child == null) ? null : PostApi.parse(elem.child)
        };
    }

    public static getByParentID(parentID: number): Promise<Loading<ExtendedPostChild[]>> {
        return PostChildApi.get().readList(`/api/post-child/${parentID}/parent`, PostChildApi.parseExtended);
    }

    public static getByChildID(childID: number): Promise<Loading<ExtendedPostChild[]>> {
        return PostChildApi.get().readList(`/api/post-child/${childID}/child`, PostChildApi.parseExtended);
    }

    public static insert(parentID: number, childID: number): Promise<Loading<PostChild>> {
        return PostChildApi.get().postReply(`/api/post-child?parentID=${parentID}&childID=${childID}`, PostChildApi.parse);
    }

}
