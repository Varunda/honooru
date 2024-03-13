import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

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
}

export class PostApi extends ApiWrapper<Post> {

    private static _instance: PostApi = new PostApi();
    public static get(): PostApi { return PostApi._instance; }

    public static parse(elem: any): Post {
        return {
            timestamp: new Date(elem.timestamp),
            ...elem
        };
    }

    public static parseSearchResultsQuery(elem: any): SearchResultsQuery {
        return {
            ...elem
        };
    }

    public static parseSearchResults(elem: any) : SearchResults {
        return {
            ...elem,
            query: PostApi.parseSearchResultsQuery(elem.query),
            timings: elem.timings,
            results: elem.results.map((iter: any) => PostApi.parse(iter))
        }
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

    public static async remakeThumbnail(postID: number): Promise<Loading<void>> {
        return PostApi.get().post(`/api/post/${postID}/remake-thumbnail`);
    }

}
