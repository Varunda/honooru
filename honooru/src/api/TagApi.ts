import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { TagType } from "api/TagTypeApi";

export class Tag {

}

export class ExtendedTag {
    public id: number = 0;
    public name: string = "";
    public typeID: number = 0;
    public typeName: string = "";
    public hexColor: string = "";
    public uses: number = 0;
    public description: string | null = null;
}

export class TagSearchResults {
    public input: string = "";
    public tags: ExtendedTag[] = [];
}

export class TagApi extends ApiWrapper<Tag> {

    private static _instance: TagApi = new TagApi();
    public static get(): TagApi { return TagApi._instance; }

    public static parse(elem: any): Tag {
        return {

        }
    }

    public static parseExtendedTag(elem: any): ExtendedTag {
        return {
            ...elem
        };
    }

    public static parseTagSearchResults(elem: any): TagSearchResults {
        return {
            input: elem.input,
            tags: elem.tags.map((iter: any) => TagApi.parseExtendedTag(iter))
        }
    }

    public static async getExtendedByID(tagID: number): Promise<Loading<ExtendedTag>> {
        return TagApi.get().readSingle(`/api/tag/${tagID}/extended`, TagApi.parseExtendedTag);
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

}