import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class TagAlias {
    public alias: string = "";
    public tagID: number = 0;
}

export class TagAliasApi extends ApiWrapper<TagAlias> {
    private static _instance: TagAliasApi = new TagAliasApi();
    public static get(): TagAliasApi { return TagAliasApi._instance; }

    public static parse(elem: any): TagAlias {
        return {
            ...elem
        }
    }

    public static getByTagID(tagID: number): Promise<Loading<TagAlias[]>> {
        return TagAliasApi.get().readList(`/api/tag-alias/${tagID}`, TagAliasApi.parse);
    }

    public static insert(alias: string, tagID: number): Promise<Loading<void>> {
        return TagAliasApi.get().post(`/api/tag-alias/${tagID}?alias=${encodeURI(alias)}`);
    }

    public static delete(alias: string): Promise<Loading<void>> {
        return TagAliasApi.get().delete(`/api/tag-alias/?alias=${encodeURI(alias)}`);
    }

}
