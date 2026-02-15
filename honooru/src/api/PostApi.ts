import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { MediaAsset, MediaAssetApi } from "./MediaAssetApi";
import { ExtendedTag, TagApi } from "./TagApi";

export class SearchResultsQuery {
    public input: string = "";
    public queryAst: any;
    public offset: number = 0;
    public limit: number = 0;
}

export class SearchResults {
    public query: SearchResultsQuery = new SearchResultsQuery();
    public timings: string[] = [];
    public parsedAst: string = "";
    public results: Post[] = [];
    public tags: ExtendedTag[] = [];
    public postCount: number = 0;
}

export class Post {
    public id: number = 0;
    public posterUserId: number = 0;
    public timestamp: Date = new Date();
    public title: string | null = null;
    public description: string | null = null;
    public context: string = "";
    public lastEditorUserId: number = 0;
    public lastEdited: Date | null = null;
    public md5: string = "";
    public rating: number = 1;
    public fileName: string = "";
    public source: string = "";
    public fileExtension: string = "";
    public fileSizeBytes: number = 0;
    public iqdbHash: string = "";
    public fileType: string = "";
}

export class PostOrdering {
    public query: string = "";
    public postID: number = 0;
    public previous: Post | null = null;
    public next: Post | null = null;
}

export class IqdbSearchResult {
    public postId: string = "";
    public score: number = 0;
    public hash: string = "";
    public post: Post | null = null;
    public mediaAsset: MediaAsset | null = null;
}

export class PostApi extends ApiWrapper<Post> {

    private static _instance: PostApi = new PostApi();
    public static get(): PostApi { return PostApi._instance; }

    public static parse(elem: any): Post {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

    public static parseSearchResultsQuery(elem: any): SearchResultsQuery {
        return {
            ...elem
        };
    }

    public static parseSearchResults(elem: any) : SearchResults {
        return {
            parsedAst: elem.parsedAst,
            query: PostApi.parseSearchResultsQuery(elem.query),
            postCount: elem.postCount,
            timings: elem.timings,
            results: elem.results.map((iter: any) => PostApi.parse(iter)),
            tags: elem.tags.map((iter: any) => TagApi.parseExtendedTag(iter))
        }
    }

    public static parseSimilarResults(elem: any): IqdbSearchResult {
        return {
            ...elem,
            post: (elem.post == null) ? null : PostApi.parse(elem.post),
            mediaAsset: (elem.mediaAsset == null) ? null : MediaAssetApi.parse(elem.mediaAsset)
        };
    }

    public static parsePostOrdering(elem: any): PostOrdering {
        return {
            query: elem.query,
            postID: elem.postID,
            previous: (elem.previous == null) ? null : PostApi.parse(elem.previous),
            next: (elem.next == null) ? null : PostApi.parse(elem.next)
        }
    }

    public static async getByID(postID: number): Promise<Loading<Post>> {
        return PostApi.get().readSingle(`/api/post/${postID}`, PostApi.parse);
    }

    public static async search(q: string, limit: number = 100, offset: number = 0): Promise<Loading<SearchResults>> {
        return PostApi.get().readSingle(`/api/post/search?q=${encodeURI(q)}&limit=${limit}&offset=${offset}`, PostApi.parseSearchResults);
    }

    public static getOrdering(q: string, postID: number): Promise<Loading<PostOrdering>> {
        return PostApi.get().readSingle(`/api/post/post-order/${postID}?q=${q}`, PostApi.parsePostOrdering);
    }

    public static async upload(mediaAssetID: string, tags: string, rating: string,
        title: string | null, description: string | null, source: string, context: string): Promise<Loading<Post>> {

        let url: string = `/api/post/${mediaAssetID}`;
        url += `?tags=${encodeURIComponent(tags)}`;
        url += `&rating=${rating}`;
        url += `&source=${encodeURIComponent(source)}`;
        if (title != null) {
            url += `&title=${encodeURIComponent(title)}`;
        }
        if (description != null) {
            url += `&description=${encodeURIComponent(description)}`;
        }
        url += `&context=${encodeURIComponent(context)}`;

        console.log(`PostApi> URL for uploading: "${url}"`);

        return PostApi.get().postReply(url, PostApi.parse);
    }

    public static async update(postID: number, tags?: string, rating?: string, source?: string, title?: string, description?: string, context?: string): Promise<Loading<void>> {
        const parms: URLSearchParams = new URLSearchParams();

        if (tags != undefined) {
            parms.set("tags", tags);
        }
        if (rating != undefined) {
            parms.set("rating", rating);
        }
        if (source != undefined) {
            parms.set("source", source);
        }
        if (title != undefined) {
            parms.set("title", title);
        }
        if (description != undefined) {
            parms.set("description", description);
        }
        if (context != undefined) {
            parms.set("context", context);
        }

        return PostApi.get().post(`/api/post/${postID}?${parms.toString()}`);
    }

    public static searchIqdb(iqdb: string): Promise<Loading<IqdbSearchResult[]>> {
        return PostApi.get().readList(`/api/post/similar/${iqdb}`, PostApi.parseSimilarResults);
    }

    public static async remakeThumbnail(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/remake-thumbnail`);
    }

    public static regenerateIqdb(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/regenerate-iqdb`);
    }

    public static updateFileType(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/update-file-type`);
    }

    public static remove(postID: number): Promise<Loading<void>> {
        return PostApi.get().delete(`/api/post/${postID}`);
    }

    public static restore(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/restore`);
    }

    public static erase(postID: number): Promise<Loading<void>> {
        return PostApi.get().delete(`/api/post/${postID}/erase`);
    }

}
