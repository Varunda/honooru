import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { TagType } from "api/TagTypeApi";
import { TagAlias, TagAliasApi } from "./TagAliasApi";

export class Tag {
    public id: number = 0;
    public name: string = "";
    public typeID: number = 0;
    public timestamp: Date = new Date();
}

export class ExtendedTag {
    public id: number = 0;
    public name: string = "";
    public typeID: number = 0;
    public typeName: string = "";
    public typeOrder: number = 0;
    public hexColor: string = "";
    public uses: number = 0;
    public description: string | null = null;
}

export class ExtendedTagSearchResult {
    public tag: ExtendedTag = new ExtendedTag();
    public name: string = "";
    public alias: TagAlias | null = null;
}

export class TagSearchResults {
    public input: string = "";
    public tags: ExtendedTagSearchResult[] = [];
}

export class TagApi extends ApiWrapper<Tag> {

    private static _instance: TagApi = new TagApi();
    public static get(): TagApi { return TagApi._instance; }

    public static parse(elem: any): Tag {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        }
    }

    public static parseExtendedTag(elem: any): ExtendedTag {
        return {
            ...elem
        };
    }

    public static parseExtendedTagSearchResult(elem: any): ExtendedTagSearchResult {
        return {
            tag: TagApi.parseExtendedTag(elem.tag),
            name: TagApi.parseExtendedTag(elem.tag).name,
            alias: elem.alias == null ? null : TagAliasApi.parse(elem.alias)
        };
    }

    public static parseTagSearchResults(elem: any): TagSearchResults {
        return {
            input: elem.input,
            tags: elem.tags.map((iter: any) => TagApi.parseExtendedTagSearchResult(iter))
        }
    }

    public static async getExtendedByID(tagID: number): Promise<Loading<ExtendedTag>> {
        return TagApi.get().readSingle(`/api/tag/${tagID}/extended`, TagApi.parseExtendedTag);
    }

    public static getByName(name: string): Promise<Loading<ExtendedTag>> {
        return TagApi.get().readSingle(`/api/tag/name?name=${encodeURI(name)}`, TagApi.parseExtendedTag);
    }

    public static async search(name: string): Promise<Loading<TagSearchResults>> {
        return TagApi.get().readSingle(`/api/tag/search?name=${encodeURI(name)}`, TagApi.parseTagSearchResults);
    }

    public static async update(tagID: number, tag: ExtendedTag): Promise<Loading<Tag>> {
        return TagApi.get().postReplyForm(`/api/tag/${tagID}`, tag, TagApi.parse);
    }

    public static async queueRecount(tagID: number): Promise<Loading<void>> {
        return TagApi.get().post(`/api/tag/${tagID}/recount`);
    }

    public static async ensureImplications(tagID: number): Promise<Loading<void>> {
        return TagApi.get().post(`/api/tag/${tagID}/ensure-implications`);
    }

    public static delete(tagID: number): Promise<Loading<void>> {
        return TagApi.get().delete(`/api/tag/${tagID}`);
    }

}