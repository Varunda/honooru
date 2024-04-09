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
}

export class Post {
    public id: number = 0;
    public posterUserId: number = 0;
    public timestamp: Date = new Date();
    public title: string | null = null;
    public description: string | null = null;
    public lastEditorUserId: number = 0;
    public lastEdited: Date | null = null;
    public md5: string = "";
    public rating: number = 1;
    public fileName: string = "";
    public source: string = "";
    public fileExtension: string = "";
    public fileSizeBytes: number = 0;
    public iqdbHash: string = "";
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

    public static async getByID(postID: number): Promise<Loading<Post>> {
        return PostApi.get().readSingle(`/api/post/${postID}`, PostApi.parse);
    }

    public static async search(q: string, limit: number = 100): Promise<Loading<SearchResults>> {
        return PostApi.get().readSingle(`/api/post/search?q=${encodeURI(q)}&limit=${limit}`, PostApi.parseSearchResults);
    }

    public static async upload(mediaAssetID: string, tags: string, rating: string,
        title: string | null, description: string | null, source: string): Promise<Loading<Post>> {

        let url: string = `/api/post/${mediaAssetID}`;
        url += `?tags=${encodeURI(tags)}`;
        url += `&rating=${rating}`;
        url += `&source=${encodeURI(source)}`;
        if (title != null) {
            url += `&title=${encodeURI(title)}`;
        }
        if (description != null) {
            url += `&description=${encodeURI(description)}`;
        }

        return PostApi.get().postReply(url, PostApi.parse);
    }

    public static async update(postID: number, tags?: string, rating?: string, source?: string, title?: string, description?: string): Promise<Loading<void>> {
        const parms: URLSearchParams = new URLSearchParams();

        if (tags) {
            parms.set("tags", tags);
        }
        if (rating) {
            parms.set("rating", rating);
        }
        if (source) {
            parms.set("source", source);
        }
        if (title) {
            parms.set("title", title);
        }
        if (description) {
            parms.set("description", description);
        }

        return PostApi.get().post(`/api/post/${postID}?${parms.toString()}`);
    }

    public static searchIqdb(iqdb: string): Promise<Loading<IqdbSearchResult[]>> {
        return PostApi.get().readList(`/api/post/similar/${iqdb}`, PostApi.parseSimilarResults);
    }

    public static async remakeThumbnail(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/remake-thumbnail`);
    }

    public static remove(postID: number): Promise<Loading<void>> {
        return PostApi.get().delete(`/api/post/${postID}`);
    }

    public static erase(postID: number): Promise<Loading<void>> {
        return PostApi.get().delete(`/api/post/${postID}/erase`);
    }

}
